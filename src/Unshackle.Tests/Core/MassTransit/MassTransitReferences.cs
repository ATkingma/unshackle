using Microsoft.CodeAnalysis;
using Unshackle.Tests.Helpers;

namespace Unshackle.Tests.Core.MassTransit;

// MassTransit 8.5.12 (Apache-2.0, council round 8 T5) for the detector proofs. Transport packages (RabbitMQ, Azure Service Bus) are not
// referenced: their Using... calls do not resolve and the detector matches them by name in a file that imports MassTransit.
public static class MassTransitReferences
{
    public static IReadOnlyList<MetadataReference> MassTransit { get; } =
    [
        .. TestReferences.Net,
        MetadataReference.CreateFromFile(typeof(global::MassTransit.IConsumer<>).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::MassTransit.MassTransitStateMachine<>).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location),
    ];
}
