using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    private static void Fill(IXLWorksheet sheet)
    {
        sheet.Cell(1, 1).Value = "Name";
        sheet.Cell("B1").Value = 42;
    }
}
