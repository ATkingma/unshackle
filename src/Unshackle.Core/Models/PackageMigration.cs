namespace Unshackle.Core.Models;

// What a fix changes in the project files: the package it migrates from and the packages it migrates to.
public class PackageMigration
{
    // Also the assembly name: projects that reference this assembly are migrated.
    public required string RemovedPackage { get; init; }

    // Packages that go with it, for example MediatR.Contracts: removed as well when referenced directly, never replaced.
    public IReadOnlyList<string> AlsoRemovedPackages { get; init; } = [];

    // The first one replaces the removed package in place; the others are added next to it.
    public required IReadOnlyList<AddedPackage> AddedPackages { get; init; }
}
