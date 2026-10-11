namespace Unshackle.Core.Abstractions;

/// <summary>Tells whether files can be overwritten safely, that is: every change can be undone with source control.</summary>
public interface ISourceControl
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="directory"/> lies in a working tree with uncommitted or
    /// untracked changes, <see langword="false"/> when it is clean.
    /// </summary>
    /// <param name="directory">Any directory inside the working tree.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    /// <exception cref="InvalidOperationException">
    /// The state is unknown: the directory is not under source control, the tool is missing, or the tool failed.
    /// </exception>
    Task<bool> HasUncommittedChangesAsync(string directory, CancellationToken cancellationToken);
}
