using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace Reports;

internal static class Report
{
    internal static void Chart(ExcelWorksheet sheet)
    {
        sheet.Drawings.AddChart("Sales", eChartType.Pie);
    }
}
