namespace Unshackle.Core.Models;

// A package a fix adds to the project files, with the version our proofs compile against.
public class AddedPackage
{
    public required string Name { get; init; }

    public required string Version { get; init; }

    // Only for projects whose original code calls one of these methods of the removed package (none: every project), e.g. a source generator only in the host project.
    public IReadOnlyList<string> OnlyWithCallTo { get; init; } = [];

    // PrivateAssets of a newly added reference, e.g. "all" so an analyzer does not flow to projects referencing this one.
    public string? PrivateAssets { get; init; }

    // Warnings of this package that stay warnings under TreatWarningsAsErrors, added to the file that gets the new reference.
    public IReadOnlyList<string> WarningsNotAsErrors { get; init; } = [];
}
