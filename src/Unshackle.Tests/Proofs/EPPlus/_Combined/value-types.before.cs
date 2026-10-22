using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Data");
        short small = -7;
        byte tiny = 255;
        sheet.Cells[1, 1].Value = "Widget";
        sheet.Cells[1, 2].Value = string.Empty;
        sheet.Cells[2, 1].Value = int.MinValue;
        sheet.Cells[2, 2].Value = small;
        sheet.Cells[2, 3].Value = tiny;
        sheet.Cells[3, 1].Value = 12.5;
        sheet.Cells[3, 2].Value = 1e300;
        sheet.Cells[3, 3].Value = 2.25m;
        sheet.Cells[3, 4].Value = 1.5f;
        sheet.Cells[3, 5].Value = 1234567890123L;
        sheet.Cells[4, 1].Value = true;
        sheet.Cells[4, 2].Value = false;
        package.SaveAs(new FileInfo(path));
    }
}
