using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// The call inside a Setup/Verify lambda (`x => x.M(args)` or `x => x.P`), rewritten onto a new receiver, or why it cannot be.
public class MockCall
{
    public SyntaxNode? Node { get; init; }

    public MoqBlocker? Blocker { get; init; }

    // The type of the mocked member's value (the method's return type or the property type).
    public ITypeSymbol? Type { get; init; }

    // The mocked method or property.
    public ISymbol? Member { get; init; }

    // Every argument is It.IsAny (or there are none): the call is set up or verified for every argument.
    public bool ForAnyArguments { get; init; }

    // A Setup statement that goes (a void member set up without anything to do).
    public bool Removed { get; init; }

    // A Setup marked .Verifiable() (council round 8, M4).
    public bool Verifiable { get; init; }

    // The rewrite uses NSubstitute.ExceptionExtensions (Throws/ThrowsAsync on a value).
    public bool Throws { get; init; }

    public static MockCall Blocked(MoqBlocker blocker) => new() { Blocker = blocker };

    public static MockCall Blocked(MoqReason code, string reason) => Blocked(MoqBlocker.Of(code, reason));

    public static MockCall Done(SyntaxNode node) => new() { Node = node };

    public static MockCall Rewrite(ExpressionSyntax lambdaExpression, ExpressionSyntax receiver, SemanticModel model)
    {
        if (lambdaExpression is not SimpleLambdaExpressionSyntax { ExpressionBody: { } body } lambda
            || model.GetDeclaredSymbol(lambda.Parameter) is not { } parameter)
        {
            return Blocked(MoqBlocker.Of(MoqReason.CallShape, "the mocked call is not a lambda x => x.Member(...)"));
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
                    Member = method,
                    ForAnyArguments = arguments.ForAnyArguments,
                };
            case MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target } property
                    when target.Identifier.Text == parameter.Name && model.GetSymbolInfo(property).Symbol is IPropertySymbol { IsStatic: false } symbol:
                return new()
                {
                    Node = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, property.Name.WithoutTrivia()),
                    Type = symbol.Type,
                    Member = symbol,
                    ForAnyArguments = true,
                };
            default:
                return Blocked(MoqBlocker.Of(MoqReason.CallShape, "only x => x.Method(...) or x => x.Property on the mock itself (no extension method, chain or indexer)"));
        }
    }
}
