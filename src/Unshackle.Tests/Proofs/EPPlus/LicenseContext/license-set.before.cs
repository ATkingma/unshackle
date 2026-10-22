using OfficeOpenXml;

namespace Reports;

public static class Startup
{
    public static void Configure(string owner)
    {
        ExcelPackage.License.SetNonCommercialPersonal(owner);
    }
}
