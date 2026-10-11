namespace Unshackle.Interface.Commands;

// Process exit codes, for scripts ($LASTEXITCODE in PowerShell, $? in bash). Manual findings are not an error.
public static class ExitCodes
{
    public const int Ok = 0;

    // The solution could not be loaded, the report or the changes could not be written, or fix refused to write.
    public const int Failure = 1;

    public const int InvalidArguments = 2;
}
