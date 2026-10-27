using OfficeOpenXml;

namespace Reports;

public static class Report
{
    private static int Size(ExcelRange range)
    {
        return range.Rows * range.Columns + range.Start.Row + range.End.Column;
    }
}
