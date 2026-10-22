using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.Polly;

// Microsoft.Extensions.Resilience 10.11.0 ships net462, netstandard2.0, net8.0+; the Microsoft.Extensions 10 packages flag
// netcoreapp2.x/3.x and net5–7 as unsupported (a build warning, an error under TreatWarningsAsErrors). The project's target
// framework is read from the preprocessor symbols MSBuild defines; none (an ad-hoc compilation) counts as supported.
// Main session, 2026-10-22: an unsupported TFM is Manual (TargetFrameworkUnsupported) until council round 10.
internal static class PollyTargetFramework
{
    public static string? Unsupported(SyntaxNode node)
    {
        HashSet<string> symbols = [.. node.SyntaxTree.Options.PreprocessorSymbolNames];
        if (symbols.Contains("NETCOREAPP") && !symbols.Contains("NET8_0_OR_GREATER"))
        {
            return "a .NET (Core) target before net8.0";
        }

        return symbols.Contains("NETFRAMEWORK") && !symbols.Contains("NET462_OR_GREATER") ? "a .NET Framework target before net462" : null;
    }
}
