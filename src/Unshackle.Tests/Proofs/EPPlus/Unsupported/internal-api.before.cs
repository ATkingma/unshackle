using OfficeOpenXml;

namespace Reports;

public static class SheetFactory
{
    internal static void Create(ExcelPackage package, string name) => package.Workbook.Worksheets.Add(name);
}
