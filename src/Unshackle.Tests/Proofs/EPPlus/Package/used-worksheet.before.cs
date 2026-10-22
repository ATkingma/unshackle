using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using (var package = new ExcelPackage())
        {
            ExcelWorksheet sheet = package.Workbook.Worksheets.Add("Report");
            sheet.Cells[1, 1].Value = "Name";
            package.SaveAs(new FileInfo(path));
        }
    }
}
