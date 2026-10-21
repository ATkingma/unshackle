using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR;

public class MediatRFix : IFix, IPartialFix
{
    // The version our proofs compile every rewrite against (Directory.Packages.props): latest stable, MIT.
    public const string MediatorVersion = "3.0.2";

    public string Id => "mediatr";

    // MediatR.NotificationPublishers: left behind next to using Mediator, TaskWhenAllPublisher is ambiguous (CS0104).
    // MediatR.Pipeline: the processor and exception handler interfaces; left behind it does not bind once the package is gone.
    public IReadOnlyList<string> RemovedNamespaces { get; } = ["MediatR", "MediatR.NotificationPublishers", "MediatR.Pipeline"];

    // Half a migration builds but fails at run time (behaviors rewritten while AddMediatR stays): any Manual finding leaves the solution as it is.
    public bool AllOrNothing => true;

    public bool FixesFilesInParallel => true;

    // TAAK-274: instead of nothing, the code tied to the Manual findings stays on MediatR and the rest moves (MediatROwners).
    public Task<PartialPlan?> PlanPartialAsync(Solution solution, IReadOnlyList<Finding> roots, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(roots);
        return MediatROwners.PlanAsync(solution, roots, VendoredPackages, cancellationToken);
    }

    // Ardalis.SharedKernel builds on MediatR and has no Mediator version with the same API: its source goes in instead (TAAK-194).
    public IReadOnlyList<VendoredPackage> VendoredPackages { get; } = [ArdalisSharedKernel.Package];

    public IReadOnlyList<string> AddedNamespaces { get; } = ["Mediator"];

    // Same name and arity in Mediator 3.0.2 (checked in the real assemblies by MediatRFixTests).
    public IReadOnlyList<string> SwappedTypes => SwappedTypeNames;

    // Static so QualifiedSwappedTypePattern can rewrite MediatR.IRequest<T> written out in full (TAAK-288).
    public static IReadOnlyList<string> SwappedTypeNames { get; } =
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

    // Public types of namespace Mediator in 3.0.2 (checked by SwappedTypesTests): an own ICommand next to the new using → full name (TAAK-144).
    public IReadOnlyList<string> AddedTypes { get; } =
    [
        "Mediator.ExceptionHandlingResult`1", "Mediator.ForeachAwaitPublisher", "Mediator.IBaseCommand", "Mediator.IBaseQuery", "Mediator.IBaseRequest",
        "Mediator.IBaseStreamCommand", "Mediator.IBaseStreamQuery", "Mediator.IBaseStreamRequest", "Mediator.ICommand", "Mediator.ICommandHandler`1",
        "Mediator.ICommandHandler`2", "Mediator.ICommand`1", "Mediator.IMediator", "Mediator.IMessage", "Mediator.INotification",
        "Mediator.INotificationHandler`1", "Mediator.INotificationPublisher", "Mediator.IPipelineBehavior`2", "Mediator.IPublisher", "Mediator.IQueryHandler`2",
        "Mediator.IQuery`1", "Mediator.IRequest", "Mediator.IRequestHandler`1", "Mediator.IRequestHandler`2", "Mediator.IRequest`1",
        "Mediator.ISender", "Mediator.IStreamCommandHandler`2", "Mediator.IStreamCommand`1", "Mediator.IStreamMessage", "Mediator.IStreamPipelineBehavior`2",
        "Mediator.IStreamQueryHandler`2", "Mediator.IStreamQuery`1", "Mediator.IStreamRequestHandler`2", "Mediator.IStreamRequest`1", "Mediator.InvalidMessageException",
        "Mediator.MessageExceptionHandler`2", "Mediator.MessageExceptionHandler`3", "Mediator.MessageHandlerDelegate`2", "Mediator.MessagePostProcessor`2", "Mediator.MessagePreProcessor`2",
        "Mediator.MissingMessageHandlerException", "Mediator.NotificationHandlers`1", "Mediator.StreamHandlerDelegate`2", "Mediator.TaskWhenAllPublisher", "Mediator.Unit",
        // Written by the source generator.
        "Mediator.Mediator", "Mediator.MediatorOptions", "Mediator.AssemblyReference",
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
            // MSG0005 (message without handler; MediatR allows it): an error under TreatWarningsAsErrors breaks a migration that behaves the same (TAAK-136).
            // MSG0003 (generic handler skipped): an open generic decorator stays a container decorator, Mediator resolves handlers through DI (TAAK-193).
            // MSG0001 (multiple handlers): Mediator 3.0.2 reports it falsely for a void handler in a referenced assembly that implements
            // IRequestHandler<T> through an own interface (ICommandHandler<T> : IRequestHandler<T>); it still registers the handler (TAAK-212).
            new()
            {
                Name = "Mediator.SourceGenerator", Version = MediatorVersion, OnlyWithCallTo = ["AddMediatR", ProjectPackages.TypeofAssembly], PrivateAssets = "all",
                WarningsNotAsErrors = ["MSG0001", "MSG0003", "MSG0005"],
            },
        ],
    };

    // SendAsTask first: it must see a direct handler.Handle(...) while the handler in the same file still returns Task.
    public IReadOnlyList<IPattern> Patterns { get; } = [new SendAsTaskPattern(), new RequestHandlerPattern(), new VoidRequestHandlerPattern(), new NotificationHandlerPattern(), new RequestHandlerDelegatePattern(), new PipelineBehaviorPattern(), new StreamPipelineBehaviorPattern(), new RequestProcessorCallerPattern(), new RequestProcessorPattern(), new ExceptionHandlerPattern(), new AddMediatRPattern(), new BehaviorDiRegistrationPattern(), new AutofacMediatorModulePattern(), new GenericDecoratorPattern(), new GenericRequestPattern(), new GenericNotificationPattern(), new MediatorImplementationPattern(), new UnsupportedPattern(), new InternalHandlerAccessPattern(), new QualifiedSwappedTypePattern()];
}
