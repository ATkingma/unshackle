using Microsoft.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.Core.Abstractions;

/// <summary>
/// An <see cref="IFix.AllOrNothing"/> fix that can keep the code tied to its Manual findings on the old library and migrate the
/// rest, with both libraries side by side (TAAK-274).
/// </summary>
public interface IPartialFix
{
    /// <summary>
    /// Plans a partial run: the solution to fix instead, with the code that stays annotated (<see cref="PartialPlan.KeptAnnotationKind"/>),
    /// and the rows that report it.
    /// </summary>
    /// <param name="solution">The original solution.</param>
    /// <param name="roots">The Manual findings that block a full migration.</param>
    /// <param name="cancellationToken">Cancels the planning.</param>
    /// <returns>The plan, or <see langword="null"/> when no part can move safely.</returns>
    Task<PartialPlan?> PlanPartialAsync(Solution solution, IReadOnlyList<Finding> roots, CancellationToken cancellationToken);
}
