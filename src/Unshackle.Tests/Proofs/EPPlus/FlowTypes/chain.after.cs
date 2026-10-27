using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    private static void Fill(IXLWorksheet sheet)
    {
        sheet.Cell(1, 1).Value = "Name";
        FillTotals(sheet);
    }

    private static void FillTotals(IXLWorksheet sheet)
    {
        sheet.Cell(2, 1).Value = "Total";
        FillFooter(sheet);
    }

    private static void FillFooter(IXLWorksheet sheet)
    {
        sheet.Cell("A3").Value = 42;
    }
}
