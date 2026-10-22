using Microsoft.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly;

// Microsoft.Extensions.Resilience 10.11.0 ships net462, netstandard2.0, net8.0+; the Microsoft.Extensions 10 packages flag
// netcoreapp2.x/3.x and net5–7 as unsupported (a build warning, an error under TreatWarningsAsErrors). The project's target
// framework is read from the preprocessor symbols MSBuild defines; none (an ad-hoc compilation) counts as supported.
// Council round 10, P9: Manual TargetFrameworkUnsupported, a project blocker (G4).
internal static class PollyTargetFramework
{
    public static string? Unsupported(SyntaxNode node) => Unsupported(node.SyntaxTree.Options.PreprocessorSymbolNames);

    public static string? Unsupported(IEnumerable<string> preprocessorSymbols)
    {
        HashSet<string> symbols = [.. preprocessorSymbols];
        if (symbols.Contains("NETCOREAPP") && !symbols.Contains("NET8_0_OR_GREATER"))
        {
            return "a .NET (Core) target before net8.0";
        }

        return symbols.Contains("NETFRAMEWORK") && !symbols.Contains("NET462_OR_GREATER") ? "a .NET Framework target before net462" : null;
    }

    public static string Reason(string framework) => $"Microsoft.Extensions.Resilience {PollyFix.ResilienceVersion} does not support {framework}.";

    // The project-level row: one per project that references the v7 API and targets an unsupported framework.
    public static IReadOnlyList<ManualResult> Blockers(Project project) =>
        PollyFix.ReferencesPollyV7(project) && project.ParseOptions is { } options && Unsupported(options.PreprocessorSymbolNames) is string framework
            ? [(ManualResult)PatternResult.Manual(ProjectBlockingReason.TargetFrameworkUnsupported, Reason(framework))]
            : [];
}
