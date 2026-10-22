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
        int? last = sheet.LastRowUsed(XLCellsUsedOptions.All)?.RowNumber();
        result.Cell("A1").Value = last ?? -1;
        package.SaveAs(path);
    }
}
