namespace Unshackle.Core.Models;

/// <summary>Whether a project's restore output can be trusted (TAAK-319).</summary>
public enum AssetsState
{
    /// <summary>No <c>obj/project.assets.json</c>, or it cannot be read.</summary>
    Missing = 0,

    /// <summary>The assets file is for another project, older than the project file or its shared props, or misses a declared package.</summary>
    Stale = 1,

    /// <summary>The assets file matches the project file as it is now.</summary>
    Current = 2,
}
