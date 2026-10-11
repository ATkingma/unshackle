using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Abstractions;

/// <summary>Loads a solution from disk. Changed documents are written with <see cref="IProjectFileStore.SaveAsync"/>.</summary>
public interface ISolutionStore
{
    /// <summary>Loads a <c>.sln</c>, <c>.slnx</c> or project file with all its projects and references.</summary>
    /// <param name="path">Path to the solution or project file.</param>
    /// <param name="cancellationToken">Cancels the load.</param>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    /// <exception cref="SolutionLoadException">MSBuild reported one or more load failures.</exception>
    Task<Solution> LoadAsync(string path, CancellationToken cancellationToken);
}
