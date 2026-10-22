using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    internal static void Layout(ExcelWorksheet sheet, int row)
    {
        sheet.Cells["A1:B2"].Merge = false;
    }
}
