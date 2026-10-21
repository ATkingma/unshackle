using Shouldly;
using Unshackle.Interface;
using Unshackle.Interface.Commands;
using Xunit;

namespace Unshackle.Tests.Interface;

// Exit codes of the real command line; nothing here loads a solution successfully, so these stay fast.
public class CliTests
{
    [Theory]
    [InlineData("")]
    [InlineData("unknown")]
    [InlineData("scan App.slnx")]
    [InlineData("scan App.slnx --fix nope")]
    [InlineData("fix App.slnx --fix fluentassertions --bogus")]
    public async Task RunAsync_InvalidArguments_ReturnsInvalidArguments(string commandLine)
    {
        // Arrange
        string[] args = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Act
        int exitCode = await Cli.RunAsync(args, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.InvalidArguments);
    }

    [Fact]
    public async Task RunAsync_List_ReturnsOk()
    {
        // Arrange
        string[] args = ["list"];

        // Act
        int exitCode = await Cli.RunAsync(args, TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Ok);
    }

    // Failure, not InvalidArguments: the fix id is accepted.
    [Theory]
    [InlineData("fluentassertions")]
    [InlineData("mediatr")]
    [InlineData("automapper")]
    public async Task RunAsync_ScanMissingSolution_ReturnsFailure(string fixId)
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), "unshackle tests", Guid.NewGuid().ToString("N"), "Missing App.slnx");

        // Act
        int exitCode = await Cli.RunAsync(["scan", path, "--fix", fixId], TestContext.Current.CancellationToken);

        // Assert
        exitCode.ShouldBe(ExitCodes.Failure);
    }
}
