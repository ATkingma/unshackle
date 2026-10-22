using Microsoft.CodeAnalysis;
using Unshackle.Tests.Helpers;

namespace Unshackle.Tests.Core.Polly;

// Real assemblies for Polly proofs: "before" is Polly 7.2.4 (v7 API), "after" Polly.Core 8 as Microsoft.Extensions.Resilience brings it.
public static class PollyReferences
{
    public static MetadataReference PollyV7 { get; } = MetadataReference.CreateFromFile(typeof(global::Polly.Policy).Assembly.Location);

    public static MetadataReference PollyCore { get; } = MetadataReference.CreateFromFile(typeof(global::Polly.ResiliencePipeline).Assembly.Location);

    public static IReadOnlyList<MetadataReference> Before { get; } = [.. TestReferences.Net, PollyV7];

    public static IReadOnlyList<MetadataReference> After { get; } = [.. TestReferences.Net, PollyCore];
}
