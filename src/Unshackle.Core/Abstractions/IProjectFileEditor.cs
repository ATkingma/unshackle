using Unshackle.Core.Models;

namespace Unshackle.Core.Abstractions;

/// <summary>
/// Computes the typed edits of <see cref="IProjectFileMigration"/> (TAAK-317) without writing anything. An <see cref="IProjectFileStore"/>
/// that also implements this interface enables them in the Fixer; the changes are saved with <see cref="IProjectFileStore.SaveAsync"/>.
/// </summary>
public interface IProjectFileEditor
{
    /// <summary>Applies the edits of one project, in order, on top of the changes computed earlier in the same run.</summary>
    /// <param name="projectPath">The project file.</param>
    /// <param name="edits">The edits of that project.</param>
    /// <param name="earlier">Changes from earlier in the run (package migration, earlier projects): an edit starts from their new text.</param>
    /// <param name="cancellationToken">Cancels the edit.</param>
    /// <returns>One outcome per edit that had something to do.</returns>
    /// <exception cref="IOException">A file could not be read.</exception>
    /// <exception cref="System.Xml.XmlException">A project file is not valid XML.</exception>
    Task<IReadOnlyList<ProjectFileEditOutcome>> ApplyAsync(
        string projectPath, IEnumerable<ProjectFileEdit> edits, IEnumerable<ProjectFileChange> earlier, CancellationToken cancellationToken);
}
