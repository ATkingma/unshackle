using Shouldly;
using Unshackle.DataAccess.Solutions;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.DataAccess;

// Loading and saving a real solution is covered by Integration/SampleShopTests.
public class MsBuildSolutionStoreTests
{
    [Fact]
    public async Task LoadAsync_MissingFile_ThrowsFileNotFound()
    {
        // Arrange
        using var store = new MsBuildSolutionStore();
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Missing.slnx");

        // Act
        Task act() => store.LoadAsync(path, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<FileNotFoundException>(act);
    }

    [Fact]
    public async Task SaveAsync_NothingLoaded_ThrowsInvalidOperation()
    {
        // Arrange
        using var store = new MsBuildSolutionStore();

        // Act
        Task act() => store.SaveAsync(TestSolution.Create("class C { }", TestReferences.Net), TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<InvalidOperationException>(act);
    }
}
