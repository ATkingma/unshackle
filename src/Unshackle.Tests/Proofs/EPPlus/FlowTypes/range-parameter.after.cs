using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    private static int Size(IXLRange range)
    {
        return range.RowCount() * range.ColumnCount() + range.RangeAddress.FirstAddress.RowNumber + range.RangeAddress.LastAddress.ColumnNumber;
    }
}
