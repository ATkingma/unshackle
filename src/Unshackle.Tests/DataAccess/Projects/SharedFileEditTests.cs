using System.IO.Abstractions.TestingHelpers;
using Shouldly;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Projects;
using Xunit;

namespace Unshackle.Tests.DataAccess.Projects;

// Council round 10, G2/G3/G5: edits of shared Directory.*.props files, the old central PackageVersion, and config files that must still parse.
public class SharedFileEditTests
{
    private const string Specs = "/repo/src/Specs/Specs.csproj";
    private const string Other = "/repo/src/Other/Other.csproj";
    private const string PackagesProps = "/repo/Directory.Packages.props";
    private const string BuildProps = "/repo/Directory.Build.props";

    private const string Unversioned = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"SpecFlow\" />\n  </ItemGroup>\n</Project>\n";

    private const string Versions = "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"SpecFlow\" Version=\"3.9.74\" />\n  </ItemGroup>\n</Project>\n";

    private const string SharedReference = "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"SpecFlow\" Version=\"3.9.74\" />\n  </ItemGroup>\n</Project>\n";

    private const string Empty = "<Project Sdk=\"Microsoft.NET.Sdk\" />\n";

    private static readonly PackageRenameEdit s_rename = new() { OldName = "SpecFlow", NewName = "Reqnroll", Version = "2.4.1" };

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task ApplyAsync_CentralRenameLastUser_RemovesOldPackageVersion()
    {
        // Arrange
        _fileSystem.AddFile(Specs, new MockFileData(Unversioned));
        _fileSystem.AddFile(PackagesProps, new MockFileData(Versions));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync(Specs, s_rename, Loaded(Specs))).ShouldHaveSingleItem();

        // Assert
        outcome.Note.ShouldBeNull();
        Text(outcome, PackagesProps).ShouldBe("<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"Reqnroll\" Version=\"2.4.1\" />\n  </ItemGroup>\n</Project>\n");
    }

    [Theory]
    [InlineData(true, false, "still references it")]
    [InlineData(false, false, "is not loaded")]
    [InlineData(true, true, "GlobalPackageReference")]
    public async Task ApplyAsync_CentralRenameWithOtherUse_KeepsOldPackageVersionWithNote(bool otherLoaded, bool global, string why)
    {
        // Arrange
        _fileSystem.AddFile(Specs, new MockFileData(Unversioned));
        _fileSystem.AddFile(Other, new MockFileData(global ? Empty : Unversioned));
        _fileSystem.AddFile(PackagesProps, new MockFileData(global
            ? Versions.Replace("</ItemGroup>", "  <GlobalPackageReference Include=\"SpecFlow\" Version=\"3.9.74\" />\n  </ItemGroup>", StringComparison.Ordinal)
            : Versions));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync(Specs, s_rename, otherLoaded ? Loaded(Specs, Other) : Loaded(Specs))).ShouldHaveSingleItem();

        // Assert
        outcome.Note.ShouldNotBeNull().ShouldContain(why);
        Text(outcome, PackagesProps).ShouldContain("<PackageVersion Include=\"SpecFlow\"");
    }

    [Fact]
    public async Task ApplyAsync_SharedDeclarationAllImportersLoadedAndFree_EditsSharedFile()
    {
        // Arrange
        _fileSystem.AddFile(Specs, new MockFileData(Empty));
        _fileSystem.AddFile(Other, new MockFileData(Empty));
        _fileSystem.AddFile(BuildProps, new MockFileData(SharedReference));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync(Specs, s_rename, Loaded(Specs, Other))).ShouldHaveSingleItem();

        // Assert
        outcome.ManualReason.ShouldBeNull();
        outcome.FilePath.ShouldBe(FullPath(BuildProps));
        Text(outcome, BuildProps).ShouldBe(SharedReference.Replace("\"SpecFlow\" Version=\"3.9.74\"", "\"Reqnroll\" Version=\"2.4.1\"", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false, "is not loaded")]
    [InlineData(true, "still has blocking rows (2 manual findings)")]
    public async Task ApplyAsync_SharedDeclarationWithBlockedImporter_IsSharedDeclarationBlocked(bool otherLoaded, string why)
    {
        // Arrange
        _fileSystem.AddFile(Specs, new MockFileData(Empty));
        _fileSystem.AddFile(Other, new MockFileData(Empty));
        _fileSystem.AddFile(BuildProps, new MockFileData(SharedReference));
        ProjectFileEditContext context = otherLoaded
            ? new() { LoadedProjects = Loaded(Specs, Other).LoadedProjects, BlockedProjects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [FullPath(Other)] = "2 manual findings" } }
            : Loaded(Specs);

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync(Specs, s_rename, context)).ShouldHaveSingleItem();

        // Assert
        outcome.ManualCode.ShouldBe(nameof(ProjectBlockingReason.SharedDeclarationBlocked));
        outcome.ManualReason.ShouldNotBeNull().ShouldContain(FullPath(Other) + " also imports and " + why);
        outcome.Changes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_TransformBreaksJson_IsManual()
    {
        // Arrange
        _fileSystem.AddFile(Specs, new MockFileData(Empty));
        _fileSystem.AddFile("/repo/src/Specs/specflow.json", new MockFileData("{ \"language\": \"en\" }\n"));
        var edit = new ConfigFileTransformEdit { FileName = "specflow.json", Replacements = [new() { OldText = "\"en\" }", NewText = "\"en\"" }] };

        // Act
        ProjectFileEditOutcome outcome = (await new XmlProjectFileStore(_fileSystem).ApplyAsync(Specs, [edit], [], Loaded(Specs), TestContext.Current.CancellationToken)).ShouldHaveSingleItem();

        // Assert
        outcome.ManualReason.ShouldNotBeNull().ShouldContain("does not parse");
        outcome.Changes.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("8.4.2", true)]
    [InlineData("8.0.0", true)]
    [InlineData("8.6.4", false)]
    [InlineData("[8.0,9.0)", false)]
    public async Task ApplyAsync_RemoveAtMostVersion_KeepsAHigherOrUnknownDirectPin(string version, bool removed)
    {
        // Arrange (council round 10, P8: Microsoft.Extensions.Resilience 10.11.0 → Polly.Extensions 8.4.2 → Polly.Core 8.4.2, from the nuspecs)
        string project = $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Polly.Core\" Version=\"{version}\" />\n  </ItemGroup>\n</Project>\n";
        _fileSystem.AddFile(Specs, new MockFileData(project));
        var edit = new PackageRemoveEdit { Name = "Polly.Core", AtMostVersion = "8.4.2" };

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync(Specs, edit, Loaded(Specs))).ShouldHaveSingleItem();

        // Assert
        outcome.ManualReason.ShouldBeNull();
        if (removed)
        {
            outcome.KeptInfo.ShouldBeNull();
            Text(outcome, Specs).ShouldNotContain("Polly.Core");
        }
        else
        {
            outcome.KeptInfo.ShouldNotBeNull().ShouldContain($"Polly.Core {version} stays");
            outcome.Changes.ShouldBeEmpty();
        }
    }

    private ProjectFileEditContext Loaded(params string[] projects) => new()
    {
        LoadedProjects = new HashSet<string>(projects.Select(FullPath), StringComparer.OrdinalIgnoreCase),
        BlockedProjects = new Dictionary<string, string>(),
    };

    private string FullPath(string path) => _fileSystem.Path.GetFullPath(path);

    private string Text(ProjectFileEditOutcome outcome, string path) => outcome.Changes.Single(c => c.FilePath == FullPath(path)).NewText;

    private Task<IReadOnlyList<ProjectFileEditOutcome>> ApplyAsync(string project, ProjectFileEdit edit, ProjectFileEditContext context) =>
        new XmlProjectFileStore(_fileSystem).ApplyAsync(project, [edit], [], context, TestContext.Current.CancellationToken);
}
