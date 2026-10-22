using System.IO.Abstractions.TestingHelpers;
using Shouldly;
using Spectre.Console;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Reports;
using Unshackle.Interface.Output;
using Xunit;

namespace Unshackle.Tests.Core;

// Council round 6, Q2: the MediatR kept row got a KeptReason code. Each branch that reads a kept row must behave exactly as it did when
// the row was a plain Manual result with the prefix written into its reason (Kept == None).
public class KeptReasonTextTests
{
    private const string Why = "it is tied to Decorator.cs:24, which cannot be migrated. Migrate them together by hand to drop MediatR.";

    private const string Legacy = "Stays on MediatR next to Mediator: " + Why;

    [Fact]
    public void Rendered_MediatRKept_PrintsTheMediatRPrefix()
    {
        // Arrange
        var manual = (ManualResult)PatternResult.KeptManual(KeptReason.MediatRNextToMediator, Why);

        // Act
        string rendered = KeptReasonText.Rendered(manual);

        // Assert
        rendered.ShouldBe("Stays on MediatR next to Mediator: it is tied to Decorator.cs:24, which cannot be migrated. Migrate them together by hand to drop MediatR.");
    }

    [Theory]
    [InlineData(KeptReason.None, "")]
    [InlineData(KeptReason.CallKept, "Stays on AutoMapper: ")]
    [InlineData(KeptReason.MediatRNextToMediator, "Stays on MediatR next to Mediator: ")]
    public void Prefix_PerCode_IsThePrefixOfItsFix(KeptReason code, string expected)
    {
        // Act
        string prefix = KeptReasonText.Prefix(code);

        // Assert
        prefix.ShouldBe(expected);
    }

    // Branch: report rows (custom assertion grouping, File/Line/Fix/Pattern/Status/Reason).
    [Fact]
    public void FromAll_MediatRKept_SameRowsAsThePlainManual()
    {
        // Act
        List<string> coded = [.. ReportRow.FromAll([Coded(15), Coded(19)]).Select(Cells)];
        List<string> plain = [.. ReportRow.FromAll([Plain(15), Plain(19)]).Select(Cells)];

        // Assert
        coded.ShouldBe(plain);
    }

    // Branch: the CSV and Markdown report files.
    [Theory]
    [InlineData(".csv")]
    [InlineData(".md")]
    public async Task WriteAsync_MediatRKept_SameFileAsThePlainManual(string extension)
    {
        // Act
        string coded = await WriteAsync(extension, [Coded(15), Plain(30)]);
        string plain = await WriteAsync(extension, [Plain(15), Plain(30)]);

        // Assert
        coded.ShouldBe(plain);
    }

    // Branch: the console summary (counts per pattern, blockers, totals).
    [Fact]
    public void ShowSummary_MediatRKept_SameOutputAsThePlainManual()
    {
        // Arrange
        var waiting = new Finding
        {
            FilePath = "Handler.cs",
            Line = 3,
            FixId = "mediatr",
            PatternId = "mediatr.request-handler",
            Result = PatternResult.Manual(Fixer.NotRewrittenPrefix + "this fix changes nothing while other findings are Manual."),
        };

        // Act
        string coded = Summary([Coded(15), waiting]);
        string plain = Summary([Plain(15), waiting]);

        // Assert
        coded.ShouldBe(plain);
        coded.ShouldContain("Cart.cs:15 mediatr.kept: " + Legacy);
    }

    // Branch: AllOrNothing and AllowsPartial, which decide in the Fixer whether the partial plan runs.
    [Fact]
    public void AllowsPartial_MediatRKept_SameAsThePlainManual()
    {
        // Arrange
#pragma warning disable CA1859 // AllowsPartial is a default interface member: MediatRFix does not override it, so only IFix can call it
        IFix fix = new MediatRFix();
#pragma warning restore CA1859

        // Act
        bool coded = fix.AllowsPartial(Coded(15));
        bool plain = fix.AllowsPartial(Plain(15));

        // Assert
        fix.AllOrNothing.ShouldBeTrue();
        coded.ShouldBe(plain);
    }

    private static Finding Coded(int line) => Row(line, PatternResult.KeptManual(KeptReason.MediatRNextToMediator, Why));

    private static Finding Plain(int line) => Row(line, PatternResult.Manual(Legacy));

    private static Finding Row(int line, PatternResult result) => new()
    {
        FilePath = "Cart.cs",
        Line = line,
        FixId = "mediatr",
        PatternId = MediatROwners.KeptPatternId,
        Result = result,
    };

    private static string Cells(ReportRow row) => $"{row.File}|{row.Line}|{row.Fix}|{row.Pattern}|{row.Status}|{row.Reason}";

    private static async Task<string> WriteAsync(string extension, IEnumerable<Finding> findings)
    {
        var fileSystem = new MockFileSystem();
        IReportWriter writer = extension == ".csv" ? new CsvReportWriter(fileSystem) : new MarkdownReportWriter(fileSystem);
        await writer.WriteAsync("report" + extension, findings, TestContext.Current.CancellationToken);
        return await fileSystem.File.ReadAllTextAsync("report" + extension, TestContext.Current.CancellationToken);
    }

    private static string Summary(IReadOnlyList<Finding> findings)
    {
        var writer = new StringWriter();
        IAnsiConsole console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer), Ansi = AnsiSupport.No });
        console.Profile.Width = 300;
        new SpectreConsoleView(console, console).ShowSummary(findings);
        return writer.ToString();
    }
}
