using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Models;

// A partial run (TAAK-274): the solution to fix, in which the code that stays on the old library is annotated, and the rows that report it.
public class PartialPlan
{
    // Nodes with this annotation (and everything in them) go to no pattern.
    public const string KeptAnnotationKind = "Unshackle.Kept";

    public required Solution Solution { get; init; }

    public required IReadOnlyList<Finding> Findings { get; init; }

    // TAAK-326 (council round 13, E17): whole projects that stay on the old library, project file path -> why. Their documents keep their
    // text and their project files get no package edit, like a project with a blocking project-level row (TAAK-320/321). Empty: none.
    public IReadOnlyDictionary<string, string> KeptProjects { get; init; } = new Dictionary<string, string>();
}
