using System;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static Func<byte[]> Write()
    {
        var package = new ExcelPackage();
        package.Workbook.Worksheets.Add("Report");
        return () => package.GetAsByteArray();
    }
}
