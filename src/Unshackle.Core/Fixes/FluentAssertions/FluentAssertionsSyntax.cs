using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Unshackle.Core.Fixes.FluentAssertions;

// Shared building blocks for FluentAssertions patterns: finding assertions, the subject and the because-message.
public static class FluentAssertionsSyntax
{
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

    // True when nothing uses the AndConstraint the assertion returns: a statement, or the body of a void lambda.
    public static bool IsResultDiscarded(InvocationExpressionSyntax assertion, SemanticModel model) => assertion.Parent switch
    {
        ExpressionStatementSyntax => true,
        AnonymousFunctionExpressionSyntax lambda =>
            model.GetTypeInfo(lambda).ConvertedType is INamedTypeSymbol { DelegateInvokeMethod.ReturnsVoid: true },
        _ => false,
    };

    // Splits the arguments into the value arguments (in order) and the `because` argument, which maps to Shouldly's
    // customMessage. False when becauseArgs or named arguments are used: Shouldly cannot format the message.
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

        foreach (IArgumentOperation argument in operation.Arguments)
        {
            switch (argument.ArgumentKind, argument.Parameter?.Name)
            {
                case (ArgumentKind.DefaultValue, _):
                    break;
                case (ArgumentKind.ParamArray, _) when argument.Value is IArrayCreationOperation { Initializer: null or { ElementValues.IsEmpty: true } }:
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

        return true;
    }
}
