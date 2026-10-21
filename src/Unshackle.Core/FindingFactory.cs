using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// The finding rows the fixer reports.
internal static class FindingFactory
{
    internal static Finding NotApplied(Finding finding) => CreateFinding(finding, PatternResult.Manual(
        Fixer.NotRewrittenPrefix + "this fix changes nothing while other findings are Manual (a partial migration would break at run time). " +
        "Fix the manual findings and run the fix again."));

    internal static Finding CreateFinding(Finding finding, PatternResult result) => new()
    {
        FilePath = finding.FilePath,
        Line = finding.Line,
        FixId = finding.FixId,
        PatternId = finding.PatternId,
        Result = result,
    };

    internal static Finding CreateUsingsFinding(string filePath, int line, IFix fix, PatternResult result) =>
        new() { FilePath = filePath, Line = line, FixId = fix.Id, PatternId = Fixer.UsingsPatternId, Result = result };

    internal static Finding CreateProjectFinding(string filePath, IFix fix, PatternResult result) =>
        new() { FilePath = filePath, Line = 0, FixId = fix.Id, PatternId = Fixer.ProjectFilePatternId, Result = result };

    internal static PatternResult InternalError(IPattern pattern, Exception ex) =>
        PatternResult.Manual($"Internal error in {pattern.Id}: {ex.Message}");

    internal static Finding CreateFinding(string filePath, int line, IFix fix, IPattern pattern, PatternResult result) =>
        CreateFinding(filePath, line, fix, pattern.Id, result);

    internal static Finding CreateFinding(string filePath, int line, IFix fix, string patternId, PatternResult result) =>
        new() { FilePath = filePath, Line = line, FixId = fix.Id, PatternId = patternId, Result = result };
}
