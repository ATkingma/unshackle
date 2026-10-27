using System;
using OfficeOpenXml;

namespace Reports;

public static class Report
{
    public static Delegate Filler() => (Action<ExcelWorksheet>)Fill;

    private static void Fill(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "Name";
    }
}
