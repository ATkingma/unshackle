using OfficeOpenXml;

namespace Reports;

internal static class Report
{
    internal static int Rows(ExcelWorksheet sheet, bool wide)
    {
        var range = sheet.Cells[1, 1, 2, 2];
        if (wide)
        {
            range = sheet.Cells[1, 1, 9, 9];
        }

        return range.Rows;
    }
}
