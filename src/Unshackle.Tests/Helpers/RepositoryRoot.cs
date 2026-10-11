namespace Unshackle.Tests.Helpers;

public static class RepositoryRoot
{
    // The directory with Unshackle.slnx, found from the test output directory upwards.
    public static string Path { get; } = Find();

    private static string Find()
    {
        for (DirectoryInfo? directory = new(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "Unshackle.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Unshackle.slnx not found above " + AppContext.BaseDirectory);
    }
}
