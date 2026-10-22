using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using XLWorkbook package = new XLWorkbook();
        package.Worksheets.Add("Orders");
        package.Worksheets.Add("Customers");
        package.SaveAs(path);
    }
}
