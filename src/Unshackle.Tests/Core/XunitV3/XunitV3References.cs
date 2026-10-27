using Microsoft.CodeAnalysis;
using Unshackle.Tests.Helpers;

namespace Unshackle.Tests.Core.XunitV3;

// Real assemblies for xunit3 proofs. "Before": xUnit 2.9.3 (xunit.core, execution, assert, abstractions), copied to XunitV2/ because
// this test project itself runs on xunit.v3 and cannot compile against both. "After": xunit.v3 3.2.2 (the default target, council 18 X2), copied to XunitV3/.
public static class XunitV3References
{
    private static readonly string[] s_v2 = ["xunit.core", "xunit.execution.dotnet", "xunit.assert", "xunit.abstractions"];

    // xunit.v3.core is netstandard2.0: its IAsyncLifetime names IAsyncDisposable through Microsoft.Bcl.AsyncInterfaces (a facade on net8+).
    private static readonly string[] s_v3 = ["xunit.v3.core", "xunit.v3.common", "xunit.v3.assert", "Microsoft.Bcl.AsyncInterfaces"];

    public static IReadOnlyList<MetadataReference> Before { get; } =
        [.. TestReferences.Net, .. s_v2.Select(name => MetadataReference.CreateFromFile(Path.Combine(AppContext.BaseDirectory, "XunitV2", name + ".dll")))];

    public static IReadOnlyList<MetadataReference> After { get; } =
        [.. TestReferences.Net, .. s_v3.Select(name => MetadataReference.CreateFromFile(Path.Combine(AppContext.BaseDirectory, name == "Microsoft.Bcl.AsyncInterfaces" ? "" : "XunitV3", name + ".dll")))];
}
