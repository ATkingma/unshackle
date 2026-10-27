using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    internal static int First(ExcelWorksheet sheet) => sheet.Cells["C5:A2"].Start.Row;
}
