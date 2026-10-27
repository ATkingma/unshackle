using OfficeOpenXml;

namespace Reports;

public static class Report
{
    private static void Fill(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Name";
        Format(sheet);
    }

    private static void Format(ExcelWorksheet sheet)
    {
        sheet.Protection.IsProtected = true;
    }
}
