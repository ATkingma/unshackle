namespace Unshackle.Core.Models;

/// <summary>
/// One exact, typed edit of a project or config file (council round 8, S3). The set is closed: only the kinds in this assembly
/// exist (<see cref="PackageRenameEdit"/>, <see cref="PackageRemoveEdit"/>, <see cref="PackageAddEdit"/>, <see cref="PackageVersionEdit"/>, <see cref="PropertyEdit"/>,
/// <see cref="ConfigFileRenameEdit"/>, <see cref="ConfigFileTransformEdit"/>). Handle with a <c>switch</c>; another subtype is unreachable.
/// </summary>
public abstract class ProjectFileEdit
{
    private protected ProjectFileEdit()
    {
    }

    /// <summary>What the edit does, for the report, for example <c>PackageReference SpecFlow → Reqnroll 2.4.1</c>.</summary>
    public abstract string Describe();
}
