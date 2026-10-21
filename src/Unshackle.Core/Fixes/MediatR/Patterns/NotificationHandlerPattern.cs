using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class NotificationHandlerPattern : IPattern
{
    public string Id => "mediatr.notification-handler";

    public string Description => "INotificationHandler<T>: Task Handle(...) → ValueTask Handle(...), Task.CompletedTask → ValueTask.CompletedTask, another Task → new ValueTask(task)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        MediatRSyntax.FindHandleMethods(root, model, "INotificationHandler`1");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var method = (MethodDeclarationSyntax)node;
        if (MediatRSyntax.RejectExplicit(method, "rename the interface and return type by hand.") is { } explicitHandle)
        {
            return explicitHandle;
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

        IReadOnlyList<ExpressionSyntax?> returned = MediatRSyntax.ReturnedExpressions(method);
        if (returned.Any(e => e is null || !IsCompletedTask(e, model) && model.GetTypeInfo(e).Type?.ToDisplayString() != "System.Threading.Tasks.Task"))
        {
            return PatternResult.Manual("Handle returns something that is not a Task; Mediator needs a ValueTask, convert it by hand (e.g. make the method async).");
        }

        // Another Task (=> SendAsync(...)) is wrapped, not awaited: no state machine, and a synchronous throw stays synchronous (TAAK-154).
        MethodDeclarationSyntax rewritten = method.ReplaceNodes(
            returned.OfType<ExpressionSyntax>(),
            (original, _) => IsCompletedTask(original, model)
                ? MediatRSyntax.OnValueTask((MemberAccessExpressionSyntax)original)
                : SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.IdentifierName("ValueTask").WithLeadingTrivia(SyntaxFactory.Space),
                    SyntaxFactory.ArgumentList([SyntaxFactory.Argument(original.WithoutTrivia())]),
                    null).WithTriviaFrom(original));
        return PatternResult.Fixed(rewritten.WithReturnType(returnType), "System.Threading.Tasks");
    }

    private static bool IsCompletedTask(ExpressionSyntax expression, SemanticModel model) =>
        expression is MemberAccessExpressionSyntax && MediatRSyntax.IsTaskMember(expression, model, "CompletedTask");
}
