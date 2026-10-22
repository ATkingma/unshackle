using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new XLWorkbook();
        var sheet = package.Worksheets.Add("S");
        sheet.Cell("A1").Value = "x";
        sheet.Cell("B3").Value = 2;
        sheet.Range("A1:B2").Merge();
        package.SaveAs(path);
    }
}
