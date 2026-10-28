using System.IO.Abstractions;
using System.Xml;
using System.Xml.Linq;
using Unshackle.Core.Abstractions;

namespace Unshackle.DataAccess.Projects;

// Council round 10, R1: MSBuild properties read from the project file and the nearest Directory.Build.props/.targets, in MSBuild's
// order (props, project, targets; the last unconditional value wins). Not a full evaluation: a conditional or computed value counts as
// unknown, and unknown takes the cautious side.
public class XmlProjectProperties : IProjectProperties
{
    private const string IsPackableProperty = "IsPackable";

    private readonly IFileSystem _fileSystem;

    public XmlProjectProperties(IFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        _fileSystem = fileSystem;
    }

    public bool IsPackable(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        string full = _fileSystem.Path.GetFullPath(projectPath);
        List<XDocument> chain = [];
        foreach (string? path in new[] { FindUp(full, "Directory.Build.props"), full, FindUp(full, "Directory.Build.targets") })
        {
            if (path is null)
            {
                continue;
            }

            if (Load(path) is not XDocument document)
            {
                return true;
            }

            chain.Add(document);
        }

        bool? value = null;
        foreach (XElement property in chain.SelectMany(d => d.Descendants().Where(e => e.Name.LocalName == IsPackableProperty && e.Parent?.Name.LocalName == "PropertyGroup")))
        {
            if (property.Attribute("Condition") is not null || property.Parent!.Attribute("Condition") is not null)
            {
                return true;
            }

            value = property.Value.Trim().ToUpperInvariant() switch
            {
                "FALSE" => false,
                "TRUE" => true,
                _ => null,
            };
            if (value is null)
            {
                return true;
            }
        }

        // The SDK default: Microsoft.NET.Test.Sdk (or IsTestProject) makes a project not packable; every other project is.
        return value ?? !IsTestProject(chain);
    }

    public IReadOnlyList<string>? PackageReferences(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        string full = _fileSystem.Path.GetFullPath(projectPath);
        var ids = new List<string>();
        foreach (string? path in new[] { FindUp(full, "Directory.Build.props"), full, FindUp(full, "Directory.Build.targets") })
        {
            if (path is null)
            {
                continue;
            }

            if (Load(path) is not XDocument document)
            {
                return null;
            }

            ids.AddRange(document.Descendants()
                .Where(e => e.Name.LocalName is "PackageReference" or "GlobalPackageReference")
                .Select(e => ((string?)e.Attribute("Include"))?.Trim())
                .OfType<string>()
                .Where(id => id.Length > 0));
        }

        return [.. ids.Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    // shortcut: only App.config in the project folder; an <AppConfig> property pointing elsewhere is not followed.
    public bool HasAppConfigSection(string projectPath, string sectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        string? config = AppConfig(projectPath);
        return config is not null
            && (Load(config) is not XDocument document || document.Root?.Elements().Any(e => e.Name.LocalName == sectionName) == true);
    }

    public IReadOnlyList<string>? AppConfigSectionChildren(string projectPath, string sectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);
        string? config = AppConfig(projectPath);
        XElement? section = config is null ? null : Load(config)?.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == sectionName);
        return section is null ? null : [.. section.Elements().Select(e => e.Name.LocalName)];
    }

    public IReadOnlyList<string>? TargetFrameworks(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        string full = _fileSystem.Path.GetFullPath(projectPath);
        HashSet<string> frameworks = new(StringComparer.OrdinalIgnoreCase);
        foreach (string? path in new[] { FindUp(full, "Directory.Build.props"), full, FindUp(full, "Directory.Build.targets") })
        {
            if (path is null)
            {
                continue;
            }

            if (Load(path) is not XDocument document)
            {
                return null;
            }

            foreach (XElement property in document.Descendants().Where(e => e.Parent?.Name.LocalName == "PropertyGroup"
                && e.Name.LocalName is "TargetFramework" or "TargetFrameworks" or "TargetFrameworkVersion"))
            {
                if (property.Value.Contains("$(", StringComparison.Ordinal))
                {
                    return null;
                }

                // An old-style project: <TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion> is net472.
                IEnumerable<string> values = property.Name.LocalName == "TargetFrameworkVersion"
                    ? ["net" + property.Value.Trim().TrimStart('v', 'V').Replace(".", string.Empty, StringComparison.Ordinal)]
                    : property.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                frameworks.UnionWith(values);
            }
        }

        return frameworks.Count == 0 ? null : [.. frameworks.Order(StringComparer.OrdinalIgnoreCase)];
    }

    public bool FileExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return _fileSystem.File.Exists(path);
    }

    private string? AppConfig(string projectPath)
    {
        string? directory = _fileSystem.Path.GetDirectoryName(_fileSystem.Path.GetFullPath(projectPath));
        return directory is null || !_fileSystem.Directory.Exists(directory) ? null
            : _fileSystem.Directory.EnumerateFiles(directory).FirstOrDefault(f => _fileSystem.Path.GetFileName(f).Equals("App.config", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsTestProject(List<XDocument> chain) =>
        chain.SelectMany(d => d.Descendants()).Any(e =>
            (e.Name.LocalName == "IsTestProject" && e.Parent?.Name.LocalName == "PropertyGroup" && e.Value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
            || (e.Name.LocalName == "PackageReference" && string.Equals((string?)e.Attribute("Include"), "Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase)));

    private XDocument? Load(string path)
    {
        try
        {
            return XDocument.Parse(_fileSystem.File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
        {
            return null;
        }
    }

    // The nearest file of that name in the project's folder or above it.
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
