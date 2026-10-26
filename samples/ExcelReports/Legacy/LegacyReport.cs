using OfficeOpenXml;

namespace Legacy;

// Stays on EPPlus: Save() on a package without a file has no ClosedXML mapping, so the whole project is kept.
public static class LegacyReport
{
    public static void Write()
    {
        using var package = new ExcelPackage();
        package.Workbook.Worksheets.Add("Legacy");
        package.Save();
    }
}
