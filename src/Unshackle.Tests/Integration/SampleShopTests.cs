using System.Diagnostics;
using Shouldly;
using Unshackle.Interface;
using Unshackle.Interface.Commands;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Integration;

// The real CLI on a restored copy of samples/ in a temp directory whose path has spaces. Slow: restore + MSBuild.
[Trait("Category", "Integration")]
public class SampleShopTests
{
    private const string FirstShouldBe = "        order.Total.Should().Be(25m);";
    private const string FirstShouldBeFixed = "        order.Total.ShouldBe(25m);";

    [Fact]
    public async Task ScanDryRunFix_SampleShopCopy_ReportsThenRewrites()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string samples = Path.Combine(directory.Path, "my samples");
        CopyWithoutBuildOutput(Path.Combine(RepositoryRoot.Path, "samples"), samples);
        string solution = Path.Combine(samples, "SampleShop", "SampleShop.slnx");
        string orderTests = Path.Combine(samples, "SampleShop", "SampleShop.Tests", "OrderTests.cs");
        string report = Path.Combine(directory.Path, "report out", "r.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(report)!);
        await RestoreAsync(solution, cancellationToken);
        string original = await File.ReadAllTextAsync(orderTests, cancellationToken);

        // Act
        int scanExit = await Cli.RunAsync(["scan", solution, "--fix", "fluentassertions", "--report", report], cancellationToken);
        string afterScan = await File.ReadAllTextAsync(orderTests, cancellationToken);
        int dryRunExit = await Cli.RunAsync(["fix", solution, "--fix", "fluentassertions", "--dry-run"], cancellationToken);
        string afterDryRun = await File.ReadAllTextAsync(orderTests, cancellationToken);
        int fixExit = await Cli.RunAsync(["fix", solution, "--fix", "fluentassertions"], cancellationToken);
        string afterFix = await File.ReadAllTextAsync(orderTests, cancellationToken);

        // Assert
        scanExit.ShouldBe(ExitCodes.Ok);
        string[] rows = await File.ReadAllLinesAsync(report, cancellationToken);
        rows[0].ShouldBe("File,Line,Fix,Pattern,Status,Reason");
        rows.ShouldContain("SampleShop.Tests/OrderTests.cs,24,fluentassertions,fa.should-be,Fixed,");
        rows.ShouldContain(r => r.StartsWith("SampleShop.Tests/RepositoryTests.cs,16,fluentassertions,fa.have-count,Manual,", StringComparison.Ordinal));
        rows.Count(r => r.Contains(",Fixed,", StringComparison.Ordinal)).ShouldBeGreaterThanOrEqualTo(20);
        afterScan.ShouldBe(original);

        dryRunExit.ShouldBe(ExitCodes.Ok);
        afterDryRun.ShouldBe(original);

        fixExit.ShouldBe(ExitCodes.Ok);
        original.ShouldContain(FirstShouldBe);
        afterFix.ShouldContain(FirstShouldBeFixed);
        afterFix.ShouldNotContain(FirstShouldBe);
        afterFix.ShouldContain("using Shouldly;");
    }

    // bin/ and obj/ hold absolute paths of the original location; restore creates them again.
    private static void CopyWithoutBuildOutput(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)));
        }

        foreach (string child in Directory.EnumerateDirectories(source))
        {
            string name = Path.GetFileName(child);
            if (name is not ("bin" or "obj"))
            {
                CopyWithoutBuildOutput(child, Path.Combine(target, name));
            }
        }
    }

    private static async Task RestoreAsync(string solution, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in new[] { "restore", solution })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(startInfo)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> error = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        process.ExitCode.ShouldBe(0, await output + await error);
    }
}
