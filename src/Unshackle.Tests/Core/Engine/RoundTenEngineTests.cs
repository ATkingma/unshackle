using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSubstitute;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using Unshackle.DataAccess.Reports;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core.Engine;

// Council round 10 (TAAK-318): reason codes (C1), fix-level Info (C2), no blank first line (C3), the report-only header (G1) and
// project-level blocking rows (G4).
public class RoundTenEngineTests
{
    private const string ProjectPath = "/repo/Specs/Specs.csproj";
    private const string Code = "using FluentAssertions;\nclass C\n{\n    void M() => 1.Should();\n}";

    private enum TestReason
    {
        PolicyNotLocal,
    }

    [Theory]
    [InlineData(KeptReason.None, "PolicyNotLocal: the policy is a field.")]
    [InlineData(KeptReason.MediatRNextToMediator, KeptReasonText.MediatRPrefix + "PolicyNotLocal: the policy is a field.")]
    public void ReasonOf_ManualWithCode_RendersAsTheOldTextPrefix(KeptReason kept, string expected)
    {
        // Arrange
        ManualResult manual = (ManualResult)PatternResult.Manual(TestReason.PolicyNotLocal, "the policy is a field.") with { Kept = kept };

        // Act
        string rendered = ReportRow.ReasonOf(manual);

        // Assert
        manual.Code.ShouldBe("PolicyNotLocal");
        manual.Reason.ShouldBe("the policy is a field.");
        rendered.ShouldBe(expected);
    }

    [Fact]
    public void ReasonOf_ManualWithoutCode_IsUnchanged()
    {
        // Act
        string rendered = ReportRow.ReasonOf((ManualResult)PatternResult.Manual("PolicyNotLocal: as text."));

        // Assert
        rendered.ShouldBe("PolicyNotLocal: as text.");
    }

    [Fact]
    public async Task FixAsync_FixWithProjectInfo_ReportsOneInfoRowPerProjectFile()
    {
        // Arrange
        IFix fix = CreateFix(PatternResult.Note("ok"));
        fix.ProjectInfo(Arg.Any<Project>()).Returns(["Polly 7.2.4 found."]);

        // Act
        FixReport report = await new Fixer([fix]).FixAsync(CreateSolution(), new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        Finding row = report.Findings.Single(f => f.Result is InfoResult { Text: "Polly 7.2.4 found." });
        row.FilePath.ShouldBe(ProjectPath);
        row.PatternId.ShouldBe(Fixer.ProjectFilePatternId);
        ((InfoResult)row.Result).Kind.ShouldBe(InfoKind.Note);
    }

    [Fact]
    public async Task FixAsync_ReportOnlyFix_HeaderRowIsLeftOutOfEveryCount()
    {
        // Arrange
        IPattern pattern = CreatePattern(PatternResult.Manual("Consumer found."));
        IFix fix = Substitute.For<IFix, IReportOnlyFix>();
        fix.Id.Returns("test");
        fix.Patterns.Returns([pattern]);
        fix.RemovedNamespaces.Returns(["FluentAssertions"]);
        ((IReportOnlyFix)fix).ReportOnlyReason.Returns("No target yet.");

        // Act
        FixReport report = await new Fixer([fix]).FixAsync(CreateSolution(), new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        Finding header = report.Findings[0];
        header.Result.ShouldBeOfType<InfoResult>().Kind.ShouldBe(InfoKind.Header);
        FixReport.IsCounted(header).ShouldBeFalse();
        report.CountedFindings.ShouldBe(report.Findings.Skip(1));
        report.CountedFindings.Count(f => f.Result is ManualResult).ShouldBe(1);
    }

    [Theory]
    [InlineData("using System.Text;\n\npublic class C { }\n", "public class C { }\n")]
    [InlineData("using System.Text;\n\n\npublic class C { }\n", "\npublic class C { }\n")]
    [InlineData("using System.Text;\n\n// header\npublic class C { }\n", "// header\npublic class C { }\n")]
    // A using with a comment in front is not on the first line itself: as before, nothing extra goes.
    [InlineData("// header\nusing System.Text;\n\npublic class C { }\n", "\npublic class C { }\n")]
    public async Task FixAsync_RemovedFirstLineUsing_RemovesOnlyTheBlankLineItLeft(string code, string expected)
    {
        // Arrange
        IPattern pattern = Substitute.For<IPattern>();
        pattern.Id.Returns("test.pattern");
        pattern.Find(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>()).Returns([]);
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("test");
        fix.Patterns.Returns([pattern]);
        fix.RemovedNamespaces.Returns(["System.Text"]);
        fix.AddedNamespaces.Returns([]);
        fix.FindsOnlyOldLibraryCode.Returns(false);
        var fixer = new Fixer([fix]);

        // Act
        FixReport first = await fixer.FixAsync(TestSolution.Create(code, TestReferences.FluentAssertions), new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);
        string text = await TestSolution.GetTextAsync(first.Solution, TestContext.Current.CancellationToken);
        FixReport second = await fixer.FixAsync(TestSolution.Create(text, TestReferences.FluentAssertions), new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        text.ShouldBe(expected);
        (await TestSolution.GetTextAsync(second.Solution, TestContext.Current.CancellationToken)).ShouldBe(expected);
    }

    [Theory]
    [InlineData(ProjectBlockingReason.TargetFrameworkUnsupported, true)]
    [InlineData(ProjectBlockingReason.LicensedDependency, true)]
    [InlineData(null, false)]
    public async Task FixAsync_ProjectBlocker_BlocksFileEditsOnlyWithABlockingCode(ProjectBlockingReason? code, bool blocks)
    {
        // Arrange
        IProjectFileStore store = Substitute.For<IProjectFileStore, IProjectFileEditor>();
        var edit = new PackageRenameEdit { OldName = "SpecFlow", NewName = "Reqnroll", Version = "2.4.1" };
        IFix fix = CreateFix(PatternResult.Note("ok"));
        ((IProjectFileMigration)fix).EditsFor(Arg.Any<Project>()).Returns([edit]);
        ManualResult blocker = code is ProjectBlockingReason reason
            ? (ManualResult)PatternResult.Manual(reason, "net45 is not supported.")
            : (ManualResult)PatternResult.Manual("SpecFlow is kept.");
        ((IProjectFileMigration)fix).ProjectBlockers(Arg.Any<Project>()).Returns([blocker]);
        ((IProjectFileEditor)store).ApplyAsync(ProjectPath, Arg.Any<IEnumerable<ProjectFileEdit>>(), Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<ProjectFileEditContext>(), Arg.Any<CancellationToken>())
            .Returns([new ProjectFileEditOutcome { Edit = edit, FilePath = ProjectPath, Line = 3 }]);

        // Act
        FixReport report = await new Fixer([fix], store).FixAsync(CreateSolution(), new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldContain(f => f.FilePath == ProjectPath && f.Result == blocker);
        await ((IProjectFileEditor)store).Received(blocks ? 0 : 1).ApplyAsync(
            ProjectPath, Arg.Any<IEnumerable<ProjectFileEdit>>(), Arg.Any<IEnumerable<ProjectFileChange>>(), Arg.Any<ProjectFileEditContext>(), Arg.Any<CancellationToken>());
        report.Findings.Any(f => f.Result is ManualResult m && m.Reason.StartsWith("Project-file edits not applied: " + code, StringComparison.Ordinal)).ShouldBe(blocks);
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

    private static IFix CreateFix(PatternResult result)
    {
        IPattern pattern = CreatePattern(result);
        IFix fix = Substitute.For<IFix, IProjectFileMigration>();
        fix.Id.Returns("test");
        fix.Patterns.Returns([pattern]);
        fix.RemovedNamespaces.Returns(["FluentAssertions"]);
        fix.AddedNamespaces.Returns(["Shouldly"]);
        fix.ProjectInfo(Arg.Any<Project>()).Returns([]);
        ((IProjectFileMigration)fix).EditsFor(Arg.Any<Project>()).Returns([]);
        ((IProjectFileMigration)fix).ProjectBlockers(Arg.Any<Project>()).Returns([]);
        return fix;
    }

    private static Solution CreateSolution()
    {
        var projectId = ProjectId.CreateNewId();
        return new AdhocWorkspace().CurrentSolution
            .AddProject(ProjectInfo.Create(projectId, VersionStamp.Default, "Specs", "Specs", LanguageNames.CSharp, filePath: ProjectPath)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithMetadataReferences(TestReferences.FluentAssertions))
            .AddDocument(DocumentId.CreateNewId(projectId), "C.cs", Code, filePath: "/repo/Specs/C.cs");
    }
}
