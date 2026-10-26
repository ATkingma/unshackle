using OfficeOpenXml;

namespace Orders;

// Migrates to ClosedXML: every EPPlus use here has a pattern.
public static class OrderReport
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("Orders");
        sheet.Cells[1, 1].Value = "Name";
        sheet.Cells[1, 2].Value = "Amount";
        sheet.Cells[2, 1].Value = "Widget";
        sheet.Cells[2, 2].Value = 12.5;
        package.SaveAs(new FileInfo(path));
    }
}
