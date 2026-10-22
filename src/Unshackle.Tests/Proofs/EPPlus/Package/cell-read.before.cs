using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Orders");
        sheet.Cells[1, 1].Value = true;
        sheet.Cells[1, 2].Value = sheet.Cells[1, 1].Text;
        package.SaveAs(new FileInfo(path));
    }
}
