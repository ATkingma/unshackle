using System.Diagnostics;
using System.Text.Json.Serialization;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.DataAccess.Reports;

// One report line; the property order is the column order File,Line,Fix,Pattern,Status,Reason.
public class ReportRow
{
    public required string File { get; init; }

    public required int Line { get; init; }

    public required string Fix { get; init; }

    public required string Pattern { get; init; }

    public required string Status { get; init; }

    public required string Reason { get; init; }

    // Every dependent of a kept-for-dependent project row (council round 14, M11); the reason text names at most five. Null otherwise.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Dependents { get; init; }

    // One row per finding, except the calls of one custom assertion: one row (at the first call) with the count, because
    // the fix is one rewrite of the extension, not of every call.
    public static IEnumerable<ReportRow> FromAll(IEnumerable<Finding> findings)
    {
        List<Finding> all = [.. findings];
        var groups = CustomAssertionPattern.Group(all).ToDictionary(g => g.First(), g => g.Count());
        return all
            .Where(f => groups.ContainsKey(f) || !(f.PatternId == CustomAssertionPattern.PatternId && f.Result is ManualResult))
            .Select(f => groups.TryGetValue(f, out int count) ? WithCount(From(f), count) : From(f));
    }

    private static ReportRow WithCount(ReportRow row, int count) => new()
    {
        File = row.File,
        Line = row.Line,
        Fix = row.Fix,
        Pattern = row.Pattern,
        Status = row.Status,
        Reason = $"{count} call(s), first here. {row.Reason}",
        Dependents = row.Dependents,
    };

    // The reason of a Manual result as the report shows it: the one Core rendering (council round 6, Q1), the code stays in the model.
    public static string ReasonOf(ManualResult manual) => KeptReasonText.Rendered(manual);

    public static ReportRow From(Finding finding)
    {
        (string status, string reason) = finding.Result switch
        {
            FixedResult fixedResult => ("Fixed", string.Join(" ", new[] { fixedResult.Warning, fixedResult.Info is null ? null : "Info: " + fixedResult.Info }.OfType<string>())),
            ManualResult manual => ("Manual", ReasonOf(manual)),
            InfoResult info => ("Info", info.Text),
            _ => throw new UnreachableException(),
        };
        return new ReportRow
        {
            File = finding.FilePath,
            Line = finding.Line,
            Fix = finding.FixId,
            Pattern = finding.PatternId,
            Status = status,
            Reason = reason,
            Dependents = (finding.Result as ManualResult)?.Dependents ?? (finding.Result as FixedResult)?.Dependents,
        };
    }
}
