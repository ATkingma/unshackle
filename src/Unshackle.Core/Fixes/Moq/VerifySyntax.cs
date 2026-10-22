using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// `m.Verify(x => x.M(..), Times.Once())` → `m.Object.Received(1).M(..);` (moq-queue.md row 4). Times.Never → DidNotReceive(),
// Exactly(n) → Received(n), AtLeastOnce or no Times → Received(). A property becomes `_ = m.Object.Received(1).P;`.
// Verify() without a lambda, VerifyAll and VerifyNoOtherCalls are council M4 (queue row 10).
public static class VerifySyntax
{
    public static bool IsVerify(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation is { Parent: ExpressionStatementSyntax, Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "Verify" } }
        && invocation.ArgumentList.Arguments.Count > 0
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { Name: "Verify" } method && MoqSyntax.IsMockOfT(method.ContainingType);

    public static MockCall Plan(InvocationExpressionSyntax verify, SemanticModel model)
    {
        var method = (IMethodSymbol)model.GetSymbolInfo(verify).Symbol!;
        SeparatedSyntaxList<ArgumentSyntax> arguments = verify.ArgumentList.Arguments;
        if (method.Parameters.Any(p => p.Type.SpecialType == SpecialType.System_String) || arguments.Count > 2)
        {
            return MockCall.Blocked(MoqReason.VerifyMessage, "Verify with a failure message; NSubstitute's Received has none");
        }

        string? received = arguments.Count == 1 ? "Received()" : Received(arguments[1].Expression, model);
        if (received is null)
        {
            return MockCall.Blocked(MoqReason.TimesRange, "only Times.Once, Never, Exactly(n) and AtLeastOnce have an exact NSubstitute form");
        }

        var receiver = (MemberAccessExpressionSyntax)verify.Expression;
        ExpressionSyntax receivedAccess = SyntaxFactory.ParseExpression($"{receiver.Expression.WithoutTrivia()}.Object.{received}");
        var call = MockCall.Rewrite(arguments[0].Expression, receivedAccess, model);
        if (call.Blocker is not null)
        {
            return call;
        }

        var statement = (ExpressionStatementSyntax)verify.Parent!;
        StatementSyntax rewritten = call.Node is InvocationExpressionSyntax invocation
            ? SyntaxFactory.ExpressionStatement(invocation)
            : SyntaxFactory.ParseStatement($"_ = {call.Node};");
        return new() { Node = rewritten.WithTriviaFrom(statement), Member = call.Member };
    }

    private static string? Received(ExpressionSyntax times, SemanticModel model)
    {
        ExpressionSyntax target = times is InvocationExpressionSyntax invocation ? invocation.Expression : times;
        if (model.GetSymbolInfo(target).Symbol is not IMethodSymbol { ContainingType: { Name: "Times", ContainingNamespace.Name: "Moq" } } member)
        {
            return null;
        }

        SeparatedSyntaxList<ArgumentSyntax> arguments = (times as InvocationExpressionSyntax)?.ArgumentList.Arguments ?? default;
        return member.Name switch
        {
            "Once" when arguments.Count == 0 => "Received(1)",
            "Never" when arguments.Count == 0 => "DidNotReceive()",
            "AtLeastOnce" when arguments.Count == 0 => "Received()",
            "Exactly" when arguments is [var count] => $"Received({count.Expression.WithoutTrivia()})",
            _ => null,
        };
    }
}
