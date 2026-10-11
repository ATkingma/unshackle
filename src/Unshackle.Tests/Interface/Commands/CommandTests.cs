using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using Unshackle.Interface.Commands;
using Unshackle.Interface.Output;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Interface.Commands;

public class CommandTests
{
    private const string SolutionPath = "/repo/My App/App.slnx";

    private readonly IFixer _fixer = Substitute.For<IFixer>();
    private readonly ISolutionStore _store = Substitute.For<ISolutionStore>();
    private readonly IReportWriter _csv = Substitute.For<IReportWriter>();
    private readonly IProjectFileStore _projectFiles = Substitute.For<IProjectFileStore>();
    private readonly ISourceControl _sourceControl = Substitute.For<ISourceControl>();
    private readonly IConsoleView _view = Substitute.For<IConsoleView>();
    private readonly Solution _original = TestSolution.Create("class C { }\n", TestReferences.Net);

    public CommandTests()
    {
        _csv.Extension.Returns(".csv");
        _store.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_original);
        DocumentId id = _original.Projects.Single().DocumentIds.Single();
        Solution changed = _original.WithDocumentText(id, Microsoft.CodeAnalysis.Text.SourceText.From("class D { }\n"));
        _fixer.FixAsync(Arg.Any<Solution>(), Arg.Any<FixRequest>(), Arg.Any<CancellationToken>()).Returns(new FixReport
        {
            Solution = changed,
            Findings =
            [
                new Finding
                {
                    FilePath = "/repo/My App/tests/A.cs",
                    Line = 1,
                    FixId = "fa",
                    PatternId = "fa.x",
                    Result = PatternResult.Manual("needs a human"),
                },
            ],
        });
    }

    [Fact]
    public void ListExecute_Fixes_ShowsThemAndReturnsOk()
    {
        // Arrange
        IFix fix = Substitute.For<IFix>();
        var command = new ListCommand([fix], _view);

        // Act
        int exitCode = command.Execute();

        // Assert
        exitCode.ShouldBe(ExitCodes.Ok);
        _view.Received(1).ShowFixes(Arg.Is<IEnumerable<IFix>>(f => f.Single() == fix));
    }

    [Fact]
    public async Task ScanExecuteAsync_ManualFindings_WritesRelativeReportAndReturnsOk()
    {
        // Arrange
        ScanCommand command = CreateScan();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", "/out/r.CSV", false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Ok);
        await _csv.Received(1).WriteAsync(
            "/out/r.CSV",
            Arg.Is<IEnumerable<Finding>>(f => f.Single().FilePath == "tests/A.cs"),
            Arg.Any<CancellationToken>());
        await _projectFiles.DidNotReceive().SaveAsync(Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScanExecuteAsync_UnknownReportExtension_ReturnsInvalidArgumentsWithoutLoading()
    {
        // Arrange
        ScanCommand command = CreateScan();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", "/out/r.xlsx", false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.InvalidArguments);
        _view.Received(1).ShowError(Arg.Is<string>(m => m.Contains(".xlsx", StringComparison.Ordinal)));
        await _store.DidNotReceive().LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScanExecuteAsync_LoadWarnings_ShowsThemAndReturnsOk()
    {
        // Arrange
        _store.Warnings.Returns(["Project file not found: 'Gone.csproj'"]);
        ScanCommand command = CreateScan();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Ok);
        _view.Received(1).ShowMessage("Warning: Project file not found: 'Gone.csproj'");
    }

    [Fact]
    public async Task ScanExecuteAsync_LoadFails_ShowsErrorsAndReturnsFailure()
    {
        // Arrange
        _store.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new SolutionLoadException(["Bad project"]));
        ScanCommand command = CreateScan();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Failure);
        _view.Received(1).ShowError(Arg.Is<string>(m => m.Contains("Bad project", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ScanExecuteAsync_MissingSolution_ReturnsFailure()
    {
        // Arrange
        _store.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new FileNotFoundException("gone"));
        ScanCommand command = CreateScan();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Failure);
    }

    [Fact]
    public async Task ScanExecuteAsync_ReportNotWritable_ReturnsFailure()
    {
        // Arrange
        _csv.WriteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<Finding>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DirectoryNotFoundException("no dir"));
        ScanCommand command = CreateScan();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", "/missing/r.csv", false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Failure);
    }

    [Fact]
    public async Task FixExecuteAsync_CleanTree_SavesAndReturnsOk()
    {
        // Arrange
        FixCommand command = CreateFix();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, false, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Ok);
        await _projectFiles.Received(1).SaveAsync(Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<CancellationToken>());
        _view.Received(1).ShowMessage("1 file(s) written.");
    }

    [Fact]
    public async Task FixExecuteAsync_DirtyTree_RefusesWithoutLoading()
    {
        // Arrange
        _sourceControl.HasUncommittedChangesAsync("/repo/My App", Arg.Any<CancellationToken>()).Returns(true);
        FixCommand command = CreateFix();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, false, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Failure);
        await _store.DidNotReceive().LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _projectFiles.DidNotReceive().SaveAsync(Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixExecuteAsync_DirtyTreeWithForce_Saves()
    {
        // Arrange
        _sourceControl.HasUncommittedChangesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        FixCommand command = CreateFix();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, false, true, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Ok);
        await _projectFiles.Received(1).SaveAsync(Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixExecuteAsync_DryRunOnDirtyTree_ShowsDiffAndWritesNothing()
    {
        // Arrange
        _sourceControl.HasUncommittedChangesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        FixCommand command = CreateFix();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, true, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Ok);
        _view.Received(1).ShowDiff(
            TestSolution.FileName,
            Arg.Is<IReadOnlyList<DiffLine>>(l => l.Count == 2 && l[0].Text == "class C { }" && l[1].Text == "class D { }"));
        await _projectFiles.DidNotReceive().SaveAsync(Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixExecuteAsync_SaveFails_ReturnsFailure()
    {
        // Arrange
        _projectFiles.SaveAsync(Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<CancellationToken>()).ThrowsAsync(new IOException("disk full"));
        FixCommand command = CreateFix();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, false, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Failure);
        _view.Received(1).ShowError(Arg.Is<string>(m => m.Contains("disk full", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task FixExecuteAsync_ProjectFileChanges_SavesThemWithTheSourceInOneCall()
    {
        // Arrange
        ProjectFileChange change = ReturnProjectFileChange();
        FixCommand command = CreateFix();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, false, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Ok);
        // One call: the store checks every file, source and project, before it writes the first one.
        await _projectFiles.Received(1).SaveAsync(
            Arg.Is<IEnumerable<ProjectFileChange>>(c => c.Count() == 2
                && c.First().FilePath == TestSolution.FileName
                && c.First().OldText == "class C { }\n"
                && c.First().NewText == "class D { }\n"
                && c.Last() == change),
            Arg.Any<CancellationToken>());
        _view.Received(1).ShowMessage("2 file(s) written.");
    }

    [Fact]
    public async Task FixExecuteAsync_SourceControlCannotTell_RefusesWithoutLoading()
    {
        // Arrange
        _sourceControl.HasUncommittedChangesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("not in a git repository."));
        FixCommand command = CreateFix();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, false, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Failure);
        _view.Received(1).ShowError(Arg.Is<string>(m => m.Contains("git repository", StringComparison.Ordinal)
            && m.Contains("--force", StringComparison.Ordinal)));
        await _store.DidNotReceive().LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScanExecuteAsync_ReportExists_RefusesWithoutForce()
    {
        // Arrange
        using var directory = new TempDirectory();
        string report = Path.Combine(directory.Path, "README.csv");
        await File.WriteAllTextAsync(report, "mine", TestContext.Current.CancellationToken);
        ScanCommand command = CreateScan();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", report, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Failure);
        await _store.DidNotReceive().LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _csv.DidNotReceive().WriteAsync(Arg.Any<string>(), Arg.Any<IEnumerable<Finding>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScanExecuteAsync_ReportExistsWithForce_Overwrites()
    {
        // Arrange
        using var directory = new TempDirectory();
        string report = Path.Combine(directory.Path, "old.csv");
        await File.WriteAllTextAsync(report, "old", TestContext.Current.CancellationToken);
        ScanCommand command = CreateScan();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", report, true, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Ok);
        await _csv.Received(1).WriteAsync(report, Arg.Any<IEnumerable<Finding>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixExecuteAsync_DryRunWithProjectFileChanges_ShowsTheirDiffAndWritesNothing()
    {
        // Arrange
        ReturnProjectFileChange();
        FixCommand command = CreateFix();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, true, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Ok);
        _view.Received(1).ShowDiff(
            "/repo/My App/tests/T.csproj",
            Arg.Is<IReadOnlyList<DiffLine>>(l => l.Count == 2 && l[0].Text == "  <A />" && l[1].Text == "  <B />"));
        _view.Received(1).ShowMessage("Dry run: 2 file(s) would change, nothing written.");
        await _projectFiles.DidNotReceive().SaveAsync(Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixExecuteAsync_ProjectFileChangedOnDisk_ReturnsFailure()
    {
        // Arrange
        ReturnProjectFileChange();
        _projectFiles.SaveAsync(Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("T.csproj changed"));
        FixCommand command = CreateFix();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, false, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Failure);
        _view.Received(1).ShowError(Arg.Is<string>(m => m.Contains("T.csproj changed", StringComparison.Ordinal)));
    }

    private ProjectFileChange ReturnProjectFileChange()
    {
        var change = new ProjectFileChange
        {
            FilePath = "/repo/My App/tests/T.csproj",
            OldText = "<Project>\n  <A />\n</Project>\n",
            NewText = "<Project>\n  <B />\n</Project>\n",
        };
        DocumentId id = _original.Projects.Single().DocumentIds.Single();
        _fixer.FixAsync(Arg.Any<Solution>(), Arg.Any<FixRequest>(), Arg.Any<CancellationToken>()).Returns(new FixReport
        {
            Solution = _original.WithDocumentText(id, Microsoft.CodeAnalysis.Text.SourceText.From("class D { }\n")),
            Findings = [],
            ProjectFileChanges = [change],
        });
        return change;
    }

    private ScanCommand CreateScan() => new(_fixer, _store, [_csv], _view);

    private FixCommand CreateFix() => new(CreateScan(), _projectFiles, _sourceControl, _view);
}
