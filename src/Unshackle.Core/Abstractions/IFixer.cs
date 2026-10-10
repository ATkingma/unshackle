using Microsoft.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.Core.Abstractions;

/// <summary>The single entry point (Facade): runs one fix on a solution.</summary>
public interface IFixer
{
    /// <summary>
    /// Runs every pattern of the requested fix on every C# document. The input solution is never changed;
    /// the report holds the new solution and one finding per node a pattern found.
    /// </summary>
    /// <param name="solution">The solution to fix.</param>
    /// <param name="request">Which fix to run.</param>
    /// <param name="cancellationToken">Cancels the run between patterns.</param>
    /// <exception cref="ArgumentException">The requested fix does not exist.</exception>
    Task<FixReport> FixAsync(Solution solution, FixRequest request, CancellationToken cancellationToken);
}
