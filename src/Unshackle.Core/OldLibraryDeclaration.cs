namespace Unshackle.Core;

// A declaration for the pre-filter: the names it declares and the identifiers of its signature (TAAK-298).
internal class OldLibraryDeclaration
{
    public required IReadOnlyList<string> Names { get; init; }

    public required HashSet<string> Signature { get; init; }
}
