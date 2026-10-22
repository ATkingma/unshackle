using OfficeOpenXml;

namespace Reports;

public static class Startup
{
    public static int Configure()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        return 42;
    }
}
