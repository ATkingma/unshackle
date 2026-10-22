using System.Diagnostics.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.DataAccess.Projects;

// The files of one ApplyAsync call (TAAK-317): the text on disk (or of an earlier change) and the text so far, so edits build on each other.
internal class EditRun
{
    private readonly Dictionary<string, FileState> _files;

    public EditRun(string projectPath, IEnumerable<ProjectFileChange> earlier, StringComparer pathComparer)
    {
        ProjectPath = projectPath;
        _files = new Dictionary<string, FileState>(pathComparer);
        foreach (ProjectFileChange change in earlier)
        {
            _files[change.FilePath] = new FileState
            {
                Original = change.Creates ? null : change.OldText,
                Current = change.Deletes ? null : change.NewText,
                KeptRemovedPackageFor = change.KeptRemovedPackageFor,
            };
        }
    }

    public string ProjectPath { get; }

    public bool TryGet(string path, [NotNullWhen(true)] out FileState? state) => _files.TryGetValue(path, out state);

    public FileState Add(string path, string? text)
    {
        var state = new FileState { Original = text, Current = text };
        _files.Add(path, state);
        return state;
    }

    public void SetText(string path, string? text) => _files[path].Current = text;

    // Keeps the new text of an edited project file; returns its path.
    public string Store(ProjectFile file)
    {
        _files[file.Path].Current = file.ToChange().NewText;
        return file.Path;
    }

    // The change of a file from disk to now; null when it is as it was.
    public ProjectFileChange? ToChange(string path)
    {
        FileState state = _files[path];
        return (state.Original, state.Current) switch
        {
            (null, null) => null,
            (null, string created) => new ProjectFileChange { FilePath = path, OldText = string.Empty, NewText = created, Creates = true },
            (string old, null) => new ProjectFileChange { FilePath = path, OldText = old, NewText = string.Empty, Deletes = true },
            (string old, string current) when old == current => null,
            (string old, string current) => new ProjectFileChange { FilePath = path, OldText = old, NewText = current, KeptRemovedPackageFor = state.KeptRemovedPackageFor },
        };
    }

    public class FileState
    {
        // Null: the file did not exist.
        public required string? Original { get; init; }

        // Null: the file does not exist (anymore).
        public required string? Current { get; set; }

        public string? KeptRemovedPackageFor { get; init; }
    }
}
