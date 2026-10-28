using System.IO.Abstractions.TestingHelpers;
using Shouldly;
using Unshackle.DataAccess.Projects;
using Xunit;

namespace Unshackle.Tests.DataAccess.Projects;

// Council round 10, R1: IsPackable from the project and the nearest Directory.Build.props/.targets; unset is the SDK default.
public class XmlProjectPropertiesTests
{
    private const string Project = "/repo/src/Lib/Lib.csproj";

    private readonly MockFileSystem _fileSystem = new();

    [Theory]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\" />", null, true)]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType></PropertyGroup></Project>", null, true)]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>false</IsPackable></PropertyGroup></Project>", null, false)]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\" />", "<Project><PropertyGroup><IsPackable>false</IsPackable></PropertyGroup></Project>", false)]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>true</IsPackable></PropertyGroup></Project>", "<Project><PropertyGroup><IsPackable>false</IsPackable></PropertyGroup></Project>", true)]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"17.0.0\" /></ItemGroup></Project>", null, false)]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup Condition=\"'$(Configuration)' == 'Release'\"><IsPackable>false</IsPackable></PropertyGroup></Project>", null, true)]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><IsPackable>$(Pack)</IsPackable></PropertyGroup></Project>", null, true)]
    [InlineData("not xml", null, true)]
    public void IsPackable_ProjectAndBuildProps_LastUnconditionalValueOrSdkDefault(string project, string? buildProps, bool expected)
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(project));
        if (buildProps is not null)
        {
            _fileSystem.AddFile("/repo/Directory.Build.props", new MockFileData(buildProps));
        }

        // Act
        bool packable = new XmlProjectProperties(_fileSystem).IsPackable(Project);

        // Assert
        packable.ShouldBe(expected);
    }

    [Fact]
    public void PackageReferences_ProjectAndBuildProps_EveryInclude()
    {
        // Arrange: council round 21, B13.
        _fileSystem.AddFile(Project, new MockFileData("<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"Z.EntityFramework.Extensions.EFCore\" /></ItemGroup></Project>"));
        _fileSystem.AddFile("/repo/Directory.Build.props", new MockFileData("<Project><ItemGroup><GlobalPackageReference Include=\"Analyzers\" /></ItemGroup></Project>"));

        // Act
        IReadOnlyList<string>? references = new XmlProjectProperties(_fileSystem).PackageReferences(Project);

        // Assert
        references.ShouldBe(["Analyzers", "Z.EntityFramework.Extensions.EFCore"]);
    }

    [Theory]
    [InlineData("App.config", "<configuration><specFlow><language feature=\"de-DE\" /></specFlow></configuration>", true)]
    [InlineData("app.config", "<configuration><specFlow /></configuration>", true)]
    [InlineData("App.config", "<configuration><appSettings /></configuration>", false)]
    [InlineData("App.config", "not xml", true)]
    [InlineData("Other.config", "<configuration><specFlow /></configuration>", false)]
    public void HasAppConfigSection_AppConfigNextToProject_TopLevelElementOrUnreadable(string fileName, string content, bool expected)
    {
        // Arrange: TAAK-340 step 5, Reqnroll 3.3.4 ignores App.config.
        _fileSystem.AddFile(Project, new MockFileData("<Project Sdk=\"Microsoft.NET.Sdk\" />"));
        _fileSystem.AddFile("/repo/src/Lib/" + fileName, new MockFileData(content));

        // Act
        bool found = new XmlProjectProperties(_fileSystem).HasAppConfigSection(Project, "specFlow");

        // Assert
        found.ShouldBe(expected);
    }

    [Fact]
    public void PackageReferences_NotXml_Null()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData("not xml"));

        // Act
        IReadOnlyList<string>? references = new XmlProjectProperties(_fileSystem).PackageReferences(Project);

        // Assert
        references.ShouldBeNull();
    }
}
