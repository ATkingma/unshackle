namespace Unshackle.Core.Fixes.FluentAssertions;

// The decision for one custom assertions class, taken once per run: it converts (Reason is null) or none of its parts do.
// Class, Should() extension and every call must agree, or the build breaks.
public class CustomAssertionPlan
{
    public required string ClassName { get; init; }

    public required CustomAssertionShape Shape { get; init; }

    // Null when the class and all its uses convert; else why nothing is rewritten.
    public string? Reason { get; init; }
}
