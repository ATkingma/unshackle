using System.IO;
using ClosedXML.Excel;

namespace Clean;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new XLWorkbook();
        var sheet = package.Worksheets.Add("Orders");
        sheet.Cell(1, 1).Value = "Name";
        sheet.Cell(1, 2).Value = 3;
        package.SaveAs(path);
    }
}
