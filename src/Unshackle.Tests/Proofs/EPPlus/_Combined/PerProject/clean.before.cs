using System.IO;
using OfficeOpenXml;

namespace Clean;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Orders");
        sheet.Cells[1, 1].Value = "Name";
        sheet.Cells[1, 2].Value = 3;
        package.SaveAs(new FileInfo(path));
    }
}
