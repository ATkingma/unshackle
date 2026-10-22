using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Engine;

// TAAK-317, council round 8 S3: the Fixer applies a fix's typed project-file edits after the document pass, only without blocking Manual rows.
public class ProjectFileMigrationTests
{
    private const string ProjectPath = "/repo/Specs/Specs.csproj";
    private const string Code = "using FluentAssertions;\nclass C\n{\n    void M() => 1.Should();\n}";

    private static readonly PackageRenameEdit s_rename = new() { OldName = "SpecFlow", NewName = "Reqnroll", Version = "2.4.1" };

    private readonly IProjectFileStore _store = Substitute.For<IProjectFileStore, IProjectFileEditor>();

    private IProjectFileEditor Editor => (IProjectFileEditor)_store;

    [Fact]
    public async Task FixAsync_NoManualFinding_AppliesEditsAndReportsEachWithFileAndLine()
    {
        // Arrange
        ProjectFileChange change = new() { FilePath = ProjectPath, OldText = "a", NewText = "b" };
        Editor.ApplyAsync(ProjectPath, Arg.Any<IEnumerable<ProjectFileEdit>>(), Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<ProjectFileEditContext>(), Arg.Any<CancellationToken>())
            .Returns([new ProjectFileEditOutcome { Edit = s_rename, FilePath = ProjectPath, Line = 6, Changes = [change] }]);
        var fixer = new Fixer([CreateFix(PatternResult.Fixed(SyntaxFactory.ParseExpression("System.Console.WriteLine()")), s_rename)], _store);

        // Act
        FixReport report = await FixAsync(fixer);

        // Assert
        await Editor.Received(1).ApplyAsync(ProjectPath, Arg.Is<IEnumerable<ProjectFileEdit>>(e => e.Single() == s_rename), Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<ProjectFileEditContext>(), Arg.Any<CancellationToken>());
        report.ProjectFileChanges.ShouldBe([change]);
        Finding row = report.Findings.Single(f => f.PatternId == Fixer.ProjectFilePatternId);
        row.FilePath.ShouldBe(ProjectPath);
        row.Line.ShouldBe(6);
        FixedResult result = row.Result.ShouldBeOfType<FixedResult>();
        result.Info.ShouldBe(s_rename.Describe());
        result.Warning.ShouldBeNull();
    }

    [Fact]
    public async Task FixAsync_BlockingManualFinding_AppliesNoEditAndReportsWhy()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(PatternResult.Manual("cannot"), s_rename)], _store);

        // Act
        FixReport report = await FixAsync(fixer);

        // Assert
        await Editor.DidNotReceive().ApplyAsync(Arg.Any<string>(), Arg.Any<IEnumerable<ProjectFileEdit>>(), Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<ProjectFileEditContext>(), Arg.Any<CancellationToken>());
        report.ProjectFileChanges.ShouldBeEmpty();
        Finding row = report.Findings.Single(f => f.PatternId == Fixer.ProjectFilePatternId);
        ((ManualResult)row.Result).Reason.ShouldStartWith("Project-file edits not applied");
    }

    [Fact]
    public async Task FixAsync_KeptManualFinding_DoesNotBlock()
    {
        // Arrange
        Editor.ApplyAsync(ProjectPath, Arg.Any<IEnumerable<ProjectFileEdit>>(), Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<ProjectFileEditContext>(), Arg.Any<CancellationToken>())
            .Returns([]);
        var fixer = new Fixer([CreateFix(PatternResult.KeptManual(KeptReason.MapKept, "kept"), s_rename)], _store);

        // Act
        await FixAsync(fixer);

        // Assert
        await Editor.Received(1).ApplyAsync(ProjectPath, Arg.Any<IEnumerable<ProjectFileEdit>>(), Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<ProjectFileEditContext>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("OutputType", true)]
    [InlineData("IsTestProject", true)]
    [InlineData("LangVersion", false)]
    public async Task FixAsync_PropertyEdit_WarnsForOutputTypeAndIsTestProject(string name, bool warns)
    {
        // Arrange
        var edit = new PropertyEdit { Name = name, Value = "x" };
        Editor.ApplyAsync(ProjectPath, Arg.Any<IEnumerable<ProjectFileEdit>>(), Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<ProjectFileEditContext>(), Arg.Any<CancellationToken>())
            .Returns([new ProjectFileEditOutcome { Edit = edit, FilePath = ProjectPath, Line = 3, Changes = [] }]);
        var fixer = new Fixer([CreateFix(PatternResult.Note("ok"), edit)], _store);

        // Act
        FixReport report = await FixAsync(fixer);

        // Assert
        FixedResult result = report.Findings.Single(f => f.PatternId == Fixer.ProjectFilePatternId).Result.ShouldBeOfType<FixedResult>();
        (result.Warning is not null).ShouldBe(warns);
    }

    [Fact]
    public async Task FixAsync_ManualOutcome_ReportsManualOnItsFileAndLine()
    {
        // Arrange
        Editor.ApplyAsync(ProjectPath, Arg.Any<IEnumerable<ProjectFileEdit>>(), Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<ProjectFileEditContext>(), Arg.Any<CancellationToken>())
            .Returns([new ProjectFileEditOutcome { Edit = s_rename, FilePath = "/repo/Directory.Build.props", Line = 4, ManualReason = "shared" }]);
        var fixer = new Fixer([CreateFix(PatternResult.Note("ok"), s_rename)], _store);

        // Act
        FixReport report = await FixAsync(fixer);

        // Assert
        Finding row = report.Findings.Single(f => f.PatternId == Fixer.ProjectFilePatternId);
        row.FilePath.ShouldBe("/repo/Directory.Build.props");
        row.Line.ShouldBe(4);
        ((ManualResult)row.Result).Reason.ShouldBe($"{s_rename.Describe()}: shared");
    }

    [Fact]
    public async Task FixAsync_EditorThrows_ReportsManualAndKeepsRunning()
    {
        // Arrange
        Editor.ApplyAsync(ProjectPath, Arg.Any<IEnumerable<ProjectFileEdit>>(), Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<ProjectFileEditContext>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("locked"));
        var fixer = new Fixer([CreateFix(PatternResult.Note("ok"), s_rename)], _store);

        // Act
        FixReport report = await FixAsync(fixer);

        // Assert
        ((ManualResult)report.Findings.Single(f => f.PatternId == Fixer.ProjectFilePatternId).Result).Reason.ShouldContain("locked");
        report.ProjectFileChanges.ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_EditOfFileChangedByPackageMigration_ReplacesThatChange()
    {
        // Arrange
        ProjectFileChange packageChange = new() { FilePath = ProjectPath, OldText = "a", NewText = "b" };
        ProjectFileChange edited = new() { FilePath = ProjectPath, OldText = "a", NewText = "c" };
        _store.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>()).Returns([packageChange]);
        Editor.ApplyAsync(ProjectPath, Arg.Any<IEnumerable<ProjectFileEdit>>(), Arg.Is<IEnumerable<ProjectFileChange>>(e => e.Single() == packageChange), Arg.Any<ProjectFileEditContext>(), Arg.Any<CancellationToken>())
            .Returns([new ProjectFileEditOutcome { Edit = s_rename, FilePath = ProjectPath, Line = 1, Changes = [edited] }]);
        IFix fix = CreateFix(PatternResult.Note("ok"), s_rename);
        fix.Package.Returns(new PackageMigration { RemovedPackage = "FluentAssertions", AddedPackages = [new() { Name = "Shouldly", Version = "4.3.0" }] });
        var fixer = new Fixer([fix], _store);

        // Act
        FixReport report = await FixAsync(fixer, "class C { void M() => System.Console.WriteLine(); }");

        // Assert
        report.ProjectFileChanges.ShouldBe([edited]);
    }

    [Fact]
    public async Task FixAsync_StoreWithoutEditor_LeavesEditsAlone()
    {
        // Arrange
        IFix fix = CreateFix(PatternResult.Note("ok"), s_rename);
        var fixer = new Fixer([fix], Substitute.For<IProjectFileStore>());

        // Act
        FixReport report = await FixAsync(fixer);

        // Assert
        ((IProjectFileMigration)fix).DidNotReceive().EditsFor(Arg.Any<Project>());
        report.Findings.ShouldNotContain(f => f.PatternId == Fixer.ProjectFilePatternId);
    }

    private static IFix CreateFix(PatternResult result, ProjectFileEdit edit)
    {
        IPattern pattern = Substitute.For<IPattern>();
        pattern.Id.Returns("test.pattern");
        pattern.Find(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>())
            .Returns(call => call.Arg<SyntaxNode>().DescendantNodes().OfType<InvocationExpressionSyntax>());
        pattern.Rewrite(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>()).Returns(result);
        IFix fix = Substitute.For<IFix, IProjectFileMigration>();
        fix.Id.Returns("test");
        fix.Patterns.Returns([pattern]);
        fix.RemovedNamespaces.Returns(["FluentAssertions"]);
        fix.AddedNamespaces.Returns(["Shouldly"]);
        ((IProjectFileMigration)fix).EditsFor(Arg.Any<Project>()).Returns([edit]);
        return fix;
    }

    private static Task<FixReport> FixAsync(Fixer fixer, string code = Code)
    {
        var projectId = ProjectId.CreateNewId();
        Solution solution = new AdhocWorkspace().CurrentSolution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "Specs", "Specs", LanguageNames.CSharp, filePath: ProjectPath)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithMetadataReferences(TestReferences.FluentAssertions))
            .AddDocument(DocumentId.CreateNewId(projectId), "C.cs", code, filePath: "/repo/Specs/C.cs");
        return fixer.FixAsync(solution, new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);
    }
}
