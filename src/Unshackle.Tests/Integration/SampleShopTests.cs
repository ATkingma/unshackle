using System.Diagnostics;
using Shouldly;
using Unshackle.Interface;
using Unshackle.Interface.Commands;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Integration;

// The real CLI on a restored copy of samples/ in a temp directory whose path has spaces. Slow: restore + MSBuild (+ build and test).
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
        await DotnetAsync(cancellationToken, "restore", solution);
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

    // fix keeps FluentAssertions while the one manual finding uses it; after that is fixed by hand, a second fix removes it.
    [Fact]
    public async Task FixTwice_SampleShopCopy_MigratesProjectFilesAndBuildsAndTests()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string samples = Path.Combine(directory.Path, "my samples");
        CopyWithoutBuildOutput(Path.Combine(RepositoryRoot.Path, "samples"), samples);
        // The SDK and test runner settings of the repository, as for the samples in CI.
        File.Copy(Path.Combine(RepositoryRoot.Path, "global.json"), Path.Combine(directory.Path, "global.json"));
        string solution = Path.Combine(samples, "SampleShop", "SampleShop.slnx");
        string testProject = Path.Combine(samples, "SampleShop", "SampleShop.Tests", "SampleShop.Tests.csproj");
        string advancedProject = Path.Combine(samples, "SampleShop", "SampleShop.Advanced.Tests", "SampleShop.Advanced.Tests.csproj");
        string repositoryTests = Path.Combine(samples, "SampleShop", "SampleShop.Tests", "RepositoryTests.cs");
        string report = Path.Combine(directory.Path, "r.csv");
        await DotnetAsync(cancellationToken, "restore", solution);

        // Act
        int firstExit = await Cli.RunAsync(["fix", solution, "--fix", "fluentassertions", "--report", report], cancellationToken);
        string[] firstRows = await File.ReadAllLinesAsync(report, cancellationToken);
        string afterFirst = await File.ReadAllTextAsync(testProject, cancellationToken);
        await DotnetAsync(cancellationToken, "build", solution);
        string code = await File.ReadAllTextAsync(repositoryTests, cancellationToken);
        await File.WriteAllTextAsync(
            repositoryTests,
            code.Replace("repository.All.Should().HaveCount(1);", "repository.All.Count.ShouldBe(1);", StringComparison.Ordinal),
            cancellationToken);
        int secondExit = await Cli.RunAsync(["fix", solution, "--fix", "fluentassertions"], cancellationToken);

        // Assert
        firstExit.ShouldBe(ExitCodes.Ok);
        firstRows.ShouldContain(r => r.StartsWith("SampleShop.Tests/SampleShop.Tests.csproj,0,fluentassertions,project-file,Manual,", StringComparison.Ordinal));
        firstRows.ShouldContain("SampleShop.Tests/SampleShop.Tests.csproj,0,fluentassertions,project-file,Fixed,");
        firstRows.ShouldContain("../Directory.Packages.props,0,fluentassertions,project-file,Fixed,");
        afterFirst.ShouldContain("<PackageReference Include=\"FluentAssertions\" />");
        afterFirst.ShouldContain("<PackageReference Include=\"Shouldly\" />");

        secondExit.ShouldBe(ExitCodes.Ok);
        string afterSecond = await File.ReadAllTextAsync(testProject, cancellationToken);
        afterSecond.ShouldNotContain("FluentAssertions");
        afterSecond.ShouldContain("<Using Include=\"Shouldly\" />");
        // SampleShop.Advanced.Tests holds constructions that stay manual (TAAK-030), so it and the central versions keep FluentAssertions.
        (await File.ReadAllTextAsync(advancedProject, cancellationToken)).ShouldContain("<PackageReference Include=\"FluentAssertions\" />");
        (await File.ReadAllTextAsync(Path.Combine(samples, "Directory.Packages.props"), cancellationToken)).ShouldContain("\"FluentAssertions\"");
        await DotnetAsync(cancellationToken, "build", solution);
        await DotnetAsync(cancellationToken, "test", "--solution", solution);
    }

    // MSBuild loads a multi-target project once per target framework; every finding and file still counts once.
    [Fact]
    public async Task ScanFix_MultiTargetProject_ReportsAndWritesEachFileOnce()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string project = Path.Combine(directory.Path, "Multi.csproj");
        string code = Path.Combine(directory.Path, "Tests.cs");
        string report = Path.Combine(directory.Path, "r.csv");
        await File.WriteAllTextAsync(project, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
                <NoWarn>$(NoWarn);NU1903</NoWarn>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="FluentAssertions" Version="7.2.2" />
              </ItemGroup>
            </Project>
            """, cancellationToken);
        await File.WriteAllTextAsync(code, """
            using FluentAssertions;

            public class Tests
            {
                public void M()
                {
                    1.Should().Be(1);
                }
            }
            """, cancellationToken);
        await DotnetAsync(cancellationToken, "restore", project);

        // Act
        int scanExit = await Cli.RunAsync(["scan", project, "--fix", "fluentassertions", "--report", report], cancellationToken);
        string[] rows = await File.ReadAllLinesAsync(report, cancellationToken);
        int fixExit = await Cli.RunAsync(["fix", project, "--fix", "fluentassertions", "--force"], cancellationToken);

        // Assert
        scanExit.ShouldBe(ExitCodes.Ok);
        rows.Count(r => r.StartsWith("Tests.cs,", StringComparison.Ordinal)).ShouldBe(1);
        fixExit.ShouldBe(ExitCodes.Ok);
        string fixedCode = await File.ReadAllTextAsync(code, cancellationToken);
        fixedCode.ShouldContain("1.ShouldBe(1);");
        fixedCode.Split("using Shouldly;").Length.ShouldBe(2);
        (await File.ReadAllTextAsync(project, cancellationToken)).Split("Include=\"Shouldly\"").Length.ShouldBe(2);
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

    private static async Task DotnetAsync(CancellationToken cancellationToken, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in arguments)
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
