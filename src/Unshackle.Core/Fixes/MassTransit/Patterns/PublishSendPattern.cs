using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// TAAK-310 detector, council round 11 T6: x.Publish(...) / x.Send(...) is an Info row, only when the call resolves to a MassTransit method
// and x is (or implements) IPublishEndpoint, ISendEndpoint, ISendEndpointProvider, ConsumeContext or IBus. A name-only match is never reported.
public class PublishSendPattern : IPattern
{
    private static readonly HashSet<string> s_receivers = new(["IPublishEndpoint", "ISendEndpoint", "ISendEndpointProvider", "ConsumeContext", "IBus"], StringComparer.Ordinal);

    public string Id => "masstransit.publish-send";

    public string Description => "IPublishEndpoint/ISendEndpoint/ConsumeContext/IBus .Publish/.Send → Info (report only)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => Receiver(i, model) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is not InvocationExpressionSyntax invocation || Receiver(invocation, model) is not { } receiver)
        {
            return PatternResult.Note("MassTransit Publish/Send (no longer resolved).");
        }

        string verb = ((MemberAccessExpressionSyntax)invocation.Expression).Name.Identifier.ValueText;
        return PatternResult.Note(
            $"MassTransit {verb} on {receiver.Name}: Wolverine IMessageBus.{verb}Async, Rebus IBus.{verb}; send addresses, EndpointConvention and "
            + "publish topology move by hand (council round 11, T6)." + MassTransitSyntax.Guidance);
    }

    // The listed MassTransit type the receiver of a resolved MassTransit Publish/Send is or implements; else null.
    private static INamedTypeSymbol? Receiver(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Publish" or "Send", Expression: var target }
            || model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method || !MassTransitSyntax.IsMassTransit(method)
            || model.GetTypeInfo(target).Type is not INamedTypeSymbol type)
        {
            return null;
        }

        return new[] { type }.Concat(type.AllInterfaces)
            .FirstOrDefault(t => s_receivers.Contains(t.Name) && MassTransitSyntax.IsMassTransit(t));
    }
}
