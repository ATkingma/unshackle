namespace Unshackle.Core.Abstractions;

/// <summary>
/// MSBuild properties of a project that the workspace does not expose (council round 10, R1). Implemented in DataAccess; a fix that
/// needs one gets it through its constructor.
/// </summary>
public interface IProjectProperties
{
    /// <summary>
    /// Whether <c>dotnet pack</c> packs the project: its own or an imported <c>IsPackable</c>, otherwise the SDK default (true, false
    /// for a test project). When in doubt (a conditional value, an unreadable file) true: a packable project is the cautious side.
    /// </summary>
    /// <param name="projectPath">The project file.</param>
    bool IsPackable(string projectPath);

    /// <summary>
    /// The ids of the <c>PackageReference</c> (and <c>GlobalPackageReference</c>) items in the project file and the nearest
    /// <c>Directory.Build.props</c>/<c>.targets</c>, conditions ignored (council round 21, B13). Null when one of those files cannot be read.
    /// </summary>
    /// <param name="projectPath">The project file.</param>
    IReadOnlyList<string>? PackageReferences(string projectPath);

    /// <summary>
    /// Whether the <c>App.config</c> in the project's folder (any case) has a top-level element of that name under
    /// <c>&lt;configuration&gt;</c>. True when the file cannot be read (the cautious side); false without an App.config.
    /// </summary>
    /// <param name="projectPath">The project file.</param>
    /// <param name="sectionName">The element name, e.g. <c>specFlow</c>.</param>
    bool HasAppConfigSection(string projectPath, string sectionName);
}
