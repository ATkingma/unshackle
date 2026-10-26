using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Unshackle.Core.Abstractions;
using Shouldly;
using Unshackle.DataAccess.Solutions;
using Xunit;

namespace Unshackle.Tests.DataAccess;

// Loading a real solution is covered by Integration/SampleShopTests; writing goes through XmlProjectFileStore.
public class MsBuildSolutionStoreTests
{
    [Fact]
    public async Task LoadAsync_MissingFile_ThrowsFileNotFound()
    {
        // Arrange
        using var store = new MsBuildSolutionStore(new MockFileSystem(), ["FluentAssertions"]);

        // Act
        Task act() => store.LoadAsync("/repo/Missing.slnx", TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<FileNotFoundException>(act);
    }

    // TAAK-323: a global.json pinning an SDK that is not installed makes the build host fail in hostfxr_resolve_sdk2.
    // The folder sits next to the test binaries (not %TEMP%, where running an exe is blocked on some machines).
    [Fact]
    public async Task LoadAsync_GlobalJsonPinsMissingSdk_ThrowsSolutionLoadNamingVersion()
    {
        // Arrange
        string directory = Path.Combine(AppContext.BaseDirectory, "sdk-missing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory, "global.json"), """{ "sdk": { "version": "1.0.0", "rollForward": "disable" } }""", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directory, "App.csproj"), """<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup></Project>""", TestContext.Current.CancellationToken);
            using var store = new MsBuildSolutionStore(new FileSystem(), ["FluentAssertions"]);

            // Act
            Task act() => store.LoadAsync(Path.Combine(directory, "App.csproj"), TestContext.Current.CancellationToken);

            // Assert
            SolutionLoadException exception = await Should.ThrowAsync<SolutionLoadException>(act);
            exception.Errors.ShouldHaveSingleItem().ShouldContain("SDK 1.0.0");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
