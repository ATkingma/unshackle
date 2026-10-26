using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// TAAK-326, council round 13 E17: EPPlus migrates per project; a blocked project stays whole on EPPlus, the rest moves.
public class EPPlusPerProjectTests
{
    private const string Clean = """
        using OfficeOpenXml;

        public static class Startup
        {
            public static void Configure() { ExcelPackage.LicenseContext = LicenseContext.NonCommercial; }
        }
        """;

    private const string Blocked = """
        using OfficeOpenXml;

        public static class Report
        {
            public static void Write()
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (var package = new ExcelPackage())
                {
                    package.Workbook.Worksheets.Add("Report");
                    package.Save();
                }
            }
        }
        """;

    [Fact]
    public async Task FixAsync_OneCleanOneBlockedProject_CleanMigratesBlockedStaysWhole()
    {
        // Arrange
        Solution solution = Create(("Clean", [], Clean), ("Blocked", [], Blocked));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report, "Clean")).ShouldNotContain("LicenseContext");
        (await TextAsync(report, "Blocked")).ShouldBe(Blocked);
        Finding kept = report.Findings.Where(f => f.PatternId == "epplus.kept").ShouldHaveSingleItem();
        kept.FilePath.ShouldBe("Blocked.csproj");
        ManualResult manual = kept.Result.ShouldBeOfType<ManualResult>();
        manual.Kept.ShouldBe(KeptReason.EPPlusProjectKept);
        KeptReasonText.Rendered(manual).ShouldStartWith(KeptReasonText.EPPlusPrefix);
        report.Findings.ShouldContain(f => f.FilePath == "Blocked.cs" && f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains("package.Save"));
        report.Findings.ShouldNotContain(f => f.FilePath == "Clean.cs" && f.Result is ManualResult);
    }

    [Fact]
    public async Task FixAsync_EPPlusTypeCrossesIntoKeptProject_UserStaysWithReason()
    {
        // Arrange: Lib is kept (a public EPPlus signature); App calls it with an ExcelPackage, so App stays too; Other moves.
        const string lib = """
            using OfficeOpenXml;

            public static class Sheets
            {
                public static ExcelWorksheet First(ExcelPackage package) => package.Workbook.Worksheets.Add("A");
            }
            """;
        const string app = """
            using OfficeOpenXml;

            public static class Program
            {
                public static void Run()
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    Sheets.First(null!);
                }
            }
            """;
        Solution solution = Create(("Lib", [], lib), ("App", ["Lib"], app), ("Other", [], Clean));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report, "App")).ShouldBe(app);
        (await TextAsync(report, "Lib")).ShouldBe(lib);
        (await TextAsync(report, "Other")).ShouldNotContain("LicenseContext");
        Finding crossing = report.Findings.Where(f => f.PatternId == "epplus.kept" && f.FilePath == "App.csproj").ShouldHaveSingleItem();
        crossing.Result.ShouldBeOfType<ManualResult>().Code.ShouldBe(nameof(EPPlusReason.EPPlusTypeCrossesProject));
    }

    [Fact]
    public async Task FixAsync_EveryProjectBlocked_SolutionUntouched()
    {
        // Arrange
        Solution solution = Create(("Blocked", [], Blocked));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report, "Blocked")).ShouldBe(Blocked);
        report.Findings.ShouldNotContain(f => f.PatternId == "epplus.kept");
        report.Findings.ShouldNotContain(f => f.Result is FixedResult);
    }

    private static async Task<FixReport> FixAsync(Solution solution) =>
        await new Fixer([new EPPlusFix()]).FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);

    private static async Task<string> TextAsync(FixReport report, string project) =>
        (await report.Solution.Projects.Single(p => p.Name == project).Documents.Single().GetTextAsync(TestContext.Current.CancellationToken)).ToString();

    // Projects with a file path (the per-project plan groups by it), each with one document.
    private static Solution Create(params (string Name, string[] References, string Code)[] projects)
    {
        Solution solution = TestSolution.NewEmpty();
        Dictionary<string, ProjectId> ids = new(StringComparer.Ordinal);
        foreach ((string name, string[] references, string code) in projects)
        {
            var id = ProjectId.CreateNewId();
            ids[name] = id;
            solution = solution
                .AddProject(ProjectInfo.Create(id, VersionStamp.Default, name, name, LanguageNames.CSharp, filePath: name + ".csproj"))
                .WithProjectCompilationOptions(id, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(id, EPPlusReferences.EPPlus)
                .AddProjectReferences(id, references.Select(r => new ProjectReference(ids[r])))
                .AddDocument(DocumentId.CreateNewId(id), name + ".cs", code, filePath: name + ".cs");
        }

        return solution;
    }
}
