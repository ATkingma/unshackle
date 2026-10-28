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

    [Fact]
    public void EveryPublicAutofacPluginType_HasATwinUnderTheSwappedNamespace()
    {
        // Arrange (council round 21, S11: SpecFlow.Autofac 3.9.74 against Reqnroll.Autofac 3.3.4)
        List<string> specFlowTypes = PublicTypes("SpecFlow.Autofac.SpecFlowPlugin");
        HashSet<string> reqnrollTypes = [.. PublicTypes("Reqnroll.Autofac.ReqnrollPlugin")];

        // Act
        List<string> unmapped =
        [
            .. specFlowTypes.Where(t => !t.Contains('+', StringComparison.Ordinal)
                && !reqnrollTypes.Contains(SpecFlowNames.NewNamespace(SpecFlowNames.NamespaceOf(t)) + "." + SpecFlowNames.SimpleNameOf(t))),
        ];

        // Assert
        specFlowTypes.ShouldNotBeEmpty();
        unmapped.ShouldBeEmpty();
    }

    [Fact]
    public void EveryPublicAutofacPluginMember_HasATwin_OrIsListed()
    {
        // Arrange (council round 21, S11: a type twin is not enough when a member was renamed)
        var reqnroll = TypeSymbols("Reqnroll.Autofac.ReqnrollPlugin").ToDictionary(SpecFlowNames.FullName, StringComparer.Ordinal);

        // Act
        List<string> missing =
        [
            .. TypeSymbols("SpecFlow.Autofac.SpecFlowPlugin")
                .SelectMany(t => t.GetMembers()
                    .Where(m => m.DeclaredAccessibility == Accessibility.Public && !m.IsImplicitlyDeclared && m is not IMethodSymbol { MethodKind: not MethodKind.Ordinary })
                    .Select(m => (Type: t, m.Name)))
                .Where(x => !SpecFlowNames.RemovedMembers.Contains(SpecFlowNames.FullName(x.Type) + "." + x.Name))
                .Where(x => !(reqnroll.TryGetValue(SpecFlowNames.NewNamespace(x.Type.ContainingNamespace.ToDisplayString()) + "." + x.Type.MetadataName, out INamedTypeSymbol? twin)
                    && twin.GetMembers(SpecFlowNames.RenamedMembers.GetValueOrDefault(SpecFlowNames.FullName(x.Type) + "." + x.Name, x.Name)).Any()))
                .Select(x => SpecFlowNames.FullName(x.Type) + "." + x.Name)
                .Distinct(),
        ];

        // Assert
        missing.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("SpecFlow.Autofac", "Reqnroll.Autofac")]
    [InlineData("SpecFlow.Autofac.SpecFlowPlugin", "Reqnroll.Autofac.ReqnrollPlugin")]
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

    private static List<string> PublicTypes(params string[] assemblies) => [.. TypeSymbols(assemblies).Select(SpecFlowNames.FullName)];

    private static List<INamedTypeSymbol> TypeSymbols(params string[] assemblies)
    {
        var compilation = CSharpCompilation.Create("Names", references: [.. SpecFlowReferences.SpecFlow, .. SpecFlowReferences.Reqnroll]);
        return
        [
            .. compilation.References
                .Select(compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .Where(a => assemblies.Contains(a.Name))
                .SelectMany(a => Types(a.GlobalNamespace))
                .Where(t => t.DeclaredAccessibility == Accessibility.Public),
        ];
    }

    private static IEnumerable<INamedTypeSymbol> Types(INamespaceSymbol ns) =>
        ns.GetTypeMembers().Concat(ns.GetNamespaceMembers().SelectMany(Types));
}
