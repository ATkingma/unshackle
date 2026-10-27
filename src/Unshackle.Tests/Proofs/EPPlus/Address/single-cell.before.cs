using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    internal static int Rows(ExcelWorksheet sheet) => sheet.Cells["B2"].Rows;
}
