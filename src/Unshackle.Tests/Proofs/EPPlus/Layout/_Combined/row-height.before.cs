using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("S");
        sheet.Cells["A1"].Value = "x";
        sheet.Cells["B3"].Value = 2;
        sheet.Row(3).Height = 30;
        package.SaveAs(new FileInfo(path));
    }
}
