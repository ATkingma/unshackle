using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.Moq;

// NSubstitute 6.2.0 ships netstandard2.0 and net8.0 (council round 9, Q3): a target that cannot consume netstandard2.0 (.NET Framework
// before 4.6.1, .NET Core before 2.0, netstandard before 2.0) is TargetFrameworkUnsupported. The target is read from the
// preprocessor symbols MSBuild defines; none (an ad-hoc compilation) counts as supported.
public static class MoqTargetFramework
{
    public static string? Unsupported(IEnumerable<string> preprocessorSymbols)
    {
        HashSet<string> symbols = [.. preprocessorSymbols];
        if (symbols.Contains("NETFRAMEWORK") && !symbols.Contains("NET461_OR_GREATER"))
        {
            return "a .NET Framework target before net461";
        }

        if (symbols.Contains("NETCOREAPP") && !symbols.Contains("NETCOREAPP2_0_OR_GREATER"))
        {
            return "a .NET Core target before netcoreapp2.0";
        }

        return symbols.Contains("NETSTANDARD") && !symbols.Contains("NETSTANDARD2_0_OR_GREATER") ? "a netstandard target before netstandard2.0" : null;
    }

    public static string? Unsupported(SyntaxNode node) => Unsupported(node.SyntaxTree.Options.PreprocessorSymbolNames);

    public static string Reason(string framework) =>
        $"{framework} cannot use NSubstitute {MoqFix.NSubstituteVersion} (netstandard2.0 and net8.0); the project stays on Moq";
}
