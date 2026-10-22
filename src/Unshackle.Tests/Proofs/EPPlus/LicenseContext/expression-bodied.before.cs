using OfficeOpenXml;

namespace Reports;

public static class Startup
{
    public static void Configure() => ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
}
