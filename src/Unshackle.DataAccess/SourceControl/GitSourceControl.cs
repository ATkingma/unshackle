using System.ComponentModel;
using System.Diagnostics;
using Unshackle.Core.Abstractions;

namespace Unshackle.DataAccess.SourceControl;

public class GitSourceControl : ISourceControl
{
    public async Task<bool> HasUncommittedChangesAsync(string directory, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var startInfo = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        // ArgumentList quotes for us, so directories with spaces work on every OS.
        foreach (string argument in new[] { "-C", directory, "status", "--porcelain" })
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process process;
        try
        {
            process = Process.Start(startInfo)!;
        }
        catch (Win32Exception)
        {
            return false; // git is not installed
        }

        using (process)
        {
            Task<string> output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> error = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            await error;
            // A non-zero exit code means "not a git working tree".
            return process.ExitCode == 0 && (await output).Length > 0;
        }
    }
}
