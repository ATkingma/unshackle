using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    internal static ExcelCellAddress First(ExcelWorksheet sheet) => sheet.Cells["A1:C2"].Start;
}
