using System.Diagnostics;
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

    public static ReportRow From(Finding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);
        (string status, string reason) = finding.Result switch
        {
            FixedResult => ("Fixed", ""),
            ManualResult manual => ("Manual", manual.Reason),
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
        };
    }
}
