using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class NotificationHandlerPattern : IPattern
{
    public string Id => "mediatr.notification-handler";

    public string Description => "INotificationHandler<T>: Task Handle(...) → ValueTask Handle(...), Task.CompletedTask → ValueTask.CompletedTask";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        MediatRSyntax.FindHandleMethods(root, model, "INotificationHandler`1");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var method = (MethodDeclarationSyntax)node;
        if (method.ExplicitInterfaceSpecifier is not null)
        {
            return PatternResult.Manual("Handle is an explicit interface implementation; rename the interface and return type by hand.");
        }

        TypeSyntax? returnType = method.ReturnType is IdentifierNameSyntax or QualifiedNameSyntax { Right: IdentifierNameSyntax }
            ? MediatRSyntax.ToValueTask(method.ReturnType)
            : null;
        if (returnType is null)
        {
            return PatternResult.Manual("Handle does not return Task in the form Task; change it to ValueTask by hand.");
        }

        returnType = returnType.WithTriviaFrom(method.ReturnType);
        if (method.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            // await and a bare return work the same in an async ValueTask method.
            return PatternResult.Fixed(method.WithReturnType(returnType));
        }

        List<ExpressionSyntax?> returned = MediatRSyntax.ReturnedExpressions(method);
        if (returned.Any(e => e is not MemberAccessExpressionSyntax || !MediatRSyntax.IsTaskMember(e, model, "CompletedTask")))
        {
            return PatternResult.Manual("Handle returns a Task that is not Task.CompletedTask; Mediator needs a ValueTask, convert it by hand (e.g. make the method async).");
        }

        MethodDeclarationSyntax rewritten = method.ReplaceNodes(
            returned.OfType<MemberAccessExpressionSyntax>(),
            (original, _) => MediatRSyntax.OnValueTask(original));
        return PatternResult.Fixed(rewritten.WithReturnType(returnType), "System.Threading.Tasks");
    }
}
