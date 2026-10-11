using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class PipelineBehaviorPattern : IPattern
{
    private const string InterfaceName = "IPipelineBehavior`2";

    public string Id => "mediatr.pipeline-behavior";

    public string Description =>
        "IPipelineBehavior<TReq, TRes>: Task<TRes> Handle(request, RequestHandlerDelegate<TRes> next, ct) → ValueTask<TRes> Handle(request, MessageHandlerDelegate<TReq, TRes> next, ct), next() → next(request, ct), TReq : IMessage";

    // The type, not the method: an open behavior also needs the IMessage constraint on its type parameter.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        MediatRSyntax.FindHandleMethods(root, model, InterfaceName).Select(m => m.Parent!).Distinct();

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var type = (TypeDeclarationSyntax)node;
        MethodDeclarationSyntax method = MediatRSyntax.FindHandleMethods(type, model, InterfaceName).First(m => m.Parent == type);
        if (MediatRSyntax.RejectExplicit(method, "rewrite the pipeline behavior by hand.") is { } explicitHandle)
        {
            return explicitHandle;
        }

        TypeSyntax? returnType = method.ReturnType is GenericNameSyntax or QualifiedNameSyntax { Right: GenericNameSyntax }
            ? MediatRSyntax.ToValueTask(method.ReturnType)
            : null;
        if (returnType is null || method.ParameterList.Parameters.Count != 3
            || method.ParameterList.Parameters[1].Type is not GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } nextType)
        {
            return PatternResult.Manual("Handle does not have the form Task<TResponse> Handle(request, RequestHandlerDelegate<TResponse> next, cancellationToken); rewrite it by hand.");
        }

        ParameterSyntax request = method.ParameterList.Parameters[0];
        ParameterSyntax next = method.ParameterList.Parameters[1];
        ParameterSyntax token = method.ParameterList.Parameters[2];
        IParameterSymbol? nextSymbol = model.GetDeclaredSymbol(next);
        List<IdentifierNameSyntax> nextUses =
        [
            .. method.DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(n => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, nextSymbol)),
        ];
        // Mediator's next takes the message and the token; next() and next(token) both become next(request, token).
        List<InvocationExpressionSyntax> calls = [.. nextUses.Select(n => n.Parent).OfType<InvocationExpressionSyntax>().Where(c => nextUses.Contains(c.Expression))];
        if (calls.Count != nextUses.Count || calls.Any(c => c.ArgumentList.Arguments.Count > 1))
        {
            return PatternResult.Manual("next is used other than as next() or next(token); pass it the message and token by hand.");
        }

        bool isAsync = method.Modifiers.Any(SyntaxKind.AsyncKeyword);
        List<ExpressionSyntax?> returned = MediatRSyntax.ReturnedExpressions(method);
        if (!isAsync && returned.Any(e => !calls.Contains(e) && !IsTaskFromResult(e, model)))
        {
            return PatternResult.Manual("Handle returns a Task that is not next() or Task.FromResult(...); Mediator needs a ValueTask<TResponse>, convert it by hand (e.g. make the method async).");
        }

        GenericNameSyntax messageDelegate = SyntaxFactory.GenericName(SyntaxFactory.Identifier("MessageHandlerDelegate"))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(MediatRSyntax.CommaList([request.Type!.WithoutTrivia(), nextType.TypeArgumentList.Arguments[0]])))
            .WithTriviaFrom(nextType);
        MethodDeclarationSyntax rewritten = method.ReplaceNodes(
            calls.Cast<SyntaxNode>()
                .Append(nextType)
                .Concat(isAsync ? [] : returned.Where(e => IsTaskFromResult(e, model)).Select(e => ((InvocationExpressionSyntax)e!).Expression)),
            (original, current) => current switch
            {
                InvocationExpressionSyntax call => call.WithArgumentList(SyntaxFactory.ArgumentList(MediatRSyntax.CommaList(
                [
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(request.Identifier.ValueText)),
                    call.ArgumentList.Arguments.FirstOrDefault() ?? SyntaxFactory.Argument(SyntaxFactory.IdentifierName(token.Identifier.ValueText)),
                ])).WithTriviaFrom(call.ArgumentList)),
                MemberAccessExpressionSyntax member => MediatRSyntax.OnValueTask(member),
                GenericNameSyntax => messageDelegate,
                _ => current,
            });
        rewritten = rewritten.WithReturnType(returnType.WithTriviaFrom(method.ReturnType));

        TypeDeclarationSyntax result = type.ReplaceNode(method, rewritten);
        return PatternResult.Fixed(WithMessageConstraint(result, request.Type!, model), "System.Threading.Tasks");
    }

    private static bool IsTaskFromResult(ExpressionSyntax? expression, SemanticModel model) =>
        expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax } invocation
        && MediatRSyntax.IsTaskMember(invocation, model, "FromResult");

    // Mediator's IPipelineBehavior requires TMessage : IMessage; IRequest<T>, INotification and marker interfaces on them already are one after the swap.
    private static TypeDeclarationSyntax WithMessageConstraint(TypeDeclarationSyntax type, TypeSyntax requestType, SemanticModel model) =>
        MediatRSyntax.WithConstraint(type, model.GetTypeInfo(requestType).Type, "IMessage", t => MediatRSyntax.IsMediatR(t, "IBaseRequest", "INotification"));
}
