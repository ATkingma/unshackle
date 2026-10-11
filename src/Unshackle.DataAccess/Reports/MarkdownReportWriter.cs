using System.IO.Abstractions;
using System.Net;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.DataAccess.Reports;

public class MarkdownReportWriter : IReportWriter
{
    private readonly IFileSystem _fileSystem;

    public MarkdownReportWriter(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    public string Extension => ".md";

    public async Task WriteAsync(string path, IEnumerable<Finding> findings, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(findings);
        List<string> lines =
        [
            "| File | Line | Fix | Pattern | Status | Reason |",
            "| --- | --- | --- | --- | --- | --- |",
            .. ReportRow.FromAll(findings).Select(r =>
                $"| {Cell(r.File)} | {r.Line} | {Cell(r.Fix)} | {Cell(r.Pattern)} | {r.Status} | {Cell(r.Reason)} |"),
        ];
        await _fileSystem.File.WriteAllLinesAsync(path, lines, cancellationToken);
    }

    // Reasons quote user code: HTML must not render (a PR comment or wiki would run it), and a pipe, backtick, trailing
    // backslash or line break would break the table row. Backslashes first, so the escapes added after stay intact.
    private static string Cell(string value) => WebUtility.HtmlEncode(value)
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("|", "\\|", StringComparison.Ordinal)
        .Replace("`", "\\`", StringComparison.Ordinal)
        .ReplaceLineEndings(" ");
}
