using System.IO.Abstractions;
using System.Text;
using System.Xml.Linq;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.DataAccess.Projects;

// Edits the project file, the nearest Directory.Build.props and the nearest Directory.Packages.props of each project.
// shortcut: imported props/targets and <PackageReference Update> are left alone; add them when users need it.
// A <Using> with Alias or Static of a removed namespace throws (Manual), it would not compile without the package.
public class XmlProjectFileStore : IProjectFileStore
{
    private const string BuildProps = "Directory.Build.props";
    private const string PackagesProps = "Directory.Packages.props";
    private const string PackageReference = "PackageReference";
    private const string PackageVersion = "PackageVersion";
    private const string Using = "Using";
    private const string Include = "Include";

    private static readonly string[] s_versionNames = ["Version", "VersionOverride"];
    private static readonly byte[] s_bom = Encoding.UTF8.GetPreamble();
    // Windows and macOS file systems ignore case by default: SRC\app and src\App are the same file there.
    private static readonly StringComparer s_pathComparer =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    // A C# file that is not valid UTF-8 was loaded as Latin-1 (MsBuildSolutionStore): bytes and characters map one to one, so it is written back byte-exact.
    private static readonly Encoding s_latin1 = Encoding.GetEncoding("iso-8859-1", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    private static readonly UTF8Encoding s_strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly IFileSystem _fileSystem;

    public XmlProjectFileStore(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    public async Task<IReadOnlyList<ProjectFileChange>> EditAsync(IEnumerable<ProjectMigration> migrations, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(migrations);
        List<(ProjectMigration Migration, string? BuildProps, string? PackagesProps)> projects =
            [.. migrations.Select(m => (m, FindUp(m.ProjectPath, BuildProps), FindUp(m.ProjectPath, PackagesProps)))];

        // A shared file keeps the removed package as soon as one project that uses the file keeps it, also a project on disk
        // that was not loaded (TAAK-118).
        Dictionary<string, string> keptForUnloaded = UnloadedUsers(projects);
        bool Keeps(string path) => keptForUnloaded.ContainsKey(_fileSystem.Path.GetFullPath(path)) || projects.Any(p => p.Migration.KeepRemovedPackage
            && (SamePath(p.Migration.ProjectPath, path) || SamePath(p.BuildProps, path) || SamePath(p.PackagesProps, path)));

        var files = new Dictionary<string, ProjectFile>(s_pathComparer);
        foreach ((ProjectMigration migration, string? buildProps, string? packagesProps) in projects)
        {
            List<ProjectFile> chain = [await LoadAsync(files, migration.ProjectPath, cancellationToken)];
            if (buildProps is not null)
            {
                chain.Add(await LoadAsync(files, buildProps, cancellationToken));
            }

            // A Directory.Packages.props without central package management (kgrzybek: conditioned <PackageReference Version>, TAAK-210)
            // is just another imported props file: its references are edited like the project's, and nothing gets a <PackageVersion>.
            bool central = false;
            if (packagesProps is not null)
            {
                ProjectFile packages = await LoadAsync(files, packagesProps, cancellationToken);
                central = IsCentral(packages, chain);
                if (!central)
                {
                    chain.Add(packages);
                }
            }

            foreach (ProjectFile file in chain.Where(f => !Keeps(f.Path)))
            {
                SwapUsings(file, migration);
            }

            string removed = migration.Package.RemovedPackage;
            // Extra packages first, added next to the removed one as if it stays; the first package then replaces it in place.
            foreach ((AddedPackage added, bool first) in migration.Package.AddedPackages.Select((p, i) => (p, i == 0)).Reverse())
            {
                Func<string, bool> keeps = first ? Keeps : _ => true;
                if (EditReferences(chain, removed, added, keeps, central, first))
                {
                    ProjectFile versions = await LoadAsync(files, packagesProps!, cancellationToken);
                    Migrate(versions, PackageVersion, Find(versions, PackageVersion, removed), keeps(versions.Path),
                        Find(versions, PackageVersion, added.Name) is not null, added, versioned: true);
                }
            }

            foreach (string also in migration.Package.AlsoRemovedPackages)
            {
                foreach (ProjectFile file in chain.Where(f => !Keeps(f.Path)))
                {
                    RemoveIfPresent(Find(file, PackageReference, also));
                }

                if (central && !Keeps(packagesProps!))
                {
                    RemoveIfPresent(Find(await LoadAsync(files, packagesProps!, cancellationToken), PackageVersion, also));
                }
            }
        }

        return
        [
            .. files.Values.Select(f => f.ToChange()).Where(c => c.NewText != c.OldText).Select(c => keptForUnloaded.TryGetValue(c.FilePath, out string? project)
                ? new ProjectFileChange { FilePath = c.FilePath, OldText = c.OldText, NewText = c.NewText, KeptRemovedPackageFor = project }
                : c),
        ];
    }

    // Shared file (full path) -> a .csproj on disk under it that is not migrated but still references a removed package,
    // in its own file or through that shared Directory.Build.props. A file it cannot read counts as a user.
    private Dictionary<string, string> UnloadedUsers(List<(ProjectMigration Migration, string? BuildProps, string? PackagesProps)> projects)
    {
        var users = new Dictionary<string, string>(s_pathComparer);
        HashSet<string> migrated = new(projects.Select(p => _fileSystem.Path.GetFullPath(p.Migration.ProjectPath)), s_pathComparer);
        HashSet<string> removed = new(projects.SelectMany(p => p.Migration.Package.AlsoRemovedPackages.Prepend(p.Migration.Package.RemovedPackage)), StringComparer.OrdinalIgnoreCase);
        IEnumerable<(string Shared, string Name)> shared = projects.SelectMany(p => new[] { (p.BuildProps, BuildProps), (p.PackagesProps, PackagesProps) })
            .Where(s => s.Item1 is not null).Select(s => (_fileSystem.Path.GetFullPath(s.Item1!), s.Item2)).Distinct();
        foreach ((string sharedPath, string name) in shared)
        {
            foreach (string project in _fileSystem.Directory.EnumerateFiles(_fileSystem.Path.GetDirectoryName(sharedPath)!, "*.csproj", SearchOption.AllDirectories))
            {
                string full = _fileSystem.Path.GetFullPath(project);
                // A nearer Directory.*.props of the same name hides this one from the project.
                if (migrated.Contains(full) || !SamePath(FindUp(full, name), sharedPath))
                {
                    continue;
                }

                string? buildProps = FindUp(full, BuildProps);
                if (References(full, removed) || (buildProps is not null && References(buildProps, removed)))
                {
                    users.TryAdd(sharedPath, full);
                    break;
                }
            }
        }

        return users;
    }

    private bool References(string path, HashSet<string> packages)
    {
        try
        {
            return XDocument.Parse(_fileSystem.File.ReadAllText(path)).Descendants()
                .Any(e => e.Name.LocalName == PackageReference && packages.Contains((string?)e.Attribute(Include) ?? string.Empty));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            return true;
        }
    }

    public async Task SaveAsync(IEnumerable<ProjectFileChange> changes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changes);
        // Check every file before the first write, so a stale file never leaves a half-migrated tree.
        var checkedFiles = new List<(ProjectFileChange Change, byte[]? Original, bool Bom, bool Latin1)>();
        foreach (ProjectFileChange change in changes)
        {
            // A new file (vendored source, TAAK-194) must not be there yet: it would be overwritten.
            if (change.Creates)
            {
                if (_fileSystem.File.Exists(change.FilePath))
                {
                    throw new InvalidOperationException($"{change.FilePath} already exists; move it and run the fix again.");
                }

                checkedFiles.Add((change, null, false, false));
                continue;
            }

            byte[] original = await _fileSystem.File.ReadAllBytesAsync(change.FilePath, cancellationToken);
            bool latin1 = change.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !IsUtf8(original);
            (string text, bool bom) = latin1 ? (s_latin1.GetString(original), false) : Decode(change.FilePath, original);
            if (text != change.OldText)
            {
                throw new InvalidOperationException($"{change.FilePath} changed after it was read; run the fix again.");
            }

            checkedFiles.Add((change, original, bom, latin1));
        }

        var written = new List<(string Path, byte[]? Original)>();
        var createdDirectories = new List<string>();
        try
        {
            foreach ((ProjectFileChange change, byte[]? original, bool bom, bool latin1) in checkedFiles)
            {
                string? directory = _fileSystem.Path.GetDirectoryName(change.FilePath);
                if (change.Creates && !string.IsNullOrEmpty(directory) && !_fileSystem.Directory.Exists(directory))
                {
                    _fileSystem.Directory.CreateDirectory(directory);
                    createdDirectories.Add(directory);
                }

                byte[] bytes = (latin1 ? s_latin1 : Encoding.UTF8).GetBytes(change.NewText);
                await ReplaceAsync(change.FilePath, bom ? [.. s_bom, .. bytes] : bytes, CancellationToken.None);
                written.Add((change.FilePath, original));
            }
        }
        catch
        {
            // Put back what was already written; the caller reports the original error.
            foreach ((string path, byte[]? original) in written)
            {
                try
                {
                    if (original is null)
                    {
                        _fileSystem.File.Delete(path);
                    }
                    else
                    {
                        await ReplaceAsync(path, original, CancellationToken.None);
                    }
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Keep restoring the others; the original error is the one to report.
                }
            }

            foreach (string directory in Enumerable.Reverse(createdDirectories))
            {
                try
                {
                    _fileSystem.Directory.Delete(directory);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Not empty or locked: left behind, the original error is the one to report.
                }
            }

            throw;
        }
    }

    private bool SamePath(string? a, string b) =>
        a is not null && s_pathComparer.Equals(_fileSystem.Path.GetFullPath(a), _fileSystem.Path.GetFullPath(b));

    // Temp file plus move: a crash mid-write never leaves a truncated file.
    private async Task ReplaceAsync(string path, byte[] bytes, CancellationToken cancellationToken)
    {
        string temp = path + ".unshackle.tmp";
        try
        {
            await _fileSystem.File.WriteAllBytesAsync(temp, bytes, cancellationToken);
            _fileSystem.File.Move(temp, path, overwrite: true);
        }
        finally
        {
            if (_fileSystem.File.Exists(temp))
            {
                _fileSystem.File.Delete(temp);
            }
        }
    }

    // Central package management: a <PackageVersion> in Directory.Packages.props, or ManagePackageVersionsCentrally set to true.
    private static bool IsCentral(ProjectFile packages, List<ProjectFile> chain) =>
        packages.Document.Descendants().Any(e => e.Name.LocalName == PackageVersion)
        || chain.Append(packages).SelectMany(f => f.Document.Descendants())
            .Any(e => e.Name.LocalName == "ManagePackageVersionsCentrally" && e.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase));

    // Returns true when this added package needs a <PackageVersion> (central package management).
    private static bool EditReferences(
        List<ProjectFile> chain, string removedPackage, AddedPackage package, Func<string, bool> keeps, bool hasPackagesProps, bool first)
    {
        bool hasAdded = chain.Any(f => Find(f, PackageReference, package.Name) is not null);
        ProjectFile? owner = chain.FirstOrDefault(f => Find(f, PackageReference, removedPackage) is not null);
        // An extra package next to a reference in a conditioned group of a shared file would only reach the projects of that
        // condition; the project itself gets it.
        if (!first && owner is not null && owner != chain[0] && Find(owner, PackageReference, removedPackage)!.Parent?.Attribute("Condition") is not null)
        {
            owner = null;
        }

        if (owner is null)
        {
            // The removed package came in transitively: the project itself gets the added one.
            Migrate(chain[0], PackageReference, null, keep: true, hasAdded, package, versioned: !hasPackagesProps);
            return !hasAdded && hasPackagesProps;
        }

        XElement removed = Find(owner, PackageReference, removedPackage)!;
        bool versioned = s_versionNames.Any(n => removed.Attribute(n) is not null || removed.Element(owner.Namespace + n) is not null);
        Migrate(owner, PackageReference, removed, keeps(owner.Path), hasAdded, package, versioned);
        return !versioned && hasPackagesProps;
    }

    // Swaps the removed package for the added one in place, removes it when the added one is already there,
    // or adds the added one next to it (or next to its kind) when the removed one stays.
    private static void Migrate(ProjectFile file, string kind, XElement? removed, bool keep, bool hasAdded, AddedPackage package, bool versioned)
    {
        if (removed is not null && !keep)
        {
            if (hasAdded)
            {
                RemoveWithIndentation(removed);
            }
            else
            {
                removed.SetAttributeValue(Include, package.Name);
                foreach (string name in s_versionNames)
                {
                    removed.Attribute(name)?.SetValue(package.Version);
                    removed.Element(file.Namespace + name)?.SetValue(package.Version);
                }
            }

            return;
        }

        if (!hasAdded)
        {
            var added = new XElement(file.Namespace + kind, new XAttribute(Include, package.Name));
            if (versioned)
            {
                added.Add(new XAttribute("Version", package.Version));
            }

            if (package.PrivateAssets is not null && kind == PackageReference)
            {
                added.Add(new XAttribute("PrivateAssets", package.PrivateAssets));
            }

            AddNextTo(file, removed ?? file.Document.Descendants(file.Namespace + kind).LastOrDefault(), added);
            if (package.WarningsNotAsErrors.Count > 0 && kind == PackageReference)
            {
                var property = new XElement(file.Namespace + "WarningsNotAsErrors", $"$(WarningsNotAsErrors);{string.Join(';', package.WarningsNotAsErrors)}");
                // An unconditional group of the root; a conditional one may not apply.
                XElement? group = file.Document.Root!.Elements(file.Namespace + "PropertyGroup").LastOrDefault(g => g.Attribute("Condition") is null);
                AddNextTo(file, group?.Elements().LastOrDefault(), property, "PropertyGroup");
            }
        }
    }

    private static void SwapUsings(ProjectFile file, ProjectMigration migration)
    {
        // An alias or static using of a removed namespace stops compiling once the package goes; the Fixer makes this Manual.
        XElement? special = file.Document.Descendants(file.Namespace + Using).FirstOrDefault(u => u.Attributes().Any(a => a.Name.LocalName is "Alias" or "Static")
            && migration.RemovedNamespaces.Contains((string?)u.Attribute(Include), StringComparer.Ordinal));
        if (special is not null)
        {
            throw new InvalidDataException(
                $"{file.Path} has <Using Include=\"{(string?)special.Attribute(Include)}\"> with Alias or Static; migrate it by hand (the package is removed).");
        }

        var missing = new Queue<string>(migration.AddedNamespaces.Where(ns => Find(file, Using, ns) is null));
        List<XElement> removed =
        [
            .. file.Document.Descendants(file.Namespace + Using).Where(u => u.Attributes().All(a => a.Name == Include)
                && migration.RemovedNamespaces.Contains((string?)u.Attribute(Include), StringComparer.Ordinal)),
        ];
        foreach (XElement directive in removed)
        {
            if (missing.TryDequeue(out string? added))
            {
                directive.SetAttributeValue(Include, added);
            }
            else
            {
                RemoveWithIndentation(directive);
            }
        }
    }

    private static XElement? Find(ProjectFile file, string kind, string package) =>
        file.Document.Descendants(file.Namespace + kind)
            .FirstOrDefault(e => string.Equals((string?)e.Attribute(Include), package, StringComparison.OrdinalIgnoreCase));

    // Copies the indentation of the neighbour; without one, a new group (ItemGroup or PropertyGroup) goes after the last element of the root.
    private static void AddNextTo(ProjectFile file, XElement? neighbour, XElement added, string group = "ItemGroup")
    {
        if (neighbour is not null)
        {
            neighbour.AddAfterSelf(new XText(Indentation(neighbour)), added);
            return;
        }

        XElement root = file.Document.Root!;
        XElement? last = root.Elements().LastOrDefault();
        string outer = last is null ? file.NewLine + "  " : Indentation(last);
        string inner = outer + outer.TrimStart('\r', '\n');
        var newGroup = new XElement(file.Namespace + group, new XText(inner), added, new XText(outer));
        if (last is null)
        {
            root.Add(new XText(outer), newGroup, new XText(file.NewLine));
        }
        else
        {
            last.AddAfterSelf(new XText(outer), newGroup);
        }
    }

    private static string Indentation(XElement element) =>
        element.PreviousNode is XText text && string.IsNullOrWhiteSpace(text.Value) ? text.Value : "\n";

    private static void RemoveIfPresent(XElement? element)
    {
        if (element is not null)
        {
            RemoveWithIndentation(element);
        }
    }

    // Also drops an <ItemGroup> that becomes empty, so no hollow groups stay behind.
    private static void RemoveWithIndentation(XElement element)
    {
        XElement? parent = element.Parent;
        if (element.PreviousNode is XText text && string.IsNullOrWhiteSpace(text.Value))
        {
            text.Remove();
        }

        element.Remove();
        if (parent is { Parent: not null } && !parent.HasElements && parent.Name.LocalName == "ItemGroup")
        {
            RemoveWithIndentation(parent);
        }
    }

    private string? FindUp(string projectPath, string fileName)
    {
        for (string? directory = _fileSystem.Path.GetDirectoryName(projectPath); directory is not null;
             directory = _fileSystem.Path.GetDirectoryName(directory))
        {
            string candidate = _fileSystem.Path.Combine(directory, fileName);
            if (_fileSystem.File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private async Task<ProjectFile> LoadAsync(Dictionary<string, ProjectFile> files, string path, CancellationToken cancellationToken)
    {
        path = _fileSystem.Path.GetFullPath(path);
        if (!files.TryGetValue(path, out ProjectFile? file))
        {
            (string text, _) = await ReadAsync(path, cancellationToken);
            file = new ProjectFile(path, text);
            files.Add(path, file);
        }

        return file;
    }

    private async Task<(string Text, bool Bom)> ReadAsync(string path, CancellationToken cancellationToken)
    {
        return Decode(path, await _fileSystem.File.ReadAllBytesAsync(path, cancellationToken));
    }

    private static bool IsUtf8(byte[] bytes)
    {
        try
        {
            _ = s_strictUtf8.GetCharCount(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    // Strict: rewriting a Latin-1 or UTF-16 file as UTF-8 would silently lose characters, so such a file is refused.
    private static (string Text, bool Bom) Decode(string path, byte[] bytes)
    {
        bool bom = bytes.AsSpan().StartsWith(s_bom);
        try
        {
            return (s_strictUtf8.GetString(bom ? bytes[s_bom.Length..] : bytes), bom);
        }
        catch (DecoderFallbackException ex)
        {
            throw new InvalidDataException($"{path} is not UTF-8; convert it or migrate it by hand.", ex);
        }
    }
}
