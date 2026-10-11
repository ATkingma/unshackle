using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class FixerProjectFileTests
{
    private const string ProjectPath = "/repo/Tests/Tests.csproj";
    private const string StillUsesFluentAssertions = "using FluentAssertions; class C { void M() => 1.Should(); }";
    private const string NoFluentAssertions = "using FluentAssertions; class C { void M() { } }";

    private static readonly PackageMigration s_package =
        new() { RemovedPackage = "FluentAssertions", AddedPackages = [new() { Name = "Shouldly", Version = "4.3.0" }] };

    private readonly IProjectFileStore _projectFiles = Substitute.For<IProjectFileStore>();
    private readonly ProjectFileChange _change = new() { FilePath = ProjectPath, OldText = "<Project />", NewText = "<Project></Project>" };

    public FixerProjectFileTests()
    {
        _projectFiles.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>()).Returns([_change]);
    }

    [Fact]
    public async Task FixAsync_CodeNoLongerUsesPackage_RemovesItAndReportsFixedProjectFile()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(s_package)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, NoFluentAssertions, TestReferences.FluentAssertions);

        // Assert
        await _projectFiles.Received(1).EditAsync(
            Arg.Is<IEnumerable<ProjectMigration>>(m => m.Single().ProjectPath == ProjectPath && !m.Single().KeepRemovedPackage
                && m.Single().Package == s_package && m.Single().AddedNamespaces.Single() == "Shouldly"),
            Arg.Any<CancellationToken>());
        report.ProjectFileChanges.ShouldBe([_change]);
        Finding finding = report.Findings.ShouldHaveSingleItem();
        finding.FilePath.ShouldBe(ProjectPath);
        finding.Line.ShouldBe(0);
        finding.PatternId.ShouldBe(Fixer.ProjectFilePatternId);
        finding.Result.ShouldBeOfType<FixedResult>();
    }

    [Fact]
    public async Task FixAsync_CodeStillUsesPackage_KeepsItAndReportsManual()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(s_package)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, StillUsesFluentAssertions, TestReferences.FluentAssertions);

        // Assert
        await _projectFiles.Received(1).EditAsync(
            Arg.Is<IEnumerable<ProjectMigration>>(m => m.Single().KeepRemovedPackage),
            Arg.Any<CancellationToken>());
        Finding manual = report.Findings.Single(f => f.Result is ManualResult);
        manual.FilePath.ShouldBe(ProjectPath);
        manual.Line.ShouldBe(0);
        ((ManualResult)manual.Result).Reason.ShouldContain("FluentAssertions is still used");
    }

    [Fact]
    public async Task FixAsync_ProjectWithoutRemovedAssembly_DoesNotEditProjectFiles()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(s_package)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, "class C { }", TestReferences.Net);

        // Assert
        await _projectFiles.DidNotReceive().EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>());
        report.ProjectFileChanges.ShouldBeEmpty();
        report.Findings.ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_FixWithoutPackage_DoesNotEditProjectFiles()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(null)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, StillUsesFluentAssertions, TestReferences.FluentAssertions);

        // Assert
        await _projectFiles.DidNotReceive().EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>());
        report.ProjectFileChanges.ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_EditThrows_ReportsManualInsteadOfFailing()
    {
        // Arrange
        _projectFiles.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new System.Xml.XmlException("bad xml"));
        var fixer = new Fixer([CreateFix(s_package)], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, NoFluentAssertions, TestReferences.FluentAssertions);

        // Assert
        report.ProjectFileChanges.ShouldBeEmpty();
        ManualResult manual = report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<ManualResult>();
        manual.Reason.ShouldContain("bad xml");
    }

    [Fact]
    public async Task FixAsync_EditThrows_LeavesTheCodeOfThatProjectUnchanged()
    {
        // Arrange
        _projectFiles.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidDataException("bad formatting"));
        IFix fix = CreateFix(s_package);
        IPattern pattern = Substitute.For<IPattern>();
        pattern.Id.Returns("test.pattern");
        pattern.Find(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>())
            .Returns(call => call.Arg<SyntaxNode>().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>());
        pattern.Rewrite(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>()).Returns(PatternResult.Fixed(SyntaxFactory.ParseExpression("System.Console.WriteLine()")));
        fix.Patterns.Returns([pattern]);
        var fixer = new Fixer([fix], _projectFiles);

        // Act
        FixReport report = await FixAsync(fixer, StillUsesFluentAssertions, TestReferences.FluentAssertions);

        // Assert
        string text = (await report.Solution.Projects.Single().Documents.Single().GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        text.ShouldBe(StillUsesFluentAssertions);
        report.Findings.ShouldNotBeEmpty();
        report.Findings.ShouldAllBe(f => f.Result is ManualResult);
        ((ManualResult)report.Findings.Single(f => f.PatternId == "test.pattern").Result).Reason.ShouldContain("bad formatting");
    }

    private static IFix CreateFix(PackageMigration? package)
    {
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("test");
        fix.Patterns.Returns([]);
        fix.RemovedNamespaces.Returns(["FluentAssertions"]);
        fix.AddedNamespaces.Returns(["Shouldly"]);
        fix.Package.Returns(package);
        return fix;
    }

    // TestSolution has no project path; the Fixer only migrates projects that live in a file.
    private static Task<FixReport> FixAsync(Fixer fixer, string code, IEnumerable<MetadataReference> references)
    {
        var projectId = ProjectId.CreateNewId();
        Solution solution = new AdhocWorkspace().CurrentSolution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "Tests", "Tests", LanguageNames.CSharp, filePath: ProjectPath)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithMetadataReferences(references))
            .AddDocument(DocumentId.CreateNewId(projectId), "C.cs", code, filePath: "/repo/Tests/C.cs");
        return fixer.FixAsync(solution, new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);
    }
}
