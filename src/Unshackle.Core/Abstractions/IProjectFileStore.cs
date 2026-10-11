using Unshackle.Core.Models;

namespace Unshackle.Core.Abstractions;

/// <summary>Reads and writes project files: <c>.csproj</c>, <c>Directory.Build.props</c> and <c>Directory.Packages.props</c>.</summary>
public interface IProjectFileStore
{
    /// <summary>
    /// Computes the new text of every project file the migrations touch, without writing anything. All migrations of one run
    /// are passed together, because <c>Directory.*.props</c> files are shared: the removed package only leaves a shared file
    /// when no project that uses that file keeps it.
    /// </summary>
    /// <param name="migrations">One per project.</param>
    /// <param name="cancellationToken">Cancels the edit.</param>
    /// <returns>One change per file whose text changes.</returns>
    /// <exception cref="IOException">A project file could not be read.</exception>
    /// <exception cref="System.Xml.XmlException">A project file is not valid XML.</exception>
    Task<IReadOnlyList<ProjectFileChange>> EditAsync(IEnumerable<ProjectMigration> migrations, CancellationToken cancellationToken);

    /// <summary>
    /// Writes the new text of every change (project files and changed source documents), keeping the UTF-8 BOM of each file.
    /// Every file is checked before the first write; when a write fails, the files already written are restored.
    /// </summary>
    /// <param name="changes">Changes from <see cref="EditAsync"/>, plus the changed documents of the solution.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <exception cref="InvalidOperationException">A file no longer has its old text: nothing is written.</exception>
    /// <exception cref="IOException">A file is not UTF-8 or could not be written: nothing is left changed.</exception>
    Task SaveAsync(IEnumerable<ProjectFileChange> changes, CancellationToken cancellationToken);
}
