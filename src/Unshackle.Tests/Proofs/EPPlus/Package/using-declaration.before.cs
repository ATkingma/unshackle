using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using ExcelPackage package = new ExcelPackage();
        package.Workbook.Worksheets.Add("Orders");
        package.Workbook.Worksheets.Add("Customers");
        package.SaveAs(new FileInfo(path));
    }
}
