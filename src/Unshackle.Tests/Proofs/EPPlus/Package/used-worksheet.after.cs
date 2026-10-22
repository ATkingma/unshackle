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
            sheet.Cell(1, 1).Value = "Name";
            package.SaveAs(path);
        }
    }
}
