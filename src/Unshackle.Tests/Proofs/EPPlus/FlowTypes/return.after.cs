using ClosedXML.Excel;

namespace Reports;

internal static class Report
{
    private static IXLWorksheet AddSheet(XLWorkbook package) => package.Worksheets.Add("Report");
}
