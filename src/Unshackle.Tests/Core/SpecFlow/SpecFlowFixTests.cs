using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
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
        SpecFlowFix fix = new(Substitute.For<IProjectProperties>());

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
    public async Task ReqnrollAutofacVersion_IsTheReqnrollVersionTheProofsCompileAgainst()
    {
        // Arrange (council round 21, S11: Reqnroll ships its plugins with its own version)
        string props = File.ReadAllText(Path.Combine(RepositoryRoot.Path, "Directory.Packages.props"));
        PackageRenameEdit autofac = (await new SpecFlowFix(Substitute.For<IProjectProperties>()).EditsForAsync(CreateProject(SpecFlowReferences.SpecFlow), TestContext.Current.CancellationToken)).OfType<PackageRenameEdit>().Single(e => e.OldName == "SpecFlow.Autofac");

        // Act
        bool pinned = props.Contains($"Include=\"Reqnroll.Autofac\" Version=\"{autofac.Version}\"", StringComparison.Ordinal);

        // Assert
        autofac.NewName.ShouldBe("Reqnroll.Autofac");
        pinned.ShouldBeTrue();
    }

    [Fact]
    public async Task EditsFor_ProjectOnSpecFlow_RenamesEveryPackageToReqnroll()
    {
        // Arrange
        Project project = CreateProject(SpecFlowReferences.SpecFlow);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = await new SpecFlowFix(Substitute.For<IProjectProperties>()).EditsForAsync(project, TestContext.Current.CancellationToken);

        // Assert
        List<PackageRenameEdit> renames = [.. edits.Select(e => e.ShouldBeOfType<PackageRenameEdit>())];
        renames.Select(r => (r.OldName, r.NewName)).ShouldBe(
        [
            ("SpecFlow", "Reqnroll"), ("SpecFlow.NUnit", "Reqnroll.NUnit"), ("SpecFlow.xUnit", "Reqnroll.xUnit"), ("SpecFlow.MsTest", "Reqnroll.MsTest"),
            ("SpecFlow.Tools.MsBuild.Generation", "Reqnroll.Tools.MsBuild.Generation"), ("SpecFlow.Autofac", "Reqnroll.Autofac"),
        ]);
        renames.ShouldAllBe(r => r.Version == SpecFlowFix.ReqnrollVersion);
    }

    [Fact]
    public async Task EditsFor_RunnerPackageInReferences_MustBeDeclared()
    {
        // Arrange (council round 18, S9: a runner package nothing else brings in is declared by the project or a shared file it imports)
        Project project = CreateProject([.. SpecFlowReferences.SpecFlow, FakeReference("C:/nuget/specflow.nunit/3.9.74/lib/net461/TechTalk.SpecFlow.NUnit.SpecFlowPlugin.dll")]);

        // Act
        List<PackageRenameEdit> renames = [.. (await new SpecFlowFix(Substitute.For<IProjectProperties>()).EditsForAsync(project, TestContext.Current.CancellationToken)).OfType<PackageRenameEdit>()];

        // Assert
        renames.Where(r => r.MustBeDeclared).Select(r => r.OldName).ShouldBe(["SpecFlow.NUnit"]);
    }

    [Theory]
    [InlineData("C:/nuget/specrun.specflow/3.9.31/lib/net461/TechTalk.SpecRun.SpecFlowPlugin.dll", "specrun.specflow")]
    [InlineData("C:/nuget/specflow.assist.dynamic/1.4.2/lib/netstandard2.0/SpecFlow.Assist.Dynamic.dll", "specflow.assist.dynamic")]
    public async Task ProjectBlockers_UnportedPlugin_BlocksNamingThePackage(string path, string package)
    {
        // Arrange (council round 18, S8)
        Project project = CreateProject([.. SpecFlowReferences.SpecFlow, FakeReference(path)]);

        // Act
        IReadOnlyList<ManualResult> blockers = await new SpecFlowFix(Substitute.For<IProjectProperties>()).ProjectBlockersAsync(project, TestContext.Current.CancellationToken);

        // Assert
        blockers.ShouldHaveSingleItem().Reason.ShouldContain($"plugin package {package}, which has no Reqnroll port");
    }

    [Fact]
    public async Task ProjectBlockers_BuildOnlyPluginInProjectFile_BlocksNamingThePackageOnce()
    {
        // Arrange (council round 21, S11: LivingDoc has no reference path; the project file declares it)
        IProjectProperties properties = Substitute.For<IProjectProperties>();
        properties.PackageReferences(Arg.Any<string>()).Returns(["SpecFlow.Plus.LivingDocPlugin", "SpecFlow.NUnit", "specflow.plus.livingdocplugin"]);
        Project project = CreateProject(SpecFlowReferences.SpecFlow, "C:/src/P/P.csproj");

        // Act
        IReadOnlyList<ManualResult> blockers = await new SpecFlowFix(properties).ProjectBlockersAsync(project, TestContext.Current.CancellationToken);

        // Assert
        blockers.ShouldHaveSingleItem().Reason.ShouldContain("plugin package SpecFlow.Plus.LivingDocPlugin, which has no Reqnroll port");
    }

    [Fact]
    public async Task ProjectBlockers_UnknownSpecFlowPackageInProjectFile_Blocks()
    {
        // Arrange (council round 21, S11)
        IProjectProperties properties = Substitute.For<IProjectProperties>();
        properties.PackageReferences(Arg.Any<string>()).Returns(["SpecFlow", "SpecFlow.Autofac", "SpecFlow.Contrib.Variants", "Shouldly"]);
        Project project = CreateProject(SpecFlowReferences.SpecFlow, "C:/src/P/P.csproj");

        // Act
        IReadOnlyList<ManualResult> blockers = await new SpecFlowFix(properties).ProjectBlockersAsync(project, TestContext.Current.CancellationToken);

        // Assert
        blockers.ShouldHaveSingleItem().Reason.ShouldContain("SpecFlow.Contrib.Variants, a SpecFlow package Unshackle does not know");
    }

    [Fact]
    public async Task ProjectBlockers_ProjectWithoutSpecFlowReferences_DoesNotReadTheProjectFile()
    {
        // Arrange
        IProjectProperties properties = Substitute.For<IProjectProperties>();
        Project project = CreateProject(SpecFlowReferences.Reqnroll, "C:/src/P/P.csproj");

        // Act
        IReadOnlyList<ManualResult> blockers = await new SpecFlowFix(properties).ProjectBlockersAsync(project, TestContext.Current.CancellationToken);

        // Assert
        blockers.ShouldBeEmpty();
        properties.DidNotReceiveWithAnyArgs().PackageReferences(default!);
    }

    [Theory]
    [InlineData(@"D:\nuget\specflow.xunit\3.9.74\lib\netstandard2.0\x.dll", "specflow.xunit")]
    [InlineData("/nuget/specflow/3.9.74/lib/net461/TechTalk.SpecFlow.dll", "specflow")]
    [InlineData("/usr/share/dotnet/packs/ref/System.Runtime.dll", null)]
    public void PackageIdOf_NuGetPath_IsTheFolderAboveTheVersion(string path, string? expected)
    {
        // Act
        string? id = SpecFlowFix.PackageIdOf(path);

        // Assert
        id.ShouldBe(expected);
    }

    [Fact]
    public async Task EditsFor_ProjectWithoutSpecFlow_LeavesItAlone()
    {
        // Arrange
        Project project = CreateProject(SpecFlowReferences.Reqnroll);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = await new SpecFlowFix(Substitute.For<IProjectProperties>()).EditsForAsync(project, TestContext.Current.CancellationToken);

        // Assert
        edits.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(@"C:\src\Shop\packages\SpecFlow.3.9.74\lib\net461\TechTalk.SpecFlow.dll", true)]
    [InlineData(@"C:\src\Shop\packages\BoDi.1.5.0\lib\net45\BoDi.dll", true)]
    [InlineData(@"D:\nuget\specflow\3.9.74\lib\netstandard2.0\TechTalk.SpecFlow.dll", false)]
    public void IsPackagesFolderPath_PackagesConfigLayout_IsDetected(string path, bool expected)
    {
        // Act
        bool actual = SpecFlowFix.IsPackagesFolderPath(path);

        // Assert
        actual.ShouldBe(expected);
    }

    [Fact]
    public async Task ProjectBlockers_PackageReferenceProject_IsNotBlocked()
    {
        // Arrange
        Project project = CreateProject(SpecFlowReferences.SpecFlow);

        // Act
        IReadOnlyList<ManualResult> blockers = await new SpecFlowFix(Substitute.For<IProjectProperties>()).ProjectBlockersAsync(project, TestContext.Current.CancellationToken);

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
        FixReport report = await new Fixer([new SpecFlowFix(Substitute.For<IProjectProperties>())]).FixAsync(solution, new FixRequest { FixId = "specflow" }, TestContext.Current.CancellationToken);

        // Assert
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldBe(code);
        report.Findings.ShouldAllBe(f => f.Result is ManualResult);
    }

    // The SpecFlow assembly under another package's path: only the path matters to the fix.
    private static PortableExecutableReference FakeReference(string path) =>
        MetadataReference.CreateFromImage(File.ReadAllBytes(SpecFlowReferences.SpecFlow.OfType<PortableExecutableReference>()
            .First(r => Path.GetFileNameWithoutExtension(r.FilePath) == "TechTalk.SpecFlow").FilePath!), filePath: path);

    private static Project CreateProject(IEnumerable<MetadataReference> references, string? filePath = null)
    {
        var projectId = ProjectId.CreateNewId();
        return TestSolution.NewEmpty()
            .AddProject(projectId, "P", "P", LanguageNames.CSharp)
            .WithProjectFilePath(projectId, filePath)
            .WithProjectParseOptions(projectId, new CSharpParseOptions())
            .AddMetadataReferences(projectId, references)
            .GetProject(projectId)!;
    }
}
