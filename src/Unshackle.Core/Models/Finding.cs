namespace Unshackle.Core.Models;

public class Finding
{
    public required string FilePath { get; init; }

    // 1-based; 0 when the finding is about the whole file.
    public required int Line { get; init; }

    public required string FixId { get; init; }

    public required string PatternId { get; init; }

    public required PatternResult Result { get; init; }
}
