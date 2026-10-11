namespace Unshackle.Core.Abstractions;

/// <summary>Thrown by <see cref="ISolutionStore.LoadAsync"/> when MSBuild could not load the solution correctly.</summary>
public class SolutionLoadException : Exception
{
    /// <summary>Creates the exception with the load failures as reported by MSBuild.</summary>
    /// <param name="errors">One message per failure.</param>
    public SolutionLoadException(IEnumerable<string> errors)
        : this([.. errors])
    {
    }

    /// <summary>Creates the exception without failure details.</summary>
    public SolutionLoadException()
        : this([])
    {
    }

    /// <summary>Creates the exception with a single message.</summary>
    /// <param name="message">The failure.</param>
    public SolutionLoadException(string message)
        : this([message])
    {
    }

    /// <summary>Creates the exception with a single message and its cause.</summary>
    /// <param name="message">The failure.</param>
    /// <param name="innerException">The cause.</param>
    public SolutionLoadException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = [message];
    }

    private SolutionLoadException(IReadOnlyList<string> errors)
        : base($"The solution could not be loaded ({errors.Count} error(s)).")
    {
        Errors = errors;
    }

    /// <summary>The load failures, one message each.</summary>
    public IReadOnlyList<string> Errors { get; }
}
