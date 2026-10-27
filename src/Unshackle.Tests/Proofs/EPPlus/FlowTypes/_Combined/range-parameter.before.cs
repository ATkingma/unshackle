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
        var block = sheet.Cells[2, 1, 4, 3];
        result.Cells["A1"].Value = Size(sheet.Cells[5, 4, 2, 3]);
        result.Cells["A2"].Value = Size(block);
        result.Cells["A3"].Value = Size(sheet.Cells["B2:D7"]);
        package.SaveAs(new FileInfo(path));
    }

    private static int Size(ExcelRange range)
    {
        return range.Rows * 1000 + range.Columns * 100 + range.Start.Row * 10 + range.End.Column;
    }
}
