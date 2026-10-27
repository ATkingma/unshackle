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
        int last = 3;
        result.Cell("A1").Value = sheet.Range(5, 4, 2, 3).RowCount();
        result.Cell("A2").Value = sheet.Range(5, 4, 2, 3).RangeAddress.FirstAddress.RowNumber;
        result.Cell("A3").Value = sheet.Range(5, 4, 2, 3).RangeAddress.LastAddress.ColumnNumber;
        result.Cell("A4").Value = sheet.Range(2, 1, last, 1).RowCount();
        result.Cell("A5").Value = sheet.Range("B2:D7").ColumnCount();
        result.Cell("A6").Value = sheet.Range("B2:D7").RangeAddress.LastAddress.RowNumber;
        result.Cell("A7").Value = sheet.Range("B2:D7").RangeAddress.FirstAddress.ColumnNumber;
        package.SaveAs(path);
    }
}
