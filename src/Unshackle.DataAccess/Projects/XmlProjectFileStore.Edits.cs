using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.DataAccess.Projects;

// TAAK-317, council round 8 S3: the typed edits of IProjectFileMigration. Only the project file itself and config files next to it are
// edited; a package declared in a shared Directory.*.props is Manual (it would change other projects too), except a new <PackageVersion>
// under central package management, which changes nothing for a project that does not reference it.
public partial class XmlProjectFileStore
{
    private static readonly string[] s_itemPathAttributes = ["Include", "Update", "Remove"];

    public async Task<IReadOnlyList<ProjectFileEditOutcome>> ApplyAsync(
        string projectPath, IEnumerable<ProjectFileEdit> edits, IEnumerable<ProjectFileChange> earlier, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentNullException.ThrowIfNull(edits);
        ArgumentNullException.ThrowIfNull(earlier);
        var run = new EditRun(_fileSystem.Path.GetFullPath(projectPath), earlier, s_pathComparer);
        List<ProjectFileEditOutcome> outcomes = [];
        foreach (ProjectFileEdit edit in edits)
        {
            ProjectFileEditOutcome? outcome = edit switch
            {
                PackageRenameEdit rename => await RenamePackageAsync(run, rename, cancellationToken),
                PackageRemoveEdit remove => await RemovePackageAsync(run, remove, cancellationToken),
                PackageAddEdit add => await AddPackageAsync(run, add, cancellationToken),
                PropertyEdit property => await SetPropertyAsync(run, property, cancellationToken),
                ConfigFileRenameEdit rename => await RenameConfigFileAsync(run, rename, cancellationToken),
                ConfigFileTransformEdit transform => await TransformConfigFileAsync(run, transform, cancellationToken),
                _ => throw new UnreachableException(),
            };
            if (outcome is not null)
            {
                outcomes.Add(outcome);
            }
        }

        return outcomes;
    }

    private async Task<ProjectFileEditOutcome?> RenamePackageAsync(EditRun run, PackageRenameEdit edit, CancellationToken cancellationToken)
    {
        ProjectFile project = await LoadProjectAsync(run, cancellationToken);
        XElement? reference = Find(project, PackageReference, edit.OldName);
        if (reference is null)
        {
            return await SharedDeclarationAsync(run, edit, edit.OldName, cancellationToken);
        }

        int line = LineOf(reference);
        bool versioned = s_versionNames.Any(n => reference.Attribute(n) is not null || reference.Element(project.Namespace + n) is not null);
        ProjectFile? versions = await CentralVersionsAsync(run, project, cancellationToken);
        if (!versioned && versions is null)
        {
            return Manual(run, edit, line, "the reference has no version and there is no central package version for it; edit it by hand.");
        }

        if (Find(project, PackageReference, edit.NewName) is not null)
        {
            RemoveWithIndentation(reference);
        }
        else
        {
            reference.SetAttributeValue(Include, edit.NewName);
            foreach (string name in s_versionNames)
            {
                reference.Attribute(name)?.SetValue(edit.Version);
                reference.Element(project.Namespace + name)?.SetValue(edit.Version);
            }
        }

        List<string> touched = [run.Store(project)];
        if (!versioned && AddVersion(versions!, edit.OldName, edit.NewName, edit.Version))
        {
            touched.Add(run.Store(versions!));
        }

        return Applied(run, edit, line, touched);
    }

    private async Task<ProjectFileEditOutcome?> RemovePackageAsync(EditRun run, PackageRemoveEdit edit, CancellationToken cancellationToken)
    {
        ProjectFile project = await LoadProjectAsync(run, cancellationToken);
        XElement? reference = Find(project, PackageReference, edit.Name);
        if (reference is null)
        {
            return await SharedDeclarationAsync(run, edit, edit.Name, cancellationToken);
        }

        int line = LineOf(reference);
        RemoveWithIndentation(reference);
        return Applied(run, edit, line, [run.Store(project)]);
    }

    private async Task<ProjectFileEditOutcome?> AddPackageAsync(EditRun run, PackageAddEdit edit, CancellationToken cancellationToken)
    {
        ProjectFile project = await LoadProjectAsync(run, cancellationToken);
        if (Find(project, PackageReference, edit.Name) is not null || await SharedDeclarationAsync(run, edit, edit.Name, cancellationToken) is not null)
        {
            return null;
        }

        ProjectFile? versions = await CentralVersionsAsync(run, project, cancellationToken);
        var added = new XElement(project.Namespace + PackageReference, new XAttribute(Include, edit.Name));
        if (versions is null)
        {
            added.Add(new XAttribute("Version", edit.Version));
        }

        XElement? neighbour = project.Document.Descendants(project.Namespace + PackageReference).LastOrDefault();
        int line = neighbour is null ? 0 : LineOf(neighbour) + 1;
        AddNextTo(project, neighbour, added);
        List<string> touched = [run.Store(project)];
        if (versions is not null && AddVersion(versions, null, edit.Name, edit.Version))
        {
            touched.Add(run.Store(versions));
        }

        return Applied(run, edit, line, touched);
    }

    private async Task<ProjectFileEditOutcome?> SetPropertyAsync(EditRun run, PropertyEdit edit, CancellationToken cancellationToken)
    {
        ProjectFile project = await LoadProjectAsync(run, cancellationToken);
        XElement root = project.Document.Root!;
        List<XElement> found = [.. root.Elements(project.Namespace + "PropertyGroup").SelectMany(g => g.Elements(project.Namespace + edit.Name))];
        if (found.Count > 1 || found.Any(p => p.Attribute("Condition") is not null || p.Parent!.Attribute("Condition") is not null))
        {
            return Manual(run, edit, found.Count == 0 ? 0 : LineOf(found[0]), "the property is set more than once or under a condition; edit it by hand.");
        }

        if (found.Count == 1)
        {
            if (found[0].Value == edit.Value)
            {
                return null;
            }

            if (found[0].HasElements)
            {
                return Manual(run, edit, LineOf(found[0]), "the property has child elements; edit it by hand.");
            }

            found[0].Value = edit.Value;
            return Applied(run, edit, LineOf(found[0]), [run.Store(project)]);
        }

        // An unconditional group of the root; a conditional one may not apply.
        XElement? neighbour = root.Elements(project.Namespace + "PropertyGroup").LastOrDefault(g => g.Attribute("Condition") is null)?.Elements().LastOrDefault();
        AddNextTo(project, neighbour, new XElement(project.Namespace + edit.Name, edit.Value), "PropertyGroup");
        return Applied(run, edit, neighbour is null ? 0 : LineOf(neighbour) + 1, [run.Store(project)]);
    }

    private async Task<ProjectFileEditOutcome?> RenameConfigFileAsync(EditRun run, ConfigFileRenameEdit edit, CancellationToken cancellationToken)
    {
        string oldPath = NextToProject(run, edit.OldFileName);
        string newPath = NextToProject(run, edit.NewFileName);
        EditRun.FileState old = await StateAsync(run, oldPath, cancellationToken);
        if (old.Current is null)
        {
            return null;
        }

        if ((await StateAsync(run, newPath, cancellationToken)).Current is not null)
        {
            return Manual(run, edit, 0, $"{edit.NewFileName} already exists; merge the two by hand.", oldPath);
        }

        run.SetText(newPath, old.Current);
        run.SetText(oldPath, null);
        List<string> touched = [oldPath, newPath];
        ProjectFile project = await LoadProjectAsync(run, cancellationToken);
        List<XAttribute> items =
        [
            .. project.Document.Descendants().SelectMany(e => s_itemPathAttributes.Select(a => e.Attribute(a)).OfType<XAttribute>())
                .Where(a => a.Parent!.Parent?.Name.LocalName == "ItemGroup" && string.Equals(a.Value, edit.OldFileName, StringComparison.OrdinalIgnoreCase)),
        ];
        foreach (XAttribute item in items)
        {
            item.Value = edit.NewFileName;
        }

        if (items.Count > 0)
        {
            touched.Add(run.Store(project));
        }

        return Applied(run, edit, 0, touched, oldPath);
    }

    private async Task<ProjectFileEditOutcome?> TransformConfigFileAsync(EditRun run, ConfigFileTransformEdit edit, CancellationToken cancellationToken)
    {
        string path = NextToProject(run, edit.FileName);
        string? text = (await StateAsync(run, path, cancellationToken)).Current;
        if (text is null)
        {
            return null;
        }

        int line = 0;
        foreach (TextReplacement replacement in edit.Replacements)
        {
            int first = replacement.OldText.Length == 0 ? -1 : text.IndexOf(replacement.OldText, StringComparison.Ordinal);
            if (first < 0 || text.IndexOf(replacement.OldText, first + 1, StringComparison.Ordinal) >= 0)
            {
                return Manual(run, edit, 0, $"\"{replacement.OldText}\" does not occur exactly once; edit the file by hand.", path);
            }

            line = line == 0 ? text[..first].Count(c => c == '\n') + 1 : line;
            text = string.Concat(text.AsSpan(0, first), replacement.NewText, text.AsSpan(first + replacement.OldText.Length));
        }

        run.SetText(path, text);
        return Applied(run, edit, line, [path], path);
    }

    // A Directory.Build.props or Directory.Packages.props (without central management) above the project that declares the package.
    private async Task<ProjectFileEditOutcome?> SharedDeclarationAsync(EditRun run, ProjectFileEdit edit, string package, CancellationToken cancellationToken)
    {
        foreach (string? shared in new[] { FindUp(run.ProjectPath, BuildProps), FindUp(run.ProjectPath, PackagesProps) })
        {
            if (shared is not null && (await StateAsync(run, shared, cancellationToken)).Current is string text)
            {
                var file = new ProjectFile(_fileSystem.Path.GetFullPath(shared), text);
                if (Find(file, PackageReference, package) is XElement declared)
                {
                    return Manual(run, edit, LineOf(declared), "the package is declared in a shared file that other projects use too; edit it by hand.", file.Path);
                }
            }
        }

        return null;
    }

    // The Directory.Packages.props of the project when it manages versions centrally; null otherwise.
    private async Task<ProjectFile?> CentralVersionsAsync(EditRun run, ProjectFile project, CancellationToken cancellationToken)
    {
        string? path = FindUp(run.ProjectPath, PackagesProps);
        if (path is null || (await StateAsync(run, path, cancellationToken)).Current is not string text)
        {
            return null;
        }

        var versions = new ProjectFile(_fileSystem.Path.GetFullPath(path), text);
        return IsCentral(versions, [project]) ? versions : null;
    }

    // A <PackageVersion> for the package next to the old one (or the last one); false when it is there already.
    private static bool AddVersion(ProjectFile versions, string? next, string name, string version)
    {
        if (Find(versions, PackageVersion, name) is not null)
        {
            return false;
        }

        XElement? neighbour = (next is null ? null : Find(versions, PackageVersion, next)) ?? versions.Document.Descendants(versions.Namespace + PackageVersion).LastOrDefault();
        AddNextTo(versions, neighbour, new XElement(versions.Namespace + PackageVersion, new XAttribute(Include, name), new XAttribute("Version", version)));
        return true;
    }

    private async Task<ProjectFile> LoadProjectAsync(EditRun run, CancellationToken cancellationToken)
    {
        string text = (await StateAsync(run, run.ProjectPath, cancellationToken)).Current
            ?? throw new IOException($"{run.ProjectPath} does not exist.");
        return new ProjectFile(run.ProjectPath, text);
    }

    private async Task<EditRun.FileState> StateAsync(EditRun run, string path, CancellationToken cancellationToken)
    {
        path = _fileSystem.Path.GetFullPath(path);
        if (run.TryGet(path, out EditRun.FileState? state))
        {
            return state;
        }

        string? text = _fileSystem.File.Exists(path) ? (await ReadAsync(path, cancellationToken)).Text : null;
        return run.Add(path, text);
    }

    private string NextToProject(EditRun run, string fileName) =>
        _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine(_fileSystem.Path.GetDirectoryName(run.ProjectPath)!, fileName));

    private static int LineOf(XElement element) => ((IXmlLineInfo)element).HasLineInfo() ? ((IXmlLineInfo)element).LineNumber : 0;

    private static ProjectFileEditOutcome Applied(EditRun run, ProjectFileEdit edit, int line, IEnumerable<string> touched, string? filePath = null) => new()
    {
        Edit = edit,
        FilePath = filePath ?? run.ProjectPath,
        Line = line,
        Changes = [.. touched.Distinct(StringComparer.Ordinal).Select(run.ToChange).OfType<ProjectFileChange>()],
    };

    private static ProjectFileEditOutcome Manual(EditRun run, ProjectFileEdit edit, int line, string reason, string? filePath = null) =>
        new() { Edit = edit, FilePath = filePath ?? run.ProjectPath, Line = line, ManualReason = reason };
}
