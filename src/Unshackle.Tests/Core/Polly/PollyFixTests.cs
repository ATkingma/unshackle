using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using NSubstitute;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Polly;

public class PollyFixTests
{
    [Fact]
    public void Package_RemovesEveryPollyV7Package_AddsResilience()
    {
        // Arrange
        PollyFix fix = new();

        // Act
        string[] removed = [fix.Package.RemovedPackage, .. fix.Package.AlsoRemovedPackages];

        // Assert
        removed.ShouldBe(["Polly", "Polly.Extensions.Http", "Microsoft.Extensions.Http.Polly"]);
        fix.Package.AddedPackages.Select(p => p.Name).ShouldBe(["Microsoft.Extensions.Resilience", "Microsoft.Extensions.Http.Resilience"]);
        fix.Package.AddedPackages[0].OnlyWithCallTo.ShouldBeEmpty();
        fix.Package.AddedPackages[1].OnlyWithCallTo.ShouldBe(["AddPolicyHandler", "AddTransientHttpErrorPolicy"]);
    }

    [Fact]
    public void ResilienceVersion_MatchesTheVersionTheProofsCompileAgainst()
    {
        // Arrange
        string props = File.ReadAllText(Path.Combine(Helpers.RepositoryRoot.Path, "Directory.Packages.props"));

        // Act
        bool pinned = props.Contains($"Include=\"Microsoft.Extensions.Http.Resilience\" Version=\"{PollyFix.ResilienceVersion}\"", StringComparison.Ordinal);

        // Assert
        pinned.ShouldBeTrue();
    }

    [Fact]
    public void Patterns_IdsUsePollyPrefix()
    {
        // Arrange
        PollyFix fix = new();

        // Act
        List<string> ids = [.. fix.Patterns.Select(p => p.Id)];

        // Assert
        ids.ShouldAllBe(id => id.StartsWith("polly.", StringComparison.Ordinal));
        fix.Id.ShouldBe("polly");
    }

    [Theory]
    [InlineData("NETCOREAPP;NETCOREAPP3_0", true, true)]
    [InlineData("NETFRAMEWORK;NET452", true, true)]
    [InlineData("NETCOREAPP;NET8_0_OR_GREATER", true, false)]
    [InlineData("NETFRAMEWORK;NET462_OR_GREATER", true, false)]
    [InlineData("NETCOREAPP;NETCOREAPP3_0", false, false)]
    public async Task ProjectBlockers_UnsupportedFrameworkWithPollyV7_BlocksTheProject(string symbols, bool referencesPolly, bool blocked)
    {
        // Arrange (council round 10, P9/G4)
        Project project = CreateProject(symbols.Split(';'), referencesPolly ? PollyReferences.Before : PollyReferences.After);

        // Act
        IReadOnlyList<ManualResult> blockers = await new PollyFix().ProjectBlockersAsync(project, TestContext.Current.CancellationToken);

        // Assert
        if (blocked)
        {
            blockers.ShouldHaveSingleItem().Code.ShouldBe(nameof(ProjectBlockingReason.TargetFrameworkUnsupported));
        }
        else
        {
            blockers.ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EditsFor_PollyV7Project_AddsResilienceAndRemovesBundledPollyCore(bool referencesPolly)
    {
        // Arrange (council round 10, P8)
        Project project = CreateProject(["NETCOREAPP", "NET8_0_OR_GREATER"], referencesPolly ? PollyReferences.Before : PollyReferences.After);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = await new PollyFix().EditsForAsync(project, TestContext.Current.CancellationToken);

        // Assert
        if (!referencesPolly)
        {
            edits.ShouldBeEmpty();
            return;
        }

        edits.Count.ShouldBe(2);
        edits[0].ShouldBeOfType<PackageAddEdit>().Name.ShouldBe("Microsoft.Extensions.Resilience");
        PackageRemoveEdit remove = edits[1].ShouldBeOfType<PackageRemoveEdit>();
        remove.Name.ShouldBe("Polly.Core");
        remove.AtMostVersion.ShouldBe("8.4.2");
    }

    [Theory]
    [InlineData("NETCOREAPP;NETCOREAPP3_0", true)]
    [InlineData("NETCOREAPP;NET8_0_OR_GREATER", false)]
    public async Task FixAsync_UnsupportedFramework_LeavesTheCodeOnPollyV7(string symbols, bool blocked)
    {
        // Arrange (council round 10, P9; TAAK-320: the project blocker alone keeps the documents unrewritten)
        const string code = "using System;\nusing System.Threading.Tasks;\nusing Polly;\n"
            +"public class W { public async Task M() { await Policy.Handle<TimeoutException>().RetryAsync(1).ExecuteAsync(() => Task.CompletedTask); } }";
        var projectId = ProjectId.CreateNewId();
        Solution solution = TestSolution.NewEmpty()
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "P", "P", LanguageNames.CSharp, filePath: "P.csproj", compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                parseOptions: new CSharpParseOptions(preprocessorSymbols: symbols.Split(';')), metadataReferences: PollyReferences.Before))
            .AddDocument(DocumentId.CreateNewId(projectId), "W.cs", code, filePath: "W.cs");

        // Act
        FixReport report = await new Fixer([new PollyFix()]).FixAsync(solution, new FixRequest { FixId = "polly" }, TestContext.Current.CancellationToken);

        // Assert
        string text = (await report.Solution.Projects.Single().Documents.Single().GetTextAsync(TestContext.Current.CancellationToken)).ToString();
        if (blocked)
        {
            text.ShouldBe(code);
            report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Code == nameof(ProjectBlockingReason.TargetFrameworkUnsupported));
            report.Findings.ShouldNotContain(f => f.Result is FixedResult);
        }
        else
        {
            text.ShouldContain("new ResiliencePipelineBuilder()", Case.Sensitive, string.Join(" | ", report.Findings.Select(f => f.PatternId + " " + f.Result)));
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FixAsync_KeptPollyProject_RowNamesResilienceOnlyWhenTheEditAddedIt(bool added)
    {
        // Arrange (council round 22, P11): Polly stays (an unmigrated IAsyncPolicy); the kept row claims the add only when the project-file edit made it.
        const string code = "using Polly;\npublic class W { public IAsyncPolicy Policy { get; set; } = null!; }";
        IProjectFileStore projectFiles = Substitute.For<IProjectFileStore>();
        projectFiles.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>()).Returns(
        [
            new ProjectFileChange
            {
                FilePath = "P.csproj",
                OldText = "<PackageReference Include=\"Polly\" />",
                NewText = added ? "<PackageReference Include=\"Polly\" /><PackageReference Include=\"Microsoft.Extensions.Resilience\" />" : "<PackageReference Include=\"Polly\" /> ",
            },
        ]);
        var projectId = ProjectId.CreateNewId();
        Solution solution = TestSolution.NewEmpty()
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "P", "P", LanguageNames.CSharp, filePath: "P.csproj", compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                parseOptions: new CSharpParseOptions(preprocessorSymbols: ["NETCOREAPP", "NET8_0_OR_GREATER"]), metadataReferences: PollyReferences.Before))
            .AddDocument(DocumentId.CreateNewId(projectId), "W.cs", code, filePath: "W.cs");

        // Act
        FixReport report = await new Fixer([new PollyFix()], projectFiles).FixAsync(solution, new FixRequest { FixId = "polly" }, TestContext.Current.CancellationToken);

        // Assert
        Finding row = report.Findings.Single(f => f.PatternId == Fixer.ProjectFilePatternId && f.FilePath == "P.csproj");
        ManualResult kept = row.Result.ShouldBeOfType<ManualResult>(string.Join(" | ", report.Findings.Select(f => f.PatternId + " " + f.Result)));
        kept.Reason.ShouldBe(added
            ? "Polly is still used in this project, so its package and global usings stay; Microsoft.Extensions.Resilience was added next to it. Fix the manual findings and run the fix again."
            : "Polly is still used in this project, so its package and global usings stay. Fix the manual findings and run the fix again.");
        PartialRun.IsKeptPackageRow(row).ShouldBeTrue();
    }

    [Fact]
    public async Task FixAsync_PackageDowngradeBlocked_NoRowClaimsTheAdd()
    {
        // Arrange (council round 22, P11): a blocked project gets no project-file edit, so no row says Resilience was added.
        const string code = "using Polly;\npublic class W { public IAsyncPolicy Policy { get; set; } = null!; }";
        IProjectFileStore projectFiles = Substitute.For<IProjectFileStore>();
        projectFiles.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>()).Returns([]);
        string old = Path.Combine(Path.GetTempPath(), "packages", "microsoft.extensions.options", "2.1.0", "lib", "netstandard2.0", "Microsoft.Extensions.Options.dll");
        var projectId = ProjectId.CreateNewId();
        Solution solution = TestSolution.NewEmpty()
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "P", "P", LanguageNames.CSharp, filePath: "P.csproj", compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                parseOptions: new CSharpParseOptions(preprocessorSymbols: ["NETCOREAPP", "NET8_0_OR_GREATER"]),
                metadataReferences: [.. PollyReferences.Before, MetadataReference.CreateFromImage(File.ReadAllBytes(typeof(PollyFixTests).Assembly.Location), filePath: old)]))
            .AddDocument(DocumentId.CreateNewId(projectId), "W.cs", code, filePath: "W.cs");

        // Act
        FixReport report = await new Fixer([new PollyFix()], projectFiles).FixAsync(solution, new FixRequest { FixId = "polly" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Code == nameof(ProjectBlockingReason.PackageDowngrade));
        report.Findings.ShouldNotContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains("added next to it", StringComparison.Ordinal));
    }

    internal static Project CreateProject(string[] symbols, IEnumerable<MetadataReference> references)
    {
        var projectId = ProjectId.CreateNewId();
        return TestSolution.NewEmpty()
            .AddProject(projectId, "P", "P", LanguageNames.CSharp)
            .WithProjectParseOptions(projectId, new CSharpParseOptions(preprocessorSymbols: symbols))
            .AddMetadataReferences(projectId, references)
            .GetProject(projectId)!;
    }
}
