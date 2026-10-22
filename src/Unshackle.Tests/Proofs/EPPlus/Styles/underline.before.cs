using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Reports;

internal static class Report
{
    internal static void Format(ExcelRange cell)
    {
        cell.Style.Font.UnderLine = true;
    }
}
