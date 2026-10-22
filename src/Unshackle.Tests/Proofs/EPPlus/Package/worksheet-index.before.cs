using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        package.Workbook.Worksheets.Add("Orders");
        package.Workbook.Worksheets.Add("Customers");
        package.Workbook.Worksheets[1].Name = "Clients";
        package.SaveAs(new FileInfo(path));
    }
}
