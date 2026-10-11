using System.IO.Abstractions.TestingHelpers;
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
        using var store = new MsBuildSolutionStore(new MockFileSystem());

        // Act
        Task act() => store.LoadAsync("/repo/Missing.slnx", TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<FileNotFoundException>(act);
    }
}
