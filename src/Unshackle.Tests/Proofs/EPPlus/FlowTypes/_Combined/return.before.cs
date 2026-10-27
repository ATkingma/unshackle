using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using (var package = new ExcelPackage())
        {
            ExcelWorksheet sheet = AddSheet(package);
            sheet.Cells[1, 1].Value = "Name";
            sheet.Cells[2, 1].Value = sheet.Name;
            package.SaveAs(new FileInfo(path));
        }
    }

    private static ExcelWorksheet AddSheet(ExcelPackage package) => package.Workbook.Worksheets.Add("Report");
}
