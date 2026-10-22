using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// TAAK-310 detector: request/response, a use of IRequestClient<T> (field, parameter, local) and every RespondAsync call.
public class RequestClientPattern : DetectorPattern
{
    private static readonly HashSet<string> s_types = new(["IRequestClient"], StringComparer.Ordinal);
    private static readonly HashSet<string> s_calls = new(["RespondAsync"], StringComparer.Ordinal);

    public override string Id => "masstransit.request-client";

    public override string Description => "IRequestClient<T> / context.RespondAsync(...) → Manual (report only)";

    protected override MassTransitCode Code => MassTransitCode.RequestResponse;

    protected override string Reason =>
        "MassTransit request/response (IRequestClient<T>.GetResponse, RespondAsync): Wolverine IMessageBus.InvokeAsync<TResponse> with a handler that returns the response; " +
        "Rebus needs Rebus.Async; several response types (GetResponse<A, B>) have no equivalent.";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<GenericNameSyntax>()
            .Where(g => g.Parent is not QualifiedNameSyntax { Left: var left } || left != g)
            .Where(g => MassTransitSyntax.NamesType(g, model, root, s_types))
            .Select(g => g.Parent is QualifiedNameSyntax qualified && qualified.Right == g ? (SyntaxNode)qualified : g)
            .Concat(MassTransitSyntax.FindCalls(root, model, s_calls));
}
