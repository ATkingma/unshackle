using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.XunitV3;
using Xunit;

namespace Unshackle.Tests.Core.XunitV3;

// Council round 22 (X11, X12): the type lists in XunitV3SdkTypes come from the real assemblies, never from a hand table.
public class XunitV3SdkTypesTests
{
    private static readonly string[] s_v2SdkAssemblies = ["xunit.core", "xunit.execution.dotnet", "xunit.assert"];

    [Fact]
    public void MissingInV3_EqualsTheRealAssemblies()
    {
        // Arrange
        HashSet<string> v3 = [.. PublicTypes(XunitV3References.After, _ => true)];

        // Act
        List<string> expected = [.. PublicTypes(XunitV3References.Before, a => s_v2SdkAssemblies.Contains(a, StringComparer.OrdinalIgnoreCase))
            .Where(n => n.StartsWith("Xunit.Sdk.", StringComparison.Ordinal) && !v3.Contains(n))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)];

        // Assert
        XunitV3SdkTypes.MissingInV3.Order(StringComparer.Ordinal).ToList().ShouldBe(expected,
            "regenerate XunitV3SdkTypes.MissingInV3:\n" + string.Join("\n", expected.Select(n => $"        \"{n}\",")));
    }

    [Fact]
    public void V3Hints_KeysInV2ValuesInV3()
    {
        // Arrange
        HashSet<string> v2 = [.. PublicTypes(XunitV3References.Before, _ => true)];
        HashSet<string> v3 = [.. PublicTypes(XunitV3References.After, _ => true)];

        // Act
        List<string> wrong = [.. XunitV3SdkTypes.V3Hints.Where(h => !v2.Contains(h.Key) || !v3.Contains(h.Value)).Select(h => h.Key)];

        // Assert
        wrong.ShouldBeEmpty();
    }

    private static IEnumerable<string> PublicTypes(IReadOnlyList<MetadataReference> references, Func<string, bool> assembly)
    {
        var compilation = CSharpCompilation.Create("Types", [], references);
        return references.Select(compilation.GetAssemblyOrModuleSymbol).OfType<IAssemblySymbol>()
            .Where(a => assembly(a.Name))
            .SelectMany(a => Types(a.GlobalNamespace))
            .Where(t => t.DeclaredAccessibility == Accessibility.Public)
            .Select(t => t.ContainingNamespace.ToDisplayString() + "." + t.MetadataName);
    }

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceSymbol ns) =>
        ns.GetTypeMembers().Concat(ns.GetNamespaceMembers().SelectMany(Types));
}
