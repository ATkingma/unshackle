using System.IO.Abstractions.TestingHelpers;
using Shouldly;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Projects;
using Xunit;

namespace Unshackle.Tests.DataAccess.Projects;

// TAAK-341, council round 18 X3: the same-name version bump, on the reference and under central package management.
public class PackageVersionEditTests
{
    private const string Project = "/repo/src/Tests/Tests.csproj";
    private const string Other = "/repo/src/Other/Other.csproj";
    private const string PackagesProps = "/repo/Directory.Packages.props";

    private const string Central = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <PackageReference Include="xunit.runner.visualstudio" />
          </ItemGroup>
        </Project>

        """;

    private const string Versions = """
        <Project>
          <ItemGroup>
            <PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
          </ItemGroup>
        </Project>

        """;

    private static readonly PackageVersionEdit s_edit = new() { Name = "xunit.runner.visualstudio", From = "2.0.0", To = "3.1.5" };

    private readonly MockFileSystem _fileSystem = new();

    [Theory]
    [InlineData("2.8.2", "3.1.5")]
    [InlineData("3.1.5", null)]
    [InlineData("4.0.1", null)]
    [InlineData("1.4.0", null)]
    public async Task ApplyAsync_VersionOnReference_BumpsOnlyFromTheRange(string declared, string? bumped)
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Reference(declared)));

        // Act
        IReadOnlyList<ProjectFileEditOutcome> outcomes = await ApplyAsync(ProjectFileEditContext.None);

        // Assert
        if (bumped is null)
        {
            outcomes.ShouldBeEmpty();
        }
        else
        {
            ProjectFileEditOutcome outcome = outcomes.ShouldHaveSingleItem();
            outcome.ManualReason.ShouldBeNull();
            outcome.Line.ShouldBe(3);
            outcome.Changes.ShouldHaveSingleItem().NewText.ShouldBe(Reference(bumped));
        }
    }

    [Fact]
    public async Task ApplyAsync_BelowFromWithInfo_KeptWithInfo()
    {
        // Arrange (council round 20, X9): a 1.x runner is not raised, but the user is told to update it.
        _fileSystem.AddFile(Project, new MockFileData(Reference("1.4.0")));
        PackageVersionEdit edit = new() { Name = s_edit.Name, From = s_edit.From, To = s_edit.To, BelowFromInfo = "update it by hand." };

        // Act
        ProjectFileEditOutcome outcome = (await new XmlProjectFileStore(_fileSystem).ApplyAsync(Project, [edit], [], ProjectFileEditContext.None, TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        outcome.Changes.ShouldBeEmpty();
        outcome.KeptInfo.ShouldBe("PackageReference xunit.runner.visualstudio 1.4.0 stays: update it by hand.");
    }

    [Fact]
    public async Task ApplyAsync_PropertyVersion_KeptWithInfo()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Reference("$(RunnerVersion)")));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync(ProjectFileEditContext.None)).ShouldHaveSingleItem();

        // Assert
        outcome.Changes.ShouldBeEmpty();
        outcome.KeptInfo.ShouldNotBeNull().ShouldContain("not a plain version");
    }

    [Fact]
    public async Task ApplyAsync_CentralVersionEveryUserMigrated_BumpsThePackageVersion()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Central));
        _fileSystem.AddFile(Other, new MockFileData(Central));
        _fileSystem.AddFile(PackagesProps, new MockFileData(Versions));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync(Context(blocked: false))).ShouldHaveSingleItem();

        // Assert
        outcome.FilePath.ShouldBe(FullPath(PackagesProps));
        outcome.Changes.ShouldHaveSingleItem().NewText.ShouldBe(Versions.Replace("2.8.2", "3.1.5", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ApplyAsync_CentralVersionUsedByBlockedProject_KeptWithInfoNamingIt()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Central));
        _fileSystem.AddFile(Other, new MockFileData(Central));
        _fileSystem.AddFile(PackagesProps, new MockFileData(Versions));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync(Context(blocked: true))).ShouldHaveSingleItem();

        // Assert
        outcome.Changes.ShouldBeEmpty();
        outcome.KeptInfo.ShouldNotBeNull().ShouldContain(FullPath(Other));
        outcome.KeptInfo.ShouldContain("not migrated");
    }

    [Fact]
    public async Task ApplyAsync_CentralKeptIsManual_ManualNamingTheOtherProject()
    {
        // Arrange (council round 25, X15): a netfx project cannot restore against the shared old version (NU1605).
        _fileSystem.AddFile(Project, new MockFileData(Central));
        _fileSystem.AddFile(Other, new MockFileData(Central));
        _fileSystem.AddFile(PackagesProps, new MockFileData(Versions));
        PackageVersionEdit edit = new() { Name = s_edit.Name, From = s_edit.From, To = s_edit.To, CentralKeptIsManual = true };

        // Act
        ProjectFileEditOutcome outcome = (await new XmlProjectFileStore(_fileSystem).ApplyAsync(Project, [edit], [], Context(blocked: true), TestContext.Current.CancellationToken))
            .ShouldHaveSingleItem();

        // Assert
        outcome.Changes.ShouldBeEmpty();
        outcome.KeptInfo.ShouldBeNull();
        outcome.ManualReason.ShouldNotBeNull().ShouldContain(FullPath(Other));
        outcome.ManualReason.ShouldContain("NU1605");
    }

    [Theory]
    [InlineData("2.8.2", true)]
    [InlineData("3.1.5", false)]
    public async Task ApplyAsync_ManualOrInfoInstead_ReportsOnlyWhenTheBumpWouldApply(string declared, bool reported)
    {
        // Arrange (X15)
        _fileSystem.AddFile(Project, new MockFileData(Reference(declared)));
        PackageVersionEdit manual = new() { Name = s_edit.Name, From = s_edit.From, To = s_edit.To, ManualInstead = "check it." };
        PackageVersionEdit info = new() { Name = s_edit.Name, From = s_edit.From, To = s_edit.To, InfoInstead = "recommended." };

        // Act
        IReadOnlyList<ProjectFileEditOutcome> manualOutcomes = await new XmlProjectFileStore(_fileSystem).ApplyAsync(Project, [manual], [], ProjectFileEditContext.None, TestContext.Current.CancellationToken);
        IReadOnlyList<ProjectFileEditOutcome> infoOutcomes = await new XmlProjectFileStore(_fileSystem).ApplyAsync(Project, [info], [], ProjectFileEditContext.None, TestContext.Current.CancellationToken);

        // Assert
        if (reported)
        {
            manualOutcomes.ShouldHaveSingleItem().ManualReason.ShouldBe("check it.");
            infoOutcomes.ShouldHaveSingleItem().KeptInfo.ShouldBe("recommended.");
            manualOutcomes[0].Changes.ShouldBeEmpty();
            infoOutcomes[0].Changes.ShouldBeEmpty();
        }
        else
        {
            manualOutcomes.ShouldBeEmpty();
            infoOutcomes.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task ApplyAsync_NotDeclared_DoesNothing()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Reference("2.8.2").Replace("xunit.runner.visualstudio", "Other", StringComparison.Ordinal)));

        // Act
        IReadOnlyList<ProjectFileEditOutcome> outcomes = await ApplyAsync(ProjectFileEditContext.None);

        // Assert
        outcomes.ShouldBeEmpty();
    }

    [Fact]
    public void Describe_NamesTheRange()
    {
        // Act
        string text = s_edit.Describe();

        // Assert
        text.ShouldBe("PackageReference xunit.runner.visualstudio 2.0.0 or later → 3.1.5");
    }

    private static string Reference(string version) => $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <PackageReference Include="xunit.runner.visualstudio" Version="{version}" />
          </ItemGroup>
        </Project>

        """;

    private ProjectFileEditContext Context(bool blocked) => new()
    {
        LoadedProjects = new HashSet<string>([FullPath(Project), FullPath(Other)]),
        BlockedProjects = blocked ? new Dictionary<string, string> { [FullPath(Other)] = "a manual finding" } : new Dictionary<string, string>(),
    };

    private string FullPath(string path) => _fileSystem.Path.GetFullPath(path);

    private Task<IReadOnlyList<ProjectFileEditOutcome>> ApplyAsync(ProjectFileEditContext context) =>
        new XmlProjectFileStore(_fileSystem).ApplyAsync(Project, [s_edit], [], context, TestContext.Current.CancellationToken);
}
