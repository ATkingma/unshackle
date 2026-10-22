using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        var package = new ExcelPackage();
        package.Workbook.Worksheets.Add("Report");
        package.SaveAs(new FileInfo(path));
    }
}
