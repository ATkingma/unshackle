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
        sheet.Cell("B2").Value = 1;
        sheet.Cell("D5").Value = 2;
        result.Cell("A1").Value = sheet.LastRowUsed(XLCellsUsedOptions.All).RowNumber();
        result.Cell("A2").Value = sheet.LastColumnUsed(XLCellsUsedOptions.All).ColumnNumber();
        result.Cell("A3").Value = sheet.FirstRowUsed(XLCellsUsedOptions.All).RowNumber();
        result.Cell("A4").Value = sheet.FirstColumnUsed(XLCellsUsedOptions.All).ColumnNumber();
        package.SaveAs(path);
    }
}
