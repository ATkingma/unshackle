using OfficeOpenXml;

namespace Reports;

public static class SheetFactory
{
    public static ExcelWorksheet Create(ExcelPackage package, string name) => package.Workbook.Worksheets.Add(name);
}
