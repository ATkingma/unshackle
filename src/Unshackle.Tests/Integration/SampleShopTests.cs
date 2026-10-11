using System.Diagnostics;
using Shouldly;
using Unshackle.Core.Abstractions;
using Unshackle.DataAccess.Processes;
using Unshackle.DataAccess.Solutions;
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
        int fixExit = await Cli.RunAsync(["fix", solution, "--fix", "fluentassertions", "--force"], cancellationToken);
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
        int firstExit = await Cli.RunAsync(["fix", solution, "--fix", "fluentassertions", "--report", report, "--force"], cancellationToken);
        string[] firstRows = await File.ReadAllLinesAsync(report, cancellationToken);
        string afterFirst = await File.ReadAllTextAsync(testProject, cancellationToken);
        await DotnetAsync(cancellationToken, "build", solution);
        string code = await File.ReadAllTextAsync(repositoryTests, cancellationToken);
        await File.WriteAllTextAsync(
            repositoryTests,
            code.Replace("repository.All.Should().HaveCount(1);", "repository.All.Count.ShouldBe(1);", StringComparison.Ordinal),
            cancellationToken);
        int secondExit = await Cli.RunAsync(["fix", solution, "--fix", "fluentassertions", "--force"], cancellationToken);

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

    // mediatr is all-or-nothing: the generic CountQuery stays Manual, so the first fix changes nothing and the app still works.
    // After that is fixed by hand (in MediatR), the second fix migrates everything: no MediatR left, build and all tests green.
    [Fact]
    public async Task FixMediatRTwice_SampleShopCopy_FirstChangesNothingThenMigratesAndBuildsAndTests()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string samples = Path.Combine(directory.Path, "my samples");
        CopyWithoutBuildOutput(Path.Combine(RepositoryRoot.Path, "samples"), samples);
        File.Copy(Path.Combine(RepositoryRoot.Path, "global.json"), Path.Combine(directory.Path, "global.json"));
        string shop = Path.Combine(samples, "SampleShop");
        string solution = Path.Combine(shop, "SampleShop.slnx");
        string registration = Path.Combine(shop, "SampleShop", "ServiceCollectionExtensions.cs");
        string project = Path.Combine(shop, "SampleShop", "SampleShop.csproj");
        string firstReport = Path.Combine(directory.Path, "first.csv");
        string secondReport = Path.Combine(directory.Path, "second.csv");
        await DotnetAsync(cancellationToken, "restore", solution);
        string originalRegistration = await File.ReadAllTextAsync(registration, cancellationToken);

        // Act
        int firstExit = await Cli.RunAsync(["fix", solution, "--fix", "mediatr", "--report", firstReport, "--force"], cancellationToken);
        string[] firstRows = await File.ReadAllLinesAsync(firstReport, cancellationToken);
        string afterFirst = await File.ReadAllTextAsync(registration, cancellationToken);
        await DotnetAsync(cancellationToken, "build", solution);
        await DotnetAsync(cancellationToken, "test", "--solution", solution);
        await FixCountQueryByHandAsync(shop, cancellationToken);
        int secondExit = await Cli.RunAsync(["fix", solution, "--fix", "mediatr", "--report", secondReport, "--force"], cancellationToken);
        string[] secondRows = await File.ReadAllLinesAsync(secondReport, cancellationToken);

        // Assert
        firstExit.ShouldBe(ExitCodes.Ok);
        firstRows.ShouldContain(r => r.StartsWith("SampleShop/Features/Common/CountQuery.cs,", StringComparison.Ordinal) && r.Contains("does not support generic messages", StringComparison.Ordinal));
        firstRows.ShouldNotContain(r => r.Contains(",Fixed,", StringComparison.Ordinal));
        afterFirst.ShouldBe(originalRegistration);

        secondExit.ShouldBe(ExitCodes.Ok);
        secondRows.ShouldNotContain(r => r.Contains(",Manual,", StringComparison.Ordinal), string.Join('\n', secondRows));
        string afterSecond = await File.ReadAllTextAsync(registration, cancellationToken);
        afterSecond.ShouldContain("services.AddMediator(");
        afterSecond.ShouldContain("cfg.StreamPipelineBehaviors = [typeof(StreamLoggingBehavior<,>)];");
        (await File.ReadAllTextAsync(project, cancellationToken)).ShouldNotContain("\"MediatR\"");
        (await File.ReadAllTextAsync(Path.Combine(samples, "Directory.Packages.props"), cancellationToken)).ShouldNotContain("\"MediatR\"");
        await DotnetAsync(cancellationToken, "build", solution);
        await DotnetAsync(cancellationToken, "test", "--solution", solution);
    }

    // What a user does with the Manual finding: one non-generic query per entity type, still on MediatR.
    private static async Task FixCountQueryByHandAsync(string shop, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(Path.Combine(shop, "SampleShop", "Features", "Common", "CountQuery.cs"), """
            using MediatR;
            using SampleShop.Data;

            namespace SampleShop.Features.Common;

            public record CountOrdersQuery() : IRequest<int>;

            public record CountCustomersQuery() : IRequest<int>;

            public class CountOrdersHandler : IRequestHandler<CountOrdersQuery, int>
            {
                private readonly OrderRepository _orders;

                public CountOrdersHandler(OrderRepository orders)
                {
                    _orders = orders;
                }

                public Task<int> Handle(CountOrdersQuery request, CancellationToken cancellationToken) => Task.FromResult(_orders.All.Count);
            }

            public class CountCustomersHandler : IRequestHandler<CountCustomersQuery, int>
            {
                private readonly CustomerRepository _customers;

                public CountCustomersHandler(CustomerRepository customers)
                {
                    _customers = customers;
                }

                public Task<int> Handle(CountCustomersQuery request, CancellationToken cancellationToken) => Task.FromResult(_customers.Customers.Count);
            }

            """, cancellationToken);
        foreach (string file in new[] { Path.Combine(shop, "SampleShop", "Features", "Orders", "OrderFacade.cs"), Path.Combine(shop, "SampleShop.Tests", "PipelineTests.cs") })
        {
            string code = await File.ReadAllTextAsync(file, cancellationToken);
            await File.WriteAllTextAsync(
                file,
                code.Replace("new CountQuery<Order>()", "new CountOrdersQuery()", StringComparison.Ordinal)
                    .Replace("new CountQuery<Customer>()", "new CountCustomersQuery()", StringComparison.Ordinal),
                cancellationToken);
        }
    }

    // Without restore nothing binds: scan would report 0 findings and fix would swap usings blindly.
    [Fact]
    public async Task Scan_NotRestored_FailsWithRestoreAdvice()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string project = Path.Combine(directory.Path, "NotRestored.csproj");
        await File.WriteAllTextAsync(project, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <NoWarn>$(NoWarn);NU1903</NoWarn>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="FluentAssertions" Version="7.2.2" />
              </ItemGroup>
            </Project>
            """, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "T.cs"), "using FluentAssertions; class T { }", cancellationToken);
        using var store = new MsBuildSolutionStore(new System.IO.Abstractions.FileSystem());

        // Act
        Task act() => store.LoadAsync(project, cancellationToken);

        // Assert
        SolutionLoadException ex = await Should.ThrowAsync<SolutionLoadException>(act);
        ex.Errors.ShouldContain(e => e.Contains("dotnet restore", StringComparison.Ordinal), string.Join(" | ", ex.Errors));
    }

    // NuGet audit (vulnerable AutoMapper 14) and a project missing on disk are load warnings, not failures.
    [Fact]
    public async Task Load_VulnerablePackageAndMissingProject_LoadsRestWithWarnings()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string project = Path.Combine(directory.Path, "Audited.csproj");
        string solution = Path.Combine(directory.Path, "Audited.slnx");
        await File.WriteAllTextAsync(project, """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="FluentAssertions" Version="7.2.2" />
                <PackageReference Include="AutoMapper" Version="14.0.0" />
              </ItemGroup>
            </Project>
            """, cancellationToken);
        await File.WriteAllTextAsync(solution, """
            <Solution>
              <Project Path="Audited.csproj" />
              <Project Path="Gone/Gone.csproj" />
            </Solution>
            """, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "T.cs"), "using FluentAssertions; class T { }", cancellationToken);
        await DotnetAsync(cancellationToken, "restore", project);
        using var store = new MsBuildSolutionStore(new System.IO.Abstractions.FileSystem());

        // Act
        Microsoft.CodeAnalysis.Solution loaded = await store.LoadAsync(solution, cancellationToken);

        // Assert
        loaded.Projects.ShouldHaveSingleItem().Name.ShouldBe("Audited");
        store.Warnings.ShouldContain(w => w.Contains("Gone.csproj", StringComparison.Ordinal), string.Join(" | ", store.Warnings));
    }

    // Bogus (TAAK-112): a build project the solution restore skips resolves not even System (simulated here without framework
    // references). With nothing depending on it, it is left out with a warning; the other projects still load.
    [Fact]
    public async Task Load_LeafProjectWithoutSystemNextToRestoredOne_LeavesItOutWithWarning()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var directory = new TempDirectory();
        string solution = Path.Combine(directory.Path, "Mixed.slnx");
        foreach ((string name, string property) in new[] { ("App", ""), ("Builder", "<DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>") })
        {
            Directory.CreateDirectory(Path.Combine(directory.Path, name));
            await File.WriteAllTextAsync(Path.Combine(directory.Path, name, name + ".csproj"), $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    {property}
                  </PropertyGroup>
                </Project>
                """, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directory.Path, name, "T.cs"), "using System; class T { }", cancellationToken);
            await DotnetAsync(cancellationToken, "restore", Path.Combine(directory.Path, name, name + ".csproj"));
        }

        await File.WriteAllTextAsync(solution, """
            <Solution>
              <Project Path="App/App.csproj" />
              <Project Path="Builder/Builder.csproj" />
            </Solution>
            """, cancellationToken);
        using var store = new MsBuildSolutionStore(new System.IO.Abstractions.FileSystem());

        // Act
        Microsoft.CodeAnalysis.Solution loaded = await store.LoadAsync(solution, cancellationToken);

        // Assert
        loaded.Projects.ShouldHaveSingleItem().Name.ShouldBe("App", string.Join(" | ", store.Warnings));
        store.Warnings.ShouldContain(w => w.StartsWith("Builder:", StringComparison.Ordinal), string.Join(" | ", store.Warnings));
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
        var startInfo = new ProcessStartInfo("dotnet");

        // Isolate the child from the outer dotnet test (TAAK-045): no MSBuild/testing-platform settings it leaks into the
        // testhost, and no reused MSBuild nodes or build server left over from the cold build of the repository.
        foreach (string name in startInfo.Environment.Keys.Where(IsInheritedBuildSetting).ToList())
        {
            startInfo.Environment.Remove(name);
        }

        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "false";
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        ProcessResult result = await ProcessRunner.RunAsync(startInfo, cancellationToken);
        result.ExitCode.ShouldBe(0, result.Output + result.Error);
    }

    private static bool IsInheritedBuildSetting(string name) =>
        name.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("_MSBuild", StringComparison.OrdinalIgnoreCase)
        || name.StartsWith("TESTINGPLATFORM_", StringComparison.OrdinalIgnoreCase);
}
