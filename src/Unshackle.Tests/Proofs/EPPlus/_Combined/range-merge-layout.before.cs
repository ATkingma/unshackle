using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Orders");
        sheet.Cells["A1:C1"].Merge = true;
        sheet.Cells[1, 1].Value = "Title";
        sheet.Cells["A2:C2"].Style.Font.Bold = true;
        sheet.Row(2).Height = 30;
        sheet.View.FreezePanes(3, 1);
        package.SaveAs(new FileInfo(path));
    }
}
