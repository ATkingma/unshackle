using System.IO.Abstractions;
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
            .. findings.Select(ReportRow.From).Select(r =>
                $"| {Cell(r.File)} | {r.Line} | {Cell(r.Fix)} | {Cell(r.Pattern)} | {r.Status} | {Cell(r.Reason)} |"),
        ];
        await _fileSystem.File.WriteAllLinesAsync(path, lines, cancellationToken);
    }

    // A pipe or line break would break the table row.
    private static string Cell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal).ReplaceLineEndings(" ");
}
