using System.Globalization;
using System.IO.Abstractions;
using CsvHelper;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.DataAccess.Reports;

public class CsvReportWriter : IReportWriter
{
    private readonly IFileSystem _fileSystem;

    public CsvReportWriter(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    public string Extension => ".csv";

    public async Task WriteAsync(string path, IEnumerable<Finding> findings, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(findings);
        await using StreamWriter writer = _fileSystem.File.CreateText(path);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(findings.Select(ReportRow.From), cancellationToken);
    }
}
