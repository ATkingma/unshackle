namespace Unshackle.Core.Models;

public class VendoredFile
{
    // File name in the vendored folder, for example EntityBase.cs.
    public required string Name { get; init; }

    public required string Text { get; init; }
}
