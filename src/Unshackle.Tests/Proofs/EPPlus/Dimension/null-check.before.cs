using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    internal static bool IsEmpty(ExcelWorksheet sheet) => sheet.Dimension == null;
}
