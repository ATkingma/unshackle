using OfficeOpenXml;

namespace Reports;

public static class Report
{
    private static void Fill(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Name";
        sheet.Cells["B1"].Value = 42;
    }
}
