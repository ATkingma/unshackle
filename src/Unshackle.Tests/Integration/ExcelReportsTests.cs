using Shouldly;
using Unshackle.Interface;
using Unshackle.Interface.Commands;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Integration;

// TAAK-326, council round 13 E17: the real CLI on samples/ExcelReports, two projects on EPPlus 4.5.3.3. Orders migrates to ClosedXML,
// Legacy (an unmapped Save) stays whole on EPPlus, and the solution still builds with both packages side by side. Slow: restore + build.
[Trait("Category", "Integration")]
public class ExcelReportsTests
{
    [Fact]
    public async Task Fix_OneCleanOneBlockedProject_MigratesPerProjectAndBuilds()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string samples = Path.Combine(directory.Path, "samples");
        SampleShopTests.CopyWithoutBuildOutput(Path.Combine(RepositoryRoot.Path, "samples"), samples);
        string solution = Path.Combine(samples, "ExcelReports", "ExcelReports.slnx");
        string legacy = Path.Combine(samples, "ExcelReports", "Legacy");
        string orders = Path.Combine(samples, "ExcelReports", "Orders");
        string report = Path.Combine(directory.Path, "r.csv");
        await SampleShopTests.DotnetAsync(cancellationToken, "restore", solution);
        string legacyCode = await File.ReadAllTextAsync(Path.Combine(legacy, "LegacyReport.cs"), cancellationToken);
        string legacyProject = await File.ReadAllTextAsync(Path.Combine(legacy, "Legacy.csproj"), cancellationToken);

        // Act
        int exit = await Cli.RunAsync(["fix", solution, "--fix", "epplus", "--force", "--report", report], cancellationToken);

        // Assert
        exit.ShouldBe(ExitCodes.Ok);
        string[] rows = await File.ReadAllLinesAsync(report, cancellationToken);
        rows.ShouldContain(r => r.StartsWith("Legacy/Legacy.csproj,0,epplus,epplus.kept,Manual,\"Stays on EPPlus: ", StringComparison.Ordinal));
        rows.ShouldContain("Orders/Orders.csproj,0,epplus,project-file,Fixed,");
        (await File.ReadAllTextAsync(Path.Combine(legacy, "LegacyReport.cs"), cancellationToken)).ShouldBe(legacyCode);
        (await File.ReadAllTextAsync(Path.Combine(legacy, "Legacy.csproj"), cancellationToken)).ShouldBe(legacyProject);
        string ordersCode = await File.ReadAllTextAsync(Path.Combine(orders, "OrderReport.cs"), cancellationToken);
        ordersCode.ShouldContain("using ClosedXML.Excel;");
        ordersCode.ShouldNotContain("OfficeOpenXml");
        string ordersProject = await File.ReadAllTextAsync(Path.Combine(orders, "Orders.csproj"), cancellationToken);
        ordersProject.ShouldContain("Include=\"ClosedXML\"");
        ordersProject.ShouldNotContain("Include=\"EPPlus\"");
        await SampleShopTests.DotnetAsync(cancellationToken, "build", solution);
    }
}
