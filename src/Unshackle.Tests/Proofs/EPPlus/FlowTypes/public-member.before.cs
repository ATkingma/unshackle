using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    public static void Fill(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Name";
    }
}
