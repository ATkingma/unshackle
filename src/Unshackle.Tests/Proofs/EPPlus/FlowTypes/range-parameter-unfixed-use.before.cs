using OfficeOpenXml;

namespace Reports;

public static class Report
{
    private static string Read(ExcelRange range) => range.Address;
}
