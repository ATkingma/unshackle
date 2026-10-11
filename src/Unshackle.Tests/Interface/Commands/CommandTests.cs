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
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", "/out/r.CSV", TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Ok);
        await _csv.Received(1).WriteAsync(
            "/out/r.CSV",
            Arg.Is<IEnumerable<Finding>>(f => f.Single().FilePath == "tests/A.cs"),
            Arg.Any<CancellationToken>());
        await _store.DidNotReceive().SaveAsync(Arg.Any<Solution>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScanExecuteAsync_UnknownReportExtension_ReturnsInvalidArgumentsWithoutLoading()
    {
        // Arrange
        ScanCommand command = CreateScan();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", "/out/r.xlsx", TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.InvalidArguments);
        _view.Received(1).ShowError(Arg.Is<string>(m => m.Contains(".xlsx", StringComparison.Ordinal)));
        await _store.DidNotReceive().LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScanExecuteAsync_LoadFails_ShowsErrorsAndReturnsFailure()
    {
        // Arrange
        _store.LoadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ThrowsAsync(new SolutionLoadException(["Bad project"]));
        ScanCommand command = CreateScan();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, TestContext.Current.CancellationToken);

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
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, TestContext.Current.CancellationToken);

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
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", "/missing/r.csv", TestContext.Current.CancellationToken);

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
        await _store.Received(1).SaveAsync(Arg.Any<Solution>(), Arg.Any<CancellationToken>());
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
        await _store.DidNotReceive().SaveAsync(Arg.Any<Solution>(), Arg.Any<CancellationToken>());
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
        await _store.Received(1).SaveAsync(Arg.Any<Solution>(), Arg.Any<CancellationToken>());
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
        await _store.DidNotReceive().SaveAsync(Arg.Any<Solution>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FixExecuteAsync_SaveFails_ReturnsFailure()
    {
        // Arrange
        _store.SaveAsync(Arg.Any<Solution>(), Arg.Any<CancellationToken>()).ThrowsAsync(new IOException("disk full"));
        FixCommand command = CreateFix();

        // Act
        int exitCode = await command.ExecuteAsync(SolutionPath, "fa", null, false, false, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Failure);
        _view.Received(1).ShowError(Arg.Is<string>(m => m.Contains("disk full", StringComparison.Ordinal)));
    }

    private ScanCommand CreateScan() => new(_fixer, _store, [_csv], _view);

    private FixCommand CreateFix() => new(CreateScan(), _store, _sourceControl, _view);
}
