using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Reports;

internal static class Report
{
    internal static void Format(ExcelRange cell, ExcelHorizontalAlignment alignment)
    {
        cell.Style.HorizontalAlignment = alignment;
    }
}
