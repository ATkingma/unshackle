using System.IO;
using ClosedXML.Excel;

namespace Reports;

public class Report
{
    private IXLWorksheet _sheet;

    public static void Write(string path)
    {
        var report = new Report();
        report.Save(path);
        report.SaveTitle(path);
    }

    private void Save(string path)
    {
        using (var package = new XLWorkbook())
        {
            _sheet = package.Worksheets.Add("Data");
            _sheet.Cell(1, 1).Value = "Name";
            _sheet.Cell(2, 1).Value = _sheet.Name;
            package.SaveAs(path);
        }
    }

    private void SaveTitle(string path)
    {
        using (var package = new XLWorkbook())
        {
            IXLWorksheet sheet = package.Worksheets.Add("Title");
            sheet.Cell(1, 1).Value = _sheet.Name;
            package.SaveAs(path);
        }
    }
}
