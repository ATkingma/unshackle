namespace Unshackle.Core.Models;

// One exact replacement of a ConfigFileTransformEdit.
public class TextReplacement
{
    public required string OldText { get; init; }

    public required string NewText { get; init; }
}
