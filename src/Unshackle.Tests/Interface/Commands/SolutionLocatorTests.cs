using Shouldly;
using Unshackle.Interface.Commands;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Interface.Commands;

public class SolutionLocatorTests
{
    [Fact]
    public void Resolve_GivenPath_ReturnsIt()
    {
        // Arrange
        using var directory = new TempDirectory();

        // Act
        string path = SolutionLocator.Resolve("Other.sln", directory.Path);

        // Assert
        path.ShouldBe("Other.sln");
    }

    [Fact]
    public void Resolve_OneSolution_ReturnsIt()
    {
        // Arrange
        using var directory = new TempDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "App.slnx"), "<Solution />");

        // Act
        string path = SolutionLocator.Resolve(null, directory.Path);

        // Assert
        path.ShouldBe(Path.Combine(directory.Path, "App.slnx"));
    }

    [Fact]
    public void Resolve_NoSolution_ThrowsInvalidArguments()
    {
        // Arrange
        using var directory = new TempDirectory();

        // Act
        CommandException ex = Should.Throw<CommandException>(() => SolutionLocator.Resolve(null, directory.Path));

        // Assert
        ex.ExitCode.ShouldBe(ExitCodes.InvalidArguments);
    }

    [Fact]
    public void Resolve_TwoSolutions_ThrowsAndNamesBoth()
    {
        // Arrange
        using var directory = new TempDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "A.sln"), string.Empty);
        File.WriteAllText(Path.Combine(directory.Path, "B.slnx"), "<Solution />");

        // Act
        CommandException ex = Should.Throw<CommandException>(() => SolutionLocator.Resolve(null, directory.Path));

        // Assert
        ex.ExitCode.ShouldBe(ExitCodes.InvalidArguments);
        ex.Message.ShouldContain("A.sln");
        ex.Message.ShouldContain("B.slnx");
    }
}
