using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.SpecFlow;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.SpecFlow;

public class SpecFlowFixTests
{
    [Fact]
    public void Patterns_IdsUseSpecFlowPrefix_NamespacePatternLast()
    {
        // Arrange
        SpecFlowFix fix = new();

        // Act
        List<string> ids = [.. fix.Patterns.Select(p => p.Id)];

        // Assert
        fix.Id.ShouldBe("specflow");
        ids.ShouldAllBe(id => id.StartsWith("specflow.", StringComparison.Ordinal));
        ids[^1].ShouldBe("specflow.namespace");
        fix.AllOrNothing.ShouldBeTrue();
    }

    [Fact]
    public void ReqnrollVersion_MatchesTheVersionTheProofsCompileAgainst()
    {
        // Arrange
        string props = File.ReadAllText(Path.Combine(RepositoryRoot.Path, "Directory.Packages.props"));

        // Act
        bool pinned = props.Contains($"Include=\"Reqnroll\" Version=\"{SpecFlowFix.ReqnrollVersion}\"", StringComparison.Ordinal);

        // Assert
        pinned.ShouldBeTrue();
    }

    [Fact]
    public void EditsFor_ProjectOnSpecFlow_RenamesEveryPackageToReqnroll()
    {
        // Arrange
        Project project = CreateProject(SpecFlowReferences.SpecFlow);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = new SpecFlowFix().EditsFor(project);

        // Assert
        List<PackageRenameEdit> renames = [.. edits.Select(e => e.ShouldBeOfType<PackageRenameEdit>())];
        renames.Select(r => (r.OldName, r.NewName)).ShouldBe(
        [
            ("SpecFlow", "Reqnroll"), ("SpecFlow.NUnit", "Reqnroll.NUnit"), ("SpecFlow.xUnit", "Reqnroll.xUnit"), ("SpecFlow.MsTest", "Reqnroll.MsTest"),
            ("SpecFlow.Tools.MsBuild.Generation", "Reqnroll.Tools.MsBuild.Generation"),
        ]);
        renames.ShouldAllBe(r => r.Version == SpecFlowFix.ReqnrollVersion);
    }

    [Fact]
    public void EditsFor_ProjectWithoutSpecFlow_LeavesItAlone()
    {
        // Arrange
        Project project = CreateProject(SpecFlowReferences.Reqnroll);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = new SpecFlowFix().EditsFor(project);

        // Assert
        edits.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(@"C:\src\Shop\packages\SpecFlow.3.9.74\lib\net461\TechTalk.SpecFlow.dll", true)]
    [InlineData(@"C:\src\Shop\packages\BoDi.1.5.0\lib\net45\BoDi.dll", true)]
    [InlineData(@"C:\Users\me\.nuget\packages\specflow\3.9.74\lib\netstandard2.0\TechTalk.SpecFlow.dll", false)]
    public void IsPackagesFolderPath_PackagesConfigLayout_IsDetected(string path, bool expected)
    {
        // Act
        bool actual = SpecFlowFix.IsPackagesFolderPath(path);

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public void ProjectBlockers_PackageReferenceProject_IsNotBlocked()
    {
        // Arrange
        Project project = CreateProject(SpecFlowReferences.SpecFlow);

        // Act
        IReadOnlyList<ManualResult> blockers = new SpecFlowFix().ProjectBlockers(project);

        // Assert
        blockers.ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_StringStepCallInOneFile_LeavesTheWholeSolutionOnSpecFlow()
    {
        // Arrange (AllOrNothing: Reqnroll code next to SpecFlow packages would not build)
        const string code = """
            using TechTalk.SpecFlow;

            [Binding]
            public class LoginSteps : Steps
            {
                [Given("I am logged in")]
                public void GivenIAmLoggedIn() => Given("I open the login page");
            }
            """;
        Solution solution = TestSolution.Create(code, SpecFlowReferences.SpecFlow);

        // Act
        FixReport report = await new Fixer([new SpecFlowFix()]).FixAsync(solution, new FixRequest { FixId = "specflow" }, TestContext.Current.CancellationToken);

        // Assert
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldBe(code);
        report.Findings.ShouldAllBe(f => f.Result is ManualResult);
    }

    private static Project CreateProject(IEnumerable<MetadataReference> references)
    {
        var projectId = ProjectId.CreateNewId();
        return TestSolution.NewEmpty()
            .AddProject(projectId, "P", "P", LanguageNames.CSharp)
            .WithProjectParseOptions(projectId, new CSharpParseOptions())
            .AddMetadataReferences(projectId, references)
            .GetProject(projectId)!;
    }
}
