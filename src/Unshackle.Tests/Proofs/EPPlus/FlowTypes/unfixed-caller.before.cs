using OfficeOpenXml;

namespace Reports;

public class Report
{
    private readonly ExcelWorksheet _sheet;

    public Report(ExcelWorksheet sheet)
    {
        _sheet = sheet;
    }

    public void Write()
    {
        Fill(_sheet);
    }

    private static void Fill(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Name";
    }
}
