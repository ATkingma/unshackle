using System.Drawing;
using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("S");
        sheet.Cells["A1"].Value = "x";
        sheet.Cells["A1"].Style.Font.Color.SetColor(Color.Red);
        package.SaveAs(new FileInfo(path));
    }
}
