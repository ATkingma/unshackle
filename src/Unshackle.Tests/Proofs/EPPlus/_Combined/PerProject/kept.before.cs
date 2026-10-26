using System.IO;
using OfficeOpenXml;

namespace Kept;

public static class Kept
{
    public static void Append(FileInfo file)
    {
        using var package = new ExcelPackage(file);
        package.Workbook.Worksheets.Add("More");
        package.Save();
    }
}
