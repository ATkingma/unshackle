using System.IO.Abstractions.TestingHelpers;
using Shouldly;
using Unshackle.DataAccess.Projects;
using Xunit;

namespace Unshackle.Tests.DataAccess.Projects;

// Council round 11, E11: the IsWorksheets1Based key in config files, as file:line.
public class FileConfigKeySearchTests
{
    private const string Key = "IsWorksheets1Based";

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public void Find_KeyInConfigAndAppSettings_ReturnsEveryLineInPathOrder()
    {
        // Arrange
        _fileSystem.AddFile("/repo/src/Web/appsettings.Development.json", new MockFileData("{\n  \"EPPlus\": {\n    \"ExcelPackage\": { \"IsWorksheets1Based\": true }\n  }\n}\n"));
        _fileSystem.AddFile("/repo/src/App/App.config", new MockFileData("<configuration>\n<appSettings>\n<add key=\"EPPlus:ExcelPackage.IsWorksheets1Based\" value=\"true\" />\n</appSettings>\n</configuration>\n"));
        _fileSystem.AddFile("/repo/src/App/Program.cs", new MockFileData("// IsWorksheets1Based\n"));
        _fileSystem.AddFile("/repo/src/App/settings.json", new MockFileData("IsWorksheets1Based\n"));

        // Act
        IReadOnlyList<string> hits = new FileConfigKeySearch(_fileSystem).Find("/repo", Key);

        // Assert
        hits.ShouldBe([$"{FullPath("/repo/src/App/App.config")}:3", $"{FullPath("/repo/src/Web/appsettings.Development.json")}:3"]);
    }

    [Theory]
    [InlineData("bin")]
    [InlineData("obj")]
    [InlineData(".git")]
    public void Find_KeyOnlyUnderSkippedFolder_ReturnsNothing(string folder)
    {
        // Arrange
        _fileSystem.AddFile($"/repo/src/App/{folder}/Debug/App.config", new MockFileData("<add key=\"IsWorksheets1Based\" />\n"));

        // Act / Assert
        new FileConfigKeySearch(_fileSystem).Find("/repo", Key).ShouldBeEmpty();
    }

    [Fact]
    public void Find_KeyNowhere_ReturnsNothing()
    {
        // Arrange
        _fileSystem.AddFile("/repo/Web.config", new MockFileData("<configuration />\n"));

        // Act / Assert
        new FileConfigKeySearch(_fileSystem).Find("/repo", Key).ShouldBeEmpty();
    }

    private string FullPath(string path) => _fileSystem.Path.GetFullPath(path);
}
