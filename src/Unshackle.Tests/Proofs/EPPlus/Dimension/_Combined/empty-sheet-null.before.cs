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
        int? last = sheet.Dimension?.End.Row;
        result.Cells["A1"].Value = last ?? -1;
        package.SaveAs(new FileInfo(path));
    }
}
