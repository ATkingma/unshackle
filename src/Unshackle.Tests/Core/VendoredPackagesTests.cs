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

// The ardalis layout: Core and Infrastructure reference Ardalis.SharedKernel, UseCases gets it through Core (TAAK-194).
public class VendoredPackagesTests
{
    private const string CoreCode = "using Ardalis.SharedKernel; namespace App; public class Order : EntityBase, IAggregateRoot { }";
    private const string UseCasesCode = "using Ardalis.SharedKernel; namespace App; public record GetOrder(int Id) : IQuery<int>;";
    private const string InfrastructureCode = "using Ardalis.SharedKernel; namespace App; public class Data(IDomainEventDispatcher dispatcher) { }";

    private readonly IProjectFileStore _projectFiles = Substitute.For<IProjectFileStore>();
    private static readonly AdhocWorkspace s_workspace = new();

    public VendoredPackagesTests()
    {
        _projectFiles.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    [Fact]
    public async Task FixAsync_UsersShareOneProject_PutsSourceThereAndSwapsThePackage()
    {
        // Arrange
        Solution solution = s_workspace.CurrentSolution;
        (solution, ProjectId core) = AddProject(solution, "Core", CoreCode);
        (solution, ProjectId useCases) = AddProject(solution, "UseCases", UseCasesCode, core);
        (solution, _) = AddProject(solution, "Infrastructure", InfrastructureCode, core, useCases);
        var fixer = new Fixer([new MediatRFix()], _projectFiles);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult, string.Join(" | ", report.Findings.Select(f => f.Result is ManualResult m ? m.Reason : f.PatternId)));
        string folder = Path.Combine(Root, "Core", "Ardalis.SharedKernel");
        report.Solution.GetProject(core)!.Documents.Where(d => d.Name != "Core.cs").Select(d => d.FilePath)
            .ShouldBe(ArdalisSharedKernel.Package.Versions.Single(v => v.Version == "1.4.0").Files.Select(f => Path.Combine(folder, f.Name)), ignoreOrder: true);
        report.Solution.Projects.Where(p => p.Id != core).SelectMany(p => p.Documents).Count().ShouldBe(2);
        report.ProjectFileChanges.ShouldContain(c => c.Creates && c.FilePath == Path.Combine(folder, "LICENSE.txt") && c.NewText.Contains("Steve Smith"));
        await _projectFiles.Received(1).EditAsync(
            Arg.Is<IEnumerable<ProjectMigration>>(m => IsVendoredSwap(m)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixAsync_UsersShareNoProject_ReportsManual()
    {
        // Arrange
        Solution solution = s_workspace.CurrentSolution;
        (solution, _) = AddProject(solution, "Core", CoreCode);
        (solution, _) = AddProject(solution, "Infrastructure", InfrastructureCode);
        var fixer = new Fixer([new MediatRFix()], _projectFiles);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.Where(f => f.PatternId == VendoredPackages.PatternId).ShouldAllBe(f => f.Result is ManualResult
            && ((ManualResult)f.Result).Reason.Contains("no single project for its source"));
        report.Findings.Count(f => f.PatternId == VendoredPackages.PatternId).ShouldBe(2);
        report.ProjectFileChanges.ShouldBeEmpty();
    }

    // Core swaps the package for its dependencies; the others only lose it.
    private static bool IsVendoredSwap(IEnumerable<ProjectMigration> migrations)
    {
        List<ProjectMigration> vendored = [.. migrations.Where(m => m.Package.RemovedPackage == ArdalisSharedKernel.Name)];
        return vendored.Count == 3
            && vendored[0].ProjectPath == ProjectPath("Core")
            && vendored[0].Package.AddedPackages.Select(p => p.Name).SequenceEqual(["Ardalis.GuardClauses", "Ardalis.Specification", "Microsoft.Extensions.Logging.Abstractions"])
            && vendored.Skip(1).Select(m => m.ProjectPath).SequenceEqual([ProjectPath("UseCases"), ProjectPath("Infrastructure")])
            && vendored.Skip(1).All(m => m.Package.AddedPackages.Count == 0 && m.Package.AlsoRemovedPackages.SequenceEqual([ArdalisSharedKernel.Name]));
    }

    private static string Root => Path.Combine(Path.GetTempPath(), "unshackle-vendored");

    private static string ProjectPath(string name) => Path.Combine(Root, name, name + ".csproj");

    // In memory every project references the package itself; the root is still the one the others reference.
    private static (Solution Solution, ProjectId Id) AddProject(Solution solution, string name, string code, params ProjectId[] references)
    {
        var id = ProjectId.CreateNewId();
        solution = solution
            .AddProject(ProjectInfo.Create(id, VersionStamp.Default, name, name, LanguageNames.CSharp, filePath: ProjectPath(name)))
            .WithProjectCompilationOptions(id, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(id, TestReferences.MediatRSharedKernel)
            .AddProjectReferences(id, references.Select(r => new ProjectReference(r)))
            .AddDocument(DocumentId.CreateNewId(id), name + ".cs", code, filePath: Path.Combine(Root, name, name + ".cs"));
        return (solution, id);
    }
}
