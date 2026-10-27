using Microsoft.CodeAnalysis;
using Unshackle.Tests.Helpers;

namespace Unshackle.Tests.Core.SpecFlow;

// Real assemblies for SpecFlow proofs, from the test output folder: "before" SpecFlow 3.9.74 (TechTalk.SpecFlow with its BoDi container),
// "after" Reqnroll 3.3.4. Both BSD-3-Clause.
public static class SpecFlowReferences
{
    // Council round 21, S11: the Autofac plugins (SpecFlow.Autofac 3.9.74, Reqnroll.Autofac 3.3.4, both BSD-3-Clause) prove the namespace map.
    public static IReadOnlyList<MetadataReference> SpecFlow { get; } =
        [.. TestReferences.Net, Reference("TechTalk.SpecFlow"), Reference("BoDi"), Reference("SpecFlow.Autofac.SpecFlowPlugin"), Reference("Autofac")];

    public static IReadOnlyList<MetadataReference> Reqnroll { get; } =
        [.. TestReferences.Net, Reference("Reqnroll"), Reference("Reqnroll.Autofac.ReqnrollPlugin"), Reference("Autofac")];

    public static string PathOf(string assembly) => Path.Combine(AppContext.BaseDirectory, assembly + ".dll");

    private static PortableExecutableReference Reference(string assembly) => MetadataReference.CreateFromFile(PathOf(assembly));
}
