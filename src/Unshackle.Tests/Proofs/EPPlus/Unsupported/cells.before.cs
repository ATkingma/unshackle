using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    internal static void Fill(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Name";
    }
}
