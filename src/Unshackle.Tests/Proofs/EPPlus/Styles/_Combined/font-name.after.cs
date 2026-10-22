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
        sheet.Cell("A1").Style.Font.FontName = "Courier New";
        package.SaveAs(path);
    }
}
