using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
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
        // Arrange: the proofs' "after" assemblies are downloaded at exactly this version (council round 18, X2).
        string project = File.ReadAllText(Path.Combine(RepositoryRoot.Path, "src", "Unshackle.Tests", "Unshackle.Tests.csproj"));

        // Act
        bool pinned = project.Contains($"Include=\"xunit.v3.extensibility.core\" Version=\"[{XunitV3Fix.V3Version}]\"", StringComparison.Ordinal)
            && XunitV3Fix.V3Version.StartsWith("3.2.", StringComparison.Ordinal);

        // Assert
        pinned.ShouldBeTrue();
    }

    [Fact]
    public void TestSdkVersion_IsTheObjectModelTheRunnerNeeds()
    {
        // Arrange: the real xunit.runner.visualstudio nuspec (PackageDownload, copied to XunitV3/); on .NET Framework it needs
        // Microsoft.TestPlatform.ObjectModel at least this version, which Microsoft.NET.Test.Sdk of the same version brings.
        var nuspec = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "XunitV3", "xunit.runner.visualstudio.nuspec"));

        // Act
        XElement package = nuspec.Root!.Elements().First(e => e.Name.LocalName == "metadata");
        string? version = package.Elements().First(e => e.Name.LocalName == "version").Value;
        string? objectModel = package.Descendants().Where(e => e.Name.LocalName == "dependency" && (string?)e.Attribute("id") == "Microsoft.TestPlatform.ObjectModel")
            .Select(e => (string?)e.Attribute("version")).Max(StringComparer.Ordinal);

        // Assert
        version.ShouldBe(XunitV3Fix.RunnerVersion);
        objectModel.ShouldBe(XunitV3Fix.TestSdkVersion);
    }

    [Fact]
    public void Patterns_IdsUseXunit3Prefix()
    {
        // Arrange
        XunitV3Fix fix = new(Substitute.For<IProjectProperties>());

        // Act
        List<string> ids = [.. fix.Patterns.Select(p => p.Id)];

        // Assert
        ids.ShouldAllBe(id => id.StartsWith("xunit3.", StringComparison.Ordinal));
        fix.Id.ShouldBe("xunit3");
        fix.AllOrNothing.ShouldBeTrue();
        ((Unshackle.Core.Abstractions.IFix)fix).Package.ShouldBeNull();
    }

    [Fact]
    public async Task EditsFor_TestProject_SetsExeAndRenamesEveryV2Package()
    {
        // Arrange
        Project project = CreateTestProject(["NETCOREAPP", "NET8_0_OR_GREATER"]);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = await new XunitV3Fix(Substitute.For<IProjectProperties>()).EditsForAsync(project, TestContext.Current.CancellationToken);

        // Assert
        edits.Select(e => e.Describe()).ShouldBe(
        [
            "<OutputType> set to Exe",
            "PackageReference xunit → xunit.v3 3.2.2",
            "PackageReference xunit.core → xunit.v3 3.2.2",
            "PackageReference xunit.runner.visualstudio 2.0.0 or later → 3.1.5",
            "PackageReference Microsoft.NET.Test.Sdk 15.0.0 or later → 17.13.0",
            "PackageReference xunit.assert → xunit.v3.assert 3.2.2",
            "PackageReference xunit.extensibility.core → xunit.v3.extensibility.core 3.2.2",
            "PackageReference xunit.extensibility.execution → xunit.v3.extensibility.core 3.2.2",
            "PackageReference xunit.abstractions removed",
        ]);
        // Council round 20, X9: a 1.x runner is kept with an Info row.
        edits.OfType<PackageVersionEdit>().First().BelowFromInfo.ShouldNotBeNull().ShouldContain("1.x runner");
        (await new XunitV3Fix(Substitute.For<IProjectProperties>()).ProjectInfoAsync(project, TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("net472", false, null)]
    [InlineData("net8.0,net48", false, null)]
    [InlineData("net8.0", false, "17.13.0+ recommended")]
    [InlineData(null, true, null)]
    public async Task EditsFor_TestSdk_BumpOnlyForNetFramework(string? frameworks, bool manual, string? info)
    {
        // Arrange (council round 25, X15)
        IProjectProperties properties = Substitute.For<IProjectProperties>();
        properties.TargetFrameworks("C:/src/P/P.csproj").Returns(frameworks?.Split(','));
        Project test = CreateTestProject(["NETCOREAPP", "NET8_0_OR_GREATER"]);
        Project project = test.Solution.WithProjectFilePath(test.Id, "C:/src/P/P.csproj").GetProject(test.Id)!;

        // Act
        PackageVersionEdit edit = (await new XunitV3Fix(properties).EditsForAsync(project, TestContext.Current.CancellationToken))
            .OfType<PackageVersionEdit>().Single(e => e.Name == "Microsoft.NET.Test.Sdk");

        // Assert
        (edit.ManualInstead is not null).ShouldBe(manual);
        if (info is null)
        {
            edit.InfoInstead.ShouldBeNull();
        }
        else
        {
            edit.InfoInstead.ShouldNotBeNull().ShouldContain(info);
        }

        edit.CentralKeptIsManual.ShouldBe(!manual && info is null);
    }

    [Theory]
    [InlineData("net472", true)]
    [InlineData("net48", true)]
    [InlineData("net481", true)]
    [InlineData("net8.0", false)]
    [InlineData("net10.0", false)]
    [InlineData("netstandard2.0", false)]
    [InlineData("netcoreapp3.1", false)]
    [InlineData("net8.0-windows", false)]
    public void IsNetFramework_OnlyDotlessNet4AndOlder(string framework, bool expected) =>
        XunitV3Fix.IsNetFramework(framework).ShouldBe(expected);

    [Theory]
    [InlineData(true, false, "references Microsoft.NET.Test.Sdk and has no xUnit test attributes")]
    [InlineData(false, true, "does not reference Microsoft.NET.Test.Sdk and has xUnit test attributes")]
    [InlineData(false, false, "does not reference Microsoft.NET.Test.Sdk and has no xUnit test attributes")]
    public async Task EditsFor_XunitPackageWithoutBothTestSigns_NoExeLibraryPackagesAndInfo(bool sdk, bool attributes, string why)
    {
        // Arrange (council round 18, X4): either sign alone is not a test project.
        Project project = CreateProject(["NETCOREAPP", "NET8_0_OR_GREATER"], sdk ? [.. XunitV3References.Before, TestSdk] : XunitV3References.Before, attributes ? TestCode : HelperCode);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = await new XunitV3Fix(Substitute.For<IProjectProperties>()).EditsForAsync(project, TestContext.Current.CancellationToken);

        // Assert
        edits.Select(e => e.Describe()).ShouldBe(
        [
            "PackageReference xunit → xunit.v3.extensibility.core 3.2.2",
            "PackageReference xunit.core → xunit.v3.extensibility.core 3.2.2",
            "PackageReference xunit.v3.assert 3.2.2 added",
            "PackageReference xunit.assert → xunit.v3.assert 3.2.2",
            "PackageReference xunit.extensibility.core → xunit.v3.extensibility.core 3.2.2",
            "PackageReference xunit.extensibility.execution → xunit.v3.extensibility.core 3.2.2",
            "PackageReference xunit.abstractions removed",
        ]);
        (await new XunitV3Fix(Substitute.For<IProjectProperties>()).ProjectInfoAsync(project, TestContext.Current.CancellationToken)).ShouldHaveSingleItem().ShouldContain(why);
        (await new XunitV3Fix(Substitute.For<IProjectProperties>()).ProjectBlockersAsync(project, TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task HasTestAttributes_DerivedFactAttribute_Counts()
    {
        // Arrange: a custom attribute that derives from FactAttribute marks a test too.
        Project project = CreateProject(["NETCOREAPP", "NET8_0_OR_GREATER"], [.. XunitV3References.Before, TestSdk],
            "using Xunit;\npublic class SlowFactAttribute : FactAttribute { }\npublic class T { [SlowFact] public void A() { } }\n");

        // Act
        bool test = await XunitV3Assemblies.IsTestProjectAsync(project, TestContext.Current.CancellationToken);

        // Assert
        test.ShouldBeTrue();
    }

    [Fact]
    public async Task EditsFor_HelperLibraryWithoutExecution_KeepsOutputType()
    {
        // Arrange: a shared helper project references only xunit.extensibility.core (+ abstractions), not the runner side.
        Project project = CreateProject(["NETSTANDARD"], [.. XunitV3References.Before.Where(r => !(r.Display ?? string.Empty).Contains("execution", StringComparison.Ordinal))]);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = await new XunitV3Fix(Substitute.For<IProjectProperties>()).EditsForAsync(project, TestContext.Current.CancellationToken);

        // Assert
        edits.ShouldNotContain(e => e is PropertyEdit);
        edits.ShouldContain(e => e is PackageRenameEdit && ((PackageRenameEdit)e).OldName == "xunit.extensibility.core");
        (await new XunitV3Fix(Substitute.For<IProjectProperties>()).ProjectBlockersAsync(project, TestContext.Current.CancellationToken)).ShouldBeEmpty();
        (await new XunitV3Fix(Substitute.For<IProjectProperties>()).ProjectInfoAsync(project, TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public async Task EditsFor_ProjectAlreadyOnV3_LeavesItAlone()
    {
        // Arrange
        Project project = CreateProject(["NETCOREAPP", "NET8_0_OR_GREATER"], XunitV3References.After);

        // Act
        IReadOnlyList<ProjectFileEdit> edits = await new XunitV3Fix(Substitute.For<IProjectProperties>()).EditsForAsync(project, TestContext.Current.CancellationToken);

        // Assert
        edits.ShouldBeEmpty();
        (await new XunitV3Fix(Substitute.For<IProjectProperties>()).ProjectInfoAsync(project, TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("NETCOREAPP;NETCOREAPP3_1", true)]
    [InlineData("NETCOREAPP;NET6_0_OR_GREATER", true)]
    [InlineData("NETFRAMEWORK;NET462_OR_GREATER", true)]
    [InlineData("NETSTANDARD;NETSTANDARD2_0", true)]
    [InlineData("NETCOREAPP;NET8_0_OR_GREATER", false)]
    [InlineData("NETFRAMEWORK;NET472_OR_GREATER", false)]
    public async Task ProjectBlockers_TestProjectOnUnsupportedFramework_BlocksIt(string symbols, bool blocked)
    {
        // Arrange
        Project project = CreateTestProject(symbols.Split(';'));

        // Act
        IReadOnlyList<ManualResult> blockers = await new XunitV3Fix(Substitute.For<IProjectProperties>()).ProjectBlockersAsync(project, TestContext.Current.CancellationToken);

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
    public async Task FixAsync_OneManualFinding_LeavesEveryFileAsItIs()
    {
        // Arrange: a Fixed async void test next to a Manual dual-disposal fixture; half a migration would not build (AllOrNothing).
        const string code = "using System;\nusing System.Threading.Tasks;\nusing Xunit;\n"
            + "public class T { [Fact] public async void A() { await Task.Yield(); } }\n"
            + "public class F : IAsyncLifetime, IDisposable { public Task InitializeAsync() => Task.CompletedTask; public Task DisposeAsync() => Task.CompletedTask; public void Dispose() { } }\n";
        Solution solution = TestSolution.Create(code, XunitV3References.Before);

        // Act
        FixReport report = await new Fixer([new XunitV3Fix(Substitute.For<IProjectProperties>())]).FixAsync(solution, new FixRequest { FixId = "xunit3" }, TestContext.Current.CancellationToken);

        // Assert
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldBe(code);
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Code == nameof(XunitV3Reason.DualDisposal));
        report.Findings.ShouldNotContain(f => f.Result is FixedResult);
        BlockingProject blocking = report.BlockingProjects.ShouldHaveSingleItem();
        blocking.Reasons.ShouldHaveSingleItem().ShouldContain("implements IAsyncLifetime and IDisposable");
    }

    [Fact]
    public async Task FixAsync_AllFixed_RewritesEveryPattern()
    {
        // Arrange
        const string code = "using System.Threading.Tasks;\nusing Xunit;\nusing Xunit.Abstractions;\n"
            + "public class T : IAsyncLifetime { public T(ITestOutputHelper o) { } public Task InitializeAsync() => Task.CompletedTask; public async Task DisposeAsync() { await Task.Yield(); } [Fact] public async void A() { await Task.Yield(); } }\n";
        Solution solution = TestSolution.Create(code, XunitV3References.Before);

        // Act
        FixReport report = await new Fixer([new XunitV3Fix(Substitute.For<IProjectProperties>())]).FixAsync(solution, new FixRequest { FixId = "xunit3" }, TestContext.Current.CancellationToken);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldBe("using System.Threading.Tasks;\nusing Xunit;\n"
            + "public class T : IAsyncLifetime { public T(ITestOutputHelper o) { } public ValueTask InitializeAsync() => default; public async ValueTask DisposeAsync() { await Task.Yield(); } [Fact] public async Task A() { await Task.Yield(); } }\n",
            string.Join(" | ", report.Findings.Select(f => f.PatternId + " " + f.Result)));
        TestSolution.CompileErrors(text, XunitV3References.After).ShouldBeEmpty();
    }

    private const string TestCode = "using Xunit;\npublic class T { [Fact] public void A() { } }\n";

    private const string HelperCode = "public static class Helper { }\n";

    // A compile reference that Microsoft.NET.Test.Sdk brings (through Microsoft.TestPlatform.TestHost).
    private static MetadataReference TestSdk { get; } =
        MetadataReference.CreateFromFile(Path.Combine(AppContext.BaseDirectory, "Microsoft.TestPlatform.CoreUtilities.dll"));

    // Council round 18, X4: xUnit v2, Microsoft.NET.Test.Sdk and a [Fact].
    internal static Project CreateTestProject(string[] symbols) => CreateProject(symbols, [.. XunitV3References.Before, TestSdk], TestCode);

    internal static Project CreateProject(string[] symbols, IEnumerable<MetadataReference> references, string? code = null)
    {
        var projectId = ProjectId.CreateNewId();
        Solution solution = TestSolution.NewEmpty()
            .AddProject(projectId, "P", "P", LanguageNames.CSharp)
            .WithProjectParseOptions(projectId, new CSharpParseOptions(preprocessorSymbols: symbols))
            .AddMetadataReferences(projectId, references);
        if (code is not null)
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(projectId), "T.cs", code);
        }

        return solution.GetProject(projectId)!;
    }
}
