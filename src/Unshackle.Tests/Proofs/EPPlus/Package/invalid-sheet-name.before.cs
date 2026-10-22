using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using (var package = new ExcelPackage())
        {
            package.Workbook.Worksheets.Add("Q1/Q2");
            package.SaveAs(new FileInfo(path));
        }
    }
}
