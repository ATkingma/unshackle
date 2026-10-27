using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using (var package = new XLWorkbook())
        {
            IXLWorksheet sheet = package.Worksheets.Add("Report");
            Fill(sheet);
            package.SaveAs(path);
        }
    }

    private static void Fill(IXLWorksheet sheet)
    {
        sheet.Cell(1, 1).Value = "Name";
        sheet.Cell("B1").Value = 42;
        sheet.Cell(2, 1).Value = sheet.Name;
    }
}
