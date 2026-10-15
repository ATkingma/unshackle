namespace Unshackle.Interface.Commands;

// `unshackle fix --fix x` without a path: the one solution in the current directory, as `dotnet build` does.
public static class SolutionLocator
{
    /// <summary>The given path, or the only .slnx/.sln in <paramref name="directory"/>; otherwise a CommandException that says what to pass.</summary>
    public static string Resolve(string? given, string directory)
    {
        if (!string.IsNullOrWhiteSpace(given))
        {
            return given;
        }

        List<string> found = [.. Directory.EnumerateFiles(directory, "*.slnx").Concat(Directory.EnumerateFiles(directory, "*.sln")).Order(StringComparer.Ordinal)];
        return found.Count switch
        {
            1 => found[0],
            0 => throw new CommandException(ExitCodes.InvalidArguments, $"No .sln or .slnx in {directory}. Pass the solution: unshackle fix <solution> --fix <fix>."),
            _ => throw new CommandException(
                ExitCodes.InvalidArguments,
                $"Several solutions in {directory} ({string.Join(", ", found.Select(Path.GetFileName))}). Pass the one to migrate: unshackle fix <solution> --fix <fix>."),
        };
    }
}
