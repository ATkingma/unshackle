using System.IO.Abstractions;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.DataAccess.Projects;

// TAAK-319: reads obj/project.assets.json next to a project. Current only when it was restored for this project, is not older than the
// project file and its nearest Directory.Build.props / Directory.Packages.props, and lists every PackageReference the project file declares.
public class JsonPackageAssetsReader : IPackageAssetsReader
{
    private readonly IFileSystem _fileSystem;

    public JsonPackageAssetsReader(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    public PackageAssets Read(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        string project = _fileSystem.Path.GetFullPath(projectPath);
        string assetsPath = _fileSystem.Path.Combine(_fileSystem.Path.GetDirectoryName(project)!, "obj", "project.assets.json");
        if (!_fileSystem.File.Exists(assetsPath))
        {
            return PackageAssets.Missing;
        }

        try
        {
            using var document = JsonDocument.Parse(_fileSystem.File.ReadAllText(assetsPath));
            JsonElement root = document.RootElement;
            var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            List<PackageRequirement> requirements = [];
            List<PackageRequirement> projectRequirements = [];
            foreach (JsonProperty target in Properties(root, "targets"))
            {
                foreach (JsonProperty library in target.Value.EnumerateObject())
                {
                    string? kind = library.Value.TryGetProperty("type", out JsonElement type) ? type.GetString() : null;
                    if (kind == "project")
                    {
                        // Council round 17 T3: what a project reference brings, to compare with a pin.
                        projectRequirements.AddRange(Properties(library.Value, "dependencies")
                            .Select(d => new PackageRequirement { Package = d.Name, RequiredBy = library.Name, Range = d.Value.GetString() ?? string.Empty }));
                        continue;
                    }

                    if (kind != "package" || library.Name.Split('/') is not [string id, string version])
                    {
                        continue;
                    }

                    if (!resolved.TryGetValue(id, out string? earlier) || PackageRequirement.ParseVersion(version) > PackageRequirement.ParseVersion(earlier))
                    {
                        resolved[id] = version;
                    }

                    requirements.AddRange(Properties(library.Value, "dependencies")
                        .Select(d => new PackageRequirement { Package = d.Name, RequiredBy = library.Name, Range = d.Value.GetString() ?? string.Empty }));
                }
            }

            HashSet<string> direct = new(StringComparer.OrdinalIgnoreCase);
            JsonElement projectSection = root.TryGetProperty("project", out JsonElement p) ? p : default;
            foreach (JsonProperty framework in Properties(projectSection, "frameworks"))
            {
                foreach (JsonProperty dependency in Properties(framework.Value, "dependencies"))
                {
                    direct.Add(dependency.Name);
                    string range = dependency.Value.ValueKind == JsonValueKind.Object && dependency.Value.TryGetProperty("version", out JsonElement v) ? v.GetString() ?? string.Empty
                        : dependency.Value.ValueKind == JsonValueKind.String ? dependency.Value.GetString() ?? string.Empty
                        : string.Empty;
                    requirements.Add(new PackageRequirement { Package = dependency.Name, RequiredBy = "project", Range = range });
                }
            }

            AssetsState state = IsCurrent(project, assetsPath, projectSection, direct) ? AssetsState.Current : AssetsState.Stale;
            return new PackageAssets { State = state, Resolved = resolved, Requirements = requirements, ProjectReferenceRequirements = projectRequirements };
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException)
        {
            return PackageAssets.Missing;
        }
    }

    // The properties of an object child; none when the element or the child is not an object.
    private static List<JsonProperty> Properties(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out JsonElement child) && child.ValueKind == JsonValueKind.Object
            ? [.. child.EnumerateObject()]
            : [];

    private bool IsCurrent(string project, string assetsPath, JsonElement projectSection, HashSet<string> direct)
    {
        // Restored for this project file.
        if (projectSection.ValueKind != JsonValueKind.Object
            || !projectSection.TryGetProperty("restore", out JsonElement restore)
            || !restore.TryGetProperty("projectPath", out JsonElement restoredFor)
            || !string.Equals(_fileSystem.Path.GetFullPath(restoredFor.GetString() ?? string.Empty), project, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Not older than what decides the restore.
        DateTime written = _fileSystem.File.GetLastWriteTimeUtc(assetsPath);
        foreach (string? input in new[] { project, FindUp(project, "Directory.Build.props"), FindUp(project, "Directory.Packages.props") })
        {
            if (input is not null && _fileSystem.File.GetLastWriteTimeUtc(input) > written)
            {
                return false;
            }
        }

        // Every PackageReference of the project file was restored.
        try
        {
            return XDocument.Parse(_fileSystem.File.ReadAllText(project)).Descendants()
                .Where(e => e.Name.LocalName == "PackageReference" && e.Attribute("Include") is not null)
                .All(e => direct.Contains((string)e.Attribute("Include")!));
        }
        catch (XmlException)
        {
            return false;
        }
    }

    private string? FindUp(string projectPath, string fileName)
    {
        for (string? directory = _fileSystem.Path.GetDirectoryName(projectPath); directory is not null; directory = _fileSystem.Path.GetDirectoryName(directory))
        {
            string candidate = _fileSystem.Path.Combine(directory, fileName);
            if (_fileSystem.File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}
