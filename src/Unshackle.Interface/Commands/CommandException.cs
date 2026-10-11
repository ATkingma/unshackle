namespace Unshackle.Interface.Commands;

// Stops a command with a message for the user and an exit code from ExitCodes.
public class CommandException : Exception
{
    public CommandException(int exitCode, string message)
        : base(message)
    {
        ExitCode = exitCode;
    }

    public CommandException()
    {
        ExitCode = ExitCodes.Failure;
    }

    public CommandException(string message)
        : this(ExitCodes.Failure, message)
    {
    }

    public CommandException(string message, Exception innerException)
        : base(message, innerException)
    {
        ExitCode = ExitCodes.Failure;
    }

    public int ExitCode { get; }
}
