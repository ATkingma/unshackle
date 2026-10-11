using Microsoft.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.Core.Abstractions;

/// <summary>
/// A pattern that needs the rest of the solution to decide, for example callers in other projects. The Fixer calls
/// <see cref="RewriteAsync"/> instead of <see cref="IPattern.Rewrite"/>; that one stays the answer without the solution.
/// </summary>
public interface ISolutionPattern : IPattern
{
    /// <summary>Like <see cref="IPattern.Rewrite"/>, with the solution <paramref name="model"/> belongs to.</summary>
    /// <param name="node">A node from <see cref="IPattern.Find"/>, still part of the original tree.</param>
    /// <param name="model">The semantic model of the original tree.</param>
    /// <param name="solution">The solution <paramref name="model"/> belongs to.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken);
}
