using OfficeOpenXml;

namespace Reports;

internal class Report
{
    internal Report(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Name";
    }
}
