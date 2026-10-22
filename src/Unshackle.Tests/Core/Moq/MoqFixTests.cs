using Shouldly;
using Unshackle.Core.Fixes.Moq;
using Xunit;

namespace Unshackle.Tests.Core.Moq;

public class MoqFixTests
{
    [Fact]
    public void Package_SwapsMoqForNSubstitute()
    {
        // Arrange
        var fix = new MoqFix();

        // Act
        Unshackle.Core.Models.PackageMigration package = fix.Package;

        // Assert
        fix.Id.ShouldBe("moq");
        package.RemovedPackage.ShouldBe("Moq");
        package.AddedPackages.ShouldHaveSingleItem().Name.ShouldBe("NSubstitute");
        fix.RemovedNamespaces.ShouldContain("Moq");
        fix.AddedNamespaces.ShouldBe(["NSubstitute"]);
    }

    [Fact]
    public void Constructor_NullReferences_Throws()
    {
        // Arrange
        Func<MoqFix> create = () => new MoqFix(null!);

        // Act
        Exception exception = Should.Throw<ArgumentNullException>(create);

        // Assert
        exception.ShouldNotBeNull();
    }
}
