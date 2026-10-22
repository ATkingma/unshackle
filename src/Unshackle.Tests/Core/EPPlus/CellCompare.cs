using Microsoft.CodeAnalysis;
using Unshackle.Tests.Helpers;

namespace Unshackle.Tests.Core.EPPlus;

// The cell-compare harness (council round 8, E1): runs a "before" snippet on EPPlus 4.5.3.3 and an "after" snippet on ClosedXML,
// saves both workbooks, reopens them and returns every fact that differs (empty: the same workbook).
public static class CellCompare
{
    public static IReadOnlyList<string> Differences(string before, string after) =>
        Differences(before, EPPlusReferences.EPPlus, after, EPPlusReferences.ClosedXml);

    public static IReadOnlyList<string> Differences(
        string before, IEnumerable<MetadataReference> beforeReferences, string after, IEnumerable<MetadataReference> afterReferences)
    {
        using var directory = new TempDirectory();
        string beforePath = Path.Combine(directory.Path, "before.xlsx");
        string afterPath = Path.Combine(directory.Path, "after.xlsx");
        WorkbookProgram.Run(before, beforeReferences, beforePath);
        WorkbookProgram.Run(after, afterReferences, afterPath);
        return Differences(WorkbookSnapshot.Read(beforePath), WorkbookSnapshot.Read(afterPath));
    }

    public static IReadOnlyList<string> Differences(WorkbookSnapshot before, WorkbookSnapshot after) =>
        [.. before.Facts.Keys.Union(after.Facts.Keys).Order(StringComparer.Ordinal)
            .Select(key => (key, old: before.Facts.GetValueOrDefault(key, "<missing>"), now: after.Facts.GetValueOrDefault(key, "<missing>")))
            .Where(f => f.old != f.now)
            .Select(f => $"{f.key}: before '{f.old}', after '{f.now}'")];
}
