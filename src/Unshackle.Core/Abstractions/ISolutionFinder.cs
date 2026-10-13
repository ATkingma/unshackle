using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Abstractions;

/// <summary>
/// A pattern whose findings depend on other projects, for example code in another project that needs a change in this one.
/// The Fixer calls <see cref="FindAsync"/> instead of <see cref="IPattern.Find"/>; that one stays the answer without the solution.
/// </summary>
public interface ISolutionFinder : IPattern
{
    /// <summary>Like <see cref="IPattern.Find"/>, with the solution <paramref name="model"/> belongs to.</summary>
    /// <param name="root">The syntax root of the document.</param>
    /// <param name="model">The semantic model of the same document.</param>
    /// <param name="solution">The solution <paramref name="model"/> belongs to.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    Task<IReadOnlyList<SyntaxNode>> FindAsync(SyntaxNode root, SemanticModel model, Solution solution, CancellationToken cancellationToken);
}
