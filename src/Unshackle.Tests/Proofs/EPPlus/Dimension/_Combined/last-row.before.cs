using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("S");
        var result = package.Workbook.Worksheets.Add("Result");
        sheet.Cells["B2"].Value = 1;
        sheet.Cells["D5"].Value = 2;
        result.Cells["A1"].Value = sheet.Dimension.End.Row;
        result.Cells["A2"].Value = sheet.Dimension.End.Column;
        result.Cells["A3"].Value = sheet.Dimension.Start.Row;
        result.Cells["A4"].Value = sheet.Dimension.Start.Column;
        package.SaveAs(new FileInfo(path));
    }
}
