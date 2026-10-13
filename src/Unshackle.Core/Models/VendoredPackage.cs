namespace Unshackle.Core.Models;

// A package that builds on the removed library and has no version for the new one that keeps its API (Ardalis.SharedKernel
// on MediatR). Its source, migrated to the new library, goes into the solution in place of the package (TAAK-194).
public class VendoredPackage
{
    // Also the assembly name: projects that reference this assembly lose the package.
    public required string Name { get; init; }

    // Folder in the project that gets the source, relative to the project file.
    public required string Folder { get; init; }

    // Text that goes on top of every source file, before the source itself; {0} is the version.
    public required string Header { get; init; }

    // Extra files in the folder, written as they are (for example the license of the package).
    public IReadOnlyList<VendoredFile> ExtraFiles { get; init; } = [];

    // The versions there is source for; another version stays Manual.
    public required IReadOnlyList<VendoredVersion> Versions { get; init; }
}
