using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using var package = new XLWorkbook();
        var sheet = package.Worksheets.Add("S");
        var items = new System.Collections.Generic.List<Item> { new Item { Name = "a", Count = 1 }, new Item { Name = "b", Count = 2 } };
        sheet.Cell("B2").InsertData(items);
        package.SaveAs(path);
    }
}

public class Item
{
    public string Name { get; set; } = string.Empty;

    public int Count { get; set; }
}
