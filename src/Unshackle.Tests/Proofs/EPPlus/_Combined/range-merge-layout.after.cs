using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new XLWorkbook();
        var sheet = package.Worksheets.Add("Orders");
        sheet.Range("A1:C1").Merge();
        sheet.Cell(1, 1).Value = "Title";
        sheet.Range("A2:C2").Style.Font.Bold = true;
        sheet.Row(2).Height = 30;
        sheet.SheetView.Freeze(2, 0);
        package.SaveAs(path);
    }
}
