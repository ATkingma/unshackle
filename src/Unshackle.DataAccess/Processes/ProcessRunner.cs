using System.Diagnostics;

namespace Unshackle.DataAccess.Processes;

// The one place that starts a child process: output is captured and the whole tree is killed on cancellation.
public static class ProcessRunner
{
    /// <summary>Runs the process to completion and captures its output.</summary>
    /// <exception cref="System.ComponentModel.Win32Exception">The executable was not found.</exception>
    public static async Task<ProcessResult> RunAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(startInfo);
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        using Process process = Process.Start(startInfo)!;
        try
        {
            Task<string> output = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> error = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);
            return new ProcessResult { ExitCode = process.ExitCode, Output = await output, Error = await error };
        }
        catch (OperationCanceledException)
        {
            // Disposing only closes the handle; without this, git or dotnet (and its MSBuild nodes) keep running.
            process.Kill(entireProcessTree: true);
            throw;
        }
    }
}
