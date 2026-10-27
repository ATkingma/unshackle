using OfficeOpenXml;

namespace Reports;

public static class Report
{
    private static void Fill(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Name";
        Fill2(sheet);
    }

    private static void Fill2(ExcelWorksheet sheet)
    {
        sheet.Cells[2, 1].Value = "Name";
        Fill3(sheet);
    }

    private static void Fill3(ExcelWorksheet sheet)
    {
        sheet.Cells[3, 1].Value = "Name";
        Fill4(sheet);
    }

    private static void Fill4(ExcelWorksheet sheet)
    {
        sheet.Cells[4, 1].Value = "Name";
    }
}
