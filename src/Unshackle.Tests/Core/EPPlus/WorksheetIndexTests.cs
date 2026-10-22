using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.EPPlus.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// TAAK-309 queue row 3 (E3): a worksheet index is rewritten only for EPPlus 5+ (0-based); 4.x is not provable.
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

    // The harness fact behind the 4.x rule: EPPlus 4.5.3.3's .NET Standard build (the one .NET Core loads) is 0-based by default.
    [Fact]
    public void Harness_EPPlus4OnNetStandard_IsZeroBased()
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
    public void Rewrite_EPPlus5Index_ZeroBasedToPosition(string index, string expected)
    {
        // Arrange
        (SyntaxNode node, SemanticModel model) = Parse(Code.Replace("INDEX", index, StringComparison.Ordinal), [.. TestReferences.Net, s_epplus5]);

        // Act
        PatternResult result = new PackagePattern().Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes.ShouldHaveSingleItem().ToString().ShouldContain(expected);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("-1")]
    public void Rewrite_EPPlus5IndexBeyondAddedSheets_Manual(string index)
    {
        // Arrange
        (SyntaxNode node, SemanticModel model) = Parse(Code.Replace("INDEX", index, StringComparison.Ordinal), [.. TestReferences.Net, s_epplus5]);

        // Act
        PatternResult result = new PackagePattern().Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("is used as");
    }

    [Fact]
    public void Rewrite_NoEPPlusReference_UnknownVersion()
    {
        // Arrange: found by name (E2), but no assembly tells the version.
        (SyntaxNode node, SemanticModel model) = Parse(Code.Replace("INDEX", "0", StringComparison.Ordinal), TestReferences.Net);

        // Act
        PatternResult result = new PackagePattern().Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldStartWith("[UnknownEPPlusVersion]");
    }

    private static (SyntaxNode Node, SemanticModel Model) Parse(string code, IEnumerable<MetadataReference> references)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], references).GetSemanticModel(tree);
        return (new PackagePattern().Find(tree.GetRoot(TestContext.Current.CancellationToken), model).ShouldHaveSingleItem(), model);
    }

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
