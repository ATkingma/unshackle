using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.XunitV3;

// The xUnit v2 assemblies (= the names in the packages' lib folders) and the project-level facts the fix needs from them.
internal static class XunitV3Assemblies
{
    public const string Core = "xunit.core";

    public const string Abstractions = "xunit.abstractions";

    // xunit.extensibility.execution, which the xunit and xunit.core packages bring in: only a project that runs tests has it.
    private static readonly HashSet<string> s_execution = new(["xunit.execution.dotnet", "xunit.execution.desktop"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> s_v3 = new(["xunit.v3.core", "xunit.v3.common", "xunit.v3.assert"], StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> V2 { get; } = [Core, "xunit.assert", Abstractions, "xunit.execution.dotnet", "xunit.execution.desktop"];

    private static readonly HashSet<string> s_v2 = new(V2, StringComparer.OrdinalIgnoreCase);

    // A project on v2: it references a v2 assembly and no v3 one (a project already on v3 is left alone).
    public static bool ReferencesV2(Project project)
    {
        List<string> names = Names(project);
        return names.Any(s_v2.Contains) && !names.Any(s_v3.Contains);
    }

    // A test project: the xunit (or xunit.core) package, which in v3 becomes xunit.v3 and needs OutputType Exe.
    public static bool IsTestProject(Project project) => ReferencesV2(project) && Names(project).Any(s_execution.Contains);

    public static bool IsFrom(ISymbol? symbol, string assembly) =>
        string.Equals(symbol?.ContainingAssembly?.Name, assembly, StringComparison.OrdinalIgnoreCase);

    private static List<string> Names(Project project) =>
        [.. project.MetadataReferences.OfType<PortableExecutableReference>()
            .Select(r => r.FilePath is string path ? Path.GetFileNameWithoutExtension(path) : null)
            .OfType<string>()];
}
