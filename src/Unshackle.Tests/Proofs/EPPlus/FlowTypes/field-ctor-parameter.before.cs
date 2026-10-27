using OfficeOpenXml;

namespace Reports;

internal class Report
{
    private readonly ExcelWorksheet _sheet;

    public Report(ExcelWorksheet sheet)
    {
        _sheet = sheet;
    }

    public string Name => _sheet.Name;
}
