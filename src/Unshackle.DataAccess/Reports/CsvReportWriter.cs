using System.Globalization;
using System.IO.Abstractions;
using CsvHelper;
using CsvHelper.Configuration;
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
        // Paths and reasons come from user code; keep spreadsheets from running them as formulas.
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture) { InjectionOptions = InjectionOptions.Escape };
        await using var csv = new CsvWriter(writer, configuration);
        await csv.WriteRecordsAsync(findings.Select(ReportRow.From), cancellationToken);
    }
}
