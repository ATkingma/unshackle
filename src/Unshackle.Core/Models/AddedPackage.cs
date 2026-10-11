namespace Unshackle.Core.Models;

// A package a fix adds to the project files, with the version our proofs compile against.
public class AddedPackage
{
    public required string Name { get; init; }

    public required string Version { get; init; }
}
