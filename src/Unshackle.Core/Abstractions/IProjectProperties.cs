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
}
