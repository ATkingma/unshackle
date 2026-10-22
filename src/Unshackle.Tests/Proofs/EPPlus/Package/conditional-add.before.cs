using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        if (path.Length > 0)
        {
            package.Workbook.Worksheets.Add("Orders");
        }

        package.Workbook.Worksheets["Orders"].Name = "Clients";
        package.SaveAs(new FileInfo(path));
    }
}
