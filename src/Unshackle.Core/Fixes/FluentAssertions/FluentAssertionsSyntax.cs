using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Unshackle.Core.Fixes.FluentAssertions;

// Shared building blocks for FluentAssertions patterns: finding assertions, the subject and the because-message.
public static class FluentAssertionsSyntax
{
    public const string InsideAssertionScopeReason =
        "Inside an AssertionScope: Shouldly stops at the first failure; group the assertions with ShouldSatisfyAllConditions.";

    // Every invocation of one of the methods whose symbol lives in a FluentAssertions namespace, whatever its shape.
    public static IEnumerable<InvocationExpressionSyntax> FindInvocations(SyntaxNode root, SemanticModel model, params string[] methodNames) =>
        root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(i => i.Expression is MemberAccessExpressionSyntax access
                && methodNames.Contains(access.Name.Identifier.ValueText)
                && IsFluentAssertions(model.GetSymbolInfo(i).Symbol));

    public static bool IsFluentAssertions(ISymbol? symbol) =>
        symbol?.ContainingNamespace?.ToDisplayString() is { } ns
        && (ns == "FluentAssertions" || ns.StartsWith("FluentAssertions.", StringComparison.Ordinal));

    // x in `x.Should().Method(...)`; null for other shapes such as `x.Should().A().And.Method(...)`.
    public static ExpressionSyntax? GetSubject(InvocationExpressionSyntax assertion) =>
        assertion.Expression is MemberAccessExpressionSyntax
        {
            Expression: InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Should" } should,
                ArgumentList.Arguments.Count: 0,
            },
        }
            ? should.Expression
            : null;

    // `x.ShouldNotBeNull()` when x may be null in a nullable-enabled context, else x. FluentAssertions accepts a nullable
    // subject and fails on null; Shouldly's non-nullable receivers would add CS8602/CS8604. ShouldNotBeNull keeps the failure.
    public static ExpressionSyntax NotNull(ExpressionSyntax subject, SemanticModel model) =>
        model.GetTypeInfo(subject).Nullability.FlowState == NullableFlowState.MaybeNull
            ? SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression, subject.WithoutTrailingTrivia(), SyntaxFactory.IdentifierName("ShouldNotBeNull")))
            : subject;

    // `x.ShouldNotBeNull()` around a receiver for a member access (parenthesized when needed, see IsSafeReceiver):
    // one null rule for every Shouldly receiver that is read directly, like Count or Length.
    public static ExpressionSyntax NotNullReceiver(ExpressionSyntax subject, SemanticModel model)
    {
        ExpressionSyntax receiver = IsSafeReceiver(subject) ? subject.WithoutTrailingTrivia() : SyntaxFactory.ParenthesizedExpression(subject.WithoutTrivia());
        return model.GetTypeInfo(subject).Nullability.FlowState == NullableFlowState.MaybeNull
            ? SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName("ShouldNotBeNull")))
            : receiver;
    }

    // True when the expression can be the receiver of a member access without parentheses.
    public static bool IsSafeReceiver(ExpressionSyntax subject) => subject is
        IdentifierNameSyntax or GenericNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax
        or ElementAccessExpressionSyntax or ParenthesizedExpressionSyntax or ThisExpressionSyntax or ObjectCreationExpressionSyntax
        || subject.IsKind(SyntaxKind.SuppressNullableWarningExpression);

    // Conservative: `this`, locals, parameters, fields and properties without arguments, and member accesses on those.
    // shortcut: property getters are assumed pure; refuse properties too when a getter with side effects shows up.
    public static bool IsSafeToEvaluateTwice(ExpressionSyntax expression, SemanticModel model) => expression switch
    {
        ThisExpressionSyntax => true,
        IdentifierNameSyntax => IsStorage(model.GetSymbolInfo(expression).Symbol),
        MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression, Name: IdentifierNameSyntax } access =>
            IsStorage(model.GetSymbolInfo(access).Symbol)
            && (model.GetSymbolInfo(access.Expression).Symbol is INamedTypeSymbol || IsSafeToEvaluateTwice(access.Expression, model)),
        _ => false,
    };

    // `var subject = x;` for a subject that IsSafeToEvaluateTwice refuses, to put before the statements that now each read
    // `subject`: FluentAssertions evaluates x once too, before any assertion. Only for a statement that
    // TryGetSplittableStatement accepts. The name is unused in the member. One pattern pass hoists several subjects of a
    // member on the same tree, so each gets its own number: k-th such subject → subject{k}, then k + n, k + 2n, ….
    public static (StatementSyntax Declaration, IdentifierNameSyntax Name) Hoist(ExpressionSyntax subject, SemanticModel model)
    {
        SyntaxNode scope = subject.Ancestors().FirstOrDefault(a => a is MemberDeclarationSyntax and not GlobalStatementSyntax) ?? subject.SyntaxTree.GetRoot();
        HashSet<string> used = [.. scope.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.ValueText)];
        List<ExpressionSyntax> unsafeSubjects =
        [
            .. scope.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Select(i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Should" } should && i.ArgumentList.Arguments.Count == 0 ? should.Expression : null)
                .OfType<ExpressionSyntax>()
                .Where(e => !IsSafeToEvaluateTwice(e, model)),
        ];
        int count = Math.Max(unsafeSubjects.Count, 1);
        int k = unsafeSubjects.IndexOf(subject) + 1;
        string name;
        for (int i = Math.Max(k, 1); used.Contains(name = i == 1 ? "subject" : "subject" + i); i += count)
        {
        }

        return (SyntaxFactory.ParseStatement($"var {name} = {subject.WithoutTrivia()};"), SyntaxFactory.IdentifierName(name));
    }

    private static bool IsStorage(ISymbol? symbol) =>
        symbol is ILocalSymbol or IParameterSymbol or IFieldSymbol or IPropertySymbol { IsIndexer: false };

    public const string NotSplittableReason =
        "The assertion is not directly in a block (e.g. a lambda body or an if without braces), so it cannot hold two statements: rewrite it by hand.";

    // For assertions rewritten to several statements: the statement around the assertion, else the assertion itself.
    public static SyntaxNode WithStatement(InvocationExpressionSyntax assertion) =>
        assertion.Parent is ExpressionStatementSyntax statement ? statement : assertion;

    // The assertion of a node from WithStatement.
    public static InvocationExpressionSyntax AssertionOfStatement(SyntaxNode node) =>
        (InvocationExpressionSyntax)((node as ExpressionStatementSyntax)?.Expression ?? node);

    // The statement around the assertion when it may be replaced by several statements: directly in a block or a switch section.
    public static bool TryGetSplittableStatement(SyntaxNode node, [NotNullWhen(true)] out ExpressionStatementSyntax? statement)
    {
        statement = node as ExpressionStatementSyntax ?? node.Parent as ExpressionStatementSyntax;
        return statement is { Parent: BlockSyntax or SwitchSectionSyntax };
    }

    // ShouldBe<T>(this T actual, T expected) infers T from both sides, and the receiver allows no numeric or nullable
    // conversion. FluentAssertions converted the argument to the subject type instead, e.g. byte b; b.Should().Be(1).
    // Returns the expected expression (cast when needed), or null when no safe form exists.
    public static ExpressionSyntax? ConvertExpected(ExpressionSyntax expected, ITypeSymbol subjectType, SemanticModel model, int position)
    {
        ITypeSymbol? expectedType = model.GetTypeInfo(expected).Type;
        if (expectedType is null || SymbolEqualityComparer.Default.Equals(expectedType, subjectType)
            || model.Compilation.ClassifyConversion(expectedType, subjectType).IsImplicit)
        {
            return expected;
        }

        Conversion toExpected = model.Compilation.ClassifyConversion(subjectType, expectedType);
        if (toExpected.IsImplicit && (toExpected.IsReference || toExpected.IsBoxing))
        {
            return expected;
        }

        if (!model.ClassifyConversion(expected, subjectType).IsImplicit)
        {
            return null;
        }

        ExpressionSyntax operand = expected is LiteralExpressionSyntax or IdentifierNameSyntax or MemberAccessExpressionSyntax
            ? expected.WithoutTrivia()
            : SyntaxFactory.ParenthesizedExpression(expected.WithoutTrivia());
        return SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(subjectType.ToMinimalDisplayString(model, position)), operand);
    }

    // Arguments separated by ", ", without their own trivia; a null (e.g. no because) is left out.
    public static ArgumentListSyntax Arguments(params IEnumerable<ArgumentSyntax?> arguments) =>
        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            arguments.OfType<ArgumentSyntax>().Select((a, i) => i == 0 ? a.WithoutTrivia() : a.WithoutTrivia().WithLeadingTrivia(SyntaxFactory.Space))));

    // `receiver.name(arguments)`.
    public static InvocationExpressionSyntax Invoke(ExpressionSyntax receiver, string name, params IEnumerable<ArgumentSyntax?> arguments) =>
        SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName(name)),
            Arguments(arguments));

    // For assertions whose `.Which`/`.Subject` Shouldly returns directly: the `….Which` member access wherever it is used
    // (`var y = ….Which;`, `….Which.P.Should()…`, an argument), else the assertion itself.
    public static SyntaxNode WithWhich(InvocationExpressionSyntax assertion) =>
        assertion.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Which" or "Subject" } which && which.Expression == assertion
            ? which
            : assertion;

    // The assertion of a node from WithWhich.
    public static InvocationExpressionSyntax AssertionOfWhich(SyntaxNode node) =>
        (InvocationExpressionSyntax)((node as MemberAccessExpressionSyntax)?.Expression ?? node);

    // A node from WithWhich: a discarded assertion, or `.Which`, whose value the Shouldly call returns as the same type.
    public static bool IsResultDiscardedOrWhich(AssertionContext context) =>
        context.Node is MemberAccessExpressionSyntax || IsResultDiscarded(context.Assertion, context.Model);

    public static string WhichUsedReason(string name) =>
        $"The result of {name}(...) is used other than through .Which/.Subject (.And chain or a value): rewrite it by hand.";

    // True when nothing uses the AndConstraint the assertion returns: a statement, the body of a void lambda or the
    // expression body of a void member. `a?.B.Should()...` counts too: the `?.` only skips the assertion, as it does after rewriting.
    public static bool IsResultDiscarded(InvocationExpressionSyntax assertion, SemanticModel model)
    {
        SyntaxNode node = assertion;
        while (node.Parent is ConditionalAccessExpressionSyntax conditional && conditional.WhenNotNull == node)
        {
            node = conditional;
        }

        return node.Parent switch
        {
            ExpressionStatementSyntax => true,
            AnonymousFunctionExpressionSyntax lambda =>
                model.GetTypeInfo(lambda).ConvertedType is INamedTypeSymbol { DelegateInvokeMethod.ReturnsVoid: true },
            ArrowExpressionClauseSyntax { Parent: { } member } => model.GetDeclaredSymbol(member) is IMethodSymbol { ReturnsVoid: true },
            _ => false,
        };
    }

    // Splits the arguments into the value arguments (in order) and the `because` argument, which maps to Shouldly's
    // customMessage. becauseArgs become `string.Format(CultureInfo.InvariantCulture, because, args)`: FluentAssertions formats
    // with the invariant culture too (probed under nl-NL). False for named arguments, or becauseArgs with a because that is
    // no constant or does not format: FluentAssertions swallows that FormatException, string.Format would throw it.
    public static bool TryGetArguments(
        InvocationExpressionSyntax assertion,
        SemanticModel model,
        out IReadOnlyList<ArgumentSyntax> values,
        out ArgumentSyntax? because)
    {
        var valueList = new List<ArgumentSyntax>();
        values = valueList;
        because = null;
        if (assertion.ArgumentList.Arguments.Any(a => a.NameColon is not null)
            || model.GetOperation(assertion) is not IInvocationOperation operation)
        {
            return false;
        }

        IReadOnlyList<SyntaxNode> becauseArgs = [];
        foreach (IArgumentOperation argument in operation.Arguments)
        {
            switch (argument.ArgumentKind, argument.Parameter?.Name)
            {
                case (ArgumentKind.DefaultValue, _):
                    break;
                case (ArgumentKind.ParamArray, _) when argument.Value is IArrayCreationOperation { Initializer: null or { ElementValues.IsEmpty: true } }:
                    break;
                case (ArgumentKind.ParamArray, "becauseArgs") when argument.Value is IArrayCreationOperation { Initializer: { } initializer }:
                    becauseArgs = [.. initializer.ElementValues.Select(e => e.Syntax)];
                    break;
                case (ArgumentKind.ParamArray, _) or (_, "becauseArgs"):
                    return false;
                case (_, _) when argument.Syntax is not ArgumentSyntax: // the receiver of an extension method call
                    break;
                case (_, "because"):
                    because = (ArgumentSyntax)argument.Syntax;
                    break;
                default:
                    valueList.Add((ArgumentSyntax)argument.Syntax);
                    break;
            }
        }

        if (becauseArgs.Count == 0)
        {
            return true;
        }

        if (because is null || model.GetConstantValue(because.Expression) is not { Value: string format } || !CanFormat(format, becauseArgs.Count))
        {
            return false;
        }

        because = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
            $"string.Format(System.Globalization.CultureInfo.InvariantCulture, {because.Expression.WithoutTrivia()}, {string.Join(", ", becauseArgs.Select(a => a.WithoutTrivia()))})"));
        return true;
    }

    private static bool CanFormat(string format, int count)
    {
        try
        {
            _ = string.Format(System.Globalization.CultureInfo.InvariantCulture, format, new object?[count]);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // True when node runs inside a FluentAssertions AssertionScope: the body of `using (new AssertionScope())`
    // or a statement after `using var scope = new AssertionScope();` in the same block or deeper.
    public static bool IsInsideAssertionScope(SyntaxNode node, SemanticModel model)
    {
        SyntaxNode child = node;
        foreach (SyntaxNode ancestor in node.Ancestors())
        {
            switch (ancestor)
            {
                case UsingStatementSyntax usingStatement when usingStatement.Statement == child
                    && IsAssertionScope(model.GetTypeInfo((SyntaxNode?)usingStatement.Declaration?.Type ?? usingStatement.Expression!).Type):
                    return true;
                case BlockSyntax block when block.Statements
                    .TakeWhile(s => s != child)
                    .OfType<LocalDeclarationStatementSyntax>()
                    .Any(d => d.UsingKeyword != default && IsAssertionScope(model.GetTypeInfo(d.Declaration.Type).Type)):
                    return true;
            }

            child = ancestor;
        }

        return false;
    }

    // AssertionScope is sealed in FluentAssertions 7, so no subclasses to look for.
    public static bool IsAssertionScope(ITypeSymbol? type) =>
        type is { Name: "AssertionScope" } && type.ContainingNamespace?.ToDisplayString() == "FluentAssertions.Execution";
}
