using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class StreamPipelineBehaviorPattern : IPattern
{
    private const string InterfaceName = "IStreamPipelineBehavior`2";

    public string Id => "mediatr.stream-pipeline-behavior";

    public string Description =>
        "IStreamPipelineBehavior<TReq, TRes>: StreamHandlerDelegate<TRes> next → StreamHandlerDelegate<TReq, TRes> next, next() → next(request, ct), TReq : IStreamMessage";

    // The type, not the method: an open behavior may also need the IStreamMessage constraint.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        MediatRSyntax.FindHandleMethods(root, model, InterfaceName).Select(m => m.Parent!).Distinct();

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var type = (TypeDeclarationSyntax)node;
        MethodDeclarationSyntax method = MediatRSyntax.FindHandleMethods(type, model, InterfaceName).First(m => m.Parent == type);
        if (MediatRSyntax.RejectExplicit(method, "rewrite the stream behavior by hand.") is { } explicitHandle)
        {
            return explicitHandle;
        }

        if (method.ParameterList.Parameters is not [{ Type: { } requestType } request, { Type: GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 } nextType } next, { } token])
        {
            return PatternResult.Manual("Handle does not have the form Handle(request, StreamHandlerDelegate<TResponse> next, cancellationToken); rewrite it by hand.");
        }

        IParameterSymbol? nextSymbol = model.GetDeclaredSymbol(next);
        List<IdentifierNameSyntax> nextUses =
        [
            .. method.DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(n => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, nextSymbol)),
        ];
        List<InvocationExpressionSyntax> calls =
            [.. nextUses.Select(n => n.Parent).OfType<InvocationExpressionSyntax>().Where(c => nextUses.Contains(c.Expression) && c.ArgumentList.Arguments.Count == 0)];
        if (calls.Count != nextUses.Count)
        {
            return PatternResult.Manual("next is used other than as next(); pass it the message and token by hand.");
        }

        GenericNameSyntax streamDelegate = nextType
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(MediatRSyntax.CommaList([requestType.WithoutTrivia(), nextType.TypeArgumentList.Arguments[0]])))
            .WithTriviaFrom(nextType);
        MethodDeclarationSyntax rewritten = method.ReplaceNodes(
            calls.Cast<SyntaxNode>().Append(nextType),
            (original, current) => current switch
            {
                InvocationExpressionSyntax call => call.WithArgumentList(SyntaxFactory.ArgumentList(MediatRSyntax.CommaList(
                [
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(request.Identifier.ValueText)),
                    SyntaxFactory.Argument(SyntaxFactory.IdentifierName(token.Identifier.ValueText)),
                ])).WithTriviaFrom(call.ArgumentList)),
                _ => streamDelegate,
            });

        // IStreamRequest<T> is an IStreamMessage after the swap.
        return PatternResult.Fixed(MediatRSyntax.WithConstraint(
            type.ReplaceNode(method, rewritten), model.GetTypeInfo(requestType).Type, "IStreamMessage", t => MediatRSyntax.IsMediatR(t, "IStreamRequest")));
    }
}
