using System.Diagnostics;
using Shouldly;
using Unshackle.DataAccess.Processes;
using Unshackle.DataAccess.SourceControl;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.DataAccess;

// Needs git on the PATH (present on developer machines and GitHub runners).
public class GitSourceControlTests
{
    [Fact]
    public async Task HasUncommittedChangesAsync_NotARepository_ThrowsNotARepository()
    {
        // Arrange
        using var directory = new TempDirectory();
        await File.WriteAllTextAsync(Path.Combine(directory.Path, "a.cs"), "class A { }", TestContext.Current.CancellationToken);

        // Act
        Task act() => new GitSourceControl().HasUncommittedChangesAsync(directory.Path, TestContext.Current.CancellationToken);

        // Assert
        (await Should.ThrowAsync<InvalidOperationException>(act)).Message.ShouldContain("not in a git repository");
    }

    [Fact]
    public async Task HasUncommittedChangesAsync_BrokenIndex_ThrowsGitError()
    {
        // Arrange
        using var directory = new TempDirectory();
        await GitInitAsync(directory.Path);
        await File.WriteAllTextAsync(Path.Combine(directory.Path, ".git", "index"), "garbage", TestContext.Current.CancellationToken);

        // Act
        Task act() => new GitSourceControl().HasUncommittedChangesAsync(directory.Path, TestContext.Current.CancellationToken);

        // Assert
        (await Should.ThrowAsync<InvalidOperationException>(act)).Message.ShouldContain("git could not check");
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
        var startInfo = new ProcessStartInfo("git");
        startInfo.ArgumentList.Add("init");
        startInfo.ArgumentList.Add(directory);
        ProcessResult result = await ProcessRunner.RunAsync(startInfo, TestContext.Current.CancellationToken);
        result.ExitCode.ShouldBe(0, result.Error);
    }
}
