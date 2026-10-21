namespace Unshackle.Core;

// One file as the pre-filter sees it: its identifiers and the declarations other files can reach (TAAK-298).
internal class OldLibraryFile
{
    public required string Path { get; init; }

    public required bool Escaped { get; init; }

    public required HashSet<string> Identifiers { get; init; }

    public required IReadOnlyList<OldLibraryDeclaration> Declarations { get; init; }
}
