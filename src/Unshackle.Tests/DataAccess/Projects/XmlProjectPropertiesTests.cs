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
}
