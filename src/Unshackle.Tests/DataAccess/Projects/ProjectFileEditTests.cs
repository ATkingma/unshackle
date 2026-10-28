using System.IO.Abstractions.TestingHelpers;
using Shouldly;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Projects;
using Xunit;

namespace Unshackle.Tests.DataAccess.Projects;

// TAAK-317, council round 8 S3: a before/after snapshot proof per typed edit kind of IProjectFileEditor.
public class ProjectFileEditTests
{
    private const string Project = "/repo/src/Specs/Specs.csproj";
    private const string PackagesProps = "/repo/Directory.Packages.props";
    private const string BuildProps = "/repo/Directory.Build.props";
    private const string OldConfig = "/repo/src/Specs/specflow.json";
    private const string NewConfig = "/repo/src/Specs/reqnroll.json";

    private const string Before = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="SpecFlow.xUnit" Version="3.9.74" />
            <PackageReference Include="xunit" Version="2.9.0" />
          </ItemGroup>
          <ItemGroup>
            <None Update="specflow.json" CopyToOutputDirectory="PreserveNewest" />
          </ItemGroup>
        </Project>

        """;

    private const string CentralBefore = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <PackageReference Include="SpecFlow.xUnit" />
          </ItemGroup>
        </Project>

        """;

    private const string VersionsBefore = """
        <Project>
          <ItemGroup>
            <PackageVersion Include="SpecFlow.xUnit" Version="3.9.74" />
          </ItemGroup>
        </Project>

        """;

    private const string Config = """
        { "language": { "feature": "en-US" }, "bindingCulture": { "name": "en-US" } }

        """;

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task ApplyAsync_PackageRename_RenamesReferenceAndVersionInPlace()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));
        var edit = new PackageRenameEdit { OldName = "SpecFlow.xUnit", NewName = "Reqnroll.xUnit", Version = "2.4.1" };

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([edit])).ShouldHaveSingleItem();

        // Assert
        outcome.ManualReason.ShouldBeNull();
        outcome.Line.ShouldBe(6);
        Text(outcome, Project).ShouldBe("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Reqnroll.xUnit" Version="2.4.1" />
                <PackageReference Include="xunit" Version="2.9.0" />
              </ItemGroup>
              <ItemGroup>
                <None Update="specflow.json" CopyToOutputDirectory="PreserveNewest" />
              </ItemGroup>
            </Project>

            """);
    }

    [Fact]
    public async Task ApplyAsync_PackageRenameCentral_RenamesReferenceAndAddsPackageVersion()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(CentralBefore));
        _fileSystem.AddFile(PackagesProps, new MockFileData(VersionsBefore));
        var edit = new PackageRenameEdit { OldName = "SpecFlow.xUnit", NewName = "Reqnroll.xUnit", Version = "2.4.1" };

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([edit])).ShouldHaveSingleItem();

        // Assert
        Text(outcome, Project).ShouldBe(CentralBefore.Replace("SpecFlow.xUnit", "Reqnroll.xUnit", StringComparison.Ordinal));
        Text(outcome, PackagesProps).ShouldBe("""
            <Project>
              <ItemGroup>
                <PackageVersion Include="SpecFlow.xUnit" Version="3.9.74" />
                <PackageVersion Include="Reqnroll.xUnit" Version="2.4.1" />
              </ItemGroup>
            </Project>

            """);
    }

    [Fact]
    public async Task ApplyAsync_PackageRemove_RemovesReferenceLine()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([new PackageRemoveEdit { Name = "xunit" }])).ShouldHaveSingleItem();

        // Assert
        outcome.Line.ShouldBe(7);
        Text(outcome, Project).ShouldBe("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="SpecFlow.xUnit" Version="3.9.74" />
              </ItemGroup>
              <ItemGroup>
                <None Update="specflow.json" CopyToOutputDirectory="PreserveNewest" />
              </ItemGroup>
            </Project>

            """);
    }

    [Fact]
    public async Task ApplyAsync_PackageAdd_AddsReferenceAfterTheLastOne()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([new PackageAddEdit { Name = "Reqnroll.Tools.MsBuild.Generation", Version = "2.4.1" }])).ShouldHaveSingleItem();

        // Assert
        outcome.Line.ShouldBe(8);
        Text(outcome, Project).ShouldBe("""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="SpecFlow.xUnit" Version="3.9.74" />
                <PackageReference Include="xunit" Version="2.9.0" />
                <PackageReference Include="Reqnroll.Tools.MsBuild.Generation" Version="2.4.1" />
              </ItemGroup>
              <ItemGroup>
                <None Update="specflow.json" CopyToOutputDirectory="PreserveNewest" />
              </ItemGroup>
            </Project>

            """);
    }

    [Fact]
    public async Task ApplyAsync_PackageAddAlreadyThere_DoesNothing()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));

        // Act
        IReadOnlyList<ProjectFileEditOutcome> outcomes = await ApplyAsync([new PackageAddEdit { Name = "xunit", Version = "2.9.0" }]);

        // Assert
        outcomes.ShouldBeEmpty();
    }

    // TAAK-329, G10: a lost transitive package under CPM gets its PackageVersion only when absent; the same version is fine, another is Manual.
    [Theory]
    [InlineData(null, false)]
    [InlineData("8.0.0", false)]
    [InlineData("9.0.1", true)]
    public async Task ApplyAsync_TransitivePinCentral_AddsVersionOrManualOnConflict(string? central, bool manual)
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(CentralBefore));
        _fileSystem.AddFile(PackagesProps, new MockFileData(central is null ? VersionsBefore
            : VersionsBefore.Replace("</ItemGroup>", $"  <PackageVersion Include=\"Microsoft.Extensions.Configuration\" Version=\"{central}\" />\n  </ItemGroup>", StringComparison.Ordinal)));
        var edit = new PackageAddEdit { Name = "Microsoft.Extensions.Configuration", Version = "8.0.0", KeepsTransitiveOf = "EPPlus" };

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([edit])).ShouldHaveSingleItem();

        // Assert
        if (manual)
        {
            outcome.ManualReason.ShouldNotBeNull().ShouldContain("9.0.1");
            outcome.FilePath.ShouldBe(FullPath(PackagesProps));
            outcome.Changes.ShouldBeEmpty();
        }
        else
        {
            outcome.ManualReason.ShouldBeNull();
            Text(outcome, Project).ShouldContain("<PackageReference Include=\"Microsoft.Extensions.Configuration\" />");
            outcome.Changes.Any(c => c.FilePath == FullPath(PackagesProps)).ShouldBe(central is null);
            edit.Describe().ShouldContain("transitively with EPPlus");
        }
    }

    [Fact]
    public async Task ApplyAsync_PropertyMissing_AddsItToThePropertyGroup()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([new PropertyEdit { Name = "IsTestProject", Value = "true" }])).ShouldHaveSingleItem();

        // Assert
        outcome.Line.ShouldBe(4);
        Text(outcome, Project).ShouldBe(Before.Replace(
            "<TargetFramework>net8.0</TargetFramework>",
            "<TargetFramework>net8.0</TargetFramework>" + NewLineAndIndent("<TargetFramework>") + "<IsTestProject>true</IsTestProject>",
            StringComparison.Ordinal));
    }

    [Fact]
    public async Task ApplyAsync_PropertyPresent_SetsItsValue()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([new PropertyEdit { Name = "TargetFramework", Value = "net10.0" }])).ShouldHaveSingleItem();

        // Assert
        outcome.Line.ShouldBe(3);
        Text(outcome, Project).ShouldBe(Before.Replace("net8.0", "net10.0", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ApplyAsync_PropertyUnderCondition_IsManual()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before.Replace("<PropertyGroup>", "<PropertyGroup Condition=\"'$(CI)' == 'true'\">", StringComparison.Ordinal)));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([new PropertyEdit { Name = "TargetFramework", Value = "net10.0" }])).ShouldHaveSingleItem();

        // Assert
        outcome.ManualReason.ShouldNotBeNull();
        outcome.Changes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_ConfigFileRename_MovesFileAndRenamesItsItem()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));
        _fileSystem.AddFile(OldConfig, new MockFileData(Config));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([new ConfigFileRenameEdit { OldFileName = "specflow.json", NewFileName = "reqnroll.json" }])).ShouldHaveSingleItem();

        // Assert
        outcome.FilePath.ShouldBe(FullPath(OldConfig));
        outcome.Changes.Count.ShouldBe(3);
        ProjectFileChange deleted = Change(outcome, OldConfig);
        deleted.Deletes.ShouldBeTrue();
        deleted.OldText.ShouldBe(Config);
        ProjectFileChange created = Change(outcome, NewConfig);
        created.Creates.ShouldBeTrue();
        created.NewText.ShouldBe(Config);
        Text(outcome, Project).ShouldBe(Before.Replace("Update=\"specflow.json\"", "Update=\"reqnroll.json\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ApplyAsync_UsingRename_RenamesTheUsingItem()
    {
        // Arrange
        string before = Before.Replace("<None Update", "<Using Include=\"TechTalk.SpecFlow\" />\n    <None Update", StringComparison.Ordinal);
        _fileSystem.AddFile(Project, new MockFileData(before));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([new UsingRenameEdit { OldNamespace = "TechTalk.SpecFlow", NewNamespace = "Reqnroll" }])).ShouldHaveSingleItem();

        // Assert
        outcome.ManualReason.ShouldBeNull();
        Text(outcome, Project).ShouldBe(before.Replace("<Using Include=\"TechTalk.SpecFlow\"", "<Using Include=\"Reqnroll\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ApplyAsync_UsingRenameWithoutItem_DoesNothing()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));

        // Act
        IReadOnlyList<ProjectFileEditOutcome> outcomes = await ApplyAsync([new UsingRenameEdit { OldNamespace = "TechTalk.SpecFlow", NewNamespace = "Reqnroll" }]);

        // Assert
        outcomes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_UsingRenameInSharedBuildProps_IsManual()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));
        _fileSystem.AddFile(BuildProps, new MockFileData("<Project>\n  <ItemGroup>\n    <Using Include=\"TechTalk.SpecFlow\" />\n  </ItemGroup>\n</Project>\n"));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([new UsingRenameEdit { OldNamespace = "TechTalk.SpecFlow", NewNamespace = "Reqnroll" }])).ShouldHaveSingleItem();

        // Assert
        outcome.ManualReason.ShouldNotBeNull();
        outcome.Changes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_ConfigFileRenameTargetExists_IsManual()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));
        _fileSystem.AddFile(OldConfig, new MockFileData(Config));
        _fileSystem.AddFile(NewConfig, new MockFileData("{}"));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([new ConfigFileRenameEdit { OldFileName = "specflow.json", NewFileName = "reqnroll.json" }])).ShouldHaveSingleItem();

        // Assert
        outcome.ManualReason.ShouldNotBeNull();
        outcome.Changes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_ConfigFileTransform_ReplacesExactTextOnce()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));
        _fileSystem.AddFile(OldConfig, new MockFileData(Config));
        var edit = new ConfigFileTransformEdit { FileName = "specflow.json", Replacements = [new() { OldText = "\"bindingCulture\"", NewText = "\"bindingCultureX\"" }] };

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([edit])).ShouldHaveSingleItem();

        // Assert
        outcome.Line.ShouldBe(1);
        Text(outcome, OldConfig).ShouldBe(Config.Replace("\"bindingCulture\"", "\"bindingCultureX\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ApplyAsync_ConfigFileTransformTextTwice_IsManual()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));
        _fileSystem.AddFile(OldConfig, new MockFileData(Config));
        var edit = new ConfigFileTransformEdit { FileName = "specflow.json", Replacements = [new() { OldText = "en-US", NewText = "nl-NL" }] };

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([edit])).ShouldHaveSingleItem();

        // Assert
        outcome.ManualReason!.ShouldContain("exactly once");
        outcome.Changes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_TransformThenRename_BuildOnEachOther()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));
        _fileSystem.AddFile(OldConfig, new MockFileData(Config));
        ProjectFileEdit[] edits =
        [
            new ConfigFileTransformEdit { FileName = "specflow.json", Replacements = [new() { OldText = "\"language\"", NewText = "\"lang\"" }] },
            new ConfigFileRenameEdit { OldFileName = "specflow.json", NewFileName = "reqnroll.json" },
        ];

        // Act
        IReadOnlyList<ProjectFileEditOutcome> outcomes = await ApplyAsync(edits);

        // Assert
        Change(outcomes[1], NewConfig).NewText.ShouldBe(Config.Replace("\"language\"", "\"lang\"", StringComparison.Ordinal));
        Change(outcomes[1], OldConfig).OldText.ShouldBe(Config);
    }

    [Fact]
    public async Task ApplyAsync_PackageInSharedBuildProps_IsManual()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(CentralBefore.Replace("<PackageReference Include=\"SpecFlow.xUnit\" />", "<PackageReference Include=\"Other\" Version=\"1.0.0\" />", StringComparison.Ordinal)));
        _fileSystem.AddFile(BuildProps, new MockFileData("<Project>\n  <ItemGroup>\n    <PackageReference Include=\"SpecFlow.xUnit\" Version=\"3.9.74\" />\n  </ItemGroup>\n</Project>\n"));

        // Act
        ProjectFileEditOutcome outcome = (await ApplyAsync([new PackageRemoveEdit { Name = "SpecFlow.xUnit" }])).ShouldHaveSingleItem();

        // Assert
        outcome.FilePath.ShouldBe(FullPath(BuildProps));
        outcome.Line.ShouldBe(3);
        outcome.ManualReason!.ShouldContain("shared file");
    }

    [Fact]
    public async Task ApplyAsync_EarlierChange_StartsFromItsNewTextAndKeepsItsOldText()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(Before));
        string earlierText = Before.Replace("xunit\" Version=\"2.9.0\"", "xunit\" Version=\"2.9.3\"", StringComparison.Ordinal);
        ProjectFileChange earlier = new() { FilePath = FullPath(Project), OldText = Before, NewText = earlierText };

        // Act
        IReadOnlyList<ProjectFileEditOutcome> outcomes = await new XmlProjectFileStore(_fileSystem).ApplyAsync(
            Project, [new PackageRemoveEdit { Name = "SpecFlow.xUnit" }], [earlier], ProjectFileEditContext.None, TestContext.Current.CancellationToken);

        // Assert
        ProjectFileChange change = Change(outcomes.ShouldHaveSingleItem(), Project);
        change.OldText.ShouldBe(Before);
        change.NewText.ShouldContain("Version=\"2.9.3\"");
        change.NewText.ShouldNotContain("SpecFlow.xUnit");
    }

    [Fact]
    public async Task SaveAsync_DeletesChange_DeletesTheFile()
    {
        // Arrange
        _fileSystem.AddFile(OldConfig, new MockFileData(Config));
        var store = new XmlProjectFileStore(_fileSystem);

        // Act
        await store.SaveAsync(
            [
                new ProjectFileChange { FilePath = FullPath(OldConfig), OldText = Config, NewText = string.Empty, Deletes = true },
                new ProjectFileChange { FilePath = FullPath(NewConfig), OldText = string.Empty, NewText = Config, Creates = true },
            ],
            TestContext.Current.CancellationToken);

        // Assert
        _fileSystem.File.Exists(OldConfig).ShouldBeFalse();
        _fileSystem.File.ReadAllText(NewConfig).ShouldBe(Config);
    }

    // The line break and indentation in front of an element of the Before text, as the store copies it for a new neighbour.
    private static string NewLineAndIndent(string element)
    {
        int start = Before.IndexOf(element, StringComparison.Ordinal);
        int lineStart = Before.LastIndexOf('\n', start);
        return Before[(Before[lineStart - 1] == '\r' ? lineStart - 1 : lineStart)..start];
    }

    private string FullPath(string path) => _fileSystem.Path.GetFullPath(path);

    private ProjectFileChange Change(ProjectFileEditOutcome outcome, string path) => outcome.Changes.Single(c => c.FilePath == FullPath(path));

    private string Text(ProjectFileEditOutcome outcome, string path) => Change(outcome, path).NewText;

    private Task<IReadOnlyList<ProjectFileEditOutcome>> ApplyAsync(IEnumerable<ProjectFileEdit> edits) =>
        new XmlProjectFileStore(_fileSystem).ApplyAsync(Project, edits, [], ProjectFileEditContext.None, TestContext.Current.CancellationToken);
}
