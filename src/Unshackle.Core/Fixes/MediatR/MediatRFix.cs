using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR;

public class MediatRFix : IFix
{
    // The version our proofs compile every rewrite against (Directory.Packages.props): latest stable, MIT.
    public const string MediatorVersion = "3.0.2";

    public string Id => "mediatr";

    public IReadOnlyList<string> RemovedNamespaces { get; } = ["MediatR"];

    public IReadOnlyList<string> AddedNamespaces { get; } = ["Mediator"];

    // Same name and arity in Mediator 3.0.2 (checked in the real assemblies by MediatRFixTests). Not IPipelineBehavior:
    // Mediator's has another delegate and an IMessage constraint, so it needs its own pattern.
    public IReadOnlyList<string> SwappedTypes { get; } =
    [
        "MediatR.IBaseRequest",
        "MediatR.IRequest",
        "MediatR.IRequest`1",
        "MediatR.IRequestHandler`1",
        "MediatR.IRequestHandler`2",
        "MediatR.INotification",
        "MediatR.INotificationHandler`1",
        "MediatR.ISender",
        "MediatR.IPublisher",
        "MediatR.IMediator",
        "MediatR.Unit",
    ];

    // The source generator writes AddMediator and the dispatcher; without it the abstractions do nothing.
    public PackageMigration Package { get; } = new()
    {
        RemovedPackage = "MediatR",
        // MediatR 12 keeps IRequest, INotification and Unit here; left behind, they clash with Mediator's after the swap.
        AlsoRemovedPackages = ["MediatR.Contracts"],
        AddedPackages =
        [
            new() { Name = "Mediator.Abstractions", Version = MediatorVersion },
            new() { Name = "Mediator.SourceGenerator", Version = MediatorVersion },
        ],
    };

    public IReadOnlyList<IPattern> Patterns { get; } = [new RequestHandlerPattern()];
}
