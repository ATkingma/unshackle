using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// TAAK-310 detector: class C : IConsumer<T>. Council round 11, T7: a consumer whose Consume uses nothing but context.Message and the
// cancellation token, with no definition and no filter in the solution, is an Info row (easy to move by hand); every other consumer is
// Manual with the code of the first feature found (ConsumerHeaders, ConsumerRespond, ...). Never Fixed.
public class ConsumerPattern : DetectorPattern, ISolutionPattern
{
    private static readonly HashSet<string> s_names = new(["IConsumer"], StringComparer.Ordinal);

    public override string Id => "masstransit.consumer";

    public override string Description => "class C : IConsumer<T> → Info when it uses only context.Message and cancellation, else Manual per feature (report only)";

    protected override MassTransitCode Code => MassTransitCode.ConsumerContextFeature;

    protected override string Reason => "MassTransit consumer (IConsumer<T>.Consume(ConsumeContext<T>)): Rebus IHandleMessages<T>.Handle(T) with IMessageContext, Wolverine a Handle(T) convention method; context members (Headers, RespondAsync, Redeliver) need a by-hand mapping.";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => MassTransitSyntax.FindClasses(root, model, s_names);

    // Without the solution a filter or definition elsewhere cannot be ruled out.
    public override PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        Blocker(node, model, null) is { } blocker
            ? Manual(blocker)
            : PatternResult.Manual(MassTransitCode.ConsumerNotAnalyzable, Reason + " Whether a filter or consumer definition applies needs the whole solution; run the fix on the solution." + MassTransitSyntax.Guidance);

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        ConsumerFacts facts = await ConsumerFacts.ForAsync(solution, cancellationToken);
        if (Blocker(node, model, facts) is { } blocker)
        {
            return Manual(blocker);
        }

        string messages = string.Join(", ", Messages(node, model).Select(m => m.Name));
        return PatternResult.Note(
            $"Simple MassTransit consumer of {messages}: it uses only context.Message and the cancellation token, and no filter or consumer definition applies, "
            + $"so its Consume body moves as it is to a Rebus IHandleMessages<{messages}>.Handle or a Wolverine Handle({messages}) method; its registration still moves by hand (council round 11, T7)."
            + MassTransitSyntax.Guidance);
    }

    private PatternResult Manual((MassTransitCode Code, string What) blocker) =>
        PatternResult.Manual(blocker.Code, $"{Reason} This consumer {blocker.What}.{MassTransitSyntax.Guidance}");

    // The message types of the MassTransit IConsumer<T> interfaces the class implements.
    private static List<ITypeSymbol> Messages(SyntaxNode node, SemanticModel model) =>
        model.GetDeclaredSymbol(node) is INamedTypeSymbol type
            ? [.. type.AllInterfaces.Where(IsConsumer).Select(i => i.TypeArguments[0])]
            : [];

    private static bool IsConsumer(INamedTypeSymbol type) => type is { Name: "IConsumer", Arity: 1 } && MassTransitSyntax.IsMassTransit(type);

    // The first feature that keeps the consumer Manual (code and what it does, with file:line), or null when it uses only context.Message
    // and cancellation. facts is null without the solution: then filters and definitions are not checked here.
    private static (MassTransitCode Code, string What)? Blocker(SyntaxNode node, SemanticModel model, ConsumerFacts? facts)
    {
        if (node is not ClassDeclarationSyntax declaration || model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol consumer)
        {
            return (MassTransitCode.ConsumerNotAnalyzable, "cannot be read (no class symbol)");
        }

        List<INamedTypeSymbol> interfaces = [.. consumer.AllInterfaces.Where(IsConsumer)];
        if (interfaces.Count == 0)
        {
            return (MassTransitCode.ConsumerNotAnalyzable, "cannot be read (its IConsumer<T> does not resolve)");
        }

        if (interfaces.Select(i => i.TypeArguments[0]).FirstOrDefault(t => t is INamedTypeSymbol { Name: "Batch" or "Fault", Arity: 1 } special && MassTransitSyntax.IsMassTransit(special)) is { } message)
        {
            return (MassTransitCode.ConsumerSpecialMessage, $"consumes {message.Name}<T>, a message shape of MassTransit's own");
        }

        if (facts?.Definitions.GetValueOrDefault(ConsumerFacts.Key(consumer)) is { } definition)
        {
            return (MassTransitCode.ConsumerWithDefinition, $"has a consumer definition ({definition})");
        }

        if (facts?.Filter is { } filter)
        {
            return (MassTransitCode.ConsumerFilter, $"may run inside a filter the solution registers or declares ({filter})");
        }

        HashSet<SyntaxNode> allowed = [];
        foreach (INamedTypeSymbol contract in interfaces)
        {
            if (contract.GetMembers("Consume").FirstOrDefault() is not IMethodSymbol member
                || consumer.FindImplementationForInterfaceMember(member) is not IMethodSymbol { Parameters: [{ } context] } consume
                || consume.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not MethodDeclarationSyntax method
                || method.SyntaxTree != declaration.SyntaxTree)
            {
                return (MassTransitCode.ConsumerNotAnalyzable, "cannot be read (its Consume method is not in this file)");
            }

            if (ContextUse(method, model, context, allowed) is { } use)
            {
                return use;
            }

            allowed.UnionWith(method.ParameterList.DescendantNodes());
        }

        // Any other MassTransit member in the class: an injected IPublishEndpoint, IBus, a request client, a context extension.
        return declaration.DescendantNodes().OfType<SimpleNameSyntax>()
            .Where(n => !allowed.Contains(n) && declaration.BaseList?.Span.Contains(n.Span) != true)
            .Select(n => (Name: n, Symbol: model.GetSymbolInfo(n).Symbol))
            .Where(n => n.Symbol is not null && MassTransitSyntax.IsMassTransit(n.Symbol))
            .Select(n => ((MassTransitCode Code, string What)?)Feature(n.Name.Identifier.ValueText, n.Name))
            .FirstOrDefault();
    }

    // The first use of the context parameter other than context.Message and context.CancellationToken; adds the allowed names to allowed.
    private static (MassTransitCode Code, string What)? ContextUse(MethodDeclarationSyntax method, SemanticModel model, IParameterSymbol context, HashSet<SyntaxNode> allowed)
    {
        foreach (IdentifierNameSyntax name in method.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(n => n.Identifier.ValueText == context.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, context)))
        {
            if (name.Parent is MemberAccessExpressionSyntax { Name: var member } access && access.Expression == name)
            {
                if (member.Identifier.ValueText is "Message" or "CancellationToken")
                {
                    allowed.Add(member);
                    continue;
                }

                return Feature(member.Identifier.ValueText, member);
            }

            return (MassTransitCode.ConsumerContextFeature, $"hands its ConsumeContext on ({Where(name)})");
        }

        return null;
    }

    private static (MassTransitCode Code, string What) Feature(string member, SyntaxNode node) => member switch
    {
        "Headers" or "GetHeader" or "TryGetHeader" => (MassTransitCode.ConsumerHeaders, $"reads message headers ({member}, {Where(node)})"),
        "Respond" or "RespondAsync" => (MassTransitCode.ConsumerRespond, $"responds to a request ({member}, {Where(node)})"),
        "Redeliver" or "Defer" => (MassTransitCode.ConsumerRedeliver, $"redelivers or defers the message ({member}, {Where(node)})"),
        "Publish" or "Send" or "GetSendEndpoint" or "Forward" or "SchedulePublish" or "ScheduleSend" or "IPublishEndpoint" or "ISendEndpoint" or "ISendEndpointProvider" or "IBus"
            => (MassTransitCode.ConsumerPublish, $"publishes or sends ({member}, {Where(node)})"),
        _ => (MassTransitCode.ConsumerContextFeature, $"uses MassTransit {member} ({Where(node)})"),
    };

    private static string Where(SyntaxNode node)
    {
        FileLinePositionSpan span = node.GetLocation().GetLineSpan();
        return $"{Path.GetFileName(span.Path)}:{span.StartLinePosition.Line + 1}";
    }
}
