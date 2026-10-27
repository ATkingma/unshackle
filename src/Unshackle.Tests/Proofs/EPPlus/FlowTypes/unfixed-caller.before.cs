using OfficeOpenXml;

namespace Reports;

public class Report
{
    public void Write(ExcelPackage package)
    {
        Fill(package.Workbook.Worksheets[1]);
    }

    private static void Fill(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Name";
    }
}
