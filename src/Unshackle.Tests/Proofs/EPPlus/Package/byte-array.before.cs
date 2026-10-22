using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path) => File.WriteAllBytes(path, Bytes());

    public static byte[] Bytes()
    {
        using (var package = new ExcelPackage())
        {
            package.Workbook.Worksheets.Add("Report");
            return package.GetAsByteArray();
        }
    }
}
