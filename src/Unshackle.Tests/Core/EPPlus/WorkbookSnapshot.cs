using System.Globalization;
using ClosedXML.Excel;

namespace Unshackle.Tests.Core.EPPlus;

// What the cell-compare harness compares (council round 8, E1): a saved workbook reopened and flattened into facts, one per
// key ("Sheet!B2 font" → "bold=True ..."). Both sides are read by the same reader (ClosedXML), so a difference is one in the
// file, not between two readers.
public class WorkbookSnapshot
{
    public required IReadOnlyDictionary<string, string> Facts { get; init; }

    public static WorkbookSnapshot Read(string path)
    {
        var facts = new SortedDictionary<string, string>(StringComparer.Ordinal);
        using var workbook = new XLWorkbook(path);
        facts["workbook sheets"] = string.Join(" | ", workbook.Worksheets.Select(w => w.Name));
        foreach (IXLWorksheet sheet in workbook.Worksheets)
        {
            string name = sheet.Name;
            facts[$"{name} merges"] = string.Join(" ", sheet.MergedRanges.Select(r => r.RangeAddress.ToString()).Order(StringComparer.Ordinal));
            int lastColumn = 0;
            foreach (IXLCell cell in sheet.CellsUsed(XLCellsUsedOptions.All))
            {
                string key = $"{name}!{cell.Address.ToStringRelative()}";
                lastColumn = Math.Max(lastColumn, cell.Address.ColumnNumber);
                facts[key + " value"] = $"{cell.Value.Type}:{Text(cell.Value)}";
                facts[key + " formula"] = cell.HasFormula ? cell.FormulaA1 : "";
                facts[key + " text"] = cell.GetFormattedString();
                IXLStyle style = cell.Style;
                facts[key + " number format"] = NumberFormat(style.NumberFormat);
                facts[key + " font"] = $"bold={style.Font.Bold} italic={style.Font.Italic} underline={style.Font.Underline} "
                    + $"strike={style.Font.Strikethrough} size={style.Font.FontSize.ToString(CultureInfo.InvariantCulture)} name={style.Font.FontName} color={style.Font.FontColor}";
                facts[key + " fill"] = $"pattern={style.Fill.PatternType} background={style.Fill.BackgroundColor} foreground={style.Fill.PatternColor}";
                facts[key + " borders"] = $"left={style.Border.LeftBorder}/{style.Border.LeftBorderColor} right={style.Border.RightBorder}/{style.Border.RightBorderColor} "
                    + $"top={style.Border.TopBorder}/{style.Border.TopBorderColor} bottom={style.Border.BottomBorder}/{style.Border.BottomBorderColor}";
                facts[key + " alignment"] = $"horizontal={style.Alignment.Horizontal} vertical={style.Alignment.Vertical} wrap={style.Alignment.WrapText}";
            }

            for (int column = 1; column <= lastColumn; column++)
            {
                facts[$"{name} column {column.ToString(CultureInfo.InvariantCulture)} width"] =
                    sheet.Column(column).Width.ToString("0.###", CultureInfo.InvariantCulture);
            }
        }

        return new WorkbookSnapshot { Facts = facts };
    }

    // A built-in format is stored as its id alone (EPPlus) or as id plus code (ClosedXML); both mean the same, so a code that is a
    // culture-invariant built-in is shown as its id. Culture-dependent built-ins (dates, currency) are not in the table and stay distinct.
    private static readonly Dictionary<string, int> s_builtInFormats = new(StringComparer.Ordinal)
    {
        ["General"] = 0, ["0"] = 1, ["0.00"] = 2, ["#,##0"] = 3, ["#,##0.00"] = 4, ["0%"] = 9, ["0.00%"] = 10, ["0.00E+00"] = 11,
        ["# ?/?"] = 12, ["# ??/??"] = 13, ["mm:ss"] = 45, ["[h]:mm:ss"] = 46, ["mmss.0"] = 47, ["##0.0E+0"] = 48, ["@"] = 49,
    };

    private static string NumberFormat(IXLNumberFormat format) =>
        string.IsNullOrEmpty(format.Format) ? "id " + format.NumberFormatId.ToString(CultureInfo.InvariantCulture)
        : s_builtInFormats.TryGetValue(format.Format, out int id) ? "id " + id.ToString(CultureInfo.InvariantCulture)
        : format.Format;

    private static string Text(XLCellValue value) => value.Type switch
    {
        XLDataType.Blank => "",
        XLDataType.Boolean => value.GetBoolean().ToString(CultureInfo.InvariantCulture),
        XLDataType.Number => value.GetNumber().ToString("R", CultureInfo.InvariantCulture),
        XLDataType.DateTime => value.GetDateTime().ToString("O", CultureInfo.InvariantCulture),
        XLDataType.TimeSpan => value.GetTimeSpan().ToString("c", CultureInfo.InvariantCulture),
        XLDataType.Error => value.GetError().ToString(),
        _ => value.GetText(),
    };
}
