using System.Collections.Generic;
using System.Data;
using OfficeOpenXml;

namespace Reports;

public class Order
{
    public string Name { get; set; } = string.Empty;

    public System.DateTime Placed { get; set; }
}

internal static class Report
{
    internal static void Load(ExcelWorksheet sheet, List<Order> orders, DataTable table)
    {
        sheet.Cells["A1"].LoadFromCollection(orders, true);
    }
}
