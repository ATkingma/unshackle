using System.IO.Abstractions;
using Unshackle.Core.Fixes.EPPlus;

namespace Unshackle.DataAccess.Projects;

// Council round 11, E11: every line of a *.config or appsettings*.json under the solution directory that contains the key, as file:line
// (1-based), in path order. bin, obj and .git are skipped; an unreadable file is skipped too.
public class FileConfigKeySearch : IConfigKeySearch
{
    private static readonly HashSet<string> s_skipped = new(["bin", "obj", ".git"], StringComparer.OrdinalIgnoreCase);

    private readonly IFileSystem _fileSystem;

    public FileConfigKeySearch(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    public IReadOnlyList<string> Find(string solutionDirectory, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(solutionDirectory);
        ArgumentException.ThrowIfNullOrEmpty(key);
        List<string> hits = [];
        if (!_fileSystem.Directory.Exists(solutionDirectory))
        {
            return hits;
        }

        foreach (string file in Files(_fileSystem.Path.GetFullPath(solutionDirectory)).Order(StringComparer.Ordinal))
        {
            string[] lines;
            try
            {
                lines = _fileSystem.File.ReadAllLines(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(key, StringComparison.Ordinal))
                {
                    hits.Add($"{file}:{i + 1}");
                }
            }
        }

        return hits;
    }

    private IEnumerable<string> Files(string directory)
    {
        foreach (string file in _fileSystem.Directory.EnumerateFiles(directory))
        {
            string name = _fileSystem.Path.GetFileName(file);
            if (name.EndsWith(".config", StringComparison.OrdinalIgnoreCase)
                || (name.StartsWith("appsettings", StringComparison.OrdinalIgnoreCase) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            {
                yield return file;
            }
        }

        foreach (string child in _fileSystem.Directory.EnumerateDirectories(directory)
            .Where(d => !s_skipped.Contains(_fileSystem.Path.GetFileName(d))))
        {
            foreach (string file in Files(child))
            {
                yield return file;
            }
        }
    }
}
