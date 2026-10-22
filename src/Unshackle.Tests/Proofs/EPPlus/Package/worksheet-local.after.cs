using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using (var package = new XLWorkbook())
        {
            var sheet = package.Worksheets.Add("Orders");
            sheet.Name = "Sales";
            package.SaveAs(path);
        }
    }
}
