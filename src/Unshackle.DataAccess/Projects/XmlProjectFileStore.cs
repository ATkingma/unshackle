using System.IO.Abstractions;
using System.Text;
using System.Xml.Linq;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.DataAccess.Projects;

// Edits the project file, the nearest Directory.Build.props and the nearest Directory.Packages.props of each project.
// shortcut: imported props/targets, <PackageReference Update> and <Using> with Alias or Static are left alone; add them when users need it.
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

        // A shared file keeps the removed package as soon as one project that uses the file keeps it.
        bool Keeps(string path) => projects.Any(p => p.Migration.KeepRemovedPackage
            && (p.Migration.ProjectPath == path || p.BuildProps == path || p.PackagesProps == path));

        var files = new Dictionary<string, ProjectFile>(StringComparer.Ordinal);
        foreach ((ProjectMigration migration, string? buildProps, string? packagesProps) in projects)
        {
            List<ProjectFile> chain = [await LoadAsync(files, migration.ProjectPath, cancellationToken)];
            if (buildProps is not null)
            {
                chain.Add(await LoadAsync(files, buildProps, cancellationToken));
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
                if (EditReferences(chain, removed, added, keeps, packagesProps is not null))
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

                if (packagesProps is not null && !Keeps(packagesProps))
                {
                    RemoveIfPresent(Find(await LoadAsync(files, packagesProps, cancellationToken), PackageVersion, also));
                }
            }
        }

        return [.. files.Values.Select(f => f.ToChange()).Where(c => c.NewText != c.OldText)];
    }

    public async Task SaveAsync(IEnumerable<ProjectFileChange> changes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(changes);
        foreach (ProjectFileChange change in changes)
        {
            (string text, bool bom) = await ReadAsync(change.FilePath, cancellationToken);
            if (text != change.OldText)
            {
                throw new InvalidOperationException($"{change.FilePath} changed after it was read; run the fix again.");
            }

            byte[] bytes = Encoding.UTF8.GetBytes(change.NewText);
            await _fileSystem.File.WriteAllBytesAsync(change.FilePath, bom ? [.. s_bom, .. bytes] : bytes, cancellationToken);
        }
    }

    // Returns true when this added package needs a <PackageVersion> (central package management).
    private static bool EditReferences(
        List<ProjectFile> chain, string removedPackage, AddedPackage package, Func<string, bool> keeps, bool hasPackagesProps)
    {
        bool hasAdded = chain.Any(f => Find(f, PackageReference, package.Name) is not null);
        ProjectFile? owner = chain.FirstOrDefault(f => Find(f, PackageReference, removedPackage) is not null);
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
        }
    }

    private static void SwapUsings(ProjectFile file, ProjectMigration migration)
    {
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

    // Copies the indentation of the neighbour; without one, a new <ItemGroup> goes after the last element of the root.
    private static void AddNextTo(ProjectFile file, XElement? neighbour, XElement added)
    {
        if (neighbour is not null)
        {
            neighbour.AddAfterSelf(new XText(Indentation(neighbour)), added);
            return;
        }

        XElement root = file.Document.Root!;
        XElement? last = root.Elements().LastOrDefault();
        string outer = last is null ? "\n  " : Indentation(last);
        string inner = outer + outer.TrimStart('\r', '\n');
        var group = new XElement(file.Namespace + "ItemGroup", new XText(inner), added, new XText(outer));
        if (last is null)
        {
            root.Add(new XText(outer), group, new XText("\n"));
        }
        else
        {
            last.AddAfterSelf(new XText(outer), group);
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
        byte[] bytes = await _fileSystem.File.ReadAllBytesAsync(path, cancellationToken);
        bool bom = bytes.AsSpan().StartsWith(s_bom);
        return (Encoding.UTF8.GetString(bom ? bytes[s_bom.Length..] : bytes), bom);
    }
}
