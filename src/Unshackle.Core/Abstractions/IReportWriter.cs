using Unshackle.Core.Models;

namespace Unshackle.Core.Abstractions;

/// <summary>Writes findings to a report file in one format (Strategy), chosen by file extension.</summary>
public interface IReportWriter
{
    /// <summary>File extension this writer handles, including the dot, for example <c>.csv</c>.</summary>
    string Extension { get; }

    /// <summary>Writes one row per finding to <paramref name="path"/>, replacing an existing file.</summary>
    /// <param name="path">The report file.</param>
    /// <param name="findings">The findings, with <see cref="Finding.FilePath"/> as it should appear in the report.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task WriteAsync(string path, IEnumerable<Finding> findings, CancellationToken cancellationToken);
}
