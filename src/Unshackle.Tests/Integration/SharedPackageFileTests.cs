using System.IO.Abstractions;
using Shouldly;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Projects;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Integration;

// TAAK-118: a project skipped while loading still shares Directory.Packages.props on disk.
[Trait("Category", "Integration")]
public class SharedPackageFileTests
{
    private const string Project = """
        <Project Sdk="Microsoft.NET.Sdk">
          <ItemGroup>
            <PackageReference Include="FluentAssertions" />
          </ItemGroup>
        </Project>

        """;

    [Fact]
    public async Task EditAsync_SkippedProjectUsesPackageCentrally_KeepsItsVersion()
    {
        // Arrange
        using var temp = new TempDirectory();
        string migrated = Path.Combine(temp.Path, "src", "Tests", "Tests.csproj");
        string skipped = Path.Combine(temp.Path, "src", "Web", "Web.csproj");
        string versions = Path.Combine(temp.Path, "Directory.Packages.props");
        Directory.CreateDirectory(Path.GetDirectoryName(migrated)!);
        Directory.CreateDirectory(Path.GetDirectoryName(skipped)!);
        await File.WriteAllTextAsync(migrated, Project, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(skipped, Project, TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(versions, "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"FluentAssertions\" Version=\"7.2.2\" />\n  </ItemGroup>\n</Project>\n",
            TestContext.Current.CancellationToken);
        var migration = new ProjectMigration
        {
            ProjectPath = migrated,
            Package = new PackageMigration { RemovedPackage = "FluentAssertions", AddedPackages = [new() { Name = "Shouldly", Version = "4.3.0" }] },
            KeepRemovedPackage = false,
            RemovedNamespaces = ["FluentAssertions"],
            AddedNamespaces = ["Shouldly"],
        };
        var store = new XmlProjectFileStore(new FileSystem());

        // Act
        IReadOnlyList<ProjectFileChange> changes = await store.EditAsync([migration], TestContext.Current.CancellationToken);
        await store.SaveAsync(changes, TestContext.Current.CancellationToken);

        // Assert
        changes.Single(c => c.FilePath == versions).KeptRemovedPackageFor.ShouldBe(skipped);
        string text = await File.ReadAllTextAsync(versions, TestContext.Current.CancellationToken);
        text.ShouldContain("<PackageVersion Include=\"FluentAssertions\" Version=\"7.2.2\" />");
        text.ShouldContain("<PackageVersion Include=\"Shouldly\" Version=\"4.3.0\" />");
        (await File.ReadAllTextAsync(skipped, TestContext.Current.CancellationToken)).ShouldBe(Project);
        (await File.ReadAllTextAsync(migrated, TestContext.Current.CancellationToken)).ShouldContain("Include=\"Shouldly\"");
    }
}
