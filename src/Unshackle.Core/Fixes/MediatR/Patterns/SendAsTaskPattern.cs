using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class SendAsTaskPattern : IPattern
{
    public string Id => "mediatr.send-as-task";

    public string Description => "Send/Publish whose result is used as a Task (not awaited directly) → .AsTask(); Mediator returns ValueTask";

    // CreateStream returns IAsyncEnumerable in both libraries, so only Send and Publish change.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i =>
            i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Send" or "Publish" } or MemberBindingExpressionSyntax { Name.Identifier.ValueText: "Send" or "Publish" }
            && model.GetSymbolInfo(i).Symbol is IMethodSymbol { ContainingAssembly.Name: "MediatR" }
            && !IsAwaited(i));

    // A ValueTask may be awaited once and has no ContinueWith/Wait/WhenAll; .AsTask() keeps every Task use working.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var invocation = (InvocationExpressionSyntax)node;
        if (invocation.Expression is MemberBindingExpressionSyntax)
        {
            // mediator?.Send(x) is a ValueTask? after the swap; .AsTask() needs a null check the caller has to choose.
            return PatternResult.Manual("Send/Publish is called with ?. ; Mediator returns a ValueTask, so handle the null case and convert with .AsTask() by hand.");
        }

        return PatternResult.Fixed(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    invocation.WithoutTrivia(),
                    SyntaxFactory.IdentifierName("AsTask"))).WithTriviaFrom(invocation));
    }

    // await x and await x.ConfigureAwait(...) work the same on a ValueTask.
    private static bool IsAwaited(InvocationExpressionSyntax invocation) => invocation.Parent switch
    {
        AwaitExpressionSyntax => true,
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConfigureAwait", Parent: InvocationExpressionSyntax { Parent: AwaitExpressionSyntax } } => true,
        ParenthesizedExpressionSyntax parenthesized => parenthesized.Parent is AwaitExpressionSyntax,
        _ => false,
    };
}
