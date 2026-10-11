namespace Unshackle.DataAccess.Processes;

public class ProcessResult
{
    public required int ExitCode { get; init; }

    public required string Output { get; init; }

    public required string Error { get; init; }
}
