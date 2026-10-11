namespace Unshackle.Tests.Helpers;

// A fresh directory under the system temp path (with a space in the name), deleted on dispose.
public class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "unshackle tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort: a locked file must not fail the test.
        }

        GC.SuppressFinalize(this);
    }
}
