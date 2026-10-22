using System.IO.Abstractions.TestingHelpers;
using Shouldly;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Projects;
using Xunit;

namespace Unshackle.Tests.DataAccess.Projects;

// TAAK-319: obj/project.assets.json per project, and the council round 10 E7 decision it feeds (pin SixLabors.Fonts < 2.0).
public class PackageAssetsReaderTests
{
    private const string Project = "/repo/src/Reports/Reports.csproj";
    private const string AssetsPath = "/repo/src/Reports/obj/project.assets.json";
    private const string ProjectText = "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"ClosedXML\" Version=\"0.105.0\" /></ItemGroup></Project>";

    private static readonly Version s_two = new(2, 0);
    private static readonly DateTime s_restored = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly MockFileSystem _fileSystem = new();

    [Theory]
    [InlineData("1.0.1", "[1.0.0, )", false, false)]
    [InlineData("2.1.0", "[1.0.0, )", true, true)]
    [InlineData("1.0.1", "[2.0.0, )", false, true)]
    [InlineData("1.0.1", "garbage", false, true)]
    public void Read_CurrentAssets_ResolvesAndRequirements(string fonts, string range, bool resolvesTwo, bool requiresTwo)
    {
        // Arrange
        AddProject(AssetsJson(fonts, range), s_restored);

        // Act
        PackageAssets assets = new JsonPackageAssetsReader(_fileSystem).Read(Project);

        // Assert
        assets.State.ShouldBe(AssetsState.Current);
        assets.Resolved["sixlabors.fonts"].ShouldBe(fonts);
        assets.ResolvesAtLeast("SixLabors.Fonts", s_two).ShouldBe(resolvesTwo);
        assets.RequiresAtLeast("SixLabors.Fonts", s_two).ShouldBe(requiresTwo);
    }

    [Fact]
    public void Read_NoAssetsFile_IsMissing()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(ProjectText));

        // Act / Assert
        new JsonPackageAssetsReader(_fileSystem).Read(Project).State.ShouldBe(AssetsState.Missing);
    }

    [Fact]
    public void Read_BrokenAssetsFile_IsMissing()
    {
        // Arrange
        AddProject("{ not json", s_restored);

        // Act / Assert
        new JsonPackageAssetsReader(_fileSystem).Read(Project).State.ShouldBe(AssetsState.Missing);
    }

    [Theory]
    [InlineData("project newer")]
    [InlineData("props newer")]
    [InlineData("other project")]
    [InlineData("undeclared package")]
    public void Read_AssetsNotMatchingTheProject_IsStale(string why)
    {
        // Arrange
        string assets = AssetsJson("1.0.1", "[1.0.0, )");
        if (why == "other project")
        {
            assets = assets.Replace("Reports.csproj", "Other.csproj", StringComparison.Ordinal);
        }

        AddProject(assets, s_restored);
        if (why == "project newer")
        {
            _fileSystem.File.SetLastWriteTimeUtc(Project, s_restored.AddMinutes(1));
        }
        else if (why == "props newer")
        {
            _fileSystem.AddFile("/repo/Directory.Packages.props", new MockFileData("<Project />"));
            _fileSystem.File.SetLastWriteTimeUtc("/repo/Directory.Packages.props", s_restored.AddMinutes(1));
        }
        else if (why == "undeclared package")
        {
            _fileSystem.File.WriteAllText(Project, ProjectText.Replace("</ItemGroup>", "<PackageReference Include=\"New\" Version=\"1.0.0\" /></ItemGroup>", StringComparison.Ordinal));
            _fileSystem.File.SetLastWriteTimeUtc(Project, s_restored.AddMinutes(-1));
        }

        // Act / Assert
        new JsonPackageAssetsReader(_fileSystem).Read(Project).State.ShouldBe(AssetsState.Stale);
    }

    private void AddProject(string assets, DateTime restored)
    {
        _fileSystem.AddFile(Project, new MockFileData(ProjectText));
        _fileSystem.File.SetLastWriteTimeUtc(Project, restored.AddMinutes(-1));
        _fileSystem.AddFile(AssetsPath, new MockFileData(assets));
        _fileSystem.File.SetLastWriteTimeUtc(AssetsPath, restored);
    }

    // ClosedXML requires SixLabors.Fonts with the given range; it resolved to the given version.
    private string AssetsJson(string fonts, string range) => $$"""
        {
          "version": 3,
          "targets": {
            "net8.0": {
              "ClosedXML/0.105.0": { "type": "package", "dependencies": { "SixLabors.Fonts": "{{range}}" } },
              "SixLabors.Fonts/{{fonts}}": { "type": "package" }
            }
          },
          "project": {
            "restore": { "projectPath": {{System.Text.Json.JsonSerializer.Serialize(_fileSystem.Path.GetFullPath(Project))}} },
            "frameworks": { "net8.0": { "dependencies": { "ClosedXML": { "target": "Package", "version": "[0.105.0, )" } } } }
          }
        }
        """;
}
