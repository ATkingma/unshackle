using OfficeOpenXml;

namespace Reports;

public class Report
{
    private readonly ExcelPackage _package = new ExcelPackage();

    public byte[] Bytes() => _package.GetAsByteArray();
}
