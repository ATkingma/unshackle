using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Models;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// TAAK-326 (council round 13, E17): the EPPlus migration is per project. A clean project migrates while a blocked one stays on EPPlus.
public class EPPlusPerProjectTests
{
    private const string CleanPath = "/repo/Clean/Clean.csproj";
    private const string KeptPath = "/repo/Kept/Kept.csproj";

    private static readonly string s_proofs = Path.Combine(AppContext.BaseDirectory, "Proofs", "EPPlus", "_Combined", "PerProject");

    [Fact]
    public async Task FixAsync_CleanAndBlockedProject_MigratesOnlyTheCleanOne()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        (IProjectFileStore store, List<ProjectMigration> edited) = CreateStore();
        Solution solution = CreateSolution(Proof("clean.before.cs"), Proof("kept.before.cs"), keptReferencesClean: false);

        // Act
        FixReport report = await new Fixer([new EPPlusFix()], store).FixAsync(solution, new FixRequest { FixId = "epplus" }, cancellationToken);

        // Assert
        (await TextAsync(report.Solution, "Clean.cs", cancellationToken)).ShouldBe(Proof("clean.after.cs"));
        (await TextAsync(report.Solution, "Kept.cs", cancellationToken)).ShouldBe(Proof("kept.before.cs"));
        edited.Select(m => m.ProjectPath).ShouldBe([CleanPath]);
        edited.Single().KeepRemovedPackage.ShouldBeFalse();
        report.Findings.ShouldContain(f => f.FilePath == "/repo/Clean/Clean.cs" && f.Result is FixedResult);
        report.Findings.ShouldContain(f => f.FilePath == KeptPath && f.Result is ManualResult
            && ((ManualResult)f.Result).Reason == "EPPlus package kept: blocked by manual findings in this project. Resolve that first, then run the fix again.");
        report.Findings.ShouldNotContain(f => f.FilePath == "/repo/Clean/Clean.cs" && f.Result is ManualResult);
    }

    [Fact]
    public async Task FixAsync_EveryProjectBlocked_ChangesNothing()
    {
        // Arrange
        (IProjectFileStore store, _) = CreateStore();
        Solution solution = CreateSolution(Proof("kept.before.cs").Replace("class Kept", "class Other", StringComparison.Ordinal), Proof("kept.before.cs"), keptReferencesClean: false);

        // Act
        FixReport report = await new Fixer([new EPPlusFix()], store).FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);

        // Assert
        report.ProjectFileChanges.ShouldBeEmpty();
        report.Findings.ShouldNotContain(f => f.Result is FixedResult);
        report.Solution.ShouldBe(solution);
    }

    [Fact]
    public async Task PlanPartialAsync_KeptProjectUsesAnEPPlusTypedMember_KeepsTheDeclaringProject()
    {
        // Arrange
        const string clean = """
            using OfficeOpenXml;

            namespace Clean;

            public static class Factory
            {
                public static ExcelPackage Create() => new ExcelPackage();
            }
            """;
        const string kept = """
            namespace Kept;

            public static class Export
            {
                public static void Write() => Clean.Factory.Create().Save();
            }
            """;
        Solution solution = CreateSolution(clean, kept, keptReferencesClean: true);
        Finding root = Root("/repo/Kept/Kept.cs");

        // Act
        PartialPlan? plan = await new EPPlusFix().PlanPartialAsync(solution, [root], TestContext.Current.CancellationToken);

        // Assert: nothing is left to migrate, so the fix stays all-or-nothing.
        plan.ShouldBeNull();
    }

    [Fact]
    public async Task PlanPartialAsync_ThirdProjectExposesEPPlusToKeptOne_ReportsEpplusTypeCrossesProject()
    {
        // Arrange
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        const string clean = """
            namespace Clean;

            public static class Factory
            {
                public static OfficeOpenXml.ExcelPackage Create() => new OfficeOpenXml.ExcelPackage();
            }
            """;
        Solution solution = CreateSolution(clean, "namespace Kept; public static class Export { public static void Write() => Clean.Factory.Create().Save(); }", keptReferencesClean: true);
        solution = AddProject(solution, "/repo/Free/Free.csproj", "Free.cs", "namespace Free; public static class Unrelated { public static OfficeOpenXml.ExcelPackage New() => new(); }");
        IPackageAssetsReader assets = DeclaresEPPlus();

        // Act
        PartialPlan? plan = await new EPPlusFix(null, null, assets).PlanPartialAsync(solution, [Root("/repo/Kept/Kept.cs")], cancellationToken);

        // Assert
        plan.ShouldNotBeNull();
        plan.KeptProjects.Keys.Order(StringComparer.Ordinal).ShouldBe([CleanPath, KeptPath]);
        plan.KeptProjects[CleanPath].ShouldBe("EpplusTypeCrossesProject");
        ManualResult row = plan.Findings.Single(f => f.FilePath == CleanPath).Result.ShouldBeOfType<ManualResult>();
        row.Code.ShouldBe("EpplusTypeCrossesProject");
        row.Reason.ShouldContain("Clean.Factory.Create() names an EPPlus type, used by Kept");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PlanPartialAsync_KeptProjectReferencesCleanOne_MigratesOnlyWhenTheKeptOneDeclaresEPPlus(bool declares)
    {
        // Arrange
        Solution solution = CreateSolution(Proof("clean.before.cs"), Proof("kept.before.cs"), keptReferencesClean: true);
        IPackageAssetsReader? assets = declares ? DeclaresEPPlus() : null;

        // Act
        PartialPlan? plan = await new EPPlusFix(null, null, assets).PlanPartialAsync(solution, [Root("/repo/Kept/Kept.cs")], TestContext.Current.CancellationToken);

        // Assert
        if (declares)
        {
            plan.ShouldNotBeNull().KeptProjects.Keys.ShouldBe([KeptPath]);
        }
        else
        {
            plan.ShouldBeNull();
        }
    }

    [Fact]
    public async Task PlanPartialAsync_FileSharedWithKeptProject_KeepsBoth()
    {
        // Arrange
        Solution solution = CreateSolution(Proof("clean.before.cs"), Proof("kept.before.cs"), keptReferencesClean: false);
        ProjectId cleanId = solution.Projects.Single(p => p.FilePath == CleanPath).Id;
        solution = solution.AddDocument(DocumentId.CreateNewId(cleanId), "Kept.cs", Proof("kept.before.cs"), filePath: "/repo/Kept/Kept.cs");

        // Act
        PartialPlan? plan = await new EPPlusFix().PlanPartialAsync(solution, [Root("/repo/Kept/Kept.cs")], TestContext.Current.CancellationToken);

        // Assert
        plan.ShouldBeNull();
    }

    private static Finding Root(string path) =>
        new() { FilePath = path, Line = 9, FixId = "epplus", PatternId = "epplus.package", Result = PatternResult.Manual("kept") };

    private static IPackageAssetsReader DeclaresEPPlus()
    {
        IPackageAssetsReader assets = Substitute.For<IPackageAssetsReader>();
        assets.Read(Arg.Any<string>()).Returns(new PackageAssets
        {
            State = AssetsState.Current,
            Resolved = new Dictionary<string, string> { ["EPPlus"] = "4.5.3.3" },
            Requirements = [new PackageRequirement { Package = "EPPlus", RequiredBy = "project", Range = "4.5.3.3" }],
        });
        return assets;
    }

    private static string Proof(string name) => File.ReadAllText(Path.Combine(s_proofs, name)).ReplaceLineEndings("\n");

    private static async Task<string> TextAsync(Solution solution, string name, CancellationToken cancellationToken) =>
        (await solution.Projects.SelectMany(p => p.Documents).First(d => d.Name == name).GetTextAsync(cancellationToken)).ToString().ReplaceLineEndings("\n");

    private static (IProjectFileStore Store, List<ProjectMigration> Edited) CreateStore()
    {
        List<ProjectMigration> edited = [];
        IProjectFileStore store = Substitute.For<IProjectFileStore>();
        store.EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            // Each run plans its own edits (a first full run, then the partial one): the last call is what the report carries.
            edited.Clear();
            edited.AddRange(call.Arg<IEnumerable<ProjectMigration>>());
            return Task.FromResult<IReadOnlyList<ProjectFileChange>>([]);
        });
        return (store, edited);
    }

    private static Solution CreateSolution(string clean, string kept, bool keptReferencesClean)
    {
        Solution solution = AddProject(new AdhocWorkspace().CurrentSolution, CleanPath, "Clean.cs", clean);
        solution = AddProject(solution, KeptPath, "Kept.cs", kept);
        if (keptReferencesClean)
        {
            ProjectId cleanId = solution.Projects.Single(p => p.FilePath == CleanPath).Id;
            ProjectId keptId = solution.Projects.Single(p => p.FilePath == KeptPath).Id;
            solution = solution.AddProjectReference(keptId, new ProjectReference(cleanId));
        }

        return solution;
    }

    private static Solution AddProject(Solution solution, string projectPath, string fileName, string code)
    {
        var projectId = ProjectId.CreateNewId();
        string name = Path.GetFileNameWithoutExtension(projectPath);
        return solution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, name, name, LanguageNames.CSharp, filePath: projectPath)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithMetadataReferences(EPPlusReferences.EPPlus))
            .AddDocument(DocumentId.CreateNewId(projectId), fileName, code, filePath: Path.GetDirectoryName(projectPath)!.Replace('\\', '/') + "/" + fileName);
    }
}
