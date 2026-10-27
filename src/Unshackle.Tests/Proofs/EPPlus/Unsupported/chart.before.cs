using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace Reports;

internal class Report
{
    internal Report(ExcelWorksheet sheet)
    {
        sheet.Drawings.AddChart("Sales", eChartType.Pie);
    }
}
