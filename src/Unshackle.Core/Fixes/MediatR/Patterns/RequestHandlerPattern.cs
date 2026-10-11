using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class RequestHandlerPattern : IPattern
{
    public string Id => "mediatr.request-handler";

    public string Description => "IRequestHandler<TReq, TRes>: Task<TRes> Handle(...) → ValueTask<TRes> Handle(...), Task.FromResult → ValueTask.FromResult";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        MediatRSyntax.FindHandleMethods(root, model, "IRequestHandler`2");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var method = (MethodDeclarationSyntax)node;
        if (MediatRSyntax.RejectExplicit(method, "rename the interface and return type by hand.") is { } explicitHandle)
        {
            return explicitHandle;
        }

        TypeSyntax? returnType = method.ReturnType is GenericNameSyntax or QualifiedNameSyntax { Right: GenericNameSyntax }
            ? MediatRSyntax.ToValueTask(method.ReturnType)
            : null;
        if (returnType is null)
        {
            return PatternResult.Manual("Handle does not return Task<T> in the form Task<T>; change it to ValueTask<T> by hand.");
        }

        if (method.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            // await and return x work the same in an async ValueTask<T> method.
            return PatternResult.Fixed(method.WithReturnType(returnType.WithTriviaFrom(method.ReturnType)));
        }

        List<ExpressionSyntax?> returned = MediatRSyntax.ReturnedExpressions(method);
        if (returned.Any(e => e is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax } || !MediatRSyntax.IsTaskMember(e, model, "FromResult")))
        {
            return PatternResult.Manual("Handle returns a Task that is not Task.FromResult(...); Mediator needs a ValueTask<T>, convert it by hand (e.g. make the method async).");
        }

        MethodDeclarationSyntax rewritten = method.ReplaceNodes(
            returned.Select(e => (MemberAccessExpressionSyntax)((InvocationExpressionSyntax)e!).Expression),
            (original, _) => MediatRSyntax.OnValueTask(original));
        return PatternResult.Fixed(rewritten.WithReturnType(returnType.WithTriviaFrom(method.ReturnType)), "System.Threading.Tasks");
    }
}
