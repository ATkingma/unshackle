using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        package.Workbook.Worksheets.Add("Report");
        File.WriteAllBytes(path, package.GetAsByteArray());
    }
}
