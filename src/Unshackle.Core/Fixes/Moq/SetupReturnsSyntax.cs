using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// `m.Setup(x => x.M(..)).Returns(v);` → `m.Object.M(..).Returns(v);` (moq-queue.md row 3); ReturnsAsync(v) becomes
// Returns(Task.FromResult<R>(v)) or Returns(new ValueTask<R>(v)). The .Object goes when moq.create converts the mock.
public static class SetupReturnsSyntax
{
    public const string TasksNamespace = "System.Threading.Tasks";

    public static bool IsSetup(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Setup" }
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { Name: "Setup" } method && MoqSyntax.IsMockOfT(method.ContainingType);

    // The statement a Setup is part of, when it has the shape this row rewrites.
    public static ExpressionStatementSyntax? Statement(InvocationExpressionSyntax setup) =>
        setup.Parent is MemberAccessExpressionSyntax { Name.Identifier.Text: "Returns" or "ReturnsAsync", Parent: InvocationExpressionSyntax { Parent: ExpressionStatementSyntax statement } } access
        && access.Expression == setup ? statement : null;

    // The rewritten statement (in Node), or why the Setup cannot be rewritten; Type is set only when the rewrite needs System.Threading.Tasks.
    public static MockCall Plan(InvocationExpressionSyntax setup, SemanticModel model)
    {
        if (Statement(setup) is not { Expression: InvocationExpressionSyntax returns } statement)
        {
            return MockCall.Blocked(MoqReason.SetupShape, "only `m.Setup(..).Returns(v);` or `.ReturnsAsync(v);` as a statement; Callback, Throws, Verifiable and sequences are later moq-queue.md rows");
        }

        if (setup.ArgumentList.Arguments is not [var lambda] || setup.Expression is not MemberAccessExpressionSyntax { Expression: var receiver })
        {
            return MockCall.Blocked(MoqReason.SetupShape, "a Setup without a single lambda argument");
        }

        ExpressionSyntax objectAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression, receiver.WithoutTrivia(), SyntaxFactory.IdentifierName("Object"));
        var call = MockCall.Rewrite(lambda.Expression, objectAccess, model);
        if (call.Blocker is not null)
        {
            return call;
        }

        string name = ((MemberAccessExpressionSyntax)returns.Expression).Name.Identifier.Text;
        if (returns.ArgumentList.Arguments is not [{ Expression: var value }]
            || model.GetSymbolInfo(returns).Symbol is not IMethodSymbol { Parameters: [var parameter] }
            || parameter.Type.TypeKind == TypeKind.Delegate && call.Type!.TypeKind != TypeKind.Delegate
            || value is AnonymousFunctionExpressionSyntax)
        {
            return MockCall.Blocked(MoqReason.ReturnsLambda, $"{name} with a function or several arguments computes its value per call; see moq-queue.md row 7");
        }

        ExpressionSyntax returned = value.WithoutTrivia();
        bool needsTasks = false;
        if (name == "ReturnsAsync")
        {
            if (call.Type is not INamedTypeSymbol { TypeArguments: [var result] } task
                || task.OriginalDefinition.ToDisplayString() is not ("System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.ValueTask<TResult>"))
            {
                return MockCall.Blocked(MoqReason.ReturnsLambda, "ReturnsAsync on a member that is not Task<T>/ValueTask<T>");
            }

            string resultType = result.ToMinimalDisplayString(model, setup.SpanStart);
            returned = SyntaxFactory.ParseExpression(task.Name == "Task" ? $"Task.FromResult<{resultType}>({returned})" : $"new ValueTask<{resultType}>({returned})");
            needsTasks = true;
        }

        ExpressionStatementSyntax rewritten = SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, (ExpressionSyntax)call.Node!, SyntaxFactory.IdentifierName("Returns")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(returned)))));
        return new() { Node = rewritten.WithTriviaFrom(statement), Member = call.Member, ForAnyArguments = call.ForAnyArguments, Type = needsTasks ? call.Type : null };
    }
}
