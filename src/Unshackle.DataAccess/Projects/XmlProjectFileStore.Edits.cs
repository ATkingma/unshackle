using System.Diagnostics;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.DataAccess.Projects;

// TAAK-317, council round 8 S3: the typed edits of IProjectFileMigration. The project file itself and config files next to it are edited;
// a package declared in a shared Directory.*.props only when every project that imports it is loaded and unblocked (council round 10, G3),
// otherwise Manual (SharedDeclarationBlocked). Under central package management a new <PackageVersion> is added, and the old one removed
// only when no project under that Directory.Packages.props still uses it (G2).
public partial class XmlProjectFileStore
{
    private static readonly string[] s_itemPathAttributes = ["Include", "Update", "Remove"];

    public async Task<IReadOnlyList<ProjectFileEditOutcome>> ApplyAsync(
        string projectPath, IEnumerable<ProjectFileEdit> edits, IEnumerable<ProjectFileChange> earlier, ProjectFileEditContext context, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentNullException.ThrowIfNull(edits);
        ArgumentNullException.ThrowIfNull(earlier);
        ArgumentNullException.ThrowIfNull(context);
        var run = new EditRun(_fileSystem.Path.GetFullPath(projectPath), earlier, context, s_pathComparer);
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
        if (Find(project, PackageReference, edit.OldName) is XElement reference)
        {
            return await RenameInAsync(run, project, reference, edit, null, cancellationToken);
        }

        return await SharedDeclarationAsync(run, edit, edit.OldName, cancellationToken) switch
        {
            null => null,
            { Blocked: ProjectFileEditOutcome blocked } => blocked,
            { File: var shared, Declared: var declared } => await RenameInAsync(run, shared, declared, edit, shared.Path, cancellationToken),
        };
    }

    // The rename in the file that declares the reference: the project, or a shared file (filePath, G3).
    private async Task<ProjectFileEditOutcome> RenameInAsync(
        EditRun run, ProjectFile file, XElement reference, PackageRenameEdit edit, string? filePath, CancellationToken cancellationToken)
    {
        int line = LineOf(reference);
        bool versioned = s_versionNames.Any(n => reference.Attribute(n) is not null || reference.Element(file.Namespace + n) is not null);
        ProjectFile? versions = await CentralVersionsAsync(run, file, cancellationToken);
        if (!versioned && versions is null)
        {
            return Manual(run, edit, line, "the reference has no version and there is no central package version for it; edit it by hand.", filePath);
        }

        if (Find(file, PackageReference, edit.NewName) is not null)
        {
            RemoveWithIndentation(reference);
        }
        else
        {
            reference.SetAttributeValue(Include, edit.NewName);
            foreach (string name in s_versionNames)
            {
                reference.Attribute(name)?.SetValue(edit.Version);
                reference.Element(file.Namespace + name)?.SetValue(edit.Version);
            }
        }

        List<string> touched = [run.Store(file)];
        if (!versioned && AddVersion(versions!, edit.OldName, edit.NewName, edit.Version))
        {
            touched.Add(run.Store(versions!));
        }

        return Applied(run, edit, line, touched, filePath) is var applied && versions is not null && Find(versions, PackageVersion, edit.OldName) is not null
            ? WithPending(applied, versions.Path, edit.OldName)
            : applied;
    }

    private async Task<ProjectFileEditOutcome?> RemovePackageAsync(EditRun run, PackageRemoveEdit edit, CancellationToken cancellationToken)
    {
        ProjectFile project = await LoadProjectAsync(run, cancellationToken);
        if (Find(project, PackageReference, edit.Name) is XElement reference)
        {
            return await RemoveInAsync(run, project, reference, edit, null, cancellationToken);
        }

        return await SharedDeclarationAsync(run, edit, edit.Name, cancellationToken) switch
        {
            null => null,
            { Blocked: ProjectFileEditOutcome blocked } => blocked,
            { File: var shared, Declared: var declared } => await RemoveInAsync(run, shared, declared, edit, shared.Path, cancellationToken),
        };
    }

    private async Task<ProjectFileEditOutcome> RemoveInAsync(
        EditRun run, ProjectFile file, XElement reference, PackageRemoveEdit edit, string? filePath, CancellationToken cancellationToken)
    {
        int line = LineOf(reference);
        ProjectFile? central = await CentralVersionsAsync(run, file, cancellationToken);
        if (edit.AtMostVersion is string limit)
        {
            // P8: the declared version (the reference's own, else the central one) must be a plain version at most the limit.
            string? declared = s_versionNames.Select(n => (string?)reference.Attribute(n) ?? reference.Element(file.Namespace + n)?.Value).FirstOrDefault(v => v is not null)
                ?? (central is null ? null : (string?)Find(central, PackageVersion, edit.Name)?.Attribute("Version"));
            if (declared is null || !Version.TryParse(declared.Trim(), out Version? version) || !Version.TryParse(limit, out Version? max) || version > max)
            {
                return new ProjectFileEditOutcome
                {
                    Edit = edit,
                    FilePath = filePath ?? run.ProjectPath,
                    Line = line,
                    KeptInfo = $"PackageReference {edit.Name} {declared ?? "(no version)"} stays: it is not a version at most {limit}, which the new packages bring in.",
                };
            }
        }

        RemoveWithIndentation(reference);
        List<string> touched = [run.Store(file)];
        ProjectFileEditOutcome removed = Applied(run, edit, line, touched, filePath);
        return central is not null && Find(central, PackageVersion, edit.Name) is not null ? WithPending(removed, central.Path, edit.Name) : removed;
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

        // Council round 10, G5: the result must still parse.
        if (ParseError(path, text) is string error)
        {
            return Manual(run, edit, line, $"the result does not parse ({error}); edit the file by hand.", path);
        }

        run.SetText(path, text);
        return Applied(run, edit, line, [path], path);
    }

    // A Directory.Build.props or Directory.Packages.props (without central management) above the project that declares the package: the file
    // to edit, or why it may not be edited (council round 10, G3); null when no shared file declares it.
    private async Task<SharedDeclaration?> SharedDeclarationAsync(EditRun run, ProjectFileEdit edit, string package, CancellationToken cancellationToken)
    {
        foreach (string? shared in new[] { FindUp(run.ProjectPath, BuildProps), FindUp(run.ProjectPath, PackagesProps) })
        {
            if (shared is not null && (await StateAsync(run, shared, cancellationToken)).Current is string text)
            {
                var file = new ProjectFile(_fileSystem.Path.GetFullPath(shared), text);
                if (Find(file, PackageReference, package) is XElement declared)
                {
                    return SharedBlocker(run, file.Path) is string why
                        ? new SharedDeclaration(file, declared, Manual(run, edit, LineOf(declared),
                            $"the package is declared in a shared file that {why}; edit it by hand.", file.Path, nameof(ProjectBlockingReason.SharedDeclarationBlocked)))
                        : new SharedDeclaration(file, declared, null);
                }
            }
        }

        return null;
    }

    // Why a shared file may not be edited (G3): a project that imports it is not loaded, or has blocking rows; null when every one is fine.
    private string? SharedBlocker(EditRun run, string sharedPath)
    {
        foreach (string user in Importers(sharedPath))
        {
            if (!run.Context.LoadedProjects.Contains(user))
            {
                return $"{user} also imports and is not loaded";
            }

            if (run.Context.BlockedProjects.TryGetValue(user, out string? why))
            {
                return $"{user} also imports and still has blocking rows ({why})";
            }
        }

        return null;
    }

    // The project files on disk under a shared Directory.*.props that import it (a nearer file of the same name hides it), in path order.
    private IEnumerable<string> Importers(string sharedPath)
    {
        string name = _fileSystem.Path.GetFileName(sharedPath);
        return _fileSystem.Directory.EnumerateFiles(_fileSystem.Path.GetDirectoryName(sharedPath)!, "*.csproj", SearchOption.AllDirectories)
            .Select(p => _fileSystem.Path.GetFullPath(p))
            .Where(p => FindUp(p, name) is string found && SamePath(found, sharedPath))
            .Order(StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<ProjectFileEditOutcome>> RemoveOldVersionsAsync(
        IEnumerable<OldPackageVersion> versions, IEnumerable<ProjectFileChange> earlier, ProjectFileEditContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(versions);
        ArgumentNullException.ThrowIfNull(earlier);
        ArgumentNullException.ThrowIfNull(context);
        List<ProjectFileEditOutcome> outcomes = [];
        foreach (IGrouping<string, OldPackageVersion> props in versions.GroupBy(v => _fileSystem.Path.GetFullPath(v.PropsPath), s_pathComparer))
        {
            // One run per Directory.Packages.props: its removals build on each other and on every earlier change.
            var run = new EditRun(props.Key, earlier, context, s_pathComparer);
            foreach (OldPackageVersion version in props)
            {
                if ((await StateAsync(run, props.Key, cancellationToken)).Current is not string text)
                {
                    continue;
                }

                var file = new ProjectFile(props.Key, text);
                var edit = new PackageVersionRemoveEdit { Name = version.Package };
                if (Find(file, PackageVersion, version.Package) is not XElement old)
                {
                    continue;
                }

                int line = LineOf(old);
                outcomes.Add(await WhyOldVersionStaysAsync(run, file, version.Package, cancellationToken) is string why
                    ? new ProjectFileEditOutcome { Edit = edit, FilePath = props.Key, Line = line, KeptInfo = $"The PackageVersion of {version.Package} stays in {props.Key}: {why}." }
                    : RemoveVersion(run, file, old, edit, line));
            }
        }

        return outcomes;
    }

    private static ProjectFileEditOutcome RemoveVersion(EditRun run, ProjectFile file, XElement old, PackageVersionRemoveEdit edit, int line)
    {
        RemoveWithIndentation(old);
        return Applied(run, edit, line, [run.Store(file)], file.Path);
    }

    private static ProjectFileEditOutcome WithPending(ProjectFileEditOutcome outcome, string propsPath, string package) => new()
    {
        Edit = outcome.Edit,
        FilePath = outcome.FilePath,
        Line = outcome.Line,
        Changes = outcome.Changes,
        PendingOldVersions = [new OldPackageVersion { PropsPath = propsPath, Package = package }],
    };

    // Council rounds 10 and 12, G2/G7: the old <PackageVersion> goes only when it is no GlobalPackageReference, every project under the
    // Directory.Packages.props is loaded, and none of them (nor its Directory.Build.props) still references the package. Returns why it stays.
    private async Task<string?> WhyOldVersionStaysAsync(EditRun run, ProjectFile versions, string package, CancellationToken cancellationToken)
    {

        string? why = null;
        if (Find(versions, "GlobalPackageReference", package) is not null)
        {
            why = "it is a GlobalPackageReference";
        }

        foreach (string user in why is null ? Importers(versions.Path) : Enumerable.Empty<string>())
        {
            if (!run.Context.LoadedProjects.Contains(user))
            {
                why = $"{user} is not loaded";
                break;
            }

            string? buildProps = FindUp(user, BuildProps);
            string[] files = buildProps is null ? [user] : [user, _fileSystem.Path.GetFullPath(buildProps)];
            foreach (string path in files)
            {
                if ((await StateAsync(run, path, cancellationToken)).Current is string text && Find(new ProjectFile(path, text), PackageReference, package) is not null)
                {
                    why = $"{path} still references it";
                    break;
                }
            }

            if (why is not null)
            {
                break;
            }
        }

        return why;
    }

    // G5: null when the text parses as its file kind (JSON, or XML for .xml/.config/.props/.targets); other files are not checked.
    private static string? ParseError(string path, string text)
    {
        string extension = System.IO.Path.GetExtension(path).ToUpperInvariant();
        try
        {
            if (extension == ".JSON")
            {
                using var document = JsonDocument.Parse(text, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
            }
            else if (extension is ".XML" or ".CONFIG" or ".PROPS" or ".TARGETS")
            {
                _ = XDocument.Parse(text);
            }

            return null;
        }
        catch (Exception ex) when (ex is JsonException or XmlException)
        {
            return ex.Message;
        }
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

    private static ProjectFileEditOutcome Applied(EditRun run, ProjectFileEdit edit, int line, IEnumerable<string> touched, string? filePath = null, string? note = null) => new()
    {
        Edit = edit,
        FilePath = filePath ?? run.ProjectPath,
        Line = line,
        Changes = [.. touched.Distinct(StringComparer.Ordinal).Select(run.ToChange).OfType<ProjectFileChange>()],
        Note = note,
    };

    private static ProjectFileEditOutcome Manual(EditRun run, ProjectFileEdit edit, int line, string reason, string? filePath = null, string? code = null) =>
        new() { Edit = edit, FilePath = filePath ?? run.ProjectPath, Line = line, ManualReason = reason, ManualCode = code };

    // A shared file that declares a package: edit Declared in File, unless Blocked says why not (G3).
    private class SharedDeclaration
    {
        public SharedDeclaration(ProjectFile file, XElement declared, ProjectFileEditOutcome? blocked)
        {
            File = file;
            Declared = declared;
            Blocked = blocked;
        }

        public ProjectFile File { get; }

        public XElement Declared { get; }

        public ProjectFileEditOutcome? Blocked { get; }
    }
}
