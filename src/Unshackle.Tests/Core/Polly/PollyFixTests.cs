using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
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

    [Theory]
    [InlineData("NETCOREAPP;NETCOREAPP3_0", true, true)]
    [InlineData("NETFRAMEWORK;NET452", true, true)]
    [InlineData("NETCOREAPP;NET8_0_OR_GREATER", true, false)]
    [InlineData("NETFRAMEWORK;NET462_OR_GREATER", true, false)]
    [InlineData("NETCOREAPP;NETCOREAPP3_0", false, false)]
    public void ProjectBlockers_UnsupportedFrameworkWithPollyV7_BlocksTheProject(string symbols, bool referencesPolly, bool blocked)
    {
        // Arrange (council round 10, P9/G4)
        Project project = CreateProject(symbols.Split(';'), referencesPolly ? PollyReferences.Before : PollyReferences.After);

        // Act
        IReadOnlyList<ManualResult> blockers = new PollyFix().ProjectBlockers(project);

        // Assert
        if (blocked)
        {
            blockers.ShouldHaveSingleItem().Code.ShouldBe(nameof(ProjectBlockingReason.TargetFrameworkUnsupported));
        }
        else
        {
            blockers.ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EditsFor_PollyV7Project_AddsResilienceAndRemovesBundledPollyCore(bool referencesPolly)
    {
        // Arrange (council round 10, P8)
        Project project = CreateProject(["NETCOREAPP", "NET8_0_OR_GREATER"], referencesPolly ? PollyReferences.Before : PollyReferences.After);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = new PollyFix().EditsFor(project);

        // Assert
        if (!referencesPolly)
        {
            edits.ShouldBeEmpty();
            return;
        }

        edits.Count.ShouldBe(2);
        edits[0].ShouldBeOfType<PackageAddEdit>().Name.ShouldBe("Microsoft.Extensions.Resilience");
        PackageRemoveEdit remove = edits[1].ShouldBeOfType<PackageRemoveEdit>();
        remove.Name.ShouldBe("Polly.Core");
        remove.AtMostVersion.ShouldBe("8.4.2");
    }

    internal static Project CreateProject(string[] symbols, IEnumerable<MetadataReference> references)
    {
        var projectId = ProjectId.CreateNewId();
        return TestSolution.NewEmpty()
            .AddProject(projectId, "P", "P", LanguageNames.CSharp)
            .WithProjectParseOptions(projectId, new CSharpParseOptions(preprocessorSymbols: symbols))
            .AddMetadataReferences(projectId, references)
            .GetProject(projectId)!;
    }
}
