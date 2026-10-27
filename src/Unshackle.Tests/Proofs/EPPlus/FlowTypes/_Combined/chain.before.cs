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
        FillTotals(sheet);
    }

    private static void FillTotals(ExcelWorksheet sheet)
    {
        sheet.Cells[2, 1].Value = "Total";
        FillFooter(sheet);
    }

    private static void FillFooter(ExcelWorksheet sheet)
    {
        sheet.Cells["A3"].Value = sheet.Name;
    }
}
