namespace Unshackle.Core.Abstractions;

/// <summary>Tells whether files can be overwritten safely, that is: every change can be undone with source control.</summary>
public interface ISourceControl
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="directory"/> lies in a working tree with uncommitted or
    /// untracked changes. A directory outside source control, or without the tool installed, counts as clean.
    /// </summary>
    /// <param name="directory">Any directory inside the working tree.</param>
    /// <param name="cancellationToken">Cancels the check.</param>
    Task<bool> HasUncommittedChangesAsync(string directory, CancellationToken cancellationToken);
}
