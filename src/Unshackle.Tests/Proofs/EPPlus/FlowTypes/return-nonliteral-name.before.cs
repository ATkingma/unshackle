using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    private static ExcelWorksheet AddSheet(ExcelPackage package, string name) => package.Workbook.Worksheets.Add(name);
}
