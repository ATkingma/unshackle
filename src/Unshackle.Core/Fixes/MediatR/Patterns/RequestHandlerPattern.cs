using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class RequestHandlerPattern : IPattern
{
    public string Id => "mediatr.request-handler";

    public string Description => "IRequestHandler<TReq, TRes>: Task<TRes> Handle(...) → ValueTask<TRes> Handle(...), Task.FromResult → ValueTask.FromResult, another Task<TRes> → new ValueTask<TRes>(task)";

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
        ITypeSymbol? taskType = model.GetDeclaredSymbol(method)?.ReturnType;
        if (returned.Any(e => e is null || !IsFromResult(e, model) && !SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(e).Type, taskType)))
        {
            return PatternResult.Manual("Handle returns something that is not a Task<T>; Mediator needs a ValueTask<T>, convert it by hand (e.g. make the method async).");
        }

        // Another Task<T> (=> repository.GetAsync(...)) is wrapped, not awaited: no state machine, a synchronous throw stays synchronous (TAAK-154).
        TypeSyntax valueTask = returnType.WithoutTrivia().WithLeadingTrivia(SyntaxFactory.Space);
        MethodDeclarationSyntax rewritten = method.ReplaceNodes(
            returned.OfType<ExpressionSyntax>(),
            (original, _) => IsFromResult(original, model)
                ? ((InvocationExpressionSyntax)original).WithExpression(MediatRSyntax.OnValueTask((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)original).Expression))
                : SyntaxFactory.ObjectCreationExpression(valueTask, SyntaxFactory.ArgumentList([SyntaxFactory.Argument(original.WithoutTrivia())]), null)
                    .WithTriviaFrom(original));
        return PatternResult.Fixed(rewritten.WithReturnType(returnType.WithTriviaFrom(method.ReturnType)), "System.Threading.Tasks");
    }

    private static bool IsFromResult(ExpressionSyntax expression, SemanticModel model) =>
        expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax } && MediatRSyntax.IsTaskMember(expression, model, "FromResult");
}
