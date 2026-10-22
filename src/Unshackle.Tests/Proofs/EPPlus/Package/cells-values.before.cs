using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Orders");
        sheet.Cells[1, 1].Value = "Name";
        sheet.Cells[1, 2].Value = "Amount";
        sheet.Cells["A2"].Value = "Widget";
        sheet.Cells[2, 2].Value = 12.5;
        sheet.Cells[3, 1].Value = true;
        sheet.Cells[3, 2].Value = 1234567890123L;
        for (int row = 4; row <= 6; row++)
        {
            sheet.Cells[row, 1].Value = row;
            sheet.Cells[row, 2].Value = "line " + row;
        }

        package.SaveAs(new FileInfo(path));
    }
}
