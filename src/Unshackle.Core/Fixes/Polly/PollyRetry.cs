using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Polly;

// Policy.Handle<E>().WaitAndRetryAsync(n, attempt => delay) and its ExecuteAsync calls → a ResiliencePipeline with AddRetry.
// Every decision is made from the original tree, so the declaration and each of its ExecuteAsync calls agree (all or none).
internal static class PollyRetry
{
    public const string PollyAssembly = "Polly";

    // Names the rewrite introduces around the user's delay lambda body; the body must not already use them.
    private const string ArgsName = "args";
    private const string DelayFunctionName = "SleepDuration";
    private const string DiscardName = "_";

    public static bool IsWaitAndRetryAsync(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "WaitAndRetryAsync" }
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { ContainingAssembly.Name: PollyAssembly };

    // The nodes the pattern reports for one WaitAndRetryAsync call, outer nodes only (the Fixer replaces them independently).
    public static IEnumerable<SyntaxNode> Targets(InvocationExpressionSyntax creation, SemanticModel model)
    {
        if (ExecuteOn(creation) is InvocationExpressionSyntax inline)
        {
            return [inline];
        }

        if (LocalOf(creation, model) is (LocalDeclarationStatementSyntax statement, ILocalSymbol local))
        {
            return [statement, .. Uses(statement, local, model).Select(ExecuteOn).OfType<InvocationExpressionSyntax>()];
        }

        return [creation];
    }

    public static PollyPlan PlanDeclaration(LocalDeclarationStatementSyntax statement, SemanticModel model)
    {
        if (statement.Declaration.Variables.Single().Initializer?.Value is not InvocationExpressionSyntax creation
            || LocalOf(creation, model) is not (_, ILocalSymbol local))
        {
            return PollyPlan.Manual(PollyReason.PolicyNotLocal, "the policy is not a single local initialised with WaitAndRetryAsync.");
        }

        if (PlanLocalUses(statement, local, model) is string reason)
        {
            return new PollyPlan { Reason = reason };
        }

        PollyPlan pipeline = PlanPipeline(creation, model);
        if (pipeline.Node is null)
        {
            return pipeline;
        }

        LocalDeclarationStatementSyntax rewritten = statement.ReplaceNode(creation, pipeline.Node);
        if (!rewritten.Declaration.Type.IsVar)
        {
            rewritten = rewritten.WithDeclaration(rewritten.Declaration.WithType(
                SyntaxFactory.IdentifierName("ResiliencePipeline").WithTriviaFrom(rewritten.Declaration.Type)));
        }

        return PollyPlan.Of(rewritten);
    }

    public static PollyPlan PlanExecute(InvocationExpressionSyntax execute, SemanticModel model)
    {
        var access = (MemberAccessExpressionSyntax)execute.Expression;
        if (access.Expression is InvocationExpressionSyntax creation && IsWaitAndRetryAsync(creation, model))
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

        if (model.GetSymbolInfo(access.Expression).Symbol is ILocalSymbol local
            && local.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax() is VariableDeclaratorSyntax
            {
                Initializer.Value: InvocationExpressionSyntax localCreation, Parent.Parent: LocalDeclarationStatementSyntax statement,
            })
        {
            if (PlanLocalUses(statement, local, model) is string reason)
            {
                return new PollyPlan { Reason = reason };
            }

            PollyPlan pipeline = PlanPipeline(localCreation, model);
            if (pipeline.Node is null)
            {
                return pipeline;
            }

            PollyPlan callback = PlanCallback(execute, model);
            return callback.Node is null ? callback : PollyPlan.Of(execute.ReplaceNode(execute.ArgumentList.Arguments[0].Expression, callback.Node));
        }

        return PollyPlan.Manual(PollyReason.PolicyNotLocal, "ExecuteAsync is called on a policy that is not a local or inline WaitAndRetryAsync.");
    }

    // The builder expression for Policy.Handle<E>().WaitAndRetryAsync(n, attempt => delay).
    public static PollyPlan PlanPipeline(InvocationExpressionSyntax creation, SemanticModel model)
    {
        if (model.GetSymbolInfo(creation).Symbol is not IMethodSymbol method
            || method.Parameters.Length != 2
            || method.Parameters[0].Type.SpecialType != SpecialType.System_Int32
            || method.Parameters[1].Type is not INamedTypeSymbol { Name: "Func", TypeArguments: [{ SpecialType: SpecialType.System_Int32 }, { Name: "TimeSpan" }] }
            || creation.ArgumentList.Arguments.Count != 2
            || creation.ArgumentList.Arguments.Any(a => a.NameColon is not null))
        {
            return PollyPlan.Manual(PollyReason.OverloadNotSupported,
                "only WaitAndRetryAsync(int, Func<int, TimeSpan>) is rewritten so far; onRetry, Context and sleep-duration lists follow in later queue rows.");
        }

        if (((MemberAccessExpressionSyntax)creation.Expression).Expression is not InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "Handle", TypeArgumentList.Arguments: [TypeSyntax exception] } },
                ArgumentList.Arguments.Count: 0,
            } handle
            || model.GetSymbolInfo(handle).Symbol is not IMethodSymbol { ContainingType: { Name: "Policy", Arity: 0, ContainingAssembly.Name: PollyAssembly } })
        {
            return PollyPlan.Manual(PollyReason.ExceptionFilterNotSupported,
                "only Policy.Handle<E>() without a predicate is rewritten so far; Or, HandleResult and HandleInner follow in a later queue row.");
        }

        ExpressionSyntax count = creation.ArgumentList.Arguments[0].Expression;
        if (count is not LiteralExpressionSyntax || model.GetConstantValue(count).Value is not int retries || retries < 1)
        {
            return PollyPlan.Manual(PollyReason.RetryCountNotLiteral,
                $"the retry count '{count}' is not an integer literal of at least 1 (MaxRetryAttempts must be 1 or more).");
        }

        if (creation.ArgumentList.Arguments[1].Expression is not LambdaExpressionSyntax { Modifiers.Count: 0 } delay
            || Parameter(delay) is not ParameterSyntax parameter)
        {
            return PollyPlan.Manual(PollyReason.DelayNotLambda, "the sleep-duration provider is not a plain one-parameter lambda.");
        }

        string attempt = parameter.Identifier.ValueText;
        if (attempt is ArgsName or DelayFunctionName || UsesName(delay.Body, ArgsName) || UsesName(delay.Body, DelayFunctionName))
        {
            return PollyPlan.Manual(PollyReason.NameClash, $"the sleep-duration lambda already uses the name '{ArgsName}' or '{DelayFunctionName}'.");
        }

        string newLine = NewLine(creation);
        string indent = Indent(creation);
        string inner = indent + "        ";
        string function = delay.Body switch
        {
            BlockSyntax block => $"TimeSpan {DelayFunctionName}(int {attempt}){newLine}{inner}    {Reindent(block, inner + "    ", newLine)}",
            ExpressionSyntax expression => $"TimeSpan {DelayFunctionName}(int {attempt}) => {expression};",
            _ => throw new System.Diagnostics.UnreachableException(),
        };

        // v7 passes the 1-based retry number to the provider; v8's AttemptNumber is 0-based for the first retry (council P2).
        string text =
            $"new ResiliencePipelineBuilder(){newLine}"
            + $"{indent}    .AddRetry(new RetryStrategyOptions{newLine}"
            + $"{indent}    {{{newLine}"
            + $"{indent}        ShouldHandle = new PredicateBuilder().Handle<{exception}>(),{newLine}"
            + $"{indent}        MaxRetryAttempts = {retries},{newLine}"
            + $"{indent}        DelayGenerator = {ArgsName} =>{newLine}"
            + $"{indent}        {{{newLine}"
            + $"{inner}    {function}{newLine}"
            + $"{inner}    return new ValueTask<TimeSpan?>({DelayFunctionName}({ArgsName}.AttemptNumber + 1));{newLine}"
            + $"{indent}        }},{newLine}"
            + $"{indent}    }}){newLine}"
            + $"{indent}    .Build()";
        return PollyPlan.Of(SyntaxFactory.ParseExpression(text));
    }

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
            || UsesName(lambda.Body, DiscardName))
        {
            return PollyPlan.Manual(PollyReason.ExecuteNotLambda, "the ExecuteAsync argument is not a parameterless lambda.");
        }

        ParameterSyntax discard = SyntaxFactory.Parameter(SyntaxFactory.Identifier(DiscardName)).WithTrailingTrivia(lambda.ParameterList.GetTrailingTrivia());
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

    // Every use of the local must be the receiver of a rewritable ExecuteAsync; null when they all are.
    private static string? PlanLocalUses(LocalDeclarationStatementSyntax statement, ILocalSymbol local, SemanticModel model)
    {
        if (statement.Declaration.Variables.Count != 1 || statement.UsingKeyword.IsKind(SyntaxKind.UsingKeyword) || statement.IsConst)
        {
            return PollyPlan.Manual(PollyReason.PolicyNotLocal, "the policy declaration declares more than one variable.").Reason;
        }

        foreach (IdentifierNameSyntax use in Uses(statement, local, model))
        {
            if (ExecuteOn(use) is not InvocationExpressionSyntax execute)
            {
                return PollyPlan.Manual(PollyReason.PolicyUsedBeyondExecute,
                    $"'{local.Name}' is used at line {use.GetLocation().GetLineSpan().StartLinePosition.Line + 1} for more than ExecuteAsync (assigned, passed on or another member called).").Reason;
            }

            if (PlanCallback(execute, model).Reason is string reason)
            {
                return reason;
            }
        }

        return null;
    }

    private static IEnumerable<IdentifierNameSyntax> Uses(LocalDeclarationStatementSyntax statement, ILocalSymbol local, SemanticModel model) =>
        statement.SyntaxTree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(n => n.Identifier.ValueText == local.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, local));

    private static (LocalDeclarationStatementSyntax, ILocalSymbol)? LocalOf(InvocationExpressionSyntax creation, SemanticModel model) =>
        creation.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator, Parent.Parent.Parent: LocalDeclarationStatementSyntax statement }
        && model.GetDeclaredSymbol(declarator) is ILocalSymbol local
            ? (statement, local)
            : null;

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

    private static bool UsesName(SyntaxNode body, string name) =>
        body.DescendantTokens().Any(t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == name);

    private static string NewLine(SyntaxNode node) =>
        node.SyntaxTree.GetText().ToString().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

    private static string Indent(SyntaxNode node)
    {
        Microsoft.CodeAnalysis.Text.SourceText text = node.SyntaxTree.GetText();
        string line = text.Lines.GetLineFromPosition(node.SpanStart).ToString();
        return line[..(line.Length - line.TrimStart().Length)];
    }

    // The block's lines moved so its braces start at the new indentation, keeping the indentation inside it.
    private static string Reindent(BlockSyntax block, string indent, string newLine)
    {
        Microsoft.CodeAnalysis.Text.SourceText text = block.SyntaxTree.GetText();
        int column = block.OpenBraceToken.SpanStart - text.Lines.GetLineFromPosition(block.OpenBraceToken.SpanStart).Start;
        string[] lines = block.ToString().ReplaceLineEndings("\n").Split('\n');
        return string.Join(newLine, lines.Select((l, i) => i == 0 || l.Length == 0
            ? l
            : indent + (l.Length >= column && string.IsNullOrWhiteSpace(l[..column]) ? l[column..] : l.TrimStart())));
    }
}
