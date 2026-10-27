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
        var block = sheet.Range(2, 1, 4, 3);
        result.Cell("A1").Value = Size(sheet.Range(5, 4, 2, 3));
        result.Cell("A2").Value = Size(block);
        result.Cell("A3").Value = Size(sheet.Range("B2:D7"));
        package.SaveAs(path);
    }

    private static int Size(IXLRange range)
    {
        return range.RowCount() * 1000 + range.ColumnCount() * 100 + range.RangeAddress.FirstAddress.RowNumber * 10 + range.RangeAddress.LastAddress.ColumnNumber;
    }
}
