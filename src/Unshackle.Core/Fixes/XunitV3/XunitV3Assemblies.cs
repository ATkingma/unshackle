using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.XunitV3;

// The xUnit v2 assemblies (= the names in the packages' lib folders) and the project-level facts the fix needs from them.
internal static class XunitV3Assemblies
{
    public const string Core = "xunit.core";

    public const string Abstractions = "xunit.abstractions";

    // xunit.extensibility.execution, which the xunit and xunit.core packages bring in: only a project that runs tests has it.
    private static readonly HashSet<string> s_execution = new(["xunit.execution.dotnet", "xunit.execution.desktop"], StringComparer.OrdinalIgnoreCase);

    // What Microsoft.NET.Test.Sdk brings as compile references: the test platform on .NET (Core), the code-coverage shim on .NET Framework.
    private static readonly HashSet<string> s_testSdk = new(
        ["Microsoft.TestPlatform.CoreUtilities", "Microsoft.VisualStudio.TestPlatform.ObjectModel", "Microsoft.VisualStudio.CodeCoverage.Shim"],
        StringComparer.OrdinalIgnoreCase);

    // Per project: whether a source file applies an xUnit test attribute (FactAttribute or a subclass, TheoryAttribute included).
    private static readonly ConditionalWeakTable<Project, StrongBox<bool>> s_testAttributes = new();

    private static readonly HashSet<string> s_v3 = new(["xunit.v3.core", "xunit.v3.common", "xunit.v3.assert"], StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> V2 { get; } = [Core, "xunit.assert", Abstractions, "xunit.execution.dotnet", "xunit.execution.desktop"];

    private static readonly HashSet<string> s_v2 = new(V2, StringComparer.OrdinalIgnoreCase);

    // A project on v2: it references a v2 assembly and no v3 one (a project already on v3 is left alone).
    public static bool ReferencesV2(Project project)
    {
        List<string> names = Names(project);
        return names.Any(s_v2.Contains) && !names.Any(s_v3.Contains);
    }

    // The xunit (or xunit.core) package: the runner side of v2, which in v3 is xunit.v3 (an executable) or, in a library, xunit.v3.extensibility.core.
    public static bool ReferencesExecution(Project project) => ReferencesV2(project) && Names(project).Any(s_execution.Contains);

    // A test project (council round 18, X4): the xunit package, Microsoft.NET.Test.Sdk AND test attributes. Only then OutputType Exe and xunit.v3.
    public static async Task<bool> IsTestProjectAsync(Project project, CancellationToken cancellationToken) =>
        ReferencesExecution(project) && ReferencesTestSdk(project) && await HasTestAttributesAsync(project, cancellationToken);

    public static bool ReferencesTestSdk(Project project) => Names(project).Any(s_testSdk.Contains);

    public static bool ReferencesAssert(Project project) => Names(project).Contains("xunit.assert", StringComparer.OrdinalIgnoreCase);

    // Cached per project; the compilation is only built for a file that names an attribute ending in Fact or Theory.
    public static async Task<bool> HasTestAttributesAsync(Project project, CancellationToken cancellationToken)
    {
        if (s_testAttributes.TryGetValue(project, out StrongBox<bool>? known))
        {
            return known.Value;
        }

        bool found = await FindTestAttributesAsync(project, cancellationToken);
        s_testAttributes.AddOrUpdate(project, new StrongBox<bool>(found));
        return found;
    }

    private static async Task<bool> FindTestAttributesAsync(Project project, CancellationToken cancellationToken)
    {
        Compilation? compilation = null;
        foreach (Document document in project.Documents)
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            List<AttributeSyntax> candidates = [.. root.DescendantNodes().OfType<AttributeSyntax>().Where(IsCandidate)];
            if (candidates.Count == 0)
            {
                continue;
            }

            compilation ??= (await project.GetCompilationAsync(cancellationToken))!;
            INamedTypeSymbol? fact = compilation.GetTypeByMetadataName("Xunit.FactAttribute");
            SemanticModel model = compilation.GetSemanticModel(root.SyntaxTree);
            if (fact is not null && candidates.Any(a => model.GetSymbolInfo(a).Symbol?.ContainingType is INamedTypeSymbol type && DerivesFrom(type, fact)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCandidate(AttributeSyntax attribute)
    {
        string name = attribute.Name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => string.Empty,
        };
        return name.EndsWith("Fact", StringComparison.Ordinal) || name.EndsWith("Theory", StringComparison.Ordinal)
            || name.EndsWith("FactAttribute", StringComparison.Ordinal) || name.EndsWith("TheoryAttribute", StringComparison.Ordinal);
    }

    private static bool DerivesFrom(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsFrom(ISymbol? symbol, string assembly) =>
        string.Equals(symbol?.ContainingAssembly?.Name, assembly, StringComparison.OrdinalIgnoreCase);

    private static List<string> Names(Project project) =>
        [.. project.MetadataReferences.OfType<PortableExecutableReference>()
            .Select(r => r.FilePath is string path ? Path.GetFileNameWithoutExtension(path) : null)
            .OfType<string>()];
}
