namespace Unshackle.Interface.Output;

public class DiffLine
{
    public required DiffKind Kind { get; init; }

    // 1-based; in the old text for Removed, in the new text for Added.
    public required int LineNumber { get; init; }

    public required string Text { get; init; }
}
