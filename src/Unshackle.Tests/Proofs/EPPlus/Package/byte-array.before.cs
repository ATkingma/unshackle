using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static byte[] Write()
    {
        using (var package = new ExcelPackage())
        {
            package.Workbook.Worksheets.Add("Report");
            return package.GetAsByteArray();
        }
    }
}
