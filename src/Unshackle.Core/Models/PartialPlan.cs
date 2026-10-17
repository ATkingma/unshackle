using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Models;

// A partial run (TAAK-274): the solution to fix, in which the code that stays on the old library is annotated, and the rows that report it.
public class PartialPlan
{
    // Nodes with this annotation (and everything in them) go to no pattern.
    public const string KeptAnnotationKind = "Unshackle.Kept";

    public required Solution Solution { get; init; }

    public required IReadOnlyList<Finding> Findings { get; init; }
}
