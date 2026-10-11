using System.ComponentModel;
using System.Diagnostics;
using Unshackle.Core.Abstractions;
using Unshackle.DataAccess.Processes;

namespace Unshackle.DataAccess.SourceControl;

public class GitSourceControl : ISourceControl
{
    public async Task<bool> HasUncommittedChangesAsync(string directory, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var startInfo = new ProcessStartInfo("git");
        // English messages, so "not a git repository" can be recognised.
        startInfo.Environment["LC_ALL"] = "C";
        startInfo.Environment["LANGUAGE"] = "C";
        // ArgumentList quotes for us, so directories with spaces work on every OS.
        foreach (string argument in new[] { "-C", directory, "status", "--porcelain" })
        {
            startInfo.ArgumentList.Add(argument);
        }

        ProcessResult result;
        try
        {
            result = await ProcessRunner.RunAsync(startInfo, cancellationToken);
        }
        catch (Win32Exception)
        {
            throw new InvalidOperationException("git is not installed, so the working tree cannot be checked.");
        }

        if (result.ExitCode == 0)
        {
            return result.Output.Length > 0;
        }

        // A failing git (dubious ownership, broken index, lock) must never read as "clean": the user could not undo the fix.
        throw new InvalidOperationException(result.Error.Contains("not a git repository", StringComparison.OrdinalIgnoreCase)
            ? $"{directory} is not in a git repository, so the fix could not be undone."
            : $"git could not check the working tree: {result.Error.Trim()}");
    }
}
