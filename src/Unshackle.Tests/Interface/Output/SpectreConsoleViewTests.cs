using Shouldly;
using Spectre.Console;
using Unshackle.Core;
using Unshackle.Core.Models;
using Unshackle.Interface.Output;
using Xunit;

namespace Unshackle.Tests.Interface.Output;

public class SpectreConsoleViewTests
{
    [Fact]
    public void ShowSummary_WithBlockedFindings_NamesTheManualOnesThatBlock()
    {
        // Arrange
        var writer = new StringWriter();
        IAnsiConsole console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer), Ansi = AnsiSupport.No });
        console.Profile.Width = 200;
        var view = new SpectreConsoleView(console, console);

        // Act
        view.ShowSummary(
        [
            Finding("Handler.cs", 3, Fixer.NotRewrittenPrefix + "this fix changes nothing while other findings are Manual."),
            Finding("Handler.cs", 9, Fixer.NotRewrittenPrefix + "this fix changes nothing while other findings are Manual."),
            Finding("Startup.cs", 12, "AddMediatR scans the assembly of type parameter 'T'."),
            Finding("Other.cs", 4, "Manual in another project."),
        ], new Dictionary<string, string> { ["Startup.cs"] = "Web.csproj", ["Other.cs"] = "Jobs.csproj" });

        // Assert
        string output = writer.ToString();
        output.ShouldContain("2 findings wait on these 2 manual ones");
        output.ShouldContain("Web.csproj (1 blocking):");
        output.ShouldContain("Jobs.csproj (1 blocking):");
        output.ShouldContain("Startup.cs:12 mediatr.add-mediatr: AddMediatR scans the assembly of type parameter 'T'.");
        output.ShouldContain("Other.cs:4 mediatr.add-mediatr: Manual in another project.");
    }

    [Fact]
    public void ShowSummary_WithoutBlockedFindings_ListsNoBlockers()
    {
        // Arrange
        var writer = new StringWriter();
        IAnsiConsole console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer), Ansi = AnsiSupport.No });

        // Act
        new SpectreConsoleView(console, console).ShowSummary([Finding("Startup.cs", 12, "Manual for its own reason.")], new Dictionary<string, string>());

        // Assert
        writer.ToString().ShouldNotContain("wait on");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ShowSummary_ReportOnlyHeader_SaysInventoryOnlyAndLeavesTheHeaderOutOfTheTable(bool reportOnly)
    {
        // Arrange (TAAK-320)
        var writer = new StringWriter();
        IAnsiConsole console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer), Ansi = AnsiSupport.No });
        console.Profile.Width = 200;
        List<Finding> findings = [Finding("Consumer.cs", 4, "Consumer found.")];
        if (reportOnly)
        {
            findings.Insert(0, new Finding { FilePath = "x.sln", Line = 0, FixId = "mediatr", PatternId = Fixer.InventoryOnlyPatternId, Result = new InfoResult("Inventory only.") { Kind = InfoKind.Header } });
        }

        // Act
        new SpectreConsoleView(console, console).ShowSummary(findings, new Dictionary<string, string>());

        // Assert
        string output = writer.ToString();
        output.Contains(SpectreConsoleView.InventoryOnlyLine, StringComparison.Ordinal).ShouldBe(reportOnly);
        output.ShouldNotContain(Fixer.InventoryOnlyPatternId);
        output.ShouldContain("0 fixed (0 with a warning), 1 manual.");
    }

    private static Finding Finding(string file, int line, string reason) => new()
    {
        FilePath = file,
        Line = line,
        FixId = "mediatr",
        PatternId = "mediatr.add-mediatr",
        Result = new ManualResult(reason),
    };
}
