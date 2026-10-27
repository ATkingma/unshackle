using Microsoft.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.XunitV3;

// xunit.v3 4.0.2 ships net472 and net8.0 only; a test project on an older target cannot reference it (NU1202). Read from the
// preprocessor symbols MSBuild defines; none (an ad-hoc compilation) counts as supported. Like Polly (council round 10, P9).
internal static class XunitV3TargetFramework
{
    public static string? Unsupported(IEnumerable<string> preprocessorSymbols)
    {
        HashSet<string> symbols = [.. preprocessorSymbols];
        if (symbols.Contains("NETCOREAPP") && !symbols.Contains("NET8_0_OR_GREATER"))
        {
            return "a .NET (Core) target before net8.0";
        }

        if (symbols.Contains("NETFRAMEWORK") && !symbols.Contains("NET472_OR_GREATER"))
        {
            return "a .NET Framework target before net472";
        }

        return symbols.Contains("NETSTANDARD") ? "a netstandard target (a v3 test project is an executable)" : null;
    }

    public static async Task<IReadOnlyList<ManualResult>> BlockersAsync(Project project, CancellationToken cancellationToken) =>
        await XunitV3Assemblies.IsTestProjectAsync(project, cancellationToken) && project.ParseOptions is { } options && Unsupported(options.PreprocessorSymbolNames) is string framework
            ? [(ManualResult)PatternResult.Manual(ProjectBlockingReason.TargetFrameworkUnsupported, $"xunit.v3 {XunitV3Fix.V3Version} does not support {framework}.")]
            : [];
}
