using OfficeOpenXml;

namespace Reports;

internal class Report
{
    private ExcelWorksheet _sheet = null!;

    internal int Rows()
    {
        _sheet.Protection.IsProtected = true;
        return _sheet.Cells["C5:A2"].Rows;
    }
}
