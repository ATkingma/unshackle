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
        var block = sheet.Cells[5, 4, 2, 3];
        ExcelRange table = sheet.Cells["B2:D7"];
        result.Cells["A1"].Value = block.Rows;
        result.Cells["A2"].Value = block.Start.Row;
        result.Cells["A3"].Value = block.End.Column;
        result.Cells["A4"].Value = table.Columns;
        result.Cells["A5"].Value = table.End.Row;
        package.SaveAs(new FileInfo(path));
    }
}
