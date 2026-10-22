using System.IO;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("S");
        var items = new System.Collections.Generic.List<Item> { new Item { Name = "a", Count = 1 }, new Item { Name = "b", Count = 2 } };
        sheet.Cells["B2"].LoadFromCollection(items);
        package.SaveAs(new FileInfo(path));
    }
}

public class Item
{
    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }
}
