using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Shouldly;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EPPlus;
using Unshackle.Core.Fixes.EPPlus.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// TAAK-309 queue row 3; council round 8 E3 (5+: 0-based) and round 11 E10/E11 (4.x by target framework family; the IsWorksheets1Based override).
public class WorksheetIndexTests
{
    // A minimal EPPlus 5 API surface with assembly version 5.8.0.0, so the pattern sees a 5+ reference (the real test types are 4.5.3.3, E2).
    private static readonly MetadataReference s_epplus5 = CompileEPPlus5();

    private const string Code = """
        using System.IO;
        using OfficeOpenXml;

        public static class Report
        {
            public static void Write(string path)
            {
                using var package = new ExcelPackage();
                package.Workbook.Worksheets.Add("Orders");
                package.Workbook.Worksheets.Add("Customers");
                package.Workbook.Worksheets[INDEX].Name = "Clients";
                package.SaveAs(new FileInfo(path));
            }
        }
        """;

    // The harness fact behind E10: EPPlus 4.5.3.3's .NET Standard build (the one .NET Core loads) is 0-based by default.
    [Fact]
    public void Harness_EPPlus4OnNetCore_IsZeroBased()
    {
        // Arrange
        string before = Code.Replace("INDEX", "1", StringComparison.Ordinal);
        string after = """
            using ClosedXML.Excel;

            public static class Report
            {
                public static void Write(string path)
                {
                    using var package = new XLWorkbook();
                    package.Worksheets.Add("Orders");
                    package.Worksheets.Add("Customers");
                    package.Worksheet(2).Name = "Clients";
                    package.SaveAs(path);
                }
            }
            """;

        // Act
        IReadOnlyList<string> differences = CellCompare.Differences(before, after);

        // Assert
        differences.ShouldBeEmpty(string.Join(Environment.NewLine, differences));
    }

    [Theory]
    [InlineData("0", "package.Worksheet(1).Name")]
    [InlineData("1", "package.Worksheet(2).Name")]
    public async Task RewriteAsync_EPPlus5Index_ZeroBasedToPosition(string index, string expected)
    {
        // Arrange, Act
        PatternResult result = await RewriteAsync(index, s_epplus5, ["NETCOREAPP"]);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        fixedResult.NewNodes.ShouldHaveSingleItem().ToString().ShouldContain(expected);
        fixedResult.Info.ShouldBe(WorksheetIndexBase.Checked);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("-1")]
    public async Task RewriteAsync_EPPlus5IndexBeyondAddedSheets_Manual(string index)
    {
        // Arrange, Act
        PatternResult result = await RewriteAsync(index, s_epplus5, ["NETCOREAPP"]);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("is used as");
    }

    // E10: EPPlus 4.x, a non-packable project of one family: net4x 1-based, netcore 0-based.
    [Theory]
    [InlineData("NETFRAMEWORK", "1", "package.Worksheet(1).Name")]
    [InlineData("NETCOREAPP", "1", "package.Worksheet(2).Name")]
    public async Task RewriteAsync_EPPlus4OneFamily_ByFamily(string family, string index, string expected)
    {
        // Arrange, Act
        PatternResult result = await RewriteAsync(index, null, [family]);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes.ShouldHaveSingleItem().ToString().ShouldContain(expected);
    }

    // E10: netstandard (the loading framework decides), unknown, packable, mixed TFMs, and a dependent of another family are Manual.
    [Theory]
    [InlineData("netstandard", "NETSTANDARD", null, false, null)]
    [InlineData("unknown", "", null, false, null)]
    [InlineData("packable", "NETCOREAPP", null, true, null)]
    [InlineData("mixed", "NETCOREAPP", "NETFRAMEWORK", false, null)]
    [InlineData("depends on it targets", "NETCOREAPP", null, false, "NETFRAMEWORK")]
    public async Task RewriteAsync_EPPlus4NotProvable_Manual(string expected, string family, string? secondTarget, bool packable, string? dependentFamily)
    {
        // Arrange, Act
        PatternResult result = await RewriteAsync("1", null, [family], secondTarget, packable, dependentFamily);

        // Assert
        ManualResult manual = result.ShouldBeOfType<ManualResult>();
        manual.Code.ShouldBe(nameof(EPPlusReason.WorksheetIndexBaseNotProvable));
        manual.Reason.ShouldContain(expected);
    }

    // E11: the override in a config file or in code blocks every index, also on 5+, and names where it is.
    [Fact]
    public async Task RewriteAsync_KeyInConfig_WorksheetIndexConfigured()
    {
        // Arrange, Act
        PatternResult result = await RewriteAsync("0", s_epplus5, ["NETCOREAPP"], configHits: ["appsettings.json:4"]);

        // Assert
        ManualResult manual = result.ShouldBeOfType<ManualResult>();
        manual.Code.ShouldBe(nameof(EPPlusReason.WorksheetIndexConfigured));
        manual.Reason.ShouldContain("appsettings.json:4");
        manual.Reason.ShouldContain("remove it to make the index fixable");
    }

    [Fact]
    public async Task RewriteAsync_KeyInCode_WorksheetIndexConfigured()
    {
        // Arrange, Act
        PatternResult result = await RewriteAsync("1", null, ["NETCOREAPP"], extraDocument: "static class Settings { const string K = \"EPPlus:ExcelPackage.Compatibility.IsWorksheets1Based\"; }");

        // Assert
        ManualResult manual = result.ShouldBeOfType<ManualResult>();
        manual.Code.ShouldBe(nameof(EPPlusReason.WorksheetIndexConfigured));
        manual.Reason.ShouldContain("Settings.cs:1");
    }

    [Fact]
    public async Task RewriteAsync_NoConfigSearch_WorksheetIndexConfigured()
    {
        // Arrange, Act
        PatternResult result = await RewriteAsync("1", null, ["NETCOREAPP"], noConfigSearch: true);

        // Assert
        result.ShouldBeOfType<ManualResult>().Code.ShouldBe(nameof(EPPlusReason.WorksheetIndexConfigured));
    }

    [Fact]
    public void Rewrite_WithoutSolution_NotProvable()
    {
        // Arrange
        SyntaxTree tree = CSharpSyntaxTree.ParseText(Code.Replace("INDEX", "0", StringComparison.Ordinal), cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], [.. TestReferences.Net, s_epplus5]).GetSemanticModel(tree);
        var pattern = new PackagePattern();
        SyntaxNode node = pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).ShouldHaveSingleItem();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Code.ShouldBe(nameof(EPPlusReason.WorksheetIndexBaseNotProvable));
    }

    private static async Task<PatternResult> RewriteAsync(
        string index, MetadataReference? epplus5, string[] symbols, string? secondTarget = null, bool packable = false, string? dependentFamily = null,
        string[]? configHits = null, string? extraDocument = null, bool noConfigSearch = false)
    {
        IReadOnlyList<MetadataReference> references = epplus5 is null ? EPPlusReferences.EPPlus : [.. TestReferences.Net, epplus5];
        Solution solution = new AdhocWorkspace().AddSolution(
            SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, Path.Combine(Path.GetTempPath(), "Shop", "Shop.sln")));
        string projectPath = Path.Combine(Path.GetTempPath(), "Shop", "Reports", "Reports.csproj");
        var reports = ProjectId.CreateNewId();
        solution = AddProject(solution, reports, "Reports", projectPath, symbols, references);
        var documentId = DocumentId.CreateNewId(reports);
        solution = solution.AddDocument(documentId, "Report.cs", Code.Replace("INDEX", index, StringComparison.Ordinal), filePath: "Report.cs");
        if (extraDocument is not null)
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(reports), "Settings.cs", extraDocument, filePath: "Settings.cs");
        }

        if (secondTarget is not null)
        {
            solution = AddProject(solution, ProjectId.CreateNewId(), "Reports(net48)", projectPath, [secondTarget], references);
        }

        if (dependentFamily is not null)
        {
            var app = ProjectId.CreateNewId();
            solution = AddProject(solution, app, "App", Path.Combine(Path.GetTempPath(), "Shop", "App", "App.csproj"), [dependentFamily], references)
                .AddProjectReference(app, new ProjectReference(reports));
        }

        IProjectProperties properties = Substitute.For<IProjectProperties>();
        properties.IsPackable(Arg.Any<string>()).Returns(packable);
        IConfigKeySearch search = Substitute.For<IConfigKeySearch>();
        search.Find(Arg.Any<string>(), Arg.Any<string>()).Returns(configHits ?? []);
        var pattern = new PackagePattern(properties, noConfigSearch ? null : search);
        Document document = solution.GetDocument(documentId)!;
        SemanticModel model = (await document.GetSemanticModelAsync(TestContext.Current.CancellationToken))!;
        SyntaxNode root = (await document.GetSyntaxRootAsync(TestContext.Current.CancellationToken))!;
        SyntaxNode node = pattern.Find(root, model).ShouldHaveSingleItem();
        return await pattern.RewriteAsync(node, model, solution, TestContext.Current.CancellationToken);
    }

    private static Solution AddProject(Solution solution, ProjectId id, string name, string path, string[] symbols, IEnumerable<MetadataReference> references) =>
        solution.AddProject(ProjectInfo.Create(id, VersionStamp.Default, name, name, LanguageNames.CSharp, filePath: path,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(preprocessorSymbols: [.. symbols.Where(s => s.Length > 0)]),
            metadataReferences: references));

    private static PortableExecutableReference CompileEPPlus5()
    {
        const string source = """
            [assembly: System.Reflection.AssemblyVersion("5.8.0.0")]

            namespace OfficeOpenXml
            {
                public class ExcelPackage : System.IDisposable
                {
                    public ExcelWorkbook Workbook { get; } = new ExcelWorkbook();
                    public void SaveAs(System.IO.FileInfo file) { }
                    public void Dispose() { }
                }

                public class ExcelWorkbook
                {
                    public ExcelWorksheets Worksheets { get; } = new ExcelWorksheets();
                }

                public class ExcelWorksheets
                {
                    public ExcelWorksheet Add(string name) => new ExcelWorksheet();
                    public ExcelWorksheet this[int position] => new ExcelWorksheet();
                    public ExcelWorksheet this[string name] => new ExcelWorksheet();
                }

                public class ExcelWorksheet
                {
                    public string Name { get; set; } = "";
                }
            }
            """;
        var compilation = CSharpCompilation.Create("EPPlus", [CSharpSyntaxTree.ParseText(source)], TestReferences.Net,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        compilation.Emit(stream).Success.ShouldBeTrue();
        return MetadataReference.CreateFromImage(stream.ToArray());
    }
}
