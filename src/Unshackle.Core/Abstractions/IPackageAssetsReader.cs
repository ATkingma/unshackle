using Unshackle.Core.Models;

namespace Unshackle.Core.Abstractions;

/// <summary>
/// The restored package assets of a project (TAAK-319), for decisions that depend on what NuGet resolved, such as pinning a package below a
/// version (council round 10, E7). Implemented in DataAccess; a fix that needs it gets it through its constructor.
/// </summary>
public interface IPackageAssetsReader
{
    /// <summary>The assets of the project; <see cref="PackageAssets.Missing"/> when there are none or they cannot be read. Never throws for a bad file.</summary>
    /// <param name="projectPath">The project file.</param>
    PackageAssets Read(string projectPath);
}
