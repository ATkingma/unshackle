using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new XLWorkbook();
        var sheet = package.Worksheets.Add("Orders");
        sheet.Range(1, 1, 2, 2).Value = "x";
        sheet.Range("A3:C4").Value = 7;
        package.SaveAs(path);
    }
}
