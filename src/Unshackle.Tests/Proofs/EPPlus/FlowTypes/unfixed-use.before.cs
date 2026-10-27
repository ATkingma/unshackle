using OfficeOpenXml;

namespace Reports;

public static class Report
{
    private static string Fill(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Name";
        return sheet.Cells[1, 1].Text;
    }
}
