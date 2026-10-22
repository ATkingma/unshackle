using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Polly;

// Policy.Handle<E>().WaitAndRetryAsync(n, attempt => delay) and its ExecuteAsync calls → a ResiliencePipeline with AddRetry.
// Every decision is made from the original tree, so the declaration and each of its ExecuteAsync calls agree (all or none).
internal static class PollyRetry
{
    public const string PollyAssembly = "Polly";

    // Names the rewrite introduces around the user's code; FreshName picks args1, args2, ... when one is taken (council P10).
    private const string ArgsName = "args";
    private const string DelayFunctionName = "SleepDuration";
    private const string DiscardName = "_";
    private const int MaxFreshNameTries = 10;

    // Parameters of the v7 retry methods this rewrite maps; any other (Context-taking providers, sleep-duration lists) is Manual.
    private const string SleepDurationProvider = "sleepDurationProvider";
    private const string SleepDurations = "sleepDurations";

    private static readonly HashSet<string> s_rewrittenParameters = new(["retryCount", SleepDurationProvider, SleepDurations, "onRetry", "onRetryAsync"], StringComparer.Ordinal);

    // The v7 async retry methods; WaitAndRetryAsync with a provider is polly.wait-and-retry-async, the rest polly.retry-variants.
    private static readonly HashSet<string> s_retryMethods = new(["WaitAndRetryAsync", "RetryAsync", "RetryForeverAsync", "WaitAndRetryForeverAsync"], StringComparer.Ordinal);

    public static bool IsRetryCreation(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: string name } && s_retryMethods.Contains(name)
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { ContainingAssembly.Name: PollyAssembly };

    // RetryAsync, the Forever methods and WaitAndRetryAsync over a sleep-duration list (queue row 7).
    public static bool IsVariant(InvocationExpressionSyntax creation, SemanticModel model) =>
        model.GetSymbolInfo(creation).Symbol is IMethodSymbol method
        && (method.Name != "WaitAndRetryAsync" || method.Parameters.Any(p => p.Name == SleepDurations));

    // The nodes the pattern reports for one retry call, outer nodes only (the Fixer replaces them independently): the inline
    // ExecuteAsync, or the holder's declaration (local or private field) with every ExecuteAsync on it, or the call itself (Manual).
    public static IEnumerable<SyntaxNode> Targets(InvocationExpressionSyntax creation, SemanticModel model)
    {
        // The policy of an HttpClient handler is polly.add-policy-handler's node (it rewrites the whole call).
        if (IsHandlerPolicy(creation))
        {
            return [];
        }

        if (ExecuteOn(creation) is InvocationExpressionSyntax inline)
        {
            return [inline];
        }

        if (HolderOf(creation, model) is (SyntaxNode declaration, ISymbol holder))
        {
            IEnumerable<InvocationExpressionSyntax> executes = Uses(declaration, holder, model).Select(u => ExecuteOn(Receiver(u))).OfType<InvocationExpressionSyntax>();
            // A field assigned in the constructor: its declaration (type), the assignment's value and the calls are separate nodes.
            return declaration.Contains(creation) ? [declaration, .. executes] : [declaration, creation, .. executes];
        }

        return [creation];
    }

    // The retry call node itself: a constructor-assigned field's value (queue row 5), otherwise a policy stored or passed on (Manual).
    public static PollyPlan PlanCreation(InvocationExpressionSyntax creation, SemanticModel model)
    {
        if (HolderOf(creation, model) is not (SyntaxNode declaration, ISymbol holder) || declaration.Contains(creation))
        {
            return PollyPlan.Manual(PollyReason.PolicyNotLocal,
                "the policy is stored or passed on (property, return value, argument, or a field assigned outside its declaration or constructor); only a local, a private field or an inline policy used by awaited ExecuteAsync calls is rewritten so far.");
        }

        return PlanHolderUses(declaration, holder, creation, model) is PollyPlan blocked ? blocked : PlanPipeline(creation, model);
    }

    private static bool IsHandlerPolicy(InvocationExpressionSyntax creation) => creation.Parent switch
    {
        ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddPolicyHandler" } } } => true,
        SimpleLambdaExpressionSyntax { Parent: ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddTransientHttpErrorPolicy" } } } } => true,
        _ => false,
    };

    // A local declaration statement or a field declaration that holds the policy.
    public static PollyPlan PlanDeclaration(SyntaxNode declarationNode, SemanticModel model)
    {
        VariableDeclarationSyntax declaration = Declaration(declarationNode);
        if (declaration.Variables.Count != 1
            || model.GetDeclaredSymbol(declaration.Variables[0]) is not ISymbol holder
            || HolderCreation(holder, model) is not (_, InvocationExpressionSyntax creation))
        {
            return PollyPlan.Manual(PollyReason.PolicyNotLocal, "the declaration does not hold exactly one policy created by a retry call at its declaration or in its constructor.");
        }

        if (PlanHolderUses(declarationNode, holder, creation, model) is PollyPlan blocked)
        {
            return blocked;
        }

        PollyPlan pipeline = PlanPipeline(creation, model);
        if (pipeline.Node is null)
        {
            return pipeline;
        }

        SyntaxNode rewritten = declarationNode.Contains(creation) ? declarationNode.ReplaceNode(creation, pipeline.Node) : declarationNode;
        VariableDeclarationSyntax rewrittenDeclaration = Declaration(rewritten);
        if (!rewrittenDeclaration.Type.IsVar)
        {
            TypeSyntax type = SyntaxFactory.ParseTypeName(ResultType(creation, model) is string result ? $"ResiliencePipeline<{result}>" : "ResiliencePipeline")
                .WithTriviaFrom(rewrittenDeclaration.Type);
            rewritten = rewritten.ReplaceNode(rewrittenDeclaration.Type, type);
        }

        return PollyPlan.Of(rewritten);
    }

    public static PollyPlan PlanExecute(InvocationExpressionSyntax execute, SemanticModel model)
    {
        var access = (MemberAccessExpressionSyntax)execute.Expression;
        if (access.Expression is InvocationExpressionSyntax creation && IsRetryCreation(creation, model))
        {
            PollyPlan pipeline = PlanPipeline(creation, model);
            PollyPlan inlineCallback = PlanCallback(execute, model);
            if (pipeline.Node is null || inlineCallback.Node is null)
            {
                return pipeline.Node is null ? pipeline : inlineCallback;
            }

            ArgumentSyntax argument = execute.ArgumentList.Arguments[0];
            return PollyPlan.Of(execute.ReplaceNodes(
                [creation, argument.Expression],
                (original, _) => original == creation ? pipeline.Node : inlineCallback.Node));
        }

        if (model.GetSymbolInfo(access.Expression).Symbol is ISymbol { Kind: SymbolKind.Local or SymbolKind.Field } holder
            && HolderCreation(holder, model) is (SyntaxNode declaration, InvocationExpressionSyntax holderCreation))
        {
            if (PlanHolderUses(declaration, holder, holderCreation, model) is PollyPlan blocked)
            {
                return blocked;
            }

            PollyPlan pipeline = PlanPipeline(holderCreation, model);
            if (pipeline.Node is null)
            {
                return pipeline;
            }

            PollyPlan callback = PlanCallback(execute, model);
            return callback.Node is null ? callback : PollyPlan.Of(execute.ReplaceNode(execute.ArgumentList.Arguments[0].Expression, callback.Node));
        }

        return PollyPlan.Manual(PollyReason.PolicyNotLocal, "ExecuteAsync is called on a policy that is not a local, a private field or an inline retry call.");
    }

    // The builder expression for Policy.Handle<E>().WaitAndRetryAsync(n, attempt => delay).
    public static PollyPlan PlanPipeline(InvocationExpressionSyntax creation, SemanticModel model)
    {
        string indent = Indent(creation);
        PollyPlan options = PlanOptions(creation, model, indent);
        if (options.Text is not string optionsText)
        {
            return options;
        }

        string newLine = NewLine(creation);
        string generic = ResultType(creation, model) is string result ? $"<{result}>" : string.Empty;
        return PollyPlan.Of(SyntaxFactory.ParseExpression(
            $"new ResiliencePipelineBuilder{generic}(){newLine}{indent}    .AddRetry({optionsText}){newLine}{indent}    .Build()"));
    }

    // The `new RetryStrategyOptions(<T>) { ... }` text for a v7 retry call; its braces sit at indent + 4. The filter chain may start
    // at the HttpPolicyExtensions transient-error filter, written out (queue row 6).
    public static PollyPlan PlanOptions(InvocationExpressionSyntax creation, SemanticModel model, string indent)
    {
        if (model.GetSymbolInfo(creation).Symbol is not IMethodSymbol method
            || ArgumentsByName(creation, method) is not { } arguments
            || !arguments.Keys.All(s_rewrittenParameters.Contains)
            || (arguments.TryGetValue(SleepDurationProvider, out (ExpressionSyntax Expression, IParameterSymbol Parameter) checkedProvider)
                && checkedProvider.Parameter.Type is not INamedTypeSymbol { Name: "Func", TypeArguments: [{ SpecialType: SpecialType.System_Int32 }, { Name: "TimeSpan" }] }))
        {
            return PollyPlan.Manual(PollyReason.OverloadNotSupported,
                "this retry overload is not rewritten: a Context- or outcome-taking sleep-duration provider is Manual.");
        }

        string? resultType = ResultType(creation, model);
        if (FreshName(creation, model, PollyPredicate.ResponseName) is not string response)
        {
            return NameClash(creation);
        }

        PollyPlan predicate = PollyPredicate.Plan(((MemberAccessExpressionSyntax)creation.Expression).Expression, resultType, model, response);
        if (predicate.Text is not string shouldHandle)
        {
            return predicate;
        }

        if (FreshName(creation, model, ArgsName) is not string args
            || FreshName(creation, model, DelayFunctionName) is not string sleep
            || FreshName(creation, model, PollyOnRetry.CallbackName) is not string callback)
        {
            return NameClash(creation);
        }

        string generic = resultType is null ? string.Empty : $"<{resultType}>";
        string newLine = NewLine(creation);
        string inner = indent + "        ";

        string maxRetryAttempts;
        string delayLines;
        if (arguments.TryGetValue(SleepDurations, out (ExpressionSyntax Expression, IParameterSymbol Parameter) durations))
        {
            // v7 retries once per listed duration, waiting duration[i - 1] before retry i; AttemptNumber is i - 1.
            if (LiteralDurations(durations.Expression, model) is not { Count: > 0 } items)
            {
                return PollyPlan.Manual(PollyReason.SleepDurationsNotLiteral,
                    "the sleep durations are not an array or collection literal of TimeSpan.Zero / TimeSpan.FromX(literal) elements.");
            }

            maxRetryAttempts = items.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string listed = items.Count == 1
                ? items[0]
                : $"{args}.AttemptNumber switch {{ {string.Concat(items.SkipLast(1).Select((d, i) => $"{i} => {d}, "))}_ => {items[^1]} }}";
            delayLines = $"{indent}        DelayGenerator = {args} => new ValueTask<TimeSpan?>({listed}),{newLine}";
        }
        else
        {
            if (method.Name.Contains("Forever", StringComparison.Ordinal))
            {
                // v8 documents int.MaxValue as retrying indefinitely.
                maxRetryAttempts = "int.MaxValue";
            }
            else if (!arguments.TryGetValue("retryCount", out (ExpressionSyntax Expression, IParameterSymbol Parameter) retryCount))
            {
                // RetryAsync() and RetryAsync(onRetry) retry once.
                maxRetryAttempts = "1";
            }
            else if (retryCount.Expression is not LiteralExpressionSyntax || model.GetConstantValue(retryCount.Expression).Value is not int retries || retries < 1)
            {
                return PollyPlan.Manual(PollyReason.RetryCountNotLiteral,
                    $"the retry count '{retryCount.Expression}' is not an integer literal of at least 1 (MaxRetryAttempts must be 1 or more).");
            }
            else
            {
                maxRetryAttempts = retries.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            if (!arguments.TryGetValue(SleepDurationProvider, out (ExpressionSyntax Expression, IParameterSymbol Parameter) provider))
            {
                // RetryAsync/RetryForeverAsync retry without waiting.
                delayLines = $"{indent}        Delay = TimeSpan.Zero,{newLine}";
            }
            else if (provider.Expression is not LambdaExpressionSyntax { Modifiers.Count: 0 } delay || Parameter(delay) is not ParameterSyntax parameter)
            {
                return PollyPlan.Manual(PollyReason.DelayNotLambda, "the sleep-duration provider is not a plain one-parameter lambda.");
            }
            else
            {
                string attempt = parameter.Identifier.ValueText;
                string function = delay.Body switch
                {
                    BlockSyntax block => $"TimeSpan {sleep}(int {attempt}){newLine}{inner}    {Reindent(block, inner + "    ", newLine)}",
                    ExpressionSyntax expression => $"TimeSpan {sleep}(int {attempt}) => {expression};",
                    _ => throw new System.Diagnostics.UnreachableException(),
                };

                // v7 passes the 1-based retry number to the provider; v8's AttemptNumber is 0-based for the first retry (council P2).
                delayLines =
                    $"{indent}        DelayGenerator = {args} =>{newLine}"
                    + $"{indent}        {{{newLine}"
                    + $"{inner}    {function}{newLine}"
                    + $"{inner}    return new ValueTask<TimeSpan?>({sleep}({args}.AttemptNumber + 1));{newLine}"
                    + $"{indent}        }},{newLine}";
            }
        }

        string onRetry = string.Empty;
        if (arguments.TryGetValue("onRetry", out (ExpressionSyntax Expression, IParameterSymbol Parameter) onRetryArgument)
            || arguments.TryGetValue("onRetryAsync", out onRetryArgument))
        {
            PollyPlan onRetryPlan = PollyOnRetry.Plan(onRetryArgument.Expression, onRetryArgument.Parameter, resultType, args, callback, inner, newLine, model);
            if (onRetryPlan.Text is null)
            {
                return onRetryPlan;
            }

            onRetry = onRetryPlan.Text;
        }

        string text =
            $"new RetryStrategyOptions{generic}{newLine}"
            + $"{indent}    {{{newLine}"
            + $"{indent}        ShouldHandle = {shouldHandle},{newLine}"
            + $"{indent}        MaxRetryAttempts = {maxRetryAttempts},{newLine}"
            + delayLines
            + onRetry
            + $"{indent}    }}";
        return PollyPlan.OfText(text);
    }

    // The elements of new[] { ... }, new TimeSpan[] { ... } or [ ... ] when each is TimeSpan.Zero or TimeSpan.FromX(numeric literal):
    // values fixed at the call, so evaluating them inside the generator gives the same durations.
    private static List<string>? LiteralDurations(ExpressionSyntax expression, SemanticModel model)
    {
        IEnumerable<ExpressionSyntax>? elements = expression switch
        {
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer.Expressions,
            ArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions,
            CollectionExpressionSyntax collection when collection.Elements.All(e => e is ExpressionElementSyntax) =>
                collection.Elements.Cast<ExpressionElementSyntax>().Select(e => e.Expression),
            _ => null,
        };
        if (elements is null)
        {
            return null;
        }

        List<ExpressionSyntax> items = [.. elements];
        return items.All(e => IsLiteralTimeSpan(e, model)) ? [.. items.Select(e => e.ToString())] : null;
    }

    private static bool IsLiteralTimeSpan(ExpressionSyntax expression, SemanticModel model) =>
        model.GetSymbolInfo(expression).Symbol is ISymbol { ContainingType: { Name: "TimeSpan", ContainingNamespace.Name: "System" } } symbol
        && expression switch
        {
            MemberAccessExpressionSyntax => symbol.Name == "Zero",
            InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax }] } => symbol.Name.StartsWith("From", StringComparison.Ordinal),
            _ => false,
        };

    // The arguments by parameter name (named arguments may come in any order); null when one does not bind to a parameter.
    private static Dictionary<string, (ExpressionSyntax Expression, IParameterSymbol Parameter)>? ArgumentsByName(InvocationExpressionSyntax invocation, IMethodSymbol method)
    {
        var arguments = new Dictionary<string, (ExpressionSyntax, IParameterSymbol)>(StringComparer.Ordinal);
        for (int i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
        {
            ArgumentSyntax argument = invocation.ArgumentList.Arguments[i];
            IParameterSymbol? parameter = argument.NameColon is { } nameColon
                ? method.Parameters.FirstOrDefault(p => p.Name == nameColon.Name.Identifier.ValueText)
                : i < method.Parameters.Length ? method.Parameters[i] : null;
            if (parameter is null || !arguments.TryAdd(parameter.Name, (argument.Expression, parameter)))
            {
                return null;
            }
        }

        return arguments;
    }

    // T of a v7 PolicyBuilder<T> receiver (HandleResult, Policy<T>): the pipeline becomes ResiliencePipeline<T>; null for exceptions only.
    public static string? ResultType(InvocationExpressionSyntax creation, SemanticModel model) =>
        model.GetSymbolInfo(creation).Symbol is IMethodSymbol { ReceiverType: INamedTypeSymbol { Name: "PolicyBuilder", TypeArguments: [ITypeSymbol result] } }
            ? result.ToMinimalDisplayString(model, creation.SpanStart)
            : null;

    // The v8 callback for policy.ExecuteAsync(() => ...): Func<CancellationToken, ValueTask(<T>)>, awaited by the caller.
    private static PollyPlan PlanCallback(InvocationExpressionSyntax execute, SemanticModel model)
    {
        if (model.GetSymbolInfo(execute).Symbol is not IMethodSymbol { Name: "ExecuteAsync", Parameters.Length: 1, ContainingAssembly.Name: PollyAssembly }
            || execute.ArgumentList.Arguments.Count != 1)
        {
            return PollyPlan.Manual(PollyReason.ExecuteOverloadNotSupported,
                "only ExecuteAsync(Func<Task>) and ExecuteAsync(Func<Task<T>>) are rewritten; a CancellationToken, Context or continueOnCapturedContext argument is Manual.");
        }

        if (!IsAwaited(execute))
        {
            return PollyPlan.Manual(PollyReason.ResultNotAwaited,
                "the ExecuteAsync result is not awaited directly; v8 returns a ValueTask, which may only be awaited once.");
        }

        if (execute.ArgumentList.Arguments[0].Expression is not ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: 0 } lambda
            || lambda.Modifiers.Any(m => !m.IsKind(SyntaxKind.AsyncKeyword))
            || FreshName(execute, model, DiscardName) is not string discardName)
        {
            return PollyPlan.Manual(PollyReason.ExecuteNotLambda, "the ExecuteAsync argument is not a parameterless lambda.");
        }

        ParameterSyntax discard = SyntaxFactory.Parameter(SyntaxFactory.Identifier(discardName)).WithTrailingTrivia(lambda.ParameterList.GetTrailingTrivia());
        if (lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
        {
            return PollyPlan.Of(SyntaxFactory.SimpleLambdaExpression(lambda.Modifiers, discard, lambda.ArrowToken, lambda.Block, lambda.ExpressionBody)
                .WithTriviaFrom(lambda));
        }

        // A non-async lambda returns the user's Task; wrapping it in a ValueTask adds no await and so no captured context.
        if (lambda.ExpressionBody is not ExpressionSyntax body || model.GetTypeInfo(body).Type is not INamedTypeSymbol task
            || task.ContainingNamespace?.ToDisplayString() != "System.Threading.Tasks" || task.Name != "Task")
        {
            return PollyPlan.Manual(PollyReason.ExecuteNotLambda, "the ExecuteAsync lambda is not async and does not return a Task expression.");
        }

        string valueTask = "ValueTask";
        if (task.TypeArguments is [ITypeSymbol result])
        {
            if (result.IsAnonymousType || result.TypeKind == TypeKind.Error)
            {
                return PollyPlan.Manual(PollyReason.ExecuteNotLambda, "the ExecuteAsync result type cannot be written out.");
            }

            valueTask += $"<{result.ToMinimalDisplayString(model, execute.SpanStart)}>";
        }

        ExpressionSyntax wrapped = SyntaxFactory.ParseExpression($"new {valueTask}({body})");
        return PollyPlan.Of(SyntaxFactory.SimpleLambdaExpression(discard, wrapped).WithArrowToken(lambda.ArrowToken).WithTriviaFrom(lambda));
    }

    // Every use of the holder must be the receiver of a rewritable ExecuteAsync; null when they all are. A field must be private in a
    // type declared in one place (not partial), so every use is in that declaration (queue row 5).
    private static PollyPlan? PlanHolderUses(SyntaxNode declarationNode, ISymbol holder, InvocationExpressionSyntax creation, SemanticModel model)
    {
        switch (declarationNode)
        {
            case LocalDeclarationStatementSyntax statement when statement.Declaration.Variables.Count != 1 || statement.UsingKeyword.IsKind(SyntaxKind.UsingKeyword) || statement.IsConst:
                return PollyPlan.Manual(PollyReason.PolicyNotLocal, "the policy declaration declares more than one variable.");
            case FieldDeclarationSyntax field when field.Declaration.Variables.Count != 1:
                return PollyPlan.Manual(PollyReason.PolicyNotLocal, "the policy field declaration declares more than one variable.");
            case FieldDeclarationSyntax when holder.DeclaredAccessibility != Accessibility.Private || holder.ContainingType.DeclaringSyntaxReferences.Length != 1:
                return PollyPlan.Manual(PollyReason.PolicyMemberNotPrivate,
                    $"the policy field '{holder.Name}' is not private or its type is partial; uses outside this declaration cannot be checked.");
        }

        foreach (IdentifierNameSyntax use in Uses(declarationNode, holder, model))
        {
            if (creation.Parent is AssignmentExpressionSyntax assignment && assignment.Left == Receiver(use))
            {
                continue;
            }

            if (ExecuteOn(Receiver(use)) is not InvocationExpressionSyntax execute)
            {
                return PollyPlan.Manual(PollyReason.PolicyUsedBeyondExecute,
                    $"'{holder.Name}' is used at line {use.GetLocation().GetLineSpan().StartLinePosition.Line + 1} for more than ExecuteAsync (assigned, passed on or another member called).");
            }

            if (PlanCallback(execute, model) is { Code: not null } callbackBlocked)
            {
                return callbackBlocked;
            }
        }

        return null;
    }

    // A local: the whole tree; a private field: its (single) type declaration.
    private static IEnumerable<IdentifierNameSyntax> Uses(SyntaxNode declarationNode, ISymbol holder, SemanticModel model)
    {
        SyntaxNode scope = declarationNode is FieldDeclarationSyntax && holder.ContainingType.DeclaringSyntaxReferences is [SyntaxReference type]
            && type.SyntaxTree == declarationNode.SyntaxTree
            ? type.GetSyntax()
            : declarationNode.SyntaxTree.GetRoot();
        return scope.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(n => n.Identifier.ValueText == holder.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, holder));
    }

    // this._policy counts as _policy.
    private static ExpressionSyntax Receiver(IdentifierNameSyntax use) =>
        use.Parent is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access && access.Name == use ? access : use;

    // The local or field that holds this retry call, with its declaration node; null when the call is not its only creation.
    private static (SyntaxNode, ISymbol)? HolderOf(InvocationExpressionSyntax creation, SemanticModel model)
    {
        ISymbol? symbol = creation.Parent switch
        {
            EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator } => model.GetDeclaredSymbol(declarator),
            AssignmentExpressionSyntax { Left: ExpressionSyntax left } assignment when assignment.Right == creation => model.GetSymbolInfo(left).Symbol,
            _ => null,
        };
        return symbol is { Kind: SymbolKind.Local or SymbolKind.Field } && HolderCreation(symbol, model) is (SyntaxNode declaration, InvocationExpressionSyntax found) && found == creation
            ? (declaration, symbol)
            : null;
    }

    // The declaration node and the one retry call that creates the holder: its initializer, or (a field without one) the single
    // `_f = <retry>;` statement directly in a constructor body of its own type, unconditional (queue row 5).
    private static (SyntaxNode, InvocationExpressionSyntax)? HolderCreation(ISymbol holder, SemanticModel model)
    {
        if (holder.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax() is not VariableDeclaratorSyntax { Parent.Parent: SyntaxNode declaration } declarator
            || declaration is not (LocalDeclarationStatementSyntax or FieldDeclarationSyntax))
        {
            return null;
        }

        if (declarator.Initializer is { } initializer)
        {
            return initializer.Value is InvocationExpressionSyntax value && IsRetryCreation(value, model) ? (declaration, value) : null;
        }

        if (holder is not IFieldSymbol || holder.ContainingType.DeclaringSyntaxReferences is not [SyntaxReference type] || type.SyntaxTree != declaration.SyntaxTree)
        {
            return null;
        }

        List<AssignmentExpressionSyntax> assignments =
        [
            .. type.GetSyntax().DescendantNodes().OfType<AssignmentExpressionSyntax>()
                .Where(a => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(a.Left).Symbol, holder)),
        ];
        return assignments is [{ Right: InvocationExpressionSyntax right, Parent: ExpressionStatementSyntax { Parent: BlockSyntax { Parent: ConstructorDeclarationSyntax } } } only]
            && only.IsKind(SyntaxKind.SimpleAssignmentExpression) && IsRetryCreation(right, model)
            ? (declaration, right)
            : null;
    }

    private static VariableDeclarationSyntax Declaration(SyntaxNode declarationNode) => declarationNode switch
    {
        LocalDeclarationStatementSyntax statement => statement.Declaration,
        FieldDeclarationSyntax field => field.Declaration,
        _ => throw new System.Diagnostics.UnreachableException(),
    };

    private static InvocationExpressionSyntax? ExecuteOn(ExpressionSyntax receiver) =>
        receiver.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ExecuteAsync", Parent: InvocationExpressionSyntax execute } access
        && access.Expression == receiver
            ? execute
            : null;

    private static bool IsAwaited(InvocationExpressionSyntax execute) => execute.Parent switch
    {
        AwaitExpressionSyntax => true,
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConfigureAwait", Parent: InvocationExpressionSyntax { Parent: AwaitExpressionSyntax } } => true,
        _ => false,
    };

    private static ParameterSyntax? Parameter(LambdaExpressionSyntax lambda) => lambda switch
    {
        SimpleLambdaExpressionSyntax simple => simple.Parameter,
        ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [ParameterSyntax only] } => only,
        _ => null,
    };

    // The name, or the first of name1..name9, that no symbol in scope at the node and no identifier inside it (nested lambdas
    // and local functions included) already uses; null when all are taken.
    public static string? FreshName(SyntaxNode node, SemanticModel model, string name)
    {
        for (int i = 0; i < MaxFreshNameTries; i++)
        {
            string candidate = i == 0 ? name : name + i;
            if (model.LookupSymbols(node.SpanStart, name: candidate).IsEmpty && !UsesName(node, candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public static PollyPlan NameClash(SyntaxNode node) =>
        PollyPlan.Manual(PollyReason.NameClash, $"no free name for the helper variables near line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}.");

    private static bool UsesName(SyntaxNode body, string name) =>
        body.DescendantTokens().Any(t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == name);

    public static string NewLine(SyntaxNode node) =>
        node.SyntaxTree.GetText().ToString().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    public static string Indent(SyntaxNode node)
    {
        Microsoft.CodeAnalysis.Text.SourceText text = node.SyntaxTree.GetText();
        string line = text.Lines.GetLineFromPosition(node.SpanStart).ToString();
        return line[..(line.Length - line.TrimStart().Length)];
    }

    // The block's lines moved so its braces start at the new indentation, keeping the indentation inside it.
    public static string Reindent(BlockSyntax block, string indent, string newLine)
    {
        Microsoft.CodeAnalysis.Text.SourceText text = block.SyntaxTree.GetText();
        int column = block.OpenBraceToken.SpanStart - text.Lines.GetLineFromPosition(block.OpenBraceToken.SpanStart).Start;
        string[] lines = block.ToString().ReplaceLineEndings("\n").Split('\n');
        return string.Join(newLine, lines.Select((l, i) => i == 0 || l.Length == 0
            ? l
            : indent + (l.Length >= column && string.IsNullOrWhiteSpace(l[..column]) ? l[column..] : l.TrimStart())));
    }
}
