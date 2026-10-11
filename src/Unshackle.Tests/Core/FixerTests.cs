using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Unshackle.Core;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// Test names: Method_State_Expected (provisional, see TAAK-001 Keuzes).
public class FixerTests
{
    private const string Code = """
        using System.Text;

        public class C
        {
            public void M()
            {
                Foo();
            }

            private static void Foo()
            {
            }

            private static void Bar()
            {
            }
        }

        """;

    [Fact]
    public async Task FixAsync_PatternReturnsFixed_ReplacesNodeAndReportsFixed()
    {
        // Arrange
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        var fixer = new Fixer([CreateFix(pattern)]);

        // Act
        FixReport report = await FixAsync(fixer, Code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldContain("        Bar();\n");
        text.ShouldNotContain("Foo();");
        Finding finding = report.Findings.ShouldHaveSingleItem();
        finding.Result.ShouldBeOfType<FixedResult>();
        finding.Line.ShouldBe(7);
        finding.FilePath.ShouldBe(TestSolution.FileName);
        finding.FixId.ShouldBe("test");
        finding.PatternId.ShouldBe("test.pattern");
    }

    [Fact]
    public async Task FixAsync_PatternReturnsManual_KeepsDocumentAndReportsReason()
    {
        // Arrange
        IPattern pattern = CreatePattern(_ => PatternResult.Manual("by hand"));
        var fixer = new Fixer([CreateFix(pattern)]);
        Solution solution = TestSolution.Create(Code, TestReferences.Net);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        report.Solution.ShouldBeSameAs(solution);
        report.Findings.ShouldHaveSingleItem().Result.ShouldBe(PatternResult.Manual("by hand"));
    }

    [Fact]
    public async Task FixAsync_RewriteThrows_ReportsInternalErrorAsManual()
    {
        // Arrange
        IPattern pattern = CreatePattern(_ => throw new InvalidOperationException("boom"));
        var fixer = new Fixer([CreateFix(pattern)]);

        // Act
        FixReport report = await FixAsync(fixer, Code);

        // Assert
        ManualResult result = report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<ManualResult>();
        result.Reason.ShouldBe("Internal error in test.pattern: boom");
    }

    [Fact]
    public async Task FixAsync_FindThrows_ReportsInternalErrorForWholeFile()
    {
        // Arrange
        IPattern pattern = Substitute.For<IPattern>();
        pattern.Id.Returns("test.pattern");
        pattern.Find(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>()).Throws(new InvalidOperationException("boom"));
        var fixer = new Fixer([CreateFix(pattern)]);

        // Act
        FixReport report = await FixAsync(fixer, Code);

        // Assert
        Finding finding = report.Findings.ShouldHaveSingleItem();
        finding.Line.ShouldBe(0);
        finding.Result.ShouldBe(PatternResult.Manual("Internal error in test.pattern: boom"));
    }

    [Fact]
    public async Task FixAsync_PatternReturnsSeveralNodes_ReplacesStatementWithAll()
    {
        // Arrange
        IPattern pattern = Substitute.For<IPattern>();
        pattern.Id.Returns("test.pattern");
        pattern.Find(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>())
            .Returns(call => call.Arg<SyntaxNode>().DescendantNodes().OfType<ExpressionStatementSyntax>());
        pattern.Rewrite(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>())
            .Returns(PatternResult.Fixed([SyntaxFactory.ParseStatement("Bar();"), SyntaxFactory.ParseStatement("Foo();")]));
        var fixer = new Fixer([CreateFix(pattern)]);

        // Act
        FixReport report = await FixAsync(fixer, Code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldContain("    {\n        Bar();\n        Foo();\n    }");
    }

    [Fact]
    public async Task FixAsync_NamespaceNoLongerUsed_SwapsUsingAndAddsRequiredNamespace()
    {
        // Arrange
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()"), "System.Linq"));
        var fixer = new Fixer([CreateFix(pattern, removed: ["System.Text"], added: ["System.IO"])]);

        // Act
        FixReport report = await FixAsync(fixer, Code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldStartWith("using System.IO;\nusing System.Linq;\n\npublic class C");
    }

    [Fact]
    public async Task FixAsync_FileWithoutFindingsHasUnusedRemovedUsing_RemovesItWithoutAddingNamespaces()
    {
        // Arrange
        IPattern pattern = Substitute.For<IPattern>();
        pattern.Id.Returns("test.pattern");
        pattern.Find(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>()).Returns([]);
        var fixer = new Fixer([CreateFix(pattern, removed: ["System.Text"], added: ["System.IO"])]);

        // Act
        FixReport report = await FixAsync(fixer, Code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldStartWith("\npublic class C");
        report.Findings.ShouldBeEmpty();
    }

    [Fact]
    public async Task FixAsync_NamespaceStillUsed_KeepsUsing()
    {
        // Arrange
        string code = Code.Replace("Foo();", "Foo(); _ = new StringBuilder();", StringComparison.Ordinal);
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        var fixer = new Fixer([CreateFix(pattern, removed: ["System.Text"], added: ["System.IO"])]);

        // Act
        FixReport report = await FixAsync(fixer, code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldStartWith("using System.Text;\nusing System.IO;\n\npublic class C");
    }

    [Fact]
    public async Task FixAsync_GlobalUsingNoLongerUsed_SwapsItWithoutFileUsing()
    {
        // Arrange
        string code = "global " + Code;
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        var fixer = new Fixer([CreateFix(pattern, removed: ["System.Text"], added: ["System.IO"])]);

        // Act
        FixReport report = await FixAsync(fixer, code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldStartWith("global using System.IO;\n\npublic class C");
        report.Findings.ShouldAllBe(f => f.Result is FixedResult);
    }

    [Fact]
    public async Task FixAsync_GlobalUsingStillUsed_KeepsItNextToNewOneAndReportsManual()
    {
        // Arrange
        string code = "global " + Code.Replace("Foo();", "Foo(); _ = new StringBuilder();", StringComparison.Ordinal);
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        var fixer = new Fixer([CreateFix(pattern, removed: ["System.Text"], added: ["System.IO"])]);

        // Act
        FixReport report = await FixAsync(fixer, code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldStartWith("global using System.Text;\nglobal using System.IO;\n\npublic class C");
        report.Findings.ShouldContain(f => f.PatternId == Fixer.UsingsPatternId && f.Result is ManualResult);
    }

    [Fact]
    public async Task FixAsync_InactiveIfBranch_KeepsRemovedUsingAndReportsManual()
    {
        // Arrange
        string code = Code.Replace("Foo();", "Foo();\n#if NEVER\n        _ = new StringBuilder();\n#endif", StringComparison.Ordinal);
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        var fixer = new Fixer([CreateFix(pattern, removed: ["System.Text"], added: ["System.IO"])]);

        // Act
        FixReport report = await FixAsync(fixer, code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldStartWith("using System.Text;\nusing System.IO;\n\npublic class C");
        Finding manual = report.Findings.Where(f => f.PatternId == Fixer.UsingsPatternId).ShouldHaveSingleItem();
        manual.Result.ShouldBeOfType<ManualResult>();
        manual.Line.ShouldBe(9);
    }

    [Fact]
    public async Task FixAsync_UnknownFixId_ThrowsArgumentException()
    {
        // Arrange
        var fixer = new Fixer([CreateFix(CreatePattern(_ => PatternResult.Manual("unused")))]);
        Solution solution = TestSolution.Create(Code, TestReferences.Net);

        // Act
        Task act() => fixer.FixAsync(solution, new FixRequest { FixId = "unknown" }, TestContext.Current.CancellationToken);

        // Assert
        await Should.ThrowAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task FixAsync_SameFileInTwoTargetFrameworks_ReportsOnceAndFixesBothCopies()
    {
        // Arrange: a multi-target project is loaded as one project per target framework.
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        var fixer = new Fixer([CreateFix(pattern)]);
        Solution solution = TestSolution.Create(Code, TestReferences.Net);
        var secondId = ProjectId.CreateNewId();
        solution = solution
            .AddProject(secondId, "Test(net8.0)", "Test", LanguageNames.CSharp)
            .AddMetadataReferences(secondId, TestReferences.Net)
            .AddDocument(DocumentId.CreateNewId(secondId), TestSolution.FileName, Code, filePath: TestSolution.FileName);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<FixedResult>();
        foreach (Document document in report.Solution.Projects.SelectMany(p => p.Documents))
        {
            (await document.GetTextAsync(TestContext.Current.CancellationToken)).ToString().ShouldContain("Bar();");
        }
    }

    private static Task<FixReport> FixAsync(Fixer fixer, string code) =>
        fixer.FixAsync(TestSolution.Create(code, TestReferences.Net), new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

    // A pattern that finds every invocation and rewrites it with the given function.
    private static IPattern CreatePattern(Func<SyntaxNode, PatternResult> rewrite)
    {
        IPattern pattern = Substitute.For<IPattern>();
        pattern.Id.Returns("test.pattern");
        pattern.Find(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>())
            .Returns(call => call.Arg<SyntaxNode>().DescendantNodes().OfType<InvocationExpressionSyntax>());
        pattern.Rewrite(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>()).Returns(call => rewrite(call.Arg<SyntaxNode>()));
        return pattern;
    }

    private static IFix CreateFix(IPattern pattern, IReadOnlyList<string>? removed = null, IReadOnlyList<string>? added = null)
    {
        IFix fix = Substitute.For<IFix>();
        fix.Id.Returns("test");
        fix.Patterns.Returns([pattern]);
        fix.RemovedNamespaces.Returns(removed ?? []);
        fix.AddedNamespaces.Returns(added ?? []);
        return fix;
    }
}
