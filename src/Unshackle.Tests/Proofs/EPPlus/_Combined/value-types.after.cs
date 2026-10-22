using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new XLWorkbook();
        var sheet = package.Worksheets.Add("Data");
        short small = -7;
        byte tiny = 255;
        sheet.Cell(1, 1).Value = "Widget";
        sheet.Cell(1, 2).Value = string.Empty;
        sheet.Cell(2, 1).Value = int.MinValue;
        sheet.Cell(2, 2).Value = small;
        sheet.Cell(2, 3).Value = tiny;
        sheet.Cell(3, 1).Value = 12.5;
        sheet.Cell(3, 2).Value = 1e300;
        sheet.Cell(3, 3).Value = 2.25m;
        sheet.Cell(3, 4).Value = 1.5f;
        sheet.Cell(3, 5).Value = 1234567890123L;
        sheet.Cell(4, 1).Value = true;
        sheet.Cell(4, 2).Value = false;
        package.SaveAs(path);
    }
}
