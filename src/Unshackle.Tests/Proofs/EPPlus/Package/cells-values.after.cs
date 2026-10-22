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
        sheet.Cell(1, 2).Value = "Amount";
        sheet.Cell("A2").Value = "Widget";
        sheet.Cell(2, 2).Value = 12.5;
        sheet.Cell(3, 1).Value = true;
        sheet.Cell(3, 2).Value = 1234567890123L;
        for (int row = 4; row <= 6; row++)
        {
            sheet.Cell(row, 1).Value = row;
            sheet.Cell(row, 2).Value = "line " + row;
        }

        package.SaveAs(path);
    }
}
