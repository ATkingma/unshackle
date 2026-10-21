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
        // An explicit IRequestHandler<,>.Handle keeps its specifier: Mediator's interface has the same name and a Handle(request, token) of the same shape.
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

        IReadOnlyList<ExpressionSyntax?> returned = MediatRSyntax.ReturnedExpressions(method);
        ITypeSymbol? taskType = model.GetDeclaredSymbol(method)?.ReturnType;
        if (returned.Any(e => e is null || !IsFromResult(e, model) && !SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(e).Type, taskType)))
        {
            return PatternResult.Manual("Handle returns something that is not a Task<T>; Mediator needs a ValueTask<T>, convert it by hand (e.g. make the method async).");
        }

        // Another Task<T> (=> repository.GetAsync(...)) is wrapped, not awaited: no state machine, a synchronous throw stays synchronous (TAAK-154).
        // A decorator's return inner.Handle(...) stays: after the swap the inner handler returns a ValueTask<T> as well (TAAK-212).
        TypeSyntax valueTask = returnType.WithoutTrivia().WithLeadingTrivia(SyntaxFactory.Space);
        MethodDeclarationSyntax rewritten = method.ReplaceNodes(
            returned.OfType<ExpressionSyntax>().Where(e => !IsRequestHandlerCall(e, model)),
            (original, _) => IsFromResult(original, model)
                ? ((InvocationExpressionSyntax)original).WithExpression(MediatRSyntax.OnValueTask((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)original).Expression))
                : SyntaxFactory.ObjectCreationExpression(valueTask, SyntaxFactory.ArgumentList([SyntaxFactory.Argument(original.WithoutTrivia())]), null)
                    .WithTriviaFrom(original));
        return PatternResult.Fixed(rewritten.WithReturnType(returnType.WithTriviaFrom(method.ReturnType)), "System.Threading.Tasks");
    }

    // Handle(...) of IRequestHandler<TReq, TRes> itself, of an own interface deriving from it or of a class implementing it.
    private static bool IsRequestHandlerCall(ExpressionSyntax expression, SemanticModel model) =>
        expression is InvocationExpressionSyntax
        && model.GetSymbolInfo(expression).Symbol is IMethodSymbol { Name: "Handle", ContainingType: { } type }
        && type.AllInterfaces.Prepend(type).Any(i => i.OriginalDefinition.MetadataName == "IRequestHandler`2" && MediatRSyntax.IsMediatR(i, "IRequestHandler"));

    private static bool IsFromResult(ExpressionSyntax expression, SemanticModel model) =>
        expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax } && MediatRSyntax.IsTaskMember(expression, model, "FromResult");
}
