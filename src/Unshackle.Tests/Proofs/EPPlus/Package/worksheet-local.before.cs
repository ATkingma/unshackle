using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using (var package = new ExcelPackage())
        {
            var sheet = package.Workbook.Worksheets.Add("Orders");
            sheet.Name = "Sales";
            package.SaveAs(new FileInfo(path));
        }
    }
}
