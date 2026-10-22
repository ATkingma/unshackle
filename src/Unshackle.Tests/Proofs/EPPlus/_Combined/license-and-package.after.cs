using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using (var package = new XLWorkbook())
        {
            package.Worksheets.Add("Report");
            package.SaveAs(path);
        }
    }
}
