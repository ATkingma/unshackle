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
        // Act
        INamedTypeSymbol? old = Compile(TestReferences.MediatR).GetTypeByMetadataName(metadataName);
        INamedTypeSymbol? twin = Compile(TestReferences.Mediator).GetTypeByMetadataName("Mediator" + metadataName["MediatR".Length..]);

        // Assert
        old.ShouldNotBeNull();
        twin.ShouldNotBeNull();
        twin.TypeKind.ShouldBe(old.TypeKind);
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
