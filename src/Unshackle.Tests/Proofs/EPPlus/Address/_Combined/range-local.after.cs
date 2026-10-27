using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new XLWorkbook();
        var sheet = package.Worksheets.Add("S");
        var result = package.Worksheets.Add("Result");
        var block = sheet.Range(5, 4, 2, 3);
        IXLRange table = sheet.Range("B2:D7");
        result.Cell("A1").Value = block.RowCount();
        result.Cell("A2").Value = block.RangeAddress.FirstAddress.RowNumber;
        result.Cell("A3").Value = block.RangeAddress.LastAddress.ColumnNumber;
        result.Cell("A4").Value = table.ColumnCount();
        result.Cell("A5").Value = table.RangeAddress.LastAddress.RowNumber;
        package.SaveAs(path);
    }
}
