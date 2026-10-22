using OfficeOpenXml;

namespace Reports;

public static class Startup
{
    static Startup()
    {
        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.Commercial;
    }

    public static int Answer => 42;
}
