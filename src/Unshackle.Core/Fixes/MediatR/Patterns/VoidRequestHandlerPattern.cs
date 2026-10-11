using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class VoidRequestHandlerPattern : IPattern
{
    public string Id => "mediatr.void-request-handler";

    public string Description => "IRequestHandler<TReq>: Task Handle(...) → ValueTask<Unit> Handle(...) with return Unit.Value";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        MediatRSyntax.FindHandleMethods(root, model, "IRequestHandler`1");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var method = (MethodDeclarationSyntax)node;
        if (method.ExplicitInterfaceSpecifier is not null)
        {
            return PatternResult.Manual("Handle is an explicit interface implementation; rename the interface and return type by hand.");
        }

        // Mediator's IRequestHandler<TRequest> is IRequestHandler<TRequest, Unit>: Handle returns ValueTask<Unit>.
        TypeSyntax? returnType = MediatRSyntax.ToValueTask(method.ReturnType) switch
        {
            IdentifierNameSyntax name => WithUnit(name),
            QualifiedNameSyntax { Right: IdentifierNameSyntax right } qualified => qualified.WithRight(WithUnit(right)),
            _ => null,
        };
        if (returnType is null)
        {
            return PatternResult.Manual("Handle does not return Task in the form Task; change it to ValueTask<Unit> by hand.");
        }

        returnType = returnType.WithTriviaFrom(method.ReturnType);
        if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            return RewriteSync(method, model, returnType);
        }

        if (method.Body is not { Statements.Count: > 0 } body)
        {
            return PatternResult.Manual("Handle is async without a statement body; write it as a block that ends with return Unit.Value.");
        }

        return PatternResult.Fixed(RewriteAsync(body, model, method).WithReturnType(returnType));
    }

    // async: every bare "return;" and a reachable end of the body return Unit.Value.
    private static MethodDeclarationSyntax RewriteAsync(BlockSyntax body, SemanticModel model, MethodDeclarationSyntax method)
    {
        bool endReachable = model.AnalyzeControlFlow(body)?.EndPointIsReachable ?? true;
        StatementSyntax last = body.Statements[^1];
        BlockSyntax rewritten = body.ReplaceNodes(
            body.DescendantNodes(n => n is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
                .OfType<ReturnStatementSyntax>()
                .Where(r => r.Expression is null),
            (original, _) => ReturnUnit().WithTriviaFrom(original));
        if (endReachable)
        {
            // Same indentation as the last statement.
            rewritten = rewritten.AddStatements(ReturnUnit()
                .WithLeadingTrivia(last.GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).TakeLast(1))
                .WithTrailingTrivia(last.GetTrailingTrivia()));
        }

        return method.WithBody(rewritten);
    }

    // Not async: only return Task.CompletedTask is known; it becomes Unit.ValueTask.
    private static PatternResult RewriteSync(MethodDeclarationSyntax method, SemanticModel model, TypeSyntax returnType)
    {
        List<ExpressionSyntax?> returned = MediatRSyntax.ReturnedExpressions(method);
        if (returned.Any(e => e is not MemberAccessExpressionSyntax || !MediatRSyntax.IsTaskMember(e, model, "CompletedTask")))
        {
            return PatternResult.Manual("Handle returns a Task that is not Task.CompletedTask; Mediator needs a ValueTask<Unit>, convert it by hand (e.g. make the method async and return Unit.Value).");
        }

        MethodDeclarationSyntax rewritten = method.ReplaceNodes(
            returned.OfType<MemberAccessExpressionSyntax>(),
            (original, _) => SyntaxFactory.ParseExpression("Unit.ValueTask").WithTriviaFrom(original));
        return PatternResult.Fixed(rewritten.WithReturnType(returnType));
    }

    private static GenericNameSyntax WithUnit(IdentifierNameSyntax valueTask) =>
        SyntaxFactory.GenericName(valueTask.Identifier, SyntaxFactory.TypeArgumentList([SyntaxFactory.IdentifierName("Unit")]));

    private static ReturnStatementSyntax ReturnUnit() => (ReturnStatementSyntax)SyntaxFactory.ParseStatement("return Unit.Value;");
}
