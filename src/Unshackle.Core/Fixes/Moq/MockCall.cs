using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// The call inside a Setup/Verify lambda (`x => x.M(args)` or `x => x.P`), rewritten onto a new receiver, or why it cannot be.
public class MockCall
{
    public SyntaxNode? Node { get; init; }

    public string? Blocker { get; init; }

    // The type of the mocked member's value (the method's return type or the property type).
    public ITypeSymbol? Type { get; init; }

    public static MockCall Blocked(string blocker) => new() { Blocker = blocker };

    public static MockCall Done(SyntaxNode node) => new() { Node = node };

    public static MockCall Rewrite(ExpressionSyntax lambdaExpression, ExpressionSyntax receiver, SemanticModel model)
    {
        if (lambdaExpression is not SimpleLambdaExpressionSyntax { ExpressionBody: { } body } lambda
            || model.GetDeclaredSymbol(lambda.Parameter) is not { } parameter)
        {
            return Blocked("CallShape: the mocked call is not a lambda x => x.Member(...)");
        }

        switch (body)
        {
            case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target } access } invocation
                    when target.Identifier.Text == parameter.Name
                    && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { ReducedFrom: null, IsStatic: false } method:
                MockCall arguments = ArgMatchers.Rewrite(invocation.ArgumentList, method, parameter, model);
                if (arguments.Blocker is not null)
                {
                    return arguments;
                }

                return new()
                {
                    Node = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, access.Name.WithoutTrivia()),
                        (ArgumentListSyntax)arguments.Node!),
                    Type = method.ReturnType,
                };
            case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target } property
                    when target.Identifier.Text == parameter.Name && model.GetSymbolInfo(property).Symbol is IPropertySymbol { IsStatic: false } symbol:
                return new()
                {
                    Node = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, property.Name.WithoutTrivia()),
                    Type = symbol.Type,
                };
            default:
                return Blocked("CallShape: only x => x.Method(...) or x => x.Property on the mock itself (no extension method, chain or indexer)");
        }
    }
}
