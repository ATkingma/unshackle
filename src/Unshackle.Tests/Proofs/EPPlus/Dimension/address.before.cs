using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    internal static string Used(ExcelWorksheet sheet) => sheet.Dimension.Address;
}
