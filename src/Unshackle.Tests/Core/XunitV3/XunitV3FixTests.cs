using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Fixes.XunitV3;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.XunitV3;

public class XunitV3FixTests
{
    [Fact]
    public void V3Version_MatchesTheVersionTheProofsCompileAgainst()
    {
        // Arrange
        string props = File.ReadAllText(Path.Combine(RepositoryRoot.Path, "Directory.Packages.props"));

        // Act
        bool pinned = props.Contains($"Include=\"xunit.v3\" Version=\"{XunitV3Fix.V3Version}\"", StringComparison.Ordinal);

        // Assert
        pinned.ShouldBeTrue();
    }

    [Fact]
    public void Patterns_IdsUseXunit3Prefix()
    {
        // Arrange
        XunitV3Fix fix = new();

        // Act
        List<string> ids = [.. fix.Patterns.Select(p => p.Id)];

        // Assert
        ids.ShouldAllBe(id => id.StartsWith("xunit3.", StringComparison.Ordinal));
        fix.Id.ShouldBe("xunit3");
        fix.AllOrNothing.ShouldBeTrue();
        ((Unshackle.Core.Abstractions.IFix)fix).Package.ShouldBeNull();
    }

    [Fact]
    public void EditsFor_TestProject_SetsExeAndRenamesEveryV2Package()
    {
        // Arrange
        Project project = CreateProject(["NETCOREAPP", "NET8_0_OR_GREATER"], XunitV3References.Before);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = new XunitV3Fix().EditsFor(project);

        // Assert
        edits.Select(e => e.Describe()).ShouldBe(
        [
            "<OutputType> set to Exe",
            "PackageReference xunit → xunit.v3 4.0.2",
            "PackageReference xunit.core → xunit.v3 4.0.2",
            "PackageReference xunit.assert → xunit.v3.assert 4.0.2",
            "PackageReference xunit.extensibility.core → xunit.v3.extensibility.core 4.0.2",
            "PackageReference xunit.extensibility.execution → xunit.v3.extensibility.core 4.0.2",
            "PackageReference xunit.abstractions removed",
        ]);
    }

    [Fact]
    public void EditsFor_HelperLibraryWithoutExecution_KeepsOutputType()
    {
        // Arrange: a shared helper project references only xunit.extensibility.core (+ abstractions), not the runner side.
        Project project = CreateProject(["NETSTANDARD"], [.. XunitV3References.Before.Where(r => !(r.Display ?? string.Empty).Contains("execution", StringComparison.Ordinal))]);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = new XunitV3Fix().EditsFor(project);

        // Assert
        edits.ShouldNotContain(e => e is PropertyEdit);
        edits.ShouldContain(e => e is PackageRenameEdit && ((PackageRenameEdit)e).OldName == "xunit.extensibility.core");
        new XunitV3Fix().ProjectBlockers(project).ShouldBeEmpty();
        new XunitV3Fix().ProjectInfo(project).ShouldBeEmpty();
    }

    [Fact]
    public void EditsFor_ProjectAlreadyOnV3_LeavesItAlone()
    {
        // Arrange
        Project project = CreateProject(["NETCOREAPP", "NET8_0_OR_GREATER"], XunitV3References.After);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = new XunitV3Fix().EditsFor(project);

        // Assert
        edits.ShouldBeEmpty();
        new XunitV3Fix().ProjectInfo(project).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("NETCOREAPP;NETCOREAPP3_1", true)]
    [InlineData("NETCOREAPP;NET6_0_OR_GREATER", true)]
    [InlineData("NETFRAMEWORK;NET462_OR_GREATER", true)]
    [InlineData("NETSTANDARD;NETSTANDARD2_0", true)]
    [InlineData("NETCOREAPP;NET8_0_OR_GREATER", false)]
    [InlineData("NETFRAMEWORK;NET472_OR_GREATER", false)]
    public void ProjectBlockers_TestProjectOnUnsupportedFramework_BlocksIt(string symbols, bool blocked)
    {
        // Arrange
        Project project = CreateProject(symbols.Split(';'), XunitV3References.Before);

        // Act
        IReadOnlyList<ManualResult> blockers = new XunitV3Fix().ProjectBlockers(project);

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

    [Fact]
    public void ProjectInfo_TestProject_NamesTheRunnerVersion()
    {
        // Arrange
        Project project = CreateProject(["NETCOREAPP", "NET8_0_OR_GREATER"], XunitV3References.Before);

        // Act
        IReadOnlyList<string> info = new XunitV3Fix().ProjectInfo(project);

        // Assert
        info.ShouldHaveSingleItem().ShouldContain("xunit.runner.visualstudio 3.0");
    }

    [Fact]
    public async Task FixAsync_OneManualFinding_LeavesEveryFileAsItIs()
    {
        // Arrange: a Fixed async void test next to a Manual dual-disposal fixture; half a migration would not build (AllOrNothing).
        const string code = "using System;\nusing System.Threading.Tasks;\nusing Xunit;\n"
            + "public class T { [Fact] public async void A() { await Task.Yield(); } }\n"
            + "public class F : IAsyncLifetime, IDisposable { public Task InitializeAsync() => Task.CompletedTask; public Task DisposeAsync() => Task.CompletedTask; public void Dispose() { } }\n";
        Solution solution = TestSolution.Create(code, XunitV3References.Before);

        // Act
        FixReport report = await new Fixer([new XunitV3Fix()]).FixAsync(solution, new FixRequest { FixId = "xunit3" }, TestContext.Current.CancellationToken);

        // Assert
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldBe(code);
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Code == nameof(XunitV3Reason.DualDisposal));
        report.Findings.ShouldNotContain(f => f.Result is FixedResult);
    }

    [Fact]
    public async Task FixAsync_AllFixed_RewritesEveryPattern()
    {
        // Arrange
        const string code = "using System.Threading.Tasks;\nusing Xunit;\nusing Xunit.Abstractions;\n"
            + "public class T : IAsyncLifetime { public T(ITestOutputHelper o) { } public Task InitializeAsync() => Task.CompletedTask; public async Task DisposeAsync() { await Task.Yield(); } [Fact] public async void A() { await Task.Yield(); } }\n";
        Solution solution = TestSolution.Create(code, XunitV3References.Before);

        // Act
        FixReport report = await new Fixer([new XunitV3Fix()]).FixAsync(solution, new FixRequest { FixId = "xunit3" }, TestContext.Current.CancellationToken);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldBe("using System.Threading.Tasks;\nusing Xunit;\n"
            + "public class T : IAsyncLifetime { public T(ITestOutputHelper o) { } public ValueTask InitializeAsync() => default; public async ValueTask DisposeAsync() { await Task.Yield(); } [Fact] public async Task A() { await Task.Yield(); } }\n",
            string.Join(" | ", report.Findings.Select(f => f.PatternId + " " + f.Result)));
        TestSolution.CompileErrors(text, XunitV3References.After).ShouldBeEmpty();
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
