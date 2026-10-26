using Microsoft.CodeAnalysis;
using Unshackle.Tests.Helpers;

namespace Unshackle.Tests.Core.Moq;

// Moq 4.18.4 (the last version before SponsorLink) as "before", NSubstitute as "after".
public static class MoqReferences
{
    public static IReadOnlyList<MetadataReference> NSubstituteAssembly { get; } =
        [MetadataReference.CreateFromFile(typeof(global::NSubstitute.Substitute).Assembly.Location)];

    public static IReadOnlyList<MetadataReference> Moq { get; } =
    [
        .. TestReferences.Net,
        MetadataReference.CreateFromFile(typeof(global::Moq.Mock).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::Castle.DynamicProxy.ProxyGenerator).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::Microsoft.Extensions.Logging.ILogger).Assembly.Location),
    ];

    public static IReadOnlyList<MetadataReference> NSubstitute { get; } =
        [.. TestReferences.Net, .. NSubstituteAssembly, MetadataReference.CreateFromFile(typeof(global::Microsoft.Extensions.Logging.ILogger).Assembly.Location)];
}
