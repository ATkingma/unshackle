using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Engine;

// TAAK-316, council round 8 S2: a report-only fix takes inventory and changes nothing.
public class ReportOnlyFixTests
{
    private const string ProjectPath = "/repo/Tests/Tests.csproj";
    private const string Code = "using FluentAssertions;\nclass C\n{\n    void M() => 1.Should();\n}";
    private const string Reason = "No free target ships yet.";

    private readonly IProjectFileStore _projectFiles = Substitute.For<IProjectFileStore>();

    [Fact]
    public async Task FixAsync_ReportOnlyFixWithRewritingPattern_ChangesNoCodeAndReportsManual()
    {
        // Arrange
        IFix fix = CreateReportOnlyFix(CreatePattern(PatternResult.Fixed(SyntaxFactory.ParseExpression("System.Console.WriteLine()"))));
        var fixer = new Fixer([fix], _projectFiles);
        Solution solution = CreateSolution();

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        report.Solution.ShouldBeSameAs(solution);
        report.InventoryOnly.ShouldBeTrue();
        report.Findings.ShouldNotContain(f => f.Result is FixedResult);
        Finding row = report.Findings.Single(f => f.PatternId == "test.pattern");
        row.Line.ShouldBe(4);
        ((ManualResult)row.Result).Reason.ShouldStartWith(Fixer.NotRewrittenPrefix);
    }

    [Fact]
    public async Task FixAsync_ReportOnlyFixWithPackage_TouchesNoProjectFile()
    {
        // Arrange
        IFix fix = CreateReportOnlyFix(CreatePattern(PatternResult.Manual("Consumer found.")));
        fix.Package.Returns(new PackageMigration { RemovedPackage = "FluentAssertions", AddedPackages = [new() { Name = "Shouldly", Version = "4.3.0" }] });
        var fixer = new Fixer([fix], _projectFiles);

        // Act
        FixReport report = await fixer.FixAsync(CreateSolution(), new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        await _projectFiles.DidNotReceive().EditAsync(Arg.Any<IEnumerable<ProjectMigration>>(), Arg.Any<CancellationToken>());
        report.ProjectFileChanges.ShouldBeEmpty();
        report.Findings.ShouldNotContain(f => f.PatternId == Fixer.ProjectFilePatternId);
    }

    [Fact]
    public async Task FixAsync_ReportOnlyFix_FirstRowIsInventoryOnlyHeader()
    {
        // Arrange
        IFix fix = CreateReportOnlyFix(CreatePattern(PatternResult.Note("Bus registration found.")));
        var fixer = new Fixer([fix], _projectFiles);

        // Act
        FixReport report = await fixer.FixAsync(CreateSolution(), new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        Finding header = report.Findings[0];
        header.PatternId.ShouldBe(Fixer.InventoryOnlyPatternId);
        header.Line.ShouldBe(0);
        string note = header.Result.ShouldBeOfType<InfoResult>().Text;
        note.ShouldStartWith("Inventory only");
        note.ShouldEndWith(Reason);
        report.Findings.Single(f => f.PatternId == "test.pattern").Result.ShouldBe(PatternResult.Note("Bus registration found."));
    }

    [Fact]
    public async Task FixAsync_PatternThrows_ReportsInternalErrorAsManual()
    {
        // Arrange
        IPattern pattern = CreatePattern(PatternResult.Note("unused"));
        pattern.Rewrite(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>()).Returns(_ => throw new InvalidOperationException("boom"));
        var fixer = new Fixer([CreateReportOnlyFix(pattern)], _projectFiles);

        // Act
        FixReport report = await fixer.FixAsync(CreateSolution(), new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        ((ManualResult)report.Findings.Single(f => f.PatternId == "test.pattern").Result).Reason.ShouldContain("boom");
    }

    [Fact]
    public async Task FixAsync_OrdinaryFix_HasNoInventoryHeader()
    {
        // Arrange
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("test");
        IPattern pattern = CreatePattern(PatternResult.Manual("x"));
        fix.Patterns.Returns([pattern]);
        fix.RemovedNamespaces.Returns(["FluentAssertions"]);
        fix.AddedNamespaces.Returns(["Shouldly"]);
        var fixer = new Fixer([fix]);

        // Act
        FixReport report = await fixer.FixAsync(CreateSolution(), new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        report.InventoryOnly.ShouldBeFalse();
        report.Findings.ShouldNotContain(f => f.PatternId == Fixer.InventoryOnlyPatternId);
    }

    [Fact]
    public async Task FixAsync_OrdinaryFixPatternReturnsInfo_KeepsCodeAndReportsInfo()
    {
        // Arrange
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("test");
        IPattern pattern = CreatePattern(PatternResult.Note("note"));
        fix.Patterns.Returns([pattern]);
        fix.RemovedNamespaces.Returns(["FluentAssertions"]);
        fix.AddedNamespaces.Returns(["Shouldly"]);
        var fixer = new Fixer([fix]);
        Solution solution = CreateSolution();

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.Single(f => f.PatternId == "test.pattern").Result.ShouldBeOfType<InfoResult>();
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldBe(Code);
    }

    private static IFix CreateReportOnlyFix(IPattern pattern)
    {
        IFix fix = Substitute.For<IFix, IReportOnlyFix>();
        fix.Id.Returns("test");
        fix.Patterns.Returns([pattern]);
        fix.RemovedNamespaces.Returns(["FluentAssertions"]);
        fix.AddedNamespaces.Returns(["Shouldly"]);
        ((IReportOnlyFix)fix).ReportOnlyReason.Returns(Reason);
        return fix;
    }

    private static IPattern CreatePattern(PatternResult result)
    {
        IPattern pattern = Substitute.For<IPattern>();
        pattern.Id.Returns("test.pattern");
        pattern.Find(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>())
            .Returns(call => call.Arg<SyntaxNode>().DescendantNodes().OfType<InvocationExpressionSyntax>());
        pattern.Rewrite(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>()).Returns(result);
        return pattern;
    }

    private static Solution CreateSolution()
    {
        var projectId = ProjectId.CreateNewId();
        return new AdhocWorkspace().CurrentSolution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "Tests", "Tests", LanguageNames.CSharp, filePath: ProjectPath)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithMetadataReferences(TestReferences.FluentAssertions))
            .AddDocument(DocumentId.CreateNewId(projectId), "C.cs", Code, filePath: "/repo/Tests/C.cs");
    }
}
