using Shouldly;
using Xunit;

namespace Unshackle.Tests.Core.EPPlus;

// Queue row 6 (epplus.save): what saving to a stream does to that stream in each library, and that GetAsByteArray equals SaveAs to a MemoryStream.
public class SaveHarnessTests
{
    // The stream's state after SaveAs is visible to the code that follows (it usually rewinds and returns or copies it).
    [Fact]
    public void SaveAsStream_StreamStateAfterwards_Same()
    {
        // Arrange
        using var epplusStream = new MemoryStream();
        using var closedXmlStream = new MemoryStream();

        // Act
        using (var package = new OfficeOpenXml.ExcelPackage())
        {
            package.Workbook.Worksheets.Add("Data").Cells[1, 1].Value = "x";
            package.SaveAs(epplusStream);
        }

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            workbook.Worksheets.Add("Data").Cell(1, 1).Value = "x";
            workbook.SaveAs(closedXmlStream);
        }

        // Assert
        (closedXmlStream.CanRead, closedXmlStream.Position == closedXmlStream.Length)
            .ShouldBe((epplusStream.CanRead, epplusStream.Position == epplusStream.Length));
    }

    [Fact]
    public void GetAsByteArray_SameWorkbookAsSaveAsMemoryStream()
    {
        // Arrange
        const string before = """
            using System.IO;
            using OfficeOpenXml;

            public static class Report
            {
                public static void Write(string path)
                {
                    using var package = new ExcelPackage();
                    var sheet = package.Workbook.Worksheets.Add("Orders");
                    sheet.Cells[1, 1].Value = "Name";
                    File.WriteAllBytes(path, package.GetAsByteArray());
                }
            }
            """;
        const string after = """
            using System.IO;
            using ClosedXML.Excel;

            public static class Report
            {
                public static void Write(string path)
                {
                    using var package = new XLWorkbook();
                    var sheet = package.Worksheets.Add("Orders");
                    sheet.Cell(1, 1).Value = "Name";
                    using var workbookStream = new MemoryStream();
                    package.SaveAs(workbookStream);
                    File.WriteAllBytes(path, workbookStream.ToArray());
                }
            }
            """;

        // Act
        IReadOnlyList<string> differences = CellCompare.Differences(before, after);

        // Assert
        differences.ShouldBeEmpty(string.Join(Environment.NewLine, differences));
    }
}
