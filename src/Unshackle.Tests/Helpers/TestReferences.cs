using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Unshackle.Tests.Helpers;

// Real assemblies for the semantic model, per fix: "before" is the old library, "after" the new one.
public static class TestReferences
{
    public static IReadOnlyList<MetadataReference> Net { get; } = [.. Net100.References.All];

    public static IReadOnlyList<MetadataReference> FluentAssertions { get; } =
        [.. Net, MetadataReference.CreateFromFile(typeof(global::FluentAssertions.AssertionExtensions).Assembly.Location)];

    // Only Shouldly itself, as the composition root hands it to FluentAssertionsFix.
    public static IReadOnlyList<MetadataReference> ShouldlyAssembly { get; } =
        [MetadataReference.CreateFromFile(typeof(global::Shouldly.Should).Assembly.Location)];

    public static IReadOnlyList<MetadataReference> Shouldly { get; } = [.. Net, .. ShouldlyAssembly];

    // MediatR 12 keeps IRequest, INotification and Unit in MediatR.Contracts.
    public static IReadOnlyList<MetadataReference> MediatR { get; } =
    [
        .. Net,
        MetadataReference.CreateFromFile(typeof(global::MediatR.IMediator).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::MediatR.INotification).Assembly.Location),
        DependencyInjection,
    ];

    public static IReadOnlyList<MetadataReference> Mediator { get; } =
        [.. Net, MetadataReference.CreateFromFile(typeof(global::Mediator.IMediator).Assembly.Location), DependencyInjection];

    // Mediator writes AddMediator and MediatorOptions at compile time, so an "after" using them only compiles with its generator.
    public static IReadOnlyList<ISourceGenerator> MediatorGenerators { get; } =
    [
        .. new AnalyzerFileReference(Path.Combine(AppContext.BaseDirectory, "Generators", "Mediator.SourceGenerator.dll"), new AnalyzerLoader())
            .GetGenerators(LanguageNames.CSharp),
    ];

    // AddMediatR, AddMediator and the code Mediator's source generator writes need IServiceCollection.
    private static MetadataReference DependencyInjection =>
        MetadataReference.CreateFromFile(typeof(global::Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location);
}
