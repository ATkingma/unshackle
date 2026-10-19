using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
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
    public void From_FixedWithWarningAndInfo_PutsBothInReason()
    {
        // Arrange
        var finding = new Finding
        {
            FilePath = "a.cs",
            Line = 1,
            FixId = "automapper",
            PatternId = "automapper.unused-imapper",
            Result = new FixedResult([SyntaxFactory.ParseExpression("x")], []) { Warning = "lookup removed.", Info = "the class MapperHelper is now empty" },
        };

        // Act
        var row = ReportRow.From(finding);

        // Assert
        row.Status.ShouldBe("Fixed");
        row.Reason.ShouldBe("lookup removed. Info: the class MapperHelper is now empty");
    }

    // Council round 3 (Q2): the kept finding carries a reason code; the renderer prints the prefix, and this pins the rendered text.
    [Fact]
    public void From_KeptLookup_RendersThePrefixInFrontOfTheReason()
    {
        // Arrange
        var finding = new Finding
        {
            FilePath = "Seeder.cs",
            Line = 9,
            FixId = "automapper",
            PatternId = "automapper.unused-imapper",
            Result = new ManualResult("the GetService<IMapper>() lookup stays (rule 1).") { Kept = KeptReason.NoRegistration },
        };

        // Act
        var row = ReportRow.From(finding);

        // Assert
        row.Status.ShouldBe("Manual");
        row.Reason.ShouldBe("Stays on AutoMapper: the GetService<IMapper>() lookup stays (rule 1).");
    }

    // Council round 3 (Q1): the Info line is in the JSON and markdown output too, not only in the model.
    [Fact]
    public async Task JsonAndMarkdownWriteAsync_FixedWithInfo_WriteTheInfoLine()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        fileSystem.AddDirectory("/out");
        Finding[] findings =
        [
            new()
            {
                FilePath = "MapperHelper.cs",
                Line = 3,
                FixId = "automapper",
                PatternId = "automapper.unused-imapper",
                Result = new FixedResult([SyntaxFactory.ParseExpression("x")], []) { Info = "MapperHelper is now empty and kept" },
            },
        ];

        // Act
        await new JsonReportWriter(fileSystem).WriteAsync(ReportPath + ".json", findings, TestContext.Current.CancellationToken);
        await new MarkdownReportWriter(fileSystem).WriteAsync(ReportPath + ".md", findings, TestContext.Current.CancellationToken);

        // Assert
        using var json = JsonDocument.Parse(fileSystem.File.ReadAllText(ReportPath + ".json"));
        json.RootElement[0].GetProperty("Reason").GetString().ShouldBe("Info: MapperHelper is now empty and kept");
        fileSystem.File.ReadAllLines(ReportPath + ".md")[2].ShouldContain("| Fixed | Info: MapperHelper is now empty and kept |");
    }

    [Fact]
    public void FromAll_CustomAssertionCalls_OneRowPerAssertionWithCount()
    {
        // Arrange
        static Finding Custom(string file, string reason) => new()
        {
            FilePath = file,
            Line = 3,
            FixId = "fluentassertions",
            PatternId = CustomAssertionPattern.PatternId,
            Result = PatternResult.Manual(reason),
        };
        Finding[] findings = [Custom("a.cs", "BeFailure"), s_findings[0], Custom("b.cs", "BeFailure"), Custom("c.cs", "HaveEntry")];

        // Act
        ReportRow[] rows = [.. ReportRow.FromAll(findings)];

        // Assert
        rows.Select(r => (r.File, r.Reason)).ShouldBe(
        [
            ("a.cs", "2 call(s), first here. BeFailure"),
            ("tests/UserTests.cs", ""),
            ("c.cs", "1 call(s), first here. HaveEntry"),
        ]);
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
