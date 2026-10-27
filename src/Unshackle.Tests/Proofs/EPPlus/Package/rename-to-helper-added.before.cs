using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        ExcelWorksheet sheet = package.Workbook.Worksheets.Add("Data");
        AddSheet(package);
        sheet.Name = "Report";
        package.SaveAs(new FileInfo(path));
    }

    private static ExcelWorksheet AddSheet(ExcelPackage package) => package.Workbook.Worksheets.Add("Report");
}
