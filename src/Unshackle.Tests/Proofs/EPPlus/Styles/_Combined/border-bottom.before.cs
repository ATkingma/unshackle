using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("S");
        sheet.Cells["A1"].Value = "x";
        sheet.Cells["A1"].Style.Border.Bottom.Style = ExcelBorderStyle.Double;
        package.SaveAs(new FileInfo(path));
    }
}
