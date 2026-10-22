using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// moq-queue.md row 5: `m.SetupAllProperties();` and `m.SetupProperty(x => x.P[, initial]);` make Moq properties remember a set value,
// as NSubstitute properties always do. The statement goes (moq.create rewrites it in the same pass as its own analysis), an initial
// value becomes `m.P.Returns(initial);` (a configuration, not a set call that Received would count).
public static class PropertySyntax
{
    public static bool IsTracking(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation is { Parent: ExpressionStatementSyntax, Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "SetupAllProperties" or "SetupProperty" } }
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { IsStatic: false } method && MoqSyntax.IsMockOfT(method.ContainingType);

    // Records what the statement tracks; the blocker when its shape is not supported.
    public static MoqBlocker? Record(InvocationExpressionSyntax invocation, SemanticModel model, MockUseContext context)
    {
        context.TrackingPositions.Add(invocation.SpanStart);
        if (((MemberAccessExpressionSyntax)invocation.Expression).Name.Identifier.Text == "SetupAllProperties")
        {
            context.AllTracked = true;
            return null;
        }

        if (Property(invocation, model) is not { } property)
        {
            return MoqBlocker.Of(MoqReason.PropertyShape, "SetupProperty without x => x.Property of a settable property");
        }

        context.Tracked.Add(property);
        if (invocation.ArgumentList.Arguments.Count == 2)
        {
            context.SetUp.Add(property);
        }

        return null;
    }

    // The statement after the mock became a substitute: nothing, or `m.P.Returns(initial);`.
    public static IReadOnlyList<SyntaxNode> Rewrite(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (invocation.ArgumentList.Arguments is not [_, var initial] || Property(invocation, model) is not { } property)
        {
            return [];
        }

        ExpressionSyntax receiver = ((MemberAccessExpressionSyntax)invocation.Expression).Expression.WithoutTrivia();
        return [SyntaxFactory.ParseStatement($"{receiver}.{property.Name}.Returns({initial.Expression.WithoutTrivia()});").WithTriviaFrom(invocation.Parent!)];
    }

    private static IPropertySymbol? Property(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.ArgumentList.Arguments is [{ Expression: SimpleLambdaExpressionSyntax { ExpressionBody: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target } body } lambda }, ..]
        && target.Identifier.Text == lambda.Parameter.Identifier.Text
        && model.GetSymbolInfo(body).Symbol is IPropertySymbol { SetMethod: not null, GetMethod: not null, IsIndexer: false } property
            ? property
            : null;
}
