using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    internal static void Layout(ExcelWorksheet sheet, int row)
    {
        sheet.View.FreezePanes(row, 1);
    }
}
