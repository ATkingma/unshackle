using OfficeOpenXml;

namespace Reports;

public static class Report
{
    private static void Fill(ExcelWorksheet sheet, int row)
    {
        sheet.Cells[row, 1].Value = "Name";
        if (row > 1)
        {
            Fill(sheet, row - 1);
        }
    }
}
