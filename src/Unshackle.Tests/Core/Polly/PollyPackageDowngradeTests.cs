using Microsoft.CodeAnalysis;
using Shouldly;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Models;
using Xunit;

namespace Unshackle.Tests.Core.Polly;

public class PollyPackageDowngradeTests
{
    private static readonly byte[] s_image = File.ReadAllBytes(typeof(PollyPackageDowngradeTests).Assembly.Location);

    [Theory]
    [InlineData("Microsoft.Extensions.Logging.Abstractions", "8.0.0", "NETCOREAPP;NET8_0_OR_GREATER", true)]   // babylon-gateway (B1)
    [InlineData("Microsoft.Extensions.Logging.Abstractions", "8.0.3", "NETCOREAPP;NET8_0_OR_GREATER", false)]
    [InlineData("Microsoft.Extensions.Http", "8.0.0-preview.1", "NETCOREAPP;NET8_0_OR_GREATER", true)]       // eventualshop (B1)
    [InlineData("Microsoft.Extensions.DependencyInjection", "5.0.0", "NETSTANDARD;NETSTANDARD2_0", true)]    // okta (B4, CS0433)
    [InlineData("Microsoft.Extensions.DependencyInjection", "8.0.0", "NETSTANDARD;NETSTANDARD2_0", false)]
    [InlineData("Microsoft.Extensions.Options", "8.0.2", "NETCOREAPP;NET8_0_OR_GREATER;NET9_0_OR_GREATER", true)]
    [InlineData("Microsoft.Extensions.Options", "9.0.21", "NETCOREAPP;NET8_0_OR_GREATER;NET9_0_OR_GREATER", false)]
    [InlineData("Microsoft.Extensions.Options", "9.0.21", "NETCOREAPP;NET8_0_OR_GREATER;NET9_0_OR_GREATER;NET10_0_OR_GREATER", true)]
    [InlineData("Microsoft.Extensions.Telemetry.Abstractions", "9.0.0", "NETCOREAPP;NET8_0_OR_GREATER", true)]
    [InlineData("Microsoft.Extensions.Http.Polly", "3.1.0", "NETCOREAPP;NET8_0_OR_GREATER", false)]          // removed by the migration
    public void ProjectBlockers_OlderMicrosoftExtensionsPackage_BlocksWithPackageDowngrade(string id, string version, string symbols, bool blocked)
    {
        // Arrange
        Project project = PollyFixTests.CreateProject(symbols.Split(';'), [.. PollyReferences.Before, PackageReference(id, version)]);

        // Act
        IReadOnlyList<ManualResult> blockers = new PollyFix().ProjectBlockers(project);

        // Assert
        if (blocked)
        {
            ManualResult blocker = blockers.ShouldHaveSingleItem();
            blocker.Code.ShouldBe(nameof(ProjectBlockingReason.PackageDowngrade));
            blocker.Reason.ShouldContain(id);
        }
        else
        {
            blockers.ShouldBeEmpty();
        }
    }

    [Fact]
    public void ProjectBlockers_ProjectWithoutPollyV7_DoesNotBlock()
    {
        // Arrange
        Project project = PollyFixTests.CreateProject(["NETCOREAPP", "NET8_0_OR_GREATER"], [.. PollyReferences.After, PackageReference("Microsoft.Extensions.Http", "6.0.0")]);

        // Act
        IReadOnlyList<ManualResult> blockers = new PollyFix().ProjectBlockers(project);

        // Assert
        blockers.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("packs/Microsoft.AspNetCore.App.Ref/8.0.0/ref/net8.0/Microsoft.Extensions.Http.dll")]
    [InlineData("packages/newtonsoft.json/13.0.1/lib/net6.0/Newtonsoft.Json.dll")]
    [InlineData("Microsoft.Extensions.Http.dll")]
    public void Resolved_NotAMicrosoftExtensionsPackageAssembly_ReturnsNull(string path)
    {
        // Act
        (string Id, Version Version)? resolved = PollyPackageDowngrade.Resolved(Path.Combine(Path.GetTempPath(), path));

        // Assert
        resolved.ShouldBeNull();
    }

    private static PortableExecutableReference PackageReference(string id, string version) =>
        MetadataReference.CreateFromImage(s_image, filePath: Path.Combine(Path.GetTempPath(), "packages", id.ToLowerInvariant(), version, "lib", "net8.0", id + ".dll"));
}
