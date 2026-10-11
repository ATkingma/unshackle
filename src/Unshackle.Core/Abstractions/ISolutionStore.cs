using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Abstractions;

/// <summary>Loads a solution from disk and writes a changed version of it back.</summary>
public interface ISolutionStore
{
    /// <summary>Loads a <c>.sln</c>, <c>.slnx</c> or project file with all its projects and references.</summary>
    /// <param name="path">Path to the solution or project file.</param>
    /// <param name="cancellationToken">Cancels the load.</param>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="SolutionLoadException">MSBuild reported one or more load failures.</exception>
    Task<Solution> LoadAsync(string path, CancellationToken cancellationToken);

    /// <summary>Writes every changed document of <paramref name="solution"/> to disk.</summary>
    /// <param name="solution">A solution derived from the one returned by <see cref="LoadAsync"/> on this store.</param>
    /// <param name="cancellationToken">Cancels the save.</param>
    /// <exception cref="InvalidOperationException">Nothing was loaded, or the changes could not be applied.</exception>
    Task SaveAsync(Solution solution, CancellationToken cancellationToken);
}
