using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR;

public class MediatRFix : IFix
{
    // The version our proofs compile every rewrite against (Directory.Packages.props): latest stable, MIT.
    public const string MediatorVersion = "3.0.2";

    public string Id => "mediatr";

    // MediatR.NotificationPublishers: left behind next to using Mediator, TaskWhenAllPublisher is ambiguous (CS0104).
    // MediatR.Pipeline: the processor and exception handler interfaces; left behind it does not bind once the package is gone.
    public IReadOnlyList<string> RemovedNamespaces { get; } = ["MediatR", "MediatR.NotificationPublishers", "MediatR.Pipeline"];

    // Half a migration builds but fails at run time (behaviors rewritten while AddMediatR stays): any Manual finding leaves the solution as it is.
    public bool AllOrNothing => true;

    public IReadOnlyList<string> AddedNamespaces { get; } = ["Mediator"];

    // Same name and arity in Mediator 3.0.2 (checked in the real assemblies by MediatRFixTests).
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
        // Handle differs (ValueTask, MessageHandlerDelegate); PipelineBehaviorPattern rewrites it, the name stays.
        "MediatR.IPipelineBehavior`2",
        // Same name and arity; StreamPipelineBehaviorPattern rewrites next.
        "MediatR.IStreamPipelineBehavior`2",
        // Same Handle(request, cancellationToken) returning IAsyncEnumerable<TResponse>: the swap is the whole migration.
        "MediatR.IStreamRequest`1",
        "MediatR.IStreamRequestHandler`2",
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
            // In every project it generates its own Mediator class: a project referencing the host's would get CS0436 (TAAK-063).
            // So only the host (AddMediatR: its registration may still be Manual), and PrivateAssets keeps it from flowing on.
            new() { Name = "Mediator.SourceGenerator", Version = MediatorVersion, OnlyWithCallTo = ["AddMediator", "AddMediatR"], PrivateAssets = "all" },
        ],
    };

    public IReadOnlyList<IPattern> Patterns { get; } = [new RequestHandlerPattern(), new VoidRequestHandlerPattern(), new NotificationHandlerPattern(), new PipelineBehaviorPattern(), new StreamPipelineBehaviorPattern(), new RequestProcessorPattern(), new ExceptionHandlerPattern(), new AddMediatRPattern(), new SendAsTaskPattern(), new UnsupportedPattern()];
}
