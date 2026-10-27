using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    private static ExcelWorksheet AddSheet(ExcelPackage package) => package.Workbook.Worksheets.Add("Report");
}
