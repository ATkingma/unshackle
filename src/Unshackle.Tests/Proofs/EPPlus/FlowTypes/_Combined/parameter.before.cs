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
            Fill(sheet);
            package.SaveAs(new FileInfo(path));
        }
    }

    private static void Fill(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Name";
        sheet.Cells["B1"].Value = 42;
        sheet.Cells[2, 1].Value = sheet.Name;
    }
}
