namespace Unshackle.Core.Models;

// A project whose Manual findings held back an all-or-nothing fix (council round 18, X1/S2): the run summary names it and its reasons.
public class BlockingProject
{
    // The project file, or the project name when it has no file.
    public required string Project { get; init; }

    // The distinct reasons of its blocking Manual findings, in report order.
    public required IReadOnlyList<string> Reasons { get; init; }
}
