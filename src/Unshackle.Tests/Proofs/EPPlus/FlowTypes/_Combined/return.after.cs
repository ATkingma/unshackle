using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using (var package = new XLWorkbook())
        {
            IXLWorksheet sheet = AddSheet(package);
            sheet.Cell(1, 1).Value = "Name";
            sheet.Cell(2, 1).Value = sheet.Name;
            package.SaveAs(path);
        }
    }

    private static IXLWorksheet AddSheet(XLWorkbook package) => package.Worksheets.Add("Report");
}
