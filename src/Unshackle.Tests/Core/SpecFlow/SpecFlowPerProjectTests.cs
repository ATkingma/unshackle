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

// TAAK-353, council round 18 S2: a unit (a project plus the SpecFlow projects it references) with a blocking row stays whole on
// SpecFlow; the other units move to Reqnroll.
public class SpecFlowPerProjectTests
{
    private const string Clean = """
        using TechTalk.SpecFlow;

        [Binding]
        public class CleanSteps
        {
            [Given("I am logged in")]
            public void GivenIAmLoggedIn() { }
        }
        """;

    private const string Blocked = """
        using TechTalk.SpecFlow;

        [Binding]
        public class BlockedSteps : Steps
        {
            [Given("I am on the basket page")]
            public void GivenBasket() => Given("I open the basket");
        }
        """;

    private const string Bindings = """
        using TechTalk.SpecFlow;

        [Binding]
        public class SharedSteps
        {
            [Given("I open the basket")]
            public void GivenIOpenTheBasket() { }
        }
        """;

    [Fact]
    public async Task FixAsync_OneCleanOneBlockedUnit_CleanMigratesBlockedUnitStaysWhole()
    {
        // Arrange: Blocked references its binding library, so the library stays with it.
        Solution solution = Create(("Bindings", [], Bindings), ("Blocked", ["Bindings"], Blocked), ("Clean", [], Clean));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report, "Clean")).ShouldStartWith("using Reqnroll;");
        (await TextAsync(report, "Blocked")).ShouldBe(Blocked);
        (await TextAsync(report, "Bindings")).ShouldBe(Bindings);
        List<Finding> kept = [.. report.Findings.Where(f => f.PatternId == SpecFlowProjects.KeptPatternId).OrderBy(f => f.FilePath, StringComparer.Ordinal)];
        kept.Select(f => f.FilePath).ShouldBe(["Bindings.csproj", "Blocked.csproj"]);
        ManualResult manual = kept[0].Result.ShouldBeOfType<ManualResult>();
        manual.Kept.ShouldBe(KeptReason.SpecFlowProjectKept);
        manual.Reason.ShouldContain("Blocked.csproj references it");
        KeptReasonText.Rendered(manual).ShouldStartWith(KeptReasonText.SpecFlowPrefix);
        report.BlockingProjects.ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_CleanProjectUsesBindingsOfBlockedUnit_StaysToo()
    {
        // Arrange: a binding library shared by a blocked and a clean project keeps both on SpecFlow; nothing is left to move.
        Solution solution = Create(("Bindings", [], Bindings), ("Blocked", ["Bindings"], Blocked), ("Clean", ["Bindings"], Clean));

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report, "Clean")).ShouldBe(Clean);
        report.Findings.ShouldNotContain(f => f.Result is FixedResult);
        report.BlockingProjects.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task FixAsync_DependentReferencesKeptAndCleanUnit_CleanStaysWithRootBlocker()
    {
        // Arrange (council round 25, S17): Helper (no SpecFlow) references Blocked and Clean; Other is independent and still moves.
        Solution solution = Create(("Blocked", [], Blocked), ("Clean", [], Clean), ("Other", [], Clean.Replace("CleanSteps", "OtherSteps", StringComparison.Ordinal)),
            ("Helper", ["Blocked", "Clean"], "public static class Helper { }\n"));
        ProjectId helper = solution.Projects.Single(p => p.Name == "Helper").Id;
        solution = solution.WithProjectMetadataReferences(helper, []);

        // Act
        FixReport report = await FixAsync(solution);

        // Assert
        (await TextAsync(report, "Other")).ShouldStartWith("using Reqnroll;");
        (await TextAsync(report, "Clean")).ShouldBe(Clean);
        Finding clean = report.Findings.Single(f => f.PatternId == SpecFlowProjects.KeptPatternId && f.FilePath == "Clean.csproj");
        string reason = clean.Result.ShouldBeOfType<ManualResult>().Reason;
        reason.ShouldContain("Helper.csproj references both it and Blocked.csproj");
        reason.ShouldContain("root blocker: Blocked.cs:");
        report.Findings.Single(f => f.PatternId == SpecFlowProjects.ClosurePatternId).Result.ShouldBeOfType<InfoResult>().Text
            .ShouldStartWith("1 SpecFlow project(s) stay on SpecFlow only because");
    }

    private static async Task<FixReport> FixAsync(Solution solution) =>
        await new Fixer([new SpecFlowFix(Substitute.For<IProjectProperties>())]).FixAsync(solution, new FixRequest { FixId = "specflow" }, TestContext.Current.CancellationToken);

    private static async Task<string> TextAsync(FixReport report, string project) =>
        (await report.Solution.Projects.Single(p => p.Name == project).Documents.Single().GetTextAsync(TestContext.Current.CancellationToken)).ToString();

    // Projects with a file path (the plan groups by it), each with one document.
    private static Solution Create(params (string Name, string[] References, string Code)[] projects)
    {
        Solution solution = TestSolution.NewEmpty();
        Dictionary<string, ProjectId> ids = new(StringComparer.Ordinal);
        foreach ((string name, string[] references, string code) in projects)
        {
            var id = ProjectId.CreateNewId();
            ids[name] = id;
            solution = solution
                .AddProject(ProjectInfo.Create(id, VersionStamp.Default, name, name, LanguageNames.CSharp, filePath: name + ".csproj"))
                .WithProjectCompilationOptions(id, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(id, SpecFlowReferences.SpecFlow)
                .AddProjectReferences(id, references.Select(r => new ProjectReference(ids[r])))
                .AddDocument(DocumentId.CreateNewId(id), name + ".cs", code, filePath: name + ".cs");
        }

        return solution;
    }
}
