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
        NSubstitute,
        .. Autofac,
    ];

    public static IReadOnlyList<MetadataReference> Mediator { get; } =
        [.. Net, MetadataReference.CreateFromFile(typeof(global::Mediator.IMediator).Assembly.Location), DependencyInjection, NSubstitute, .. Autofac];

    // Mediator writes AddMediator and MediatorOptions at compile time, so an "after" using them only compiles with its generator.
    public static IReadOnlyList<ISourceGenerator> MediatorGenerators { get; } =
    [
        .. new AnalyzerFileReference(Path.Combine(AppContext.BaseDirectory, "Generators", "Mediator.SourceGenerator.dll"), new AnalyzerLoader())
            .GetGenerators(LanguageNames.CSharp),
    ];

    // AutoMapper 14.0.0: the last free version. "After" keeps it while a Profile stays (an emptied one too). NSubstitute plays the test
    // project's mock library, so a proof can stub IMapper (TAAK-279).
    public static IReadOnlyList<MetadataReference> AutoMapper { get; } =
        [.. Net, MetadataReference.CreateFromFile(typeof(global::AutoMapper.Profile).Assembly.Location), DependencyInjection,
            MetadataReference.CreateFromFile(typeof(global::NSubstitute.Substitute).Assembly.Location)];

    // MediatR registered by hand in an Autofac module (TAAK-196); Populate comes from Autofac.Extensions.DependencyInjection.
    private static IReadOnlyList<MetadataReference> Autofac =>
    [
        MetadataReference.CreateFromFile(typeof(global::Autofac.ContainerBuilder).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::Autofac.Extensions.DependencyInjection.AutofacRegistration).Assembly.Location),
    ];

    // What Ardalis.SharedKernel 1.4.0 depended on: its vendored source needs them once the package is gone (TAAK-194).
    public static IReadOnlyList<MetadataReference> SharedKernelDependencies { get; } =
    [
        MetadataReference.CreateFromFile(typeof(global::Ardalis.GuardClauses.Guard).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::Ardalis.Specification.IRepositoryBase<>).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(global::Microsoft.Extensions.Logging.ILogger).Assembly.Location),
    ];

    // MediatR plus a package that builds on it.
    public static IReadOnlyList<MetadataReference> MediatRSharedKernel { get; } =
        [.. MediatR, MetadataReference.CreateFromFile(typeof(global::Ardalis.SharedKernel.EntityBase).Assembly.Location), .. SharedKernelDependencies];

    public static IReadOnlyList<MetadataReference> MediatorSharedKernel { get; } = [.. Mediator, .. SharedKernelDependencies];

    // Test code that mocks a pipeline behavior's next (RequestHandlerDelegate).
    private static MetadataReference NSubstitute =>
        MetadataReference.CreateFromFile(typeof(global::NSubstitute.Substitute).Assembly.Location);

    // AddMediatR, AddMediator and the code Mediator's source generator writes need IServiceCollection.
    private static MetadataReference DependencyInjection =>
        MetadataReference.CreateFromFile(typeof(global::Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location);
}
