namespace Unshackle.Core.Models;

// What a fix changes in the project files: the package it migrates from and the one it migrates to.
public class PackageMigration
{
    // Also the assembly name: projects that reference this assembly are migrated.
    public required string RemovedPackage { get; init; }

    public required string AddedPackage { get; init; }

    public required string AddedVersion { get; init; }
}
