using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    internal static void Layout(ExcelWorksheet sheet, int row)
    {
        sheet.Column(1).Width = 20;
    }
}
