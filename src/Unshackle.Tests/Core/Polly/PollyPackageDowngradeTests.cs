using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Shouldly;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
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
    public async Task ProjectBlockers_OlderMicrosoftExtensionsPackage_BlocksWithPackageDowngrade(string id, string version, string symbols, bool blocked)
    {
        // Arrange
        Project project = PollyFixTests.CreateProject(symbols.Split(';'), [.. PollyReferences.Before, PackageReference(id, version)]);

        // Act
        IReadOnlyList<ManualResult> blockers = await new PollyFix().ProjectBlockersAsync(project, TestContext.Current.CancellationToken);

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
    public async Task ProjectBlockers_ProjectWithoutPollyV7_DoesNotBlock()
    {
        // Arrange
        Project project = PollyFixTests.CreateProject(["NETCOREAPP", "NET8_0_OR_GREATER"], [.. PollyReferences.After, PackageReference("Microsoft.Extensions.Http", "6.0.0")]);

        // Act
        IReadOnlyList<ManualResult> blockers = await new PollyFix().ProjectBlockersAsync(project, TestContext.Current.CancellationToken);

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

    [Theory]
    [InlineData("8.0.0", true)]   // babylon-gateway (B5): Abstractions references Logging.Abstractions 8.0.0 and gets Resilience via CoreApiSdk
    [InlineData("8.0.3", false)]
    public void ProjectBlockers_ConsumerThroughProjectReferenceResolvesOlderPackage_BlocksWithPackageDowngrade(string version, bool blocked)
    {
        // Arrange
        string[] symbols = ["NETCOREAPP", "NET8_0_OR_GREATER"];
        var sdk = ProjectId.CreateNewId();
        var consumer = ProjectId.CreateNewId();
        Solution solution = TestSolution.NewEmpty()
            .AddProject(sdk, "RadixDlt.CoreApiSdk", "RadixDlt.CoreApiSdk", LanguageNames.CSharp)
            .WithProjectParseOptions(sdk, new CSharpParseOptions(preprocessorSymbols: symbols))
            .AddMetadataReferences(sdk, PollyReferences.Before)
            .AddProject(consumer, "RadixDlt.NetworkGateway.Abstractions", "RadixDlt.NetworkGateway.Abstractions", LanguageNames.CSharp)
            .WithProjectParseOptions(consumer, new CSharpParseOptions(preprocessorSymbols: symbols))
            .AddMetadataReferences(consumer, [PackageReference("Microsoft.Extensions.Logging.Abstractions", version)])
            .AddProjectReference(consumer, new ProjectReference(sdk));

        // Act
        IReadOnlyList<ManualResult> blockers = new PollyFix().ProjectBlockers(solution.GetProject(sdk)!);

        // Assert
        if (blocked)
        {
            ManualResult blocker = blockers.ShouldHaveSingleItem();
            blocker.Code.ShouldBe(nameof(ProjectBlockingReason.PackageDowngrade));
            blocker.Reason.ShouldContain("RadixDlt.NetworkGateway.Abstractions: Microsoft.Extensions.Logging.Abstractions 8.0.0");
        }
        else
        {
            blockers.ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData("[10.0.8, )", AssetsState.Current, true)]    // listenarr (B6): central pin below 10.0.13, NU1109
    [InlineData("[10.0.13, )", AssetsState.Current, false)]
    [InlineData("[10.0.8, )", AssetsState.Stale, false)]     // stale assets say nothing
    public void ProjectBlockers_DeclaredVersionBelowMinimum_BlocksWithPackageDowngrade(string range, AssetsState state, bool blocked)
    {
        // Arrange: the assembly binds from the ASP.NET Core reference pack, so only the assets show the central pin.
        string[] symbols = ["NETCOREAPP", "NET8_0_OR_GREATER", "NET9_0_OR_GREATER", "NET10_0_OR_GREATER"];
        var id = ProjectId.CreateNewId();
        string path = Path.Combine(Path.GetTempPath(), "Listenarr.Api", "Listenarr.Api.csproj");
        Project project = TestSolution.NewEmpty()
            .AddProject(ProjectInfo.Create(id, VersionStamp.Default, "Listenarr.Api", "Listenarr.Api", LanguageNames.CSharp, filePath: path))
            .WithProjectParseOptions(id, new CSharpParseOptions(preprocessorSymbols: symbols))
            .AddMetadataReferences(id, PollyReferences.Before)
            .GetProject(id)!;
        IPackageAssetsReader reader = Substitute.For<IPackageAssetsReader>();
        reader.Read(path).Returns(new PackageAssets
        {
            State = state,
            Resolved = new Dictionary<string, string>(),
            Requirements = [new PackageRequirement { Package = "Microsoft.Extensions.Http", RequiredBy = "project", Range = range }],
        });

        // Act
        IReadOnlyList<ManualResult> blockers = new PollyFix(reader).ProjectBlockers(project);

        // Assert
        if (blocked)
        {
            blockers.ShouldHaveSingleItem().Reason.ShouldContain("Microsoft.Extensions.Http 10.0.8 (needs >= 10.0.13)");
        }
        else
        {
            blockers.ShouldBeEmpty();
        }
    }

    private static PortableExecutableReference PackageReference(string id, string version) =>
        MetadataReference.CreateFromImage(s_image, filePath: Path.Combine(Path.GetTempPath(), "packages", id.ToLowerInvariant(), version, "lib", "net8.0", id + ".dll"));
}
