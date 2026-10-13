namespace Unshackle.Core;

// A call that an added namespace breaks (TAAK-198): ambiguous with an own extension (CS0121, AddedType is the class that brings
// the other candidate) or a rewritten call that binds to an own method instead (TAAK-199, AddedType null).
internal class AddedConflict
{
    public required string Name { get; init; }

    public required string? AddedType { get; init; }

    public required string Reason { get; init; }

    public required int Line { get; init; }
}
