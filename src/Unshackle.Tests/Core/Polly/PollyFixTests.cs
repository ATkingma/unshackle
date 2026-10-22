using Shouldly;
using Unshackle.Core.Fixes.Polly;
using Xunit;

namespace Unshackle.Tests.Core.Polly;

public class PollyFixTests
{
    [Fact]
    public void Package_RemovesEveryPollyV7Package_AddsResilience()
    {
        // Arrange
        PollyFix fix = new();

        // Act
        string[] removed = [fix.Package.RemovedPackage, .. fix.Package.AlsoRemovedPackages];

        // Assert
        removed.ShouldBe(["Polly", "Polly.Extensions.Http", "Microsoft.Extensions.Http.Polly"]);
        fix.Package.AddedPackages.ShouldHaveSingleItem().Name.ShouldBe("Microsoft.Extensions.Resilience");
    }

    [Fact]
    public void ResilienceVersion_MatchesTheVersionTheProofsCompileAgainst()
    {
        // Arrange
        string props = File.ReadAllText(Path.Combine(Helpers.RepositoryRoot.Path, "Directory.Packages.props"));

        // Act
        bool pinned = props.Contains($"Include=\"Microsoft.Extensions.Http.Resilience\" Version=\"{PollyFix.ResilienceVersion}\"", StringComparison.Ordinal);

        // Assert
        pinned.ShouldBeTrue();
    }

    [Fact]
    public void Patterns_IdsUsePollyPrefix()
    {
        // Arrange
        PollyFix fix = new();

        // Act
        List<string> ids = [.. fix.Patterns.Select(p => p.Id)];

        // Assert
        ids.ShouldAllBe(id => id.StartsWith("polly.", StringComparison.Ordinal));
        fix.Id.ShouldBe("polly");
    }
}
