using Microsoft.CodeAnalysis;
using Unshackle.Tests.Helpers;

namespace Unshackle.Tests.Core.Polly;

// Real assemblies for Polly proofs, from the test output folder. "Before": Polly 7.2.4 with Polly.Extensions.Http and
// Microsoft.Extensions.Http.Polly (v7 API); "after": Polly.Core 8 with Microsoft.Extensions.(Http.)Resilience. Both get the
// shared Microsoft.Extensions assemblies (DI, HttpClientFactory, options, logging) that an HttpClient proof needs.
public static class PollyReferences
{
    private static readonly string[] s_v7 = ["Polly", "Polly.Extensions.Http", "Microsoft.Extensions.Http.Polly"];

    private static readonly string[] s_v8 = ["Polly.Core", "Polly.Extensions", "Polly.RateLimiting", "Microsoft.Extensions.Resilience", "Microsoft.Extensions.Http.Resilience"];

    public static IReadOnlyList<MetadataReference> Before { get; } = [.. TestReferences.Net, .. Shared(), .. Files(s_v7)];

    public static IReadOnlyList<MetadataReference> After { get; } = [.. TestReferences.Net, .. Shared(), .. Files(s_v8)];

    private static IEnumerable<MetadataReference> Shared() =>
        Directory.EnumerateFiles(AppContext.BaseDirectory, "Microsoft.Extensions.*.dll")
            .Where(path => !s_v7.Contains(Path.GetFileNameWithoutExtension(path)) && !s_v8.Contains(Path.GetFileNameWithoutExtension(path)))
            .Order(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path));

    private static IEnumerable<MetadataReference> Files(IEnumerable<string> names) =>
        names.Select(name => MetadataReference.CreateFromFile(Path.Combine(AppContext.BaseDirectory, name + ".dll")));
}
