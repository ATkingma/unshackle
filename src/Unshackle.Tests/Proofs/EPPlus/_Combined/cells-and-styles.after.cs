using System.Drawing;
using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new XLWorkbook();
        var sheet = package.Worksheets.Add("Orders");
        sheet.Cell(1, 1).Value = "Name";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 2).Style.Fill.BackgroundColor = XLColor.FromColor(Color.Red);
        sheet.Cell(2, 1).Value = 42;
        package.SaveAs(path);
    }
}
