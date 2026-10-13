using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Shared by the Throw, ThrowAsync, ThrowExactly and Invoking patterns: a Throw<T>() chain (see ThrowChain) becomes the
// exception Should.Throw<T>/Should.ThrowAsync<T> returns, with each step as a Shouldly check on it:
//   .WithMessage(p)                → e.Message.ShouldBe/ShouldStartWith/… (ThrowMessage.Check)
//   .WithParameterName(n)          → e.ParamName.ShouldBe(n)
//   .Where(x => p)                 → (p[x := e]).ShouldBeTrue()
//   .WithInnerException<TI>()      → e.InnerException.ShouldNotBeNull().ShouldBeAssignableTo<TI>()   (a new e)
//   .WithInnerExceptionExactly<TI>() → e.InnerException.ShouldBeOfType<TI>()                        (a new e)
//   .And / .Which                  → e
// A check that is not the last step needs e in a local: the statement is split (`var exception = …;` before it).
// `var ex = <chain>;` keeps ex, now the exception, when every use of ex is itself a chain (`ex.And.P`, `ex.WithInnerException<T>()`).
internal static class ThrowChains
{
    public static readonly string[] SyncNames = ["Throw", "ThrowExactly"];

    public static readonly string[] AsyncNames = ["ThrowAsync", "ThrowExactlyAsync"];

    private static readonly string[] s_steps = ["WithMessage", "WithParameterName", "Where", "WithInnerException", "WithInnerExceptionExactly"];

    private const string HoistName = "exception";

    // One node per chain of an owned Throw call, plus one per use of a local such a chain declares: the statement the chain
    // starts (so it can be split), else its outermost node.
    public static IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model, string[] names, Func<InvocationExpressionSyntax, bool> owns) =>
        FluentAssertionsSyntax.FindInvocations(root, model, names)
            .Where(owns)
            .SelectMany(t => WithUses(Build(t, model), model))
            .Select(NodeOf)
            .Distinct();

    public static PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (OriginOf(node, model) is not { } origin)
        {
            return PatternResult.Manual("Throw<T>() is not in a form this pattern knows.");
        }

        ThrowChain chain = Build(origin, model);

        // Every chain of a declared local is rewritten together with the declaration, or none is.
        ThrowChain root = chain;
        while (root.Origin is IdentifierNameSyntax && DeclarationOf(root.Origin, model) is { } declaration)
        {
            root = declaration;
        }

        if (root.Origin is IdentifierNameSyntax)
        {
            return PatternResult.Manual("The local is not declared by a Throw<T>() chain.");
        }

        if (WithUses(root, model).Select(c => Generate(c, model)).OfType<ManualResult>().FirstOrDefault() is { } manual)
        {
            return manual;
        }

        return Generate(chain, model);
    }

    public static ThrowChain Build(ExpressionSyntax origin, SemanticModel model)
    {
        var steps = new List<InvocationExpressionSyntax>();
        InvocationExpressionSyntax? configureAwait = null;
        AwaitExpressionSyntax? await = null;
        MemberAccessExpressionSyntax? tail = null;
        ExpressionSyntax current = origin;
        while (true)
        {
            if (current.Parent is MemberAccessExpressionSyntax access && access.Expression == current)
            {
                string name = access.Name.Identifier.ValueText;
                if (name is "And" or "Which")
                {
                    tail = access;
                    current = access;
                    break;
                }

                if (access.Parent is InvocationExpressionSyntax call && call.Expression == access)
                {
                    if (s_steps.Contains(name) && FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(call).Symbol))
                    {
                        steps.Add(call);
                        current = call;
                        continue;
                    }

                    if (name == "ConfigureAwait" && configureAwait is null && await is null && origin is InvocationExpressionSyntax)
                    {
                        configureAwait = call;
                        current = call;
                        continue;
                    }
                }

                break;
            }

            if (current.Parent is AwaitExpressionSyntax awaited && await is null && origin is InvocationExpressionSyntax)
            {
                await = awaited;
                current = awaited;
                continue;
            }

            // `(await …).And`: climb the parentheses only when something of the chain follows them.
            if (current.Parent is ParenthesizedExpressionSyntax)
            {
                ExpressionSyntax outer = current;
                while (outer.Parent is ParenthesizedExpressionSyntax parenthesized)
                {
                    outer = parenthesized;
                }

                if (outer.Parent is MemberAccessExpressionSyntax next && next.Expression == outer
                    && (next.Name.Identifier.ValueText is "And" or "Which" || s_steps.Contains(next.Name.Identifier.ValueText)))
                {
                    current = outer;
                    continue;
                }
            }

            break;
        }

        return new ThrowChain { Origin = origin, Steps = steps, ConfigureAwait = configureAwait, Await = await, Tail = tail, Top = current };
    }

    // The statement whose evaluation starts with the chain (`chain.And.P.ShouldBe(..);`, `var x = chain;`), else the chain.
    public static SyntaxNode NodeOf(ThrowChain chain) => LeadingStatement(chain.Top) ?? (SyntaxNode)chain.Top;

    private static StatementSyntax? LeadingStatement(ExpressionSyntax top)
    {
        SyntaxNode current = top;
        while (true)
        {
            switch (current.Parent)
            {
                case MemberAccessExpressionSyntax access when access.Expression == current:
                case InvocationExpressionSyntax invocation when invocation.Expression == current:
                case ElementAccessExpressionSyntax element when element.Expression == current:
                case ConditionalAccessExpressionSyntax conditional when conditional.Expression == current:
                case ParenthesizedExpressionSyntax:
                case AwaitExpressionSyntax:
                    current = current.Parent;
                    continue;
                case ExpressionStatementSyntax statement:
                    return statement;
                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Variables.Count: 1, Parent: LocalDeclarationStatementSyntax local } } }:
                    return local;
                default:
                    return null;
            }
        }
    }

    // The origin of the chain a node from Find belongs to: down the leading expressions to the Throw call or the local.
    private static ExpressionSyntax? OriginOf(SyntaxNode node, SemanticModel model)
    {
        ExpressionSyntax? current = node switch
        {
            ExpressionStatementSyntax statement => statement.Expression,
            LocalDeclarationStatementSyntax local => local.Declaration.Variables[0].Initializer?.Value,
            ExpressionSyntax expression => expression,
            _ => null,
        };
        while (current is not null)
        {
            if (current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } call
                && (SyncNames.Contains(access.Name.Identifier.ValueText) || AsyncNames.Contains(access.Name.Identifier.ValueText))
                && FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(call).Symbol))
            {
                return call;
            }

            if (current is IdentifierNameSyntax && model.GetSymbolInfo(current).Symbol is ILocalSymbol)
            {
                return current;
            }

            current = current switch
            {
                MemberAccessExpressionSyntax member => member.Expression,
                InvocationExpressionSyntax invocation => invocation.Expression,
                ElementAccessExpressionSyntax element => element.Expression,
                ConditionalAccessExpressionSyntax conditional => conditional.Expression,
                ParenthesizedExpressionSyntax parenthesized => parenthesized.Expression,
                AwaitExpressionSyntax awaited => awaited.Expression,
                _ => null,
            };
        }

        return null;
    }

    // The chain and, when it declares a `var` local, the chains of every use of that local (recursively).
    private static IEnumerable<ThrowChain> WithUses(ThrowChain chain, SemanticModel model)
    {
        yield return chain;
        if (DeclaredLocal(chain) is not { } declarator || model.GetDeclaredSymbol(declarator) is not ILocalSymbol local)
        {
            yield break;
        }

        foreach (IdentifierNameSyntax use in UsesOf(declarator, local, model))
        {
            foreach (ThrowChain used in WithUses(Build(use, model), model))
            {
                yield return used;
            }
        }
    }

    private static IEnumerable<IdentifierNameSyntax> UsesOf(VariableDeclaratorSyntax declarator, ILocalSymbol local, SemanticModel model) =>
        (declarator.Ancestors().FirstOrDefault(a => a is MemberDeclarationSyntax and not GlobalStatementSyntax) ?? declarator.SyntaxTree.GetRoot())
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Where(i => i.Identifier.ValueText == local.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(i).Symbol, local));

    // `var x = <chain>;` (no tail): the declarator of x.
    private static VariableDeclaratorSyntax? DeclaredLocal(ThrowChain chain) =>
        chain.Tail is null
        && chain.Top.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator }
        && declarator.Parent is VariableDeclarationSyntax { Variables.Count: 1, Type.IsVar: true, Parent: LocalDeclarationStatementSyntax }
            ? declarator
            : null;

    // The chain that declares the local an identifier origin refers to.
    private static ThrowChain? DeclarationOf(ExpressionSyntax origin, SemanticModel model)
    {
        if (model.GetSymbolInfo(origin).Symbol is not ILocalSymbol { DeclaringSyntaxReferences: [var reference] }
            || reference.GetSyntax() is not VariableDeclaratorSyntax { Initializer.Value: var value } declarator
            || declarator.SyntaxTree != origin.SyntaxTree
            || OriginOf(value, model) is not { } declaredOrigin)
        {
            return null;
        }

        ThrowChain declaration = Build(declaredOrigin, model);
        return DeclaredLocal(declaration) == declarator ? declaration : null;
    }

    // The rewrite of the node of one chain (NodeOf), without looking at the other chains of a local.
    private static PatternResult Generate(ThrowChain chain, SemanticModel model)
    {
        SyntaxNode node = NodeOf(chain);
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        if (FluentAssertionsSyntax.IsInsideExpectedFailure(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.ExpectedFailureReason);
        }

        ExpressionSyntax exception;
        if (chain.Origin is InvocationExpressionSyntax throwCall)
        {
            if (Start(chain, throwCall, model) is not { } start)
            {
                return StartFailure(chain, throwCall, model);
            }

            exception = start;
        }
        else
        {
            exception = SyntaxFactory.IdentifierName(((IdentifierNameSyntax)chain.Origin).Identifier.ValueText);
        }

        VariableDeclaratorSyntax? declarator = DeclaredLocal(chain);
        bool statement = chain.Tail is null && declarator is null && IsDiscarded(chain.Top, model);
        if (chain.Tail is null && declarator is null && !statement)
        {
            return PatternResult.Manual("The result of Throw<T>() is used as a value (passed on, returned, or a local with its type written out): Shouldly's Should.Throw returns the exception, not an assertion object.");
        }

        var statements = new List<StatementSyntax>();
        ExpressionSyntax? last = null;
        int hoists = 0;
        for (int i = 0; i < chain.Steps.Count; i++)
        {
            InvocationExpressionSyntax step = chain.Steps[i];
            var stepName = (SimpleNameSyntax)((MemberAccessExpressionSyntax)step.Expression).Name;
            string name = stepName.Identifier.ValueText;
            if (!FluentAssertionsSyntax.TryGetArguments(step, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because))
            {
                return PatternResult.Manual($"{name}(...) with named arguments, or becauseArgs whose because is no constant format string: rewrite it by hand.");
            }

            if (name is "WithInnerException" or "WithInnerExceptionExactly")
            {
                // On the awaited Task of ThrowAsync, FluentAssertions 7 spells it WithInnerException<TException, TInner>().
                if (stepName is not GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 or 2 } generic || values.Count != 0)
                {
                    return PatternResult.Manual($"{name}(Type) with a runtime type: rewrite it by hand with ShouldBeAssignableTo/ShouldBeOfType.");
                }

                GenericNameSyntax inner = generic.WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(generic.TypeArgumentList.Arguments[^1])));

                ExpressionSyntax innerException = Member(Receiver(exception), "InnerException");
                exception = name == "WithInnerException"
                    // ShouldBeAssignableTo passes on null; FluentAssertions fails when there is no inner exception (probed).
                    ? InvokeGeneric(FluentAssertionsSyntax.Invoke(innerException, "ShouldNotBeNull", because), "ShouldBeAssignableTo", inner, because)
                    : InvokeGeneric(innerException, "ShouldBeOfType", inner, because);
                continue;
            }

            bool final = i == chain.Steps.Count - 1 && statement && name != "Where";
            if (!final && exception is not IdentifierNameSyntax)
            {
                // The local of `var ex = …` itself when no step after this one changes the exception.
                bool asDeclared = declarator is not null && !chain.Steps.Skip(i + 1).Any(s => ((MemberAccessExpressionSyntax)s.Expression).Name.Identifier.ValueText.StartsWith("WithInner", StringComparison.Ordinal));
                string local = asDeclared ? declarator!.Identifier.ValueText : NewName(chain, hoists++, model);
                statements.Add(SyntaxFactory.ParseStatement($"var {local} = {exception};"));
                exception = SyntaxFactory.IdentifierName(local);
            }

            PatternResult check = Check(name, step, exception, values, because, model);
            if (check is not FixedResult { NewNodes: [ExpressionSyntax checkExpression] })
            {
                return check;
            }

            if (final)
            {
                last = checkExpression;
            }
            else
            {
                statements.Add(SyntaxFactory.ExpressionStatement(checkExpression));
            }
        }

        // What replaces the chain's top node in the node's own statement; null when the statement is gone (all in statements).
        ExpressionSyntax? replacement;
        if (chain.Tail is not null)
        {
            replacement = Receiver(exception);
        }
        else if (declarator is not null)
        {
            replacement = exception is IdentifierNameSyntax { Identifier.ValueText: var declared } && declared == declarator.Identifier.ValueText ? null : exception;
        }
        else
        {
            bool checkedLast = chain.Steps.Count > 0 && !((MemberAccessExpressionSyntax)chain.Steps[^1].Expression).Name.Identifier.ValueText.StartsWith("WithInner", StringComparison.Ordinal);
            replacement = last ?? (checkedLast ? null : exception);
        }

        if (statements.Count == 0)
        {
            ExpressionSyntax single = replacement!.WithTriviaFrom(chain.Top);
            return PatternResult.Fixed(node == chain.Top ? single : node.ReplaceNode(chain.Top, single));
        }

        if (node is not StatementSyntax { Parent: BlockSyntax or SwitchSectionSyntax } own)
        {
            return PatternResult.Manual("The Throw<T>() chain checks the exception and goes on (e.g. WithMessage(..).And or Where): that needs a local, but it is not a statement in a block.");
        }

        if (replacement is not null)
        {
            statements.Add(own.ReplaceNode(chain.Top, replacement.WithTriviaFrom(chain.Top)));
        }

        return PatternResult.Fixed(statements);
    }

    // `Should.Throw<T>(action[, because])` (`.ShouldBeOfType<T>()` for ThrowExactly), or the awaited ThrowAsync form; null
    // when StartFailure has the reason.
    private static ExpressionSyntax? Start(ThrowChain chain, InvocationExpressionSyntax throwCall, SemanticModel model) =>
        StartOrReason(chain, throwCall, model, out ExpressionSyntax? start, out _) ? start : null;

    private static PatternResult StartFailure(ThrowChain chain, InvocationExpressionSyntax throwCall, SemanticModel model)
    {
        StartOrReason(chain, throwCall, model, out _, out string reason);
        return PatternResult.Manual(reason);
    }

    private static bool StartOrReason(ThrowChain chain, InvocationExpressionSyntax throwCall, SemanticModel model, out ExpressionSyntax? start, out string reason)
    {
        start = null;
        reason = string.Empty;
        var name = (SimpleNameSyntax)((MemberAccessExpressionSyntax)throwCall.Expression).Name;
        bool async = AsyncNames.Contains(name.Identifier.ValueText);
        bool exactly = ThrowSyntax.IsExactly(throwCall);
        string method = async ? "ThrowAsync" : "Throw";
        if (name is not GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } generic)
        {
            reason = $"{method} without exactly one type argument.";
            return false;
        }

        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(throwCall);
        if (subject is null)
        {
            reason = $"{method}<T>(...) is not in the form x.Should().{method}<T>(), e.g. after .And.";
            return false;
        }

        if (FluentAssertionsSyntax.ExpectsAssertionFailure(subject, model.GetTypeInfo(generic.TypeArgumentList.Arguments[0]).Type, model))
        {
            reason = FluentAssertionsSyntax.ExpectedFailureReason;
            return false;
        }

        if (async && chain.Await is null)
        {
            reason = "ThrowAsync<T>() is not directly awaited (no await, e.g. the task is stored): Shouldly's Should.ThrowAsync is not chained like that.";
            return false;
        }

        ExpressionSyntax action = subject;
        ExpressionSyntax inspected = subject;
        if (ThrowSyntax.IsInvoking(throwCall, model))
        {
            if (!ThrowSyntax.TryGetAction(subject, model, out action, out inspected, out reason))
            {
                return false;
            }
        }
        else if (FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(subject).Symbol))
        {
            reason = async
                ? "The subject is built with FluentAssertions (e.g. FluentActions.Awaiting): pass the delegate to Should.ThrowAsync by hand."
                : "The subject is built with FluentAssertions (e.g. FluentActions.Invoking): pass the delegate to Should.Throw by hand.";
            return false;
        }

        if (model.GetTypeInfo(subject).Type is not INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: { Parameters.Length: 0 } invoke })
        {
            reason = async
                ? "The subject is not a Func<Task> or a Func<Task<T>> with T a reference type, the only delegates Should.ThrowAsync accepts."
                : "The subject is not an Action or a Func returning a reference type, the only delegates Should.Throw accepts.";
            return false;
        }

        // Should.ThrowAsync takes a Func<Task>; Func<Task<T>> converts to it only when T is a reference type.
        bool accepted = async
            ? invoke.ReturnType is INamedTypeSymbol { Name: "Task", ContainingNamespace.Name: "Tasks" } task && task.TypeArguments is not [{ IsReferenceType: false }]
            : invoke.ReturnsVoid || invoke.ReturnType.IsReferenceType;
        if (!accepted)
        {
            reason = async
                ? "The subject is not a Func<Task> or a Func<Task<T>> with T a reference type, the only delegates Should.ThrowAsync accepts."
                : "The subject is not an Action or a Func returning a reference type, the only delegates Should.Throw accepts.";
            return false;
        }

        // ThrowExactly does not unwrap AggregateException, just like Shouldly.
        if (!exactly && ThrowMessage.MayThrowAggregate(inspected, generic.TypeArgumentList.Arguments[0], model))
        {
            reason = ThrowMessage.AggregateReason;
            return false;
        }

        if (!FluentAssertionsSyntax.TryGetArguments(throwCall, model, out IReadOnlyList<ArgumentSyntax> throwValues, out ArgumentSyntax? throwBecause)
            || throwValues.Count != 0)
        {
            reason = $"{method}<T>() with named arguments, or becauseArgs whose because is no constant format string: rewrite it by hand.";
            return false;
        }

        ExpressionSyntax shouldThrow = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Should"),
                SyntaxFactory.GenericName(SyntaxFactory.Identifier(method), generic.TypeArgumentList)),
            ThrowMessage.Arguments([SyntaxFactory.Argument(action.WithoutTrivia())], throwBecause));
        if (!async)
        {
            start = exactly ? ThrowMessage.OfType(shouldThrow, generic) : shouldThrow;
            return true;
        }

        if (chain.ConfigureAwait is { } configureAwait)
        {
            // Shouldly's ThrowAsync returns Task<TException>, so the same ConfigureAwait applies.
            shouldThrow = configureAwait.WithExpression(((MemberAccessExpressionSyntax)configureAwait.Expression).WithExpression(shouldThrow)).WithoutTrivia();
        }

        ExpressionSyntax thrown = SyntaxFactory.AwaitExpression(SyntaxFactory.Token(SyntaxKind.AwaitKeyword).WithTrailingTrivia(SyntaxFactory.Space), shouldThrow);
        start = exactly ? ThrowMessage.OfType(SyntaxFactory.ParenthesizedExpression(thrown), generic) : thrown;
        return true;
    }

    // The Shouldly check for a step that does not change the exception.
    private static PatternResult Check(string name, InvocationExpressionSyntax step, ExpressionSyntax exception, IReadOnlyList<ArgumentSyntax> values, ArgumentSyntax? because, SemanticModel model)
    {
        if (values.Count != 1)
        {
            return PatternResult.Manual($"{name}(...) with an unexpected argument list: rewrite it by hand.");
        }

        switch (name)
        {
            case "WithMessage":
                return model.GetConstantValue(values[0].Expression) is { Value: string pattern }
                    ? ThrowMessage.Check(Receiver(exception), pattern, because)
                    : PatternResult.Manual("WithMessage(...) pattern is not a string constant, so its wildcards cannot be translated.");
            case "WithParameterName":
                // FluentAssertions compares the parameter name exactly (ordinal, probed), like ShouldBe on strings.
                return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(Member(Receiver(exception), "ParamName"), "ShouldBe", values[0], because));
            default:
                if (values[0].Expression is not LambdaExpressionSyntax { ExpressionBody: { } body } lambda
                    || model.GetSymbolInfo(lambda).Symbol is not IMethodSymbol { Parameters: [var parameter] })
                {
                    return PatternResult.Manual("Where(...) without a one-parameter lambda with an expression body: rewrite it by hand.");
                }

                ExpressionSyntax condition = body.ReplaceNodes(
                    body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                        .Where(i => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(i).Symbol, parameter)),
                    (original, _) => exception.WithTriviaFrom(original));
                condition = condition.WithoutTrivia();
                if (condition is not (InvocationExpressionSyntax or MemberAccessExpressionSyntax or IdentifierNameSyntax or ParenthesizedExpressionSyntax))
                {
                    condition = SyntaxFactory.ParenthesizedExpression(condition);
                }

                return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(condition, "ShouldBeTrue", because));
        }
    }

    // True when nothing uses the chain's value: a statement, the body of a void lambda (or an async lambda returning Task)
    // or the expression body of a void member.
    private static bool IsDiscarded(ExpressionSyntax top, SemanticModel model) => top.Parent switch
    {
        ExpressionStatementSyntax => true,
        AnonymousFunctionExpressionSyntax lambda => model.GetTypeInfo(lambda).ConvertedType is INamedTypeSymbol { DelegateInvokeMethod: { } invoke }
            && (invoke.ReturnsVoid
                || top is AwaitExpressionSyntax && lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword) && invoke.ReturnType is INamedTypeSymbol { Name: "Task", IsGenericType: false }),
        ArrowExpressionClauseSyntax { Parent: { } member } => model.GetDeclaredSymbol(member) is IMethodSymbol method
            && (method.ReturnsVoid || top is AwaitExpressionSyntax && method.IsAsync && method.ReturnType is INamedTypeSymbol { Name: "Task", IsGenericType: false }),
        _ => false,
    };

    // A name unused in the member: the k-th chain of the member gets exception{k}, its j-th local exception{k + j·n}, so
    // the chains of one pass, rewritten independently, never pick the same name.
    private static string NewName(ThrowChain chain, int hoist, SemanticModel model)
    {
        SyntaxNode scope = chain.Top.Ancestors().FirstOrDefault(a => a is MemberDeclarationSyntax and not GlobalStatementSyntax) ?? chain.Top.SyntaxTree.GetRoot();
        HashSet<string> used = [.. scope.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.ValueText)];
        List<int> starts =
        [
            .. FluentAssertionsSyntax.FindInvocations(scope, model, [.. SyncNames, .. AsyncNames])
                .SelectMany(t => WithUses(Build(t, model), model))
                .Select(c => c.Origin.SpanStart)
                .Distinct()
                .Order(),
        ];
        int count = Math.Max(starts.Count, 1);
        int index = Math.Max(starts.IndexOf(chain.Origin.SpanStart), 0) + (hoist * count);
        while (true)
        {
            string candidate = index == 0 ? HoistName : HoistName + (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!used.Contains(candidate))
            {
                return candidate;
            }

            index += count;
        }
    }

    // `(await …)` as a receiver.
    private static ExpressionSyntax Receiver(ExpressionSyntax exception) =>
        exception is AwaitExpressionSyntax ? SyntaxFactory.ParenthesizedExpression(exception) : exception;

    private static MemberAccessExpressionSyntax Member(ExpressionSyntax receiver, string name) =>
        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName(name));

    private static InvocationExpressionSyntax InvokeGeneric(ExpressionSyntax receiver, string name, GenericNameSyntax typeFrom, ArgumentSyntax? because) =>
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.GenericName(SyntaxFactory.Identifier(name), typeFrom.TypeArgumentList)),
            FluentAssertionsSyntax.Arguments(because));
}
