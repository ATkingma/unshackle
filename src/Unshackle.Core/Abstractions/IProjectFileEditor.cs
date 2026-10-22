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
    /// <param name="context">The loaded and the blocked projects of the solution, for edits of shared files (council round 10, G2/G3).</param>
    /// <param name="cancellationToken">Cancels the edit.</param>
    /// <returns>One outcome per edit that had something to do.</returns>
    /// <exception cref="IOException">A file could not be read.</exception>
    /// <exception cref="System.Xml.XmlException">A project file is not valid XML.</exception>
    Task<IReadOnlyList<ProjectFileEditOutcome>> ApplyAsync(
        string projectPath,
        IEnumerable<ProjectFileEdit> edits,
        IEnumerable<ProjectFileChange> earlier,
        ProjectFileEditContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Council round 12, G7: after every project's edits, decides once per <c>Directory.Packages.props</c> whether each old <c>PackageVersion</c>
    /// goes (no GlobalPackageReference, every importer loaded, none still references it) and removes it; one outcome per version.
    /// </summary>
    /// <param name="versions">The old versions the edits left behind.</param>
    /// <param name="earlier">All changes of the run so far.</param>
    /// <param name="context">The loaded and the blocked projects.</param>
    /// <param name="cancellationToken">Cancels the pass.</param>
    Task<IReadOnlyList<ProjectFileEditOutcome>> RemoveOldVersionsAsync(
        IEnumerable<OldPackageVersion> versions, IEnumerable<ProjectFileChange> earlier, ProjectFileEditContext context, CancellationToken cancellationToken);
}
