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
        int last = 3;
        result.Cells["A1"].Value = sheet.Cells[5, 4, 2, 3].Rows;
        result.Cells["A2"].Value = sheet.Cells[5, 4, 2, 3].Start.Row;
        result.Cells["A3"].Value = sheet.Cells[5, 4, 2, 3].End.Column;
        result.Cells["A4"].Value = sheet.Cells[2, 1, last, 1].Rows;
        result.Cells["A5"].Value = sheet.Cells["B2:D7"].Columns;
        result.Cells["A6"].Value = sheet.Cells["B2:D7"].End.Row;
        result.Cells["A7"].Value = sheet.Cells["B2:D7"].Start.Column;
        package.SaveAs(new FileInfo(path));
    }
}
