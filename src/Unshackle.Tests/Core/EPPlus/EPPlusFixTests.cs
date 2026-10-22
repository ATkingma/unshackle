using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Fixes.EPPlus.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// TAAK-309: the EPPlus fix skeleton and its first two patterns, through the real Fixer.
public class EPPlusFixTests
{
    [Fact]
    public void Fix_Metadata_SwapsEPPlusForClosedXml()
    {
        // Arrange
        var fix = new EPPlusFix();

        // Act
        PackageMigration? package = fix.Package;

        // Assert
        fix.Id.ShouldBe("epplus");
        fix.AllOrNothing.ShouldBeTrue();
        fix.RemovedNamespaces.ShouldBe(["OfficeOpenXml"]);
        package.ShouldNotBeNull();
        package.RemovedPackage.ShouldBe("EPPlus");
        package.AddedPackages.Select(p => (p.Name, p.Version)).ShouldBe([("ClosedXML", typeof(global::ClosedXML.Excel.XLWorkbook).Assembly.GetName().Version!.ToString(3))]);
        fix.Patterns.Select(p => p.Id).ShouldBe(["epplus.license-context", "epplus.package", "epplus.unsupported"]);
    }

    [Fact]
    public async Task Fix_LicenseLineAndPackage_BothRewrittenAndAfterCompiles()
    {
        // Arrange
        const string before = """
            using System.IO;
            using OfficeOpenXml;

            namespace Reports;

            public static class Report
            {
                public static void Write(string path)
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage())
                    {
                        package.Workbook.Worksheets.Add("Report");
                        package.SaveAs(new FileInfo(path));
                    }
                }
            }
            """;

        // Act
        (FixReport report, string after) = await FixAsync(before);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is FixedResult);
        report.Findings.Count.ShouldBe(2);
        after.ShouldNotContain("OfficeOpenXml");
        after.ShouldContain("new XLWorkbook()");
        TestSolution.CompileErrors(after, EPPlusReferences.ClosedXml).ShouldBeEmpty();
        CellCompare.Differences(before, after).ShouldBeEmpty();
    }

    [Fact]
    public async Task Fix_OwnTypesNamedLikeEPPlus_NothingFound()
    {
        // Arrange
        const string before = """
            namespace Reports;

            public enum LicenseContext { Free }

            public class ExcelPackage
            {
                public static LicenseContext LicenseContext { get; set; }
            }

            public static class Startup
            {
                public static void Configure()
                {
                    ExcelPackage.LicenseContext = LicenseContext.Free;
                    using var package = new System.IO.MemoryStream();
                    var own = new ExcelPackage();
                }
            }
            """;

        // Act
        (FixReport report, string after) = await FixAsync(before);

        // Assert
        report.Findings.ShouldBeEmpty();
        after.ShouldBe(before);
    }

    [Fact]
    public async Task Fix_TwoPackagesInOneBlock_Manual()
    {
        // Arrange
        const string before = """
            using System.IO;
            using OfficeOpenXml;

            public static class Report
            {
                public static void Write(string first, string second)
                {
                    using var a = new ExcelPackage();
                    using var b = new ExcelPackage();
                    a.Workbook.Worksheets.Add("A");
                    b.Workbook.Worksheets.Add("B");
                    a.SaveAs(new FileInfo(first));
                    b.SaveAs(new FileInfo(second));
                }
            }
            """;

        // Act
        (FixReport report, string after) = await FixAsync(before);

        // Assert
        report.Findings.ShouldAllBe(f => f.Result is ManualResult);
        report.Findings.Count(f => ((ManualResult)f.Result).Reason.Contains("Several ExcelPackage locals")).ShouldBe(2);
        after.ShouldBe(before);
    }

    [Fact]
    public async Task Fix_SaveAsStream_ManualAndSolutionUntouched()
    {
        // Arrange: AllOrNothing, so the licence line stays too.
        const string before = """
            using System.IO;
            using OfficeOpenXml;

            public static class Report
            {
                public static void Write(Stream stream)
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage())
                    {
                        package.Workbook.Worksheets.Add("Report");
                        package.SaveAs(stream);
                    }
                }
            }
            """;

        // Act
        (FixReport report, string after) = await FixAsync(before);

        // Assert
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains("package.SaveAs"));
        after.ShouldBe(before);
    }

    [Fact]
    public void LicenseContextPattern_UnresolvedExcelPackage_MatchedBySyntax()
    {
        // Arrange: E2, a 5+ licence line in a project whose model has no EPPlus at all still is found by name.
        const string code = """
            using OfficeOpenXml;

            public static class Startup
            {
                public static void Configure() { ExcelPackage.LicenseContext = LicenseContext.NonCommercial; }
            }
            """;
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.Net).GetSemanticModel(tree);
        var pattern = new LicenseContextPattern();

        // Act
        SyntaxNode[] found = [.. pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model)];

        // Assert
        found.ShouldHaveSingleItem();
        pattern.Rewrite(found[0], model).ShouldBeOfType<FixedResult>();
    }

    private static async Task<(FixReport Report, string After)> FixAsync(string code)
    {
        var fixer = new Fixer([new EPPlusFix()]);
        Solution solution = TestSolution.Create(code, EPPlusReferences.EPPlus);
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);
        return (report, await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void UnsupportedPattern_UnresolvedEPPlus5Member_NamedWithItsReceiver()
    {
        // Arrange: E2, ToCollection is EPPlus 7; against 4.5.3.3 only its receiver resolves.
        const string code = """
            using OfficeOpenXml;

            internal static class Report
            {
                internal static object Read(ExcelWorksheet sheet) => sheet.Cells.ToCollection<string>();
            }
            """;
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], EPPlusReferences.EPPlus).GetSemanticModel(tree);
        var pattern = new UnsupportedPattern();

        // Act
        SyntaxNode found = pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).ShouldHaveSingleItem();
        PatternResult result = pattern.Rewrite(found, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("ExcelRange.ToCollection (unresolved, EPPlus 5+)");
    }

    [Fact]
    public async Task UnsupportedPattern_PublicApiInTestProject_NotE6()
    {
        // Arrange: a project referencing a test framework is not packable, so a public signature is an ordinary Manual row.
        const string code = """
            using OfficeOpenXml;

            public static class SheetFactory
            {
                public static ExcelWorksheet Create(ExcelPackage package) => package.Workbook.Worksheets.Add("A");
            }
            """;
        var fixer = new Fixer([new EPPlusFix()]);
        Solution solution = TestSolution.Create(code,
            [.. EPPlusReferences.EPPlus, MetadataReference.CreateFromFile(typeof(Xunit.FactAttribute).Assembly.Location)]);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "epplus" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.StartsWith("[UnmigratedEPPlusApi]", StringComparison.Ordinal));
        report.Findings.ShouldNotContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.Contains("PublicApiExposesOldLibrary"));
    }
}
