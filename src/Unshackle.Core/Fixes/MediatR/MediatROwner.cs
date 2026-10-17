using Microsoft.CodeAnalysis.Text;

namespace Unshackle.Core.Fixes.MediatR;

// One unit of code that moves to Mediator or stays on MediatR as a whole (TAAK-274): a type declaration (base list, constraints),
// a member (signature), a top-level statement in a body, or a stand-alone typeof(Open<>).
internal class MediatROwner
{
    public required string Key { get; init; }

    public required string Path { get; init; }

    public required TextSpan Span { get; init; }

    // 1-based, like Finding.Line.
    public required int StartLine { get; init; }

    public required int EndLine { get; init; }

    // Names a MediatR type or member itself (not only a symbol that does).
    public bool NamesMediatR { get; set; }

    // A handler (or behavior, processor) class: Mediator's generator must see it from the project that calls AddMediator.
    public bool IsHandler { get; set; }

    // Calls services.AddMediatR(...).
    public bool CallsAddMediatR { get; set; }

    // Names MediatR's own Mediator class: the hand-written MediatR registration.
    public bool NamesMediatorClass { get; set; }

    // Part of MediatR's runtime: the Mediator class, or IMediator/ISender/IPublisher as a type argument or typeof in a statement
    // (services.AddTransient<ISender>(sp => sp.GetRequiredService<IMediator>())). It stays while anything stays.
    public bool NamesRuntime { get; set; }
}
