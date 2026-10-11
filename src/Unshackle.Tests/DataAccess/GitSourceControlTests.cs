using System.Diagnostics;
using Shouldly;
using Unshackle.DataAccess.SourceControl;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.DataAccess;

// Needs git on the PATH (present on developer machines and GitHub runners).
public class GitSourceControlTests
{
    [Fact]
    public async Task HasUncommittedChangesAsync_NotARepository_ReturnsFalse()
    {
        // Arrange
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "a.cs"), "class A { }", TestContext.Current.CancellationToken);

        // Act
        bool dirty = await new GitSourceControl().HasUncommittedChangesAsync(directory.Path, TestContext.Current.CancellationToken);

        // Assert
        dirty.ShouldBeFalse();
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_CleanRepository_ReturnsFalse()
    {
        // Arrange
        using var directory = new TempDirectory();
        await GitInitAsync(directory.Path);

        // Act
        bool dirty = await new GitSourceControl().HasUncommittedChangesAsync(directory.Path, TestContext.Current.CancellationToken);

        // Assert
        dirty.ShouldBeFalse();
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_UntrackedFile_ReturnsTrue()
    {
        // Arrange
        using var directory = new TempDirectory();
        await GitInitAsync(directory.Path);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "a.cs"), "class A { }", TestContext.Current.CancellationToken);

        // Act
        bool dirty = await new GitSourceControl().HasUncommittedChangesAsync(directory.Path, TestContext.Current.CancellationToken);

        // Assert
        dirty.ShouldBeTrue();
    }

    private static async Task GitInitAsync(string directory)
    {
        var startInfo = new ProcessStartInfo("git") { UseShellExecute = false, RedirectStandardOutput = true };
        startInfo.ArgumentList.Add("init");
        startInfo.ArgumentList.Add(directory);
        using Process process = Process.Start(startInfo)!;
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        process.ExitCode.ShouldBe(0);
    }
}
