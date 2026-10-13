using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;
using Shouldly;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Projects;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.DataAccess.Projects;

public class XmlProjectFileStoreTests
{
    private const string Project = "/repo/src/Tests/Tests.csproj";
    private const string OtherProject = "/repo/src/Other/Other.csproj";
    private const string BuildProps = "/repo/Directory.Build.props";
    private const string PackagesProps = "/repo/Directory.Packages.props";

    private const string CentralProject = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <!-- assertions -->
            <PackageReference Include="FluentAssertions" />
            <PackageReference Include="xunit.v3" />
          </ItemGroup>
          <ItemGroup>
            <Using Include="FluentAssertions" />
          </ItemGroup>
        </Project>

        """;

    private const string CentralVersions = """
        <!-- versions -->
        <Project>
          <ItemGroup Label="Test">
            <PackageVersion Include="FluentAssertions" Version="7.2.2" />
            <PackageVersion Include="xunit.v3" Version="4.0.2" />
          </ItemGroup>
        </Project>

        """;

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public async Task EditAsync_CentralPackageManagement_SwapsReferenceUsingAndVersionInPlace()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(CentralProject));
        _fileSystem.AddFile(PackagesProps, new MockFileData(CentralVersions));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        changes.Count.ShouldBe(2);
        Text(changes, Project).ShouldBe(CentralProject
            .Replace("<PackageReference Include=\"FluentAssertions\" />", "<PackageReference Include=\"Shouldly\" />", StringComparison.Ordinal)
            .Replace("<Using Include=\"FluentAssertions\" />", "<Using Include=\"Shouldly\" />", StringComparison.Ordinal));
        Text(changes, PackagesProps).ShouldBe(CentralVersions.Replace(
            "Include=\"FluentAssertions\" Version=\"7.2.2\"", "Include=\"Shouldly\" Version=\"4.3.0\"", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>\n  </PropertyGroup>\n  <ItemGroup>\n    <PackageReference Include=\"FluentAssertions\" />\n  </ItemGroup>\n</Project>\n",
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>\n    <WarningsNotAsErrors>$(WarningsNotAsErrors);MSG0005</WarningsNotAsErrors>\n  </PropertyGroup>\n  <ItemGroup>\n    <PackageReference Include=\"Shouldly\" />\n    <PackageReference Include=\"Extra\" />\n  </ItemGroup>\n</Project>\n")]
    [InlineData(
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"FluentAssertions\" />\n  </ItemGroup>\n</Project>\n",
        "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Shouldly\" />\n    <PackageReference Include=\"Extra\" />\n  </ItemGroup>\n  <PropertyGroup>\n    <WarningsNotAsErrors>$(WarningsNotAsErrors);MSG0005</WarningsNotAsErrors>\n  </PropertyGroup>\n</Project>\n")]
    public async Task EditAsync_AddedPackageWithWarningsNotAsErrors_AddsThemToTheSameFile(string project, string expected)
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(project));
        _fileSystem.AddFile(PackagesProps, new MockFileData(CentralVersions));
        var migration = new ProjectMigration
        {
            ProjectPath = Project,
            Package = new PackageMigration
            {
                RemovedPackage = "FluentAssertions",
                AddedPackages = [new() { Name = "Shouldly", Version = "4.3.0" }, new() { Name = "Extra", Version = "1.0.0", WarningsNotAsErrors = ["MSG0005"] }],
            },
            KeepRemovedPackage = false,
            RemovedNamespaces = ["FluentAssertions"],
            AddedNamespaces = ["Shouldly"],
        };

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(migration);

        // Assert
        Text(changes, Project).ShouldBe(expected);
    }

    [Theory]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\" />", "\n")]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\"/>", "\n")]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\"></Project>", "\n")]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\" />\r\n", "\r\n")]
    public async Task EditAsync_ProjectWithoutContent_GetsTheAddedPackageAndKeepsLineEndings(string project, string newLine)
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(project));
        string trailing = project.EndsWith('\n') ? newLine : string.Empty;

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        Text(changes, Project).ShouldBe(
            $"<Project Sdk=\"Microsoft.NET.Sdk\">{newLine}  <ItemGroup>{newLine}    <PackageReference Include=\"Shouldly\" Version=\"4.3.0\" />{newLine}  </ItemGroup>{newLine}</Project>{trailing}");
    }

    [Fact]
    public async Task EditAsync_SelfClosingProjectOfThisSolution_GetsReferenceWithoutVersionNextToNormalProject()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(CentralProject));
        _fileSystem.AddFile(PackagesProps, new MockFileData(CentralVersions));
        _fileSystem.AddFile(OtherProject, new MockFileData("<Project Sdk=\"Microsoft.NET.Sdk\" />"));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false), Migration(OtherProject, keep: false));

        // Assert
        changes.Count.ShouldBe(3);
        Text(changes, OtherProject).ShouldBe("<Project Sdk=\"Microsoft.NET.Sdk\">\n  <ItemGroup>\n    <PackageReference Include=\"Shouldly\" />\n  </ItemGroup>\n</Project>");
    }

    [Fact]
    public async Task EditAsync_TwoAddedPackagesCentral_SwapsFirstInPlaceAndAddsSecondWithPrivateAssetsAfterIt()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(CentralProject));
        _fileSystem.AddFile(PackagesProps, new MockFileData(CentralVersions));
        var migration = new ProjectMigration
        {
            ProjectPath = Project,
            Package = new PackageMigration
            {
                RemovedPackage = "FluentAssertions",
                AddedPackages = [new() { Name = "Shouldly", Version = "4.3.0" }, new() { Name = "Extra", Version = "1.0.0", PrivateAssets = "all" }],
            },
            KeepRemovedPackage = false,
            RemovedNamespaces = ["FluentAssertions"],
            AddedNamespaces = ["Shouldly"],
        };

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(migration);

        // Assert
        Text(changes, Project).ShouldBe(CentralProject
            .Replace("<PackageReference Include=\"FluentAssertions\" />",
                "<PackageReference Include=\"Shouldly\" />\n    <PackageReference Include=\"Extra\" PrivateAssets=\"all\" />", StringComparison.Ordinal)
            .Replace("<Using Include=\"FluentAssertions\" />", "<Using Include=\"Shouldly\" />", StringComparison.Ordinal));
        Text(changes, PackagesProps).ShouldBe(CentralVersions.Replace(
            "Include=\"FluentAssertions\" Version=\"7.2.2\" />",
            "Include=\"Shouldly\" Version=\"4.3.0\" />\n    <PackageVersion Include=\"Extra\" Version=\"1.0.0\" />", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EditAsync_VersionOnReference_SwapsVersionAndLeavesOtherFilesAlone()
    {
        // Arrange
        const string project = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="FluentAssertions" Version="7.2.2" />
              </ItemGroup>
            </Project>
            """;
        _fileSystem.AddFile(Project, new MockFileData(project));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        changes.ShouldHaveSingleItem().NewText.ShouldBe(project.Replace(
            "Include=\"FluentAssertions\" Version=\"7.2.2\"", "Include=\"Shouldly\" Version=\"4.3.0\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EditAsync_AlsoRemovedPackageReferenced_RemovesIt()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(
            "<Project><ItemGroup><PackageReference Include=\"MediatR\" Version=\"12.5.0\" />" +
            "<PackageReference Include=\"MediatR.Contracts\" Version=\"2.0.1\" /></ItemGroup></Project>"));
        var migration = new ProjectMigration
        {
            ProjectPath = Project,
            Package = new PackageMigration
            {
                RemovedPackage = "MediatR",
                AlsoRemovedPackages = ["MediatR.Contracts"],
                AddedPackages = [new() { Name = "Mediator.Abstractions", Version = "3.0.2" }],
            },
            KeepRemovedPackage = false,
            RemovedNamespaces = ["MediatR"],
            AddedNamespaces = ["Mediator"],
        };

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(migration);

        // Assert
        changes.ShouldHaveSingleItem().NewText.ShouldBe(
            "<Project><ItemGroup><PackageReference Include=\"Mediator.Abstractions\" Version=\"3.0.2\" /></ItemGroup></Project>");
    }

    [Fact]
    public async Task EditAsync_VersionAsChildElement_SwapsIt()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(
            "<Project><ItemGroup><PackageReference Include=\"FluentAssertions\"><Version>7.2.2</Version></PackageReference></ItemGroup></Project>"));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        changes.ShouldHaveSingleItem().NewText.ShouldBe(
            "<Project><ItemGroup><PackageReference Include=\"Shouldly\"><Version>4.3.0</Version></PackageReference></ItemGroup></Project>");
    }

    [Fact]
    public async Task EditAsync_CodeStillUsesPackage_KeepsItAndAddsTheNewOneNextToIt()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(CentralProject));
        _fileSystem.AddFile(PackagesProps, new MockFileData(CentralVersions));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: true));

        // Assert
        Text(changes, Project).ShouldBe(CentralProject.Replace(
            "    <PackageReference Include=\"FluentAssertions\" />\n",
            "    <PackageReference Include=\"FluentAssertions\" />\n    <PackageReference Include=\"Shouldly\" />\n",
            StringComparison.Ordinal));
        Text(changes, PackagesProps).ShouldBe(CentralVersions.Replace(
            "    <PackageVersion Include=\"FluentAssertions\" Version=\"7.2.2\" />\n",
            "    <PackageVersion Include=\"FluentAssertions\" Version=\"7.2.2\" />\n    <PackageVersion Include=\"Shouldly\" Version=\"4.3.0\" />\n",
            StringComparison.Ordinal));
    }

    [Fact]
    public async Task EditAsync_AddedPackageAlreadyThere_RemovesOldLinesAndEmptyGroup()
    {
        // Arrange
        const string project = """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="FluentAssertions" />
                <PackageReference Include="Shouldly" />
              </ItemGroup>
              <ItemGroup>
                <Using Include="fluentassertions" />
              </ItemGroup>
              <ItemGroup>
                <Using Include="FluentAssertions" />
                <Using Include="Shouldly" />
              </ItemGroup>
            </Project>
            """;
        _fileSystem.AddFile(Project, new MockFileData(project));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert: namespaces are case-sensitive, package ids are not
        changes.ShouldHaveSingleItem().NewText.ShouldBe("""
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <PackageReference Include="Shouldly" />
              </ItemGroup>
              <ItemGroup>
                <Using Include="fluentassertions" />
              </ItemGroup>
              <ItemGroup>
                <Using Include="Shouldly" />
              </ItemGroup>
            </Project>
            """);
    }

    [Fact]
    public async Task EditAsync_ReferenceAndUsingInDirectoryBuildProps_EditsThePropsOnly()
    {
        // Arrange
        const string props = """
            <Project>
              <ItemGroup>
                <PackageReference Include="FluentAssertions" Version="7.2.2" />
                <Using Include="FluentAssertions" />
              </ItemGroup>
            </Project>
            """;
        _fileSystem.AddFile(Project, new MockFileData("<Project Sdk=\"Microsoft.NET.Sdk\" />"));
        _fileSystem.AddFile(BuildProps, new MockFileData(props));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        ProjectFileChange change = changes.ShouldHaveSingleItem();
        change.FilePath.ShouldBe(FullPath(BuildProps));
        change.NewText.ShouldBe(props
            .Replace("\"FluentAssertions\" Version=\"7.2.2\"", "\"Shouldly\" Version=\"4.3.0\"", StringComparison.Ordinal)
            .Replace("<Using Include=\"FluentAssertions\" />", "<Using Include=\"Shouldly\" />", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EditAsync_SharedPropsAndOneProjectKeepsPackage_KeepsItInThePropsAndSwapsTheOtherProject()
    {
        // Arrange
        const string props = """
            <Project>
              <ItemGroup>
                <Using Include="FluentAssertions" />
              </ItemGroup>
            </Project>
            """;
        _fileSystem.AddFile(Project, new MockFileData(CentralProject));
        _fileSystem.AddFile(OtherProject, new MockFileData(CentralProject));
        _fileSystem.AddFile(BuildProps, new MockFileData(props));
        _fileSystem.AddFile(PackagesProps, new MockFileData(CentralVersions));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false), Migration(OtherProject, keep: true));

        // Assert
        changes.ShouldNotContain(c => c.FilePath == FullPath(BuildProps));
        Text(changes, Project).ShouldContain("<PackageReference Include=\"Shouldly\" />");
        Text(changes, Project).ShouldNotContain("FluentAssertions");
        Text(changes, OtherProject).ShouldContain("<PackageReference Include=\"FluentAssertions\" />");
        Text(changes, PackagesProps).ShouldContain("<PackageVersion Include=\"FluentAssertions\" Version=\"7.2.2\" />");
        Text(changes, PackagesProps).ShouldContain("<PackageVersion Include=\"Shouldly\" Version=\"4.3.0\" />");
    }

    [Fact]
    public async Task EditAsync_PackageOnlyTransitive_AddsNewItemGroupToProject()
    {
        // Arrange
        const string project = "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <A>1</A>\n  </PropertyGroup>\n</Project>\n";
        _fileSystem.AddFile(Project, new MockFileData(project));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        changes.ShouldHaveSingleItem().NewText.ShouldBe(
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>\n    <A>1</A>\n  </PropertyGroup>\n" +
            "  <ItemGroup>\n    <PackageReference Include=\"Shouldly\" Version=\"4.3.0\" />\n  </ItemGroup>\n</Project>\n");
    }

    [Fact]
    public async Task EditAsync_CrlfAndDeclaration_KeepsThem()
    {
        // Arrange
        string project = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Project>\r\n  <ItemGroup>\r\n" +
            "    <PackageReference Include=\"FluentAssertions\" Version=\"7.2.2\" />\r\n  </ItemGroup>\r\n</Project>\r\n";
        _fileSystem.AddFile(Project, new MockFileData(project));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        changes.ShouldHaveSingleItem().NewText.ShouldBe(project.Replace(
            "\"FluentAssertions\" Version=\"7.2.2\"", "\"Shouldly\" Version=\"4.3.0\"", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EditAsync_InvalidXml_Throws()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData("<Project>"));

        // Act
        Func<Task> edit = () => EditAsync(Migration(Project, keep: false));

        // Assert
        await edit.ShouldThrowAsync<System.Xml.XmlException>();
    }

    [Fact]
    public async Task SaveAsync_NewFile_CreatesFolderAndFile()
    {
        // Arrange
        const string source = "/repo/src/Core/Ardalis.SharedKernel/EntityBase.cs";
        _fileSystem.AddFile(Project, new MockFileData("old"));
        var store = new XmlProjectFileStore(_fileSystem);

        // Act
        await store.SaveAsync(
            [new ProjectFileChange { FilePath = source, OldText = string.Empty, NewText = "class EntityBase;", Creates = true }],
            TestContext.Current.CancellationToken);

        // Assert
        _fileSystem.File.ReadAllText(source).ShouldBe("class EntityBase;");
    }

    [Fact]
    public async Task SaveAsync_NewFileAlreadyThere_ThrowsAndWritesNothing()
    {
        // Arrange
        const string source = "/repo/src/Core/Ardalis.SharedKernel/EntityBase.cs";
        _fileSystem.AddFile(Project, new MockFileData("old"));
        _fileSystem.AddFile(source, new MockFileData("mine"));
        var store = new XmlProjectFileStore(_fileSystem);

        // Act
        Func<Task> save = () => store.SaveAsync(
            [
                new ProjectFileChange { FilePath = Project, OldText = "old", NewText = "new" },
                new ProjectFileChange { FilePath = source, OldText = string.Empty, NewText = "class EntityBase;", Creates = true },
            ],
            TestContext.Current.CancellationToken);

        // Assert
        (await save.ShouldThrowAsync<InvalidOperationException>()).Message.ShouldContain("already exists");
        _fileSystem.File.ReadAllText(Project).ShouldBe("old");
        _fileSystem.File.ReadAllText(source).ShouldBe("mine");
    }

    [Fact]
    public async Task EditAsync_VendoredPackage_RootSwapsItForItsDependenciesAndOtherProjectLosesIt()
    {
        // Arrange
        const string root =
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
            "  <ItemGroup>\n" +
            "    <PackageReference Include=\"Ardalis.GuardClauses\" />\n" +
            "    <PackageReference Include=\"Ardalis.SharedKernel\" />\n" +
            "  </ItemGroup>\n" +
            "</Project>\n";
        const string other =
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
            "  <ItemGroup>\n" +
            "    <PackageReference Include=\"Ardalis.SharedKernel\" />\n" +
            "    <PackageReference Include=\"MailKit\" />\n" +
            "  </ItemGroup>\n" +
            "</Project>\n";
        const string versions =
            "<Project>\n" +
            "  <ItemGroup>\n" +
            "    <PackageVersion Include=\"Ardalis.GuardClauses\" Version=\"4.5.0\" />\n" +
            "    <PackageVersion Include=\"Ardalis.SharedKernel\" Version=\"1.4.0\" />\n" +
            "    <PackageVersion Include=\"MailKit\" Version=\"4.0.0\" />\n" +
            "  </ItemGroup>\n" +
            "</Project>\n";
        _fileSystem.AddFile(Project, new MockFileData(root));
        _fileSystem.AddFile(OtherProject, new MockFileData(other));
        _fileSystem.AddFile(PackagesProps, new MockFileData(versions));
        ProjectMigration Vendored(string path, PackageMigration package) =>
            new() { ProjectPath = path, Package = package, KeepRemovedPackage = false, RemovedNamespaces = [], AddedNamespaces = [] };

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(
            Vendored(Project, new PackageMigration
            {
                RemovedPackage = "Ardalis.SharedKernel",
                AddedPackages = [new() { Name = "Ardalis.GuardClauses", Version = "4.1.1" }, new() { Name = "Ardalis.Specification", Version = "7.0.0" }],
            }),
            Vendored(OtherProject, new PackageMigration { RemovedPackage = "Ardalis.SharedKernel", AlsoRemovedPackages = ["Ardalis.SharedKernel"], AddedPackages = [] }));

        // Assert: an existing PackageVersion keeps its version
        Text(changes, Project).ShouldBe(root.Replace(
            "    <PackageReference Include=\"Ardalis.SharedKernel\" />", "    <PackageReference Include=\"Ardalis.Specification\" />", StringComparison.Ordinal));
        Text(changes, OtherProject).ShouldBe(other.Replace("    <PackageReference Include=\"Ardalis.SharedKernel\" />\n", string.Empty, StringComparison.Ordinal));
        Text(changes, PackagesProps).ShouldBe(versions.Replace(
            "<PackageVersion Include=\"Ardalis.SharedKernel\" Version=\"1.4.0\" />", "<PackageVersion Include=\"Ardalis.Specification\" Version=\"7.0.0\" />", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SaveAsync_FileWithBom_WritesNewTextWithBom()
    {
        // Arrange
        byte[] bom = Encoding.UTF8.GetPreamble();
        _fileSystem.AddFile(Project, new MockFileData([.. bom, .. Encoding.UTF8.GetBytes("old")]));
        var store = new XmlProjectFileStore(_fileSystem);

        // Act
        await store.SaveAsync([new ProjectFileChange { FilePath = Project, OldText = "old", NewText = "new" }], TestContext.Current.CancellationToken);

        // Assert
        _fileSystem.File.ReadAllBytes(Project).ShouldBe([.. bom, .. Encoding.UTF8.GetBytes("new")]);
    }

    [Fact]
    public async Task SaveAsync_FileChangedSinceEdit_ThrowsAndKeepsFile()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData("changed"));
        var store = new XmlProjectFileStore(_fileSystem);

        // Act
        Func<Task> save = () => store.SaveAsync(
            [new ProjectFileChange { FilePath = Project, OldText = "old", NewText = "new" }], TestContext.Current.CancellationToken);

        // Assert
        await save.ShouldThrowAsync<InvalidOperationException>();
        _fileSystem.File.ReadAllText(Project).ShouldBe("changed");
    }

    [Fact]
    public async Task SaveAsync_SecondFileChanged_WritesNothing()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData("old"));
        _fileSystem.AddFile(OtherProject, new MockFileData("changed"));
        var store = new XmlProjectFileStore(_fileSystem);

        // Act
        Func<Task> save = () => store.SaveAsync(
            [
                new ProjectFileChange { FilePath = Project, OldText = "old", NewText = "new" },
                new ProjectFileChange { FilePath = OtherProject, OldText = "old", NewText = "new" },
            ],
            TestContext.Current.CancellationToken);

        // Assert
        await save.ShouldThrowAsync<InvalidOperationException>();
        _fileSystem.File.ReadAllText(Project).ShouldBe("old");
    }

    [Fact]
    public async Task SaveAsync_SecondWriteFails_RestoresTheFirst()
    {
        // Arrange
        // A real disk: the mock file system lets a file be written over a directory.
        using var directory = new TempDirectory();
        string first = Path.Combine(directory.Path, "A.csproj");
        string second = Path.Combine(directory.Path, "B.csproj");
        await File.WriteAllTextAsync(first, "old", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(second, "old", TestContext.Current.CancellationToken);
        Directory.CreateDirectory(second + ".unshackle.tmp"); // the temp file of the second cannot be created
        var store = new XmlProjectFileStore(new FileSystem());

        // Act
        Func<Task> save = () => store.SaveAsync(
            [
                new ProjectFileChange { FilePath = first, OldText = "old", NewText = "new" },
                new ProjectFileChange { FilePath = second, OldText = "old", NewText = "new" },
            ],
            TestContext.Current.CancellationToken);

        // Assert
        await save.ShouldThrowAsync<Exception>();
        (await File.ReadAllTextAsync(first, TestContext.Current.CancellationToken)).ShouldBe("old");
        (await File.ReadAllTextAsync(second, TestContext.Current.CancellationToken)).ShouldBe("old");
        File.Exists(first + ".unshackle.tmp").ShouldBeFalse();
    }

    [Fact]
    public async Task EditAsync_ClosingTagInTrailingComment_KeepsTheComment()
    {
        // Arrange
        const string project = "<Project>\n  <ItemGroup><PackageReference Include=\"FluentAssertions\" /></ItemGroup>\n</Project>\n<!-- </Project> -->\n";
        _fileSystem.AddFile(Project, new MockFileData(project));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        Text(changes, Project).ShouldBe(project.Replace("FluentAssertions", "Shouldly", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("<Project>\n  <ItemGroup>\n    <PackageReference Include=\"FluentAssertions\"/>\n    <Using Include=\"FluentAssertions\"/>\n  </ItemGroup>\n</Project>\n")]
    [InlineData("<Project>\n  <ItemGroup>\n    <PackageReference Include=\"FluentAssertions\" >\n      <PrivateAssets>all</PrivateAssets>\n    </PackageReference>\n  </ItemGroup>\n</Project>\n")]
    [InlineData("<Project>\n  <ItemGroup>\n    <None Include=\"@(DistFiles->'%(FullPath)')\" />\n    <PackageReference Include=\"FluentAssertions\" />\n  </ItemGroup>\n</Project>\n")]
    [InlineData("<Project>\n  <ItemGroup>\n    <PackageReference\n      Include=\"FluentAssertions\"\n      Version=\"7.2.2\" />\n  </ItemGroup>\n</Project>\n")]
    [InlineData("<Project>\r\n  <ItemGroup>\n    <PackageReference Include=\"FluentAssertions\" Version=\"7.2.2\" />\r\n  </ItemGroup>\n</Project>\r\n")]
    [InlineData("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<!-- a -> b -->\r\n<Project>\r\n  <ItemGroup>\r\n    <PackageReference Include='FluentAssertions' Version='7.2.2'/> <!-- x -->\r\n  </ItemGroup>\r\n</Project>")]
    public async Task EditAsync_UnusualFormattingWithBom_ChangesOnlyTheValues(string project)
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(project)]));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        Text(changes, Project).ShouldBe(project
            .Replace("FluentAssertions", "Shouldly", StringComparison.Ordinal)
            .Replace("7.2.2", "4.3.0", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EditAsync_MixedLineEndings_RemovesOnlyTheReferenceLine()
    {
        // Arrange
        const string project = "<Project>\r\n  <ItemGroup>\n    <PackageReference Include=\"Shouldly\"/>\r\n    <PackageReference Include=\"FluentAssertions\"/>\n  </ItemGroup>\r\n</Project>\r\n";
        _fileSystem.AddFile(Project, new MockFileData(project));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        Text(changes, Project).ShouldBe(project.Replace("\r\n    <PackageReference Include=\"FluentAssertions\"/>", "", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EditAsync_AddsReferenceNextToNeighbour_KeepsItsLineEnding()
    {
        // Arrange
        const string project = "<Project>\r\n  <ItemGroup>\r\n    <PackageReference Include=\"xunit\"/>\r\n  </ItemGroup>\r\n</Project>\r\n";
        _fileSystem.AddFile(Project, new MockFileData(project));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        Text(changes, Project).ShouldBe(project.Replace(
            "<PackageReference Include=\"xunit\"/>", "<PackageReference Include=\"xunit\"/>\r\n    <PackageReference Include=\"Shouldly\" Version=\"4.3.0\" />", StringComparison.Ordinal));
    }

    [Fact]
    public async Task EditAsync_UnchangedBuildPropsWithUnusualFormatting_IsLeftAlone()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData("<Project>\n  <ItemGroup>\n    <PackageReference Include=\"FluentAssertions\" />\n  </ItemGroup>\n</Project>\n"));
        _fileSystem.AddFile(BuildProps, new MockFileData("<Project>\r\n  <PropertyGroup >\n    <Nullable>enable</Nullable><X\n  A=\"1\"/>\r\n  </PropertyGroup>\n</Project>"));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        changes.Select(c => c.FilePath).ShouldBe([FullPath(Project)]);
    }

    [Theory]
    [InlineData("Alias=\"FA\"")]
    [InlineData("Static=\"true\"")]
    public async Task EditAsync_AliasOrStaticUsingOfRemovedNamespace_Throws(string attribute)
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(
            $"<Project>\n  <ItemGroup>\n    <PackageReference Include=\"FluentAssertions\" />\n    <Using Include=\"FluentAssertions\" {attribute} />\n  </ItemGroup>\n</Project>\n"));

        // Act
        Func<Task> edit = () => EditAsync(Migration(Project, keep: false));

        // Assert
        (await edit.ShouldThrowAsync<InvalidDataException>()).Message.ShouldContain("Alias or Static");
    }

    [Fact]
    public async Task SaveAsync_NotUtf8_ThrowsAndKeepsFile()
    {
        // Arrange
        byte[] latin1 = [0x63, 0x61, 0x66, 0xE9]; // "café" in Latin-1
        _fileSystem.AddFile(Project, new MockFileData(latin1));
        var store = new XmlProjectFileStore(_fileSystem);

        // Act
        Func<Task> save = () => store.SaveAsync(
            [new ProjectFileChange { FilePath = Project, OldText = "caf�", NewText = "new" }], TestContext.Current.CancellationToken);

        // Assert
        await save.ShouldThrowAsync<InvalidDataException>();
        _fileSystem.File.ReadAllBytes(Project).ShouldBe(latin1);
    }

    [Fact]
    public async Task EditAsync_UnloadedProjectUsesCentralPackage_KeepsVersionNextToNewOne()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(CentralProject));
        _fileSystem.AddFile(OtherProject, new MockFileData(CentralProject));
        _fileSystem.AddFile(PackagesProps, new MockFileData(CentralVersions));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        Text(changes, Project).ShouldContain("<PackageReference Include=\"Shouldly\" />");
        ProjectFileChange versions = changes.Single(c => c.FilePath == FullPath(PackagesProps));
        versions.NewText.ShouldBe(CentralVersions.Replace(
            "<PackageVersion Include=\"FluentAssertions\" Version=\"7.2.2\" />",
            "<PackageVersion Include=\"FluentAssertions\" Version=\"7.2.2\" />\n    <PackageVersion Include=\"Shouldly\" Version=\"4.3.0\" />", StringComparison.Ordinal));
        versions.KeptRemovedPackageFor.ShouldBe(FullPath(OtherProject));
        changes.ShouldNotContain(c => c.FilePath == FullPath(OtherProject));
    }

    [Theory]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\" />")]
    [InlineData("<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><PackageReference Include=\"xunit.v3\" /></ItemGroup></Project>")]
    public async Task EditAsync_UnloadedProjectWithoutPackage_RemovesSharedVersion(string other)
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData(CentralProject));
        _fileSystem.AddFile(OtherProject, new MockFileData(other));
        _fileSystem.AddFile(PackagesProps, new MockFileData(CentralVersions));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        ProjectFileChange versions = changes.Single(c => c.FilePath == FullPath(PackagesProps));
        versions.NewText.ShouldNotContain("FluentAssertions");
        versions.KeptRemovedPackageFor.ShouldBeNull();
    }

    [Fact]
    public async Task EditAsync_UnloadedProjectInheritsReferenceFromSharedBuildProps_KeepsIt()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData("<Project Sdk=\"Microsoft.NET.Sdk\" />\n"));
        _fileSystem.AddFile(OtherProject, new MockFileData("<Project Sdk=\"Microsoft.NET.Sdk\" />\n"));
        _fileSystem.AddFile(BuildProps, new MockFileData(SharedReference));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        ProjectFileChange change = changes.ShouldHaveSingleItem();
        change.FilePath.ShouldBe(FullPath(BuildProps));
        change.NewText.ShouldContain("Include=\"FluentAssertions\"");
        change.NewText.ShouldContain("Include=\"Shouldly\" Version=\"4.3.0\"");
        change.KeptRemovedPackageFor.ShouldBe(FullPath(OtherProject));
    }

    [Fact]
    public async Task EditAsync_UnloadedProjectHasNearerBuildProps_DoesNotKeepOuterOne()
    {
        // Arrange
        _fileSystem.AddFile(Project, new MockFileData("<Project Sdk=\"Microsoft.NET.Sdk\" />\n"));
        _fileSystem.AddFile(OtherProject, new MockFileData("<Project Sdk=\"Microsoft.NET.Sdk\" />\n"));
        _fileSystem.AddFile("/repo/src/Other/Directory.Build.props", new MockFileData("<Project />\n"));
        _fileSystem.AddFile(BuildProps, new MockFileData(SharedReference));

        // Act
        IReadOnlyList<ProjectFileChange> changes = await EditAsync(Migration(Project, keep: false));

        // Assert
        ProjectFileChange change = changes.ShouldHaveSingleItem();
        change.NewText.ShouldNotContain("FluentAssertions");
        change.KeptRemovedPackageFor.ShouldBeNull();
    }

    private const string SharedReference =
        "<Project>\n  <ItemGroup>\n    <PackageReference Include=\"FluentAssertions\" Version=\"7.2.2\" />\n  </ItemGroup>\n</Project>\n";

    private static ProjectMigration Migration(string projectPath, bool keep) => new()
    {
        ProjectPath = projectPath,
        Package = new PackageMigration { RemovedPackage = "FluentAssertions", AddedPackages = [new() { Name = "Shouldly", Version = "4.3.0" }] },
        KeepRemovedPackage = keep,
        RemovedNamespaces = ["FluentAssertions"],
        AddedNamespaces = ["Shouldly"],
    };

    // The store reports full paths of the file system it runs on ("C:\repo\..." on Windows).
    private string FullPath(string path) => _fileSystem.Path.GetFullPath(path);

    private string Text(IReadOnlyList<ProjectFileChange> changes, string path) => changes.Single(c => c.FilePath == FullPath(path)).NewText;

    private Task<IReadOnlyList<ProjectFileChange>> EditAsync(params ProjectMigration[] migrations) =>
        new XmlProjectFileStore(_fileSystem).EditAsync(migrations, TestContext.Current.CancellationToken);
}
