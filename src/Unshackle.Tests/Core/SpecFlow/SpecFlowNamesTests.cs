using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.SpecFlow;
using Xunit;

namespace Unshackle.Tests.Core.SpecFlow;

// The name map is checked against the real assemblies: every public type of SpecFlow 3.9.74 (TechTalk.SpecFlow, BoDi) has a Reqnroll
// 3.3.4 twin under the swapped namespace, a rename entry whose target exists, or is listed as removed.
public class SpecFlowNamesTests
{
    private static readonly IReadOnlyList<string> s_specFlowTypes = PublicTypes("TechTalk.SpecFlow", "BoDi");

    private static readonly HashSet<string> s_reqnrollTypes = [.. PublicTypes("Reqnroll")];

    [Fact]
    public void RenamedTypes_EveryTarget_ExistsInReqnroll()
    {
        // Act
        List<string> missing = [.. SpecFlowNames.RenamedTypes.Values.Where(t => !s_reqnrollTypes.Contains(t))];

        // Assert
        missing.ShouldBeEmpty();
    }

    [Fact]
    public void EveryPublicSpecFlowType_HasATwin_OrIsListed()
    {
        // Act
        List<string> unmapped =
        [
            .. s_specFlowTypes.Where(t => !t.Contains('+', StringComparison.Ordinal)
                && !SpecFlowNames.RenamedTypes.ContainsKey(t)
                && !SpecFlowNames.RemovedTypes.Contains(t)
                && !SpecFlowNames.IsRemovedNamespace(SpecFlowNames.NamespaceOf(t))
                && !s_reqnrollTypes.Contains(SpecFlowNames.NewNamespace(SpecFlowNames.NamespaceOf(t)) + "." + SpecFlowNames.SimpleNameOf(t))),
        ];

        // Assert
        unmapped.ShouldBeEmpty();
    }

    [Fact]
    public void RenamedAndRemovedTypes_AllExistInSpecFlow()
    {
        // Act
        List<string> unknown = [.. SpecFlowNames.RenamedTypes.Keys.Concat(SpecFlowNames.RemovedTypes).Where(t => !s_specFlowTypes.Contains(t))];

        // Assert
        unknown.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("TechTalk.SpecFlow", "Reqnroll")]
    [InlineData("TechTalk.SpecFlow.Assist", "Reqnroll.Assist")]
    [InlineData("BoDi", "Reqnroll.BoDi")]
    [InlineData("TechTalk.SpecFlowish", null)]
    [InlineData("System", null)]
    public void NewNamespace_MapsOnlySpecFlowNamespaces(string oldNamespace, string? expected)
    {
        // Act
        string? actual = SpecFlowNames.NewNamespace(oldNamespace);

        // Assert
        actual.ShouldBe(expected);
    }

    private static List<string> PublicTypes(params string[] assemblies)
    {
        var compilation = CSharpCompilation.Create("Names", references: [.. SpecFlowReferences.SpecFlow, .. SpecFlowReferences.Reqnroll]);
        return
        [
            .. compilation.References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .Where(a => assemblies.Contains(a.Name))
                .SelectMany(a => Types(a.GlobalNamespace))
                .Where(t => t.DeclaredAccessibility == Accessibility.Public)
                .Select(SpecFlowNames.FullName),
        ];
    }

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceSymbol ns) =>
        ns.GetTypeMembers().Concat(ns.GetNamespaceMembers().SelectMany(Types));
}
