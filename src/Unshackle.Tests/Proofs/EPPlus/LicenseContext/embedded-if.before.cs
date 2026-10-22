using OfficeOpenXml;

namespace Reports;

public static class Startup
{
    public static void Configure(bool commercial)
    {
        if (commercial)
            ExcelPackage.LicenseContext = LicenseContext.Commercial;
    }
}
