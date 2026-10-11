using System.IO.Abstractions;
using System.Text.Json;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.DataAccess.Reports;

public class JsonReportWriter : IReportWriter
{
    private static readonly JsonSerializerOptions s_options = new() { WriteIndented = true };

    private readonly IFileSystem _fileSystem;

    public JsonReportWriter(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    public string Extension => ".json";

    public async Task WriteAsync(string path, IEnumerable<Finding> findings, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(findings);
        await using FileSystemStream stream = _fileSystem.File.Create(path);
        List<ReportRow> rows = [.. ReportRow.FromAll(findings)];
        await JsonSerializer.SerializeAsync(stream, rows, s_options, cancellationToken);
    }
}
