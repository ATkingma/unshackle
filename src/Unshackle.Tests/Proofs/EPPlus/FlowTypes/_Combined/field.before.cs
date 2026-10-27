using System.IO;
using OfficeOpenXml;

namespace Reports;

public class Report
{
    private ExcelWorksheet _sheet;

    public static void Write(string path)
    {
        var report = new Report();
        report.Save(path);
        report.SaveTitle(path);
    }

    private void Save(string path)
    {
        using (var package = new ExcelPackage())
        {
            _sheet = package.Workbook.Worksheets.Add("Data");
            _sheet.Cells[1, 1].Value = "Name";
            _sheet.Cells[2, 1].Value = _sheet.Name;
            package.SaveAs(new FileInfo(path));
        }
    }

    private void SaveTitle(string path)
    {
        using (var package = new ExcelPackage())
        {
            ExcelWorksheet sheet = package.Workbook.Worksheets.Add("Title");
            sheet.Cells[1, 1].Value = _sheet.Name;
            package.SaveAs(new FileInfo(path));
        }
    }
}
