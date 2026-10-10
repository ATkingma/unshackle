using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;

namespace Unshackle.Tests.Helpers;

// Real assemblies for the semantic model, per fix: "before" is the old library, "after" the new one.
public static class TestReferences
{
    public static IReadOnlyList<MetadataReference> Net { get; } = [.. Net100.References.All];

    public static IReadOnlyList<MetadataReference> FluentAssertions { get; } =
        [.. Net, MetadataReference.CreateFromFile(typeof(global::FluentAssertions.AssertionExtensions).Assembly.Location)];

    public static IReadOnlyList<MetadataReference> Shouldly { get; } =
        [.. Net, MetadataReference.CreateFromFile(typeof(global::Shouldly.Should).Assembly.Location)];
}
