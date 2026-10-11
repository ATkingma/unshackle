using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Reports;
using Xunit;

namespace Unshackle.Tests.DataAccess.Reports;

public class ReportWriterTests
{
    private const string ReportPath = "/out/report";

    private static readonly Finding[] s_findings =
    [
        new()
        {
            FilePath = "tests/UserTests.cs",
            Line = 14,
            FixId = "fluentassertions",
            PatternId = "fa.should-be",
            Result = PatternResult.Fixed(SyntaxFactory.ParseExpression("x")),
        },
        new()
        {
            FilePath = "tests/OrderTests.cs",
            Line = 8,
            FixId = "fluentassertions",
            PatternId = "fa.have-count",
            Result = PatternResult.Manual("Dictionary, | or \"Keys\""),
        },
    ];

    [Fact]
    public async Task CsvWriteAsync_Findings_WritesHeaderAndRows()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/out");
        var writer = new CsvReportWriter(fileSystem);

        // Act
        await writer.WriteAsync(ReportPath, s_findings, TestContext.Current.CancellationToken);

        // Assert
        string[] lines = fileSystem.File.ReadAllLines(ReportPath);
        lines.ShouldBe(
        [
            "File,Line,Fix,Pattern,Status,Reason",
            "tests/UserTests.cs,14,fluentassertions,fa.should-be,Fixed,",
            "tests/OrderTests.cs,8,fluentassertions,fa.have-count,Manual,\"Dictionary, | or \"\"Keys\"\"\"",
        ]);
    }

    [Fact]
    public async Task CsvWriteAsync_FormulaInCell_EscapesIt()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/out");
        var writer = new CsvReportWriter(fileSystem);
        Finding[] findings =
        [
            new()
            {
                FilePath = "=HYPERLINK(\"http://x\")",
                Line = 1,
                FixId = "fluentassertions",
                PatternId = "fa.should-be",
                Result = PatternResult.Manual("+cmd"),
            },
        ];

        // Act
        await writer.WriteAsync(ReportPath, findings, TestContext.Current.CancellationToken);

        // Assert
        string row = fileSystem.File.ReadAllLines(ReportPath)[1];
        row.ShouldNotStartWith("=");
        row.ShouldContain("'+cmd");
    }

    [Fact]
    public void From_FixedWithWarning_PutsWarningInReason()
    {
        // Arrange
        var finding = new Finding
        {
            FilePath = "a.cs",
            Line = 1,
            FixId = "fluentassertions",
            PatternId = "fa.be-equivalent-to-object",
            Result = new FixedResult([SyntaxFactory.ParseExpression("x")], []) { Warning = "runtime type" },
        };

        // Act
        var row =ReportRow.From(finding);

        // Assert
        row.Status.ShouldBe("Fixed");
        row.Reason.ShouldBe("runtime type");
    }

    [Fact]
    public async Task JsonWriteAsync_Findings_WritesArrayOfRows()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/out");
        var writer = new JsonReportWriter(fileSystem);

        // Act
        await writer.WriteAsync(ReportPath, s_findings, TestContext.Current.CancellationToken);

        // Assert
        using var json = JsonDocument.Parse(fileSystem.File.ReadAllText(ReportPath));
        json.RootElement.GetArrayLength().ShouldBe(2);
        JsonElement manual = json.RootElement[1];
        manual.GetProperty("File").GetString().ShouldBe("tests/OrderTests.cs");
        manual.GetProperty("Line").GetInt32().ShouldBe(8);
        manual.GetProperty("Status").GetString().ShouldBe("Manual");
        manual.GetProperty("Reason").GetString().ShouldBe("Dictionary, | or \"Keys\"");
    }

    [Fact]
    public async Task MarkdownWriteAsync_Findings_WritesTableWithEscapedPipes()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/out");
        var writer = new MarkdownReportWriter(fileSystem);

        // Act
        await writer.WriteAsync(ReportPath, s_findings, TestContext.Current.CancellationToken);

        // Assert
        string[] lines = fileSystem.File.ReadAllLines(ReportPath);
        lines.ShouldBe(
        [
            "| File | Line | Fix | Pattern | Status | Reason |",
            "| --- | --- | --- | --- | --- | --- |",
            "| tests/UserTests.cs | 14 | fluentassertions | fa.should-be | Fixed |  |",
            "| tests/OrderTests.cs | 8 | fluentassertions | fa.have-count | Manual | Dictionary, \\| or &quot;Keys&quot; |",
        ]);
    }

    [Fact]
    public async Task MarkdownWriteAsync_HtmlBackticksAndTrailingBackslash_EscapesEveryCell()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/out");
        var writer = new MarkdownReportWriter(fileSystem);
        Finding finding = new()
        {
            FilePath = @"a\",
            Line = 1,
            FixId = "f",
            PatternId = "p",
            Result = PatternResult.Manual("<script>x</script> `c`\r\nnext"),
        };

        // Act
        await writer.WriteAsync(ReportPath, [finding], TestContext.Current.CancellationToken);

        // Assert
        fileSystem.File.ReadAllLines(ReportPath)[2].ShouldBe(
            @"| a\\ | 1 | f | p | Manual | &lt;script&gt;x&lt;/script&gt; \`c\` next |");
    }
}
