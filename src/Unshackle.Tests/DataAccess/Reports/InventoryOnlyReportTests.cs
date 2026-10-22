using System.IO.Abstractions.TestingHelpers;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Reports;
using Xunit;

namespace Unshackle.Tests.DataAccess.Reports;

// TAAK-316: a report-only fix's report says "inventory only"; its Info rows are neither Fixed nor Manual.
public class InventoryOnlyReportTests
{
    private const string ReportPath = "/out/report";

    private static readonly Finding[] s_findings =
    [
        new() { FilePath = "App.sln", Line = 0, FixId = "masstransit", PatternId = Fixer.InventoryOnlyPatternId, Result = PatternResult.Note("Inventory only: x") },
        new() { FilePath = "src/Bus.cs", Line = 3, FixId = "masstransit", PatternId = "masstransit.consumer", Result = PatternResult.Manual("Consumer") },
    ];

    [Fact]
    public async Task MarkdownWriteAsync_InventoryOnly_WritesHeaderBeforeTable()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/out");
        var writer = new MarkdownReportWriter(fileSystem);

        // Act
        await writer.WriteAsync(ReportPath, s_findings, TestContext.Current.CancellationToken);

        // Assert
        string[] lines = fileSystem.File.ReadAllLines(ReportPath);
        lines[0].ShouldBe(MarkdownReportWriter.InventoryOnlyHeader);
        lines[1].ShouldBeEmpty();
        lines[2].ShouldStartWith("| File |");
        lines[4].ShouldBe($"| App.sln | 0 | masstransit | {Fixer.InventoryOnlyPatternId} | Info | Inventory only: x |");
    }

    [Fact]
    public async Task MarkdownWriteAsync_OrdinaryFindings_WritesNoHeader()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/out");
        var writer = new MarkdownReportWriter(fileSystem);

        // Act
        await writer.WriteAsync(ReportPath, s_findings[1..], TestContext.Current.CancellationToken);

        // Assert
        fileSystem.File.ReadAllLines(ReportPath)[0].ShouldBe("| File | Line | Fix | Pattern | Status | Reason |");
    }

    [Fact]
    public async Task CsvWriteAsync_InfoRow_HasInfoStatus()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/out");
        var writer = new CsvReportWriter(fileSystem);

        // Act
        await writer.WriteAsync(ReportPath, s_findings, TestContext.Current.CancellationToken);

        // Assert
        fileSystem.File.ReadAllLines(ReportPath)[1].ShouldBe($"App.sln,0,masstransit,{Fixer.InventoryOnlyPatternId},Info,Inventory only: x");
    }
}
