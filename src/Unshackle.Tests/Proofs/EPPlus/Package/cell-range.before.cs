using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Orders");
        sheet.Cells[1, 1, 2, 2].Value = "x";
        sheet.Cells["A3:C4"].Value = 7;
        package.SaveAs(new FileInfo(path));
    }
}
