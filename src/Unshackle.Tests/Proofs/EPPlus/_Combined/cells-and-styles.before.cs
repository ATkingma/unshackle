using System.Drawing;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Orders");
        sheet.Cells[1, 1].Value = "Name";
        sheet.Cells[1, 1].Style.Font.Bold = true;
        sheet.Cells[1, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
        sheet.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(Color.Red);
        sheet.Cells[2, 1].Value = 42;
        package.SaveAs(new FileInfo(path));
    }
}
