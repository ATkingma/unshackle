using System.IO;
using ClosedXML.Excel;

namespace Reports;

public static class Report
{
    public static void Write(string path)
    {
        using (var package = new XLWorkbook())
        {
            var sheet = package.Worksheets.Add("Orders");
            sheet.Cell(1, 1).Value = "Name";
            using var workbookStream = new MemoryStream();
            package.SaveAs(workbookStream);
            File.WriteAllBytes(path, workbookStream.ToArray());
        }
    }
}
