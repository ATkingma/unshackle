using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class SwappedTypesTests
{
    private const string ProjectPath = "/repo/App/App.csproj";
    private const string OnlySwappedTypes = """
        using MediatR;

        public class Ping : IRequest<Unit>
        {
            public Unit Done => Unit.Value;
        }

        """;

    private readonly IProjectFileStore _projectFiles = Substitute.For<IProjectFileStore>();

    public static TheoryData<string> SwappedTypeNames => [.. new MediatRFix().SwappedTypes];

    [Theory]
    [MemberData(nameof(SwappedTypeNames))]
    public void SwappedTypes_EachType_ExistsInMediatRAndMediator(string metadataName)
    {
        // Arrange
        string twinName = "Mediator" + metadataName["MediatR".Length..];

        // Act
        INamedTypeSymbol? old = Compile(TestReferences.MediatR).GetTypeByMetadataName(metadataName);
        INamedTypeSymbol? twin = Compile(TestReferences.Mediator).GetTypeByMetadataName(twinName);

        // Assert
        old.ShouldNotBeNull();
        twin.ShouldNotBeNull();
        twin.TypeKind.ShouldBe(old.TypeKind);
    }

    [Fact]
    public void AddedTypes_MatchPublicAndGeneratedTypesOfMediatorNamespace()
    {
        // Arrange
        MediatRFix fix = new();

        // Act
        List<string> actual = [.. Compile(TestReferences.Mediator).GetTypeByMetadataName("Mediator.IMediator")!.ContainingNamespace
            .GetTypeMembers()
            .Where(t => t.DeclaredAccessibility == Accessibility.Public)
            .Select(t => "Mediator." + t.MetadataName)
            .Concat(["Mediator.Mediator", "Mediator.MediatorOptions", "Mediator.AssemblyReference"]) // written by the source generator
            .Order(StringComparer.Ordinal)];

        // Assert
        fix.AddedTypes.Order(StringComparer.Ordinal).ShouldBe(actual);
    }

    [Fact]
    public async Task FixAsync_FileWithOnlySwappedTypes_SwapsUsingAndRemovesPackage()
    {
        // Arrange
        var fixer = new Fixer([new MediatRFix()], _projectFiles);
        (Solution solution, DocumentId documentId) = CreateSolution(OnlySwappedTypes);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        string text = (await report.Solution.GetDocument(documentId)!.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        text.ShouldBe(OnlySwappedTypes.Replace("using MediatR;", "using Mediator;", StringComparison.Ordinal));
        await _projectFiles.Received(1).EditAsync(
            Arg.Is<IEnumerable<ProjectMigration>>(m => !m.Single().KeepRemovedPackage),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixAsync_StreamRequestHandler_CompilesAgainstMediator()
    {
        // Arrange
        const string code = """
            using System.Collections.Generic;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;
            using MediatR;

            public record StreamPing(int Count) : IStreamRequest<int>;

            public class StreamPingHandler : IStreamRequestHandler<StreamPing, int>
            {
                public async IAsyncEnumerable<int> Handle(StreamPing request, [EnumeratorCancellation] CancellationToken cancellationToken)
                {
                    for (int i = 0; i < request.Count; i++)
                    {
                        await Task.Yield();
                        yield return i;
                    }
                }
            }

            """;
        var fixer = new Fixer([new MediatRFix()], _projectFiles);
        (Solution solution, DocumentId documentId) = CreateSolution(code);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        string text = (await report.Solution.GetDocument(documentId)!.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        text.ShouldBe(code.Replace("using MediatR;", "using Mediator;", StringComparison.Ordinal));
        TestSolution.CompileErrors(text, TestReferences.Mediator, TestReferences.MediatorGenerators).ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_FileStillUsingOtherMediatRType_KeepsUsingAndPackage()
    {
        // Arrange
        const string code = "using MediatR;\n\npublic class C { public RequestHandlerDelegate<Unit>? Next { get; set; } }\n";
        var fixer = new Fixer([new MediatRFix()], _projectFiles);
        (Solution solution, DocumentId documentId) = CreateSolution(code);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        (await report.Solution.GetDocument(documentId)!.GetTextAsync(TestContext.Current.CancellationToken)).ToString().ShouldBe(code);
        await _projectFiles.Received(1).EditAsync(
            Arg.Is<IEnumerable<ProjectMigration>>(m => m.Single().KeepRemovedPackage),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixAsync_ProjectWithoutAddMediatR_AddsAbstractionsWithoutSourceGenerator()
    {
        // Arrange
        var fixer = new Fixer([new MediatRFix()], _projectFiles);
        (Solution solution, _) = CreateSolution(OnlySwappedTypes);

        // Act
        await fixer.FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        await _projectFiles.Received(1).EditAsync(
            Arg.Is<IEnumerable<ProjectMigration>>(m => m.Single().Package.AddedPackages.Single().Name == "Mediator.Abstractions"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixAsync_ProjectCallingAddMediatR_AddsSourceGenerator()
    {
        // Arrange
        const string code = """
            using Microsoft.Extensions.DependencyInjection;

            public static class Registration
            {
                public static void Add(IServiceCollection services) => services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Registration).Assembly));
            }

            """;
        var fixer = new Fixer([new MediatRFix()], _projectFiles);
        (Solution solution, _) = CreateSolution(code);

        // Act
        await fixer.FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        await _projectFiles.Received(1).EditAsync(
            Arg.Is<IEnumerable<ProjectMigration>>(m => m.Single().Package.AddedPackages.Any(p => p.Name == "Mediator.SourceGenerator")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixAsync_AutofacScanOfMediatRAssembly_AddsSourceGenerator()
    {
        // Arrange
        const string code = """
            using Autofac;
            using MediatR;

            public class MediatorModule : Module
            {
                protected override void Load(ContainerBuilder builder) =>
                    builder.RegisterAssemblyTypes(typeof(IMediator).Assembly).AsImplementedInterfaces();
            }

            """;
        var fixer = new Fixer([new MediatRFix()], _projectFiles);
        (Solution solution, _) = CreateSolution(code);

        // Act
        await fixer.FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        await _projectFiles.Received(1).EditAsync(
            Arg.Is<IEnumerable<ProjectMigration>>(m => m.Single().Package.AddedPackages.Any(p => p.Name == "Mediator.SourceGenerator")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixAsync_ProjectCallingOwnAddMediator_AddsNoSourceGenerator()
    {
        // Arrange
        const string code = """
            using MediatR;
            using Microsoft.Extensions.DependencyInjection;

            public record Ping : IRequest<Unit>;

            public static class Module
            {
                public static void Add(IServiceCollection services) => Shared.AddMediator(services);
            }

            public static class Shared
            {
                public static void AddMediator(IServiceCollection services) { }
            }

            """;
        var fixer = new Fixer([new MediatRFix()], _projectFiles);
        (Solution solution, _) = CreateSolution(code);

        // Act
        await fixer.FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        await _projectFiles.Received(1).EditAsync(
            Arg.Is<IEnumerable<ProjectMigration>>(m => m.Single().Package.AddedPackages.All(p => p.Name != "Mediator.SourceGenerator")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixAsync_GlobalUsingSwappedWithOwnTypeOfSameName_QualifiesOwnTypeInEveryFile()
    {
        // Arrange
        const string global = "global using MediatR;\n";
        const string code = """
            namespace App
            {
                public interface ICommand { }

                public record Ping : IRequest<Unit>;

                public class Uses
                {
                    public ICommand? Command { get; set; }
                }
            }

            """;
        var fixer = new Fixer([new MediatRFix()], _projectFiles);
        (Solution solution, DocumentId documentId) = CreateSolution(code);
        solution = solution.AddDocument(DocumentId.CreateNewId(documentId.ProjectId), "Usings.cs", global, filePath: "/repo/App/Usings.cs");

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        string text = (await report.Solution.GetDocument(documentId)!.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        text.ShouldContain("public global::App.ICommand? Command");
        text.ShouldContain("public interface ICommand { }");
    }

    private static CSharpCompilation Compile(IEnumerable<MetadataReference> references) =>
        CSharpCompilation.Create("Check", references: references);

    private static (Solution Solution, DocumentId DocumentId) CreateSolution(string code)
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);
        Solution solution = new AdhocWorkspace().CurrentSolution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "App", "App", LanguageNames.CSharp, filePath: ProjectPath)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithMetadataReferences(TestReferences.MediatR))
            .AddDocument(documentId, "Ping.cs", code, filePath: "/repo/App/Ping.cs");
        return (solution, documentId);
    }
}
