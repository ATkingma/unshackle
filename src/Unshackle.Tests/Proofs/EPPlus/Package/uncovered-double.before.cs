using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Orders");
        double total = 0.1 + 0.2;
        sheet.Cells[1, 1].Value = total;
        package.SaveAs(new FileInfo(path));
    }
}
