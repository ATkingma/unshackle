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
    public async Task FixAsync_AllOrNothingWithAManualFinding_KeepsSolutionAndReportsEveryFindingManual()
    {
        // Arrange
        IPattern pattern = CreatePattern(node => node.ToString() == "Foo()" ? PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")) : PatternResult.Manual("by hand"));
        IFix fix = CreateFix(pattern);
        fix.AllOrNothing.Returns(true);
        var fixer = new Fixer([fix]);
        Solution solution = TestSolution.Create(Code.Replace("Foo();", "Foo(); Bar();", StringComparison.Ordinal), TestReferences.Net);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        report.Solution.ShouldBeSameAs(solution);
        report.ProjectFileChanges.ShouldBeEmpty();
        report.Findings.Count.ShouldBe(2);
        report.Findings.ShouldContain(f => f.Result.Equals(PatternResult.Manual("by hand")));
        report.Findings.ShouldContain(f => f.Result is ManualResult && ((ManualResult)f.Result).Reason.StartsWith("Not rewritten", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FixAsync_AllOrNothingWithoutManualFindings_Rewrites()
    {
        // Arrange
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        IFix fix = CreateFix(pattern);
        fix.AllOrNothing.Returns(true);
        var fixer = new Fixer([fix]);

        // Act
        FixReport report = await FixAsync(fixer, Code);

        // Assert
        (await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken)).ShouldNotContain("Foo();");
        report.Findings.ShouldHaveSingleItem().Result.ShouldBeOfType<FixedResult>();
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
        FixReport report = await FixAsync(fixer, Code.Replace("Foo();", "new StringBuilder().Append(1);", StringComparison.Ordinal));

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldStartWith("using System.IO;\nusing System.Linq;\n\npublic class C");
    }

    [Fact]
    public async Task FixAsync_FileNamesNothingOfRemovedNamespace_AddsNoNamespace()
    {
        // Arrange
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        var fixer = new Fixer([CreateFix(pattern, removed: ["System.Text"], added: ["System.IO"])]);

        // Act
        FixReport report = await FixAsync(fixer, Code.Replace("using System.Text;", "using System;", StringComparison.Ordinal));

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldStartWith("using System;\n\npublic class C", customMessage: "the file uses the old library only through its own types (IDE0005)");
    }

    [Fact]
    public async Task FixAsync_UsingsInsideNamespace_AddsNewOneThere()
    {
        // Arrange
        const string code = """
            namespace N;

            using System.Collections.Generic;

            public class C
            {
                public void M()
                {
                    new System.Text.StringBuilder().Append(1);
                }

                private static void Bar()
                {
                }
            }

            """;
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        var fixer = new Fixer([CreateFix(pattern, removed: ["System.Text"], added: ["System.IO"])]);

        // Act
        FixReport report = await FixAsync(fixer, code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldStartWith("namespace N;\n\nusing System.Collections.Generic;\nusing System.IO;\n\npublic class C", customMessage: "IDE0065");
    }

    [Fact]
    public async Task FixAsync_UsingsInsideNamespaceThatShadowsAddedOne_AddsNewOneOnTop()
    {
        // Arrange
        const string code = """
            namespace N
            {
                using System.Collections.Generic;

                public class C
                {
                    public void M()
                    {
                        new System.Text.StringBuilder().Append(1);
                    }

                    private static void Bar()
                    {
                    }
                }
            }

            namespace N.Other
            {
                public class X
                {
                }
            }

            """;
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        var fixer = new Fixer([CreateFix(pattern, removed: ["System.Text"], added: ["Other"])]);

        // Act
        FixReport report = await FixAsync(fixer, code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldStartWith("using Other;\n\nnamespace N\n{\n    using System.Collections.Generic;\n\n", customMessage: "inside N, Other is N.Other (TAAK-131)");
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
    public async Task FixAsync_TwoKeptGlobalUsings_AddsNewOneOnce()
    {
        // Arrange
        string code = "global using System.Collections;\nglobal " + Code.Replace("Foo();", "Foo(); _ = new StringBuilder();", StringComparison.Ordinal);
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        var fixer = new Fixer([CreateFix(pattern, removed: ["System.Collections", "System.Text"], added: ["System.IO"])]);

        // Act
        FixReport report = await FixAsync(fixer, code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldStartWith("global using System.Collections;\nglobal using System.IO;\nglobal using System.Text;\n\npublic class C");
    }

    [Fact]
    public async Task FixAsync_AddedGlobalUsingAlreadyThere_DoesNotAddItAgain()
    {
        // Arrange
        string code = "global using System.IO;\nglobal " + Code;
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        var fixer = new Fixer([CreateFix(pattern, removed: ["System.Text"], added: ["System.IO"])]);

        // Act
        FixReport report = await FixAsync(fixer, code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldStartWith("global using System.IO;\n\npublic class C");
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
    public async Task FixAsync_InactiveIfBranchWithoutRemovedNames_RemovesUsingAndReportsNoManual()
    {
        // Arrange: like jason's `#if (UseApiOnly)` blocks: inactive code that names nothing of the removed namespace.
        string code = Code.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("Foo();", "Foo();\n#if NEVER\n        var sb = new Other();\n#endif", StringComparison.Ordinal);
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        var fixer = new Fixer([CreateFix(pattern, removed: ["System.Text"], added: ["System.IO"])]);

        // Act
        FixReport report = await FixAsync(fixer, code);

        // Assert
        string text = await TestSolution.GetTextAsync(report.Solution, TestContext.Current.CancellationToken);
        text.ShouldNotContain("using");
        report.Findings.ShouldNotContain(f => f.PatternId == Fixer.UsingsPatternId && f.Result is ManualResult);
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

    [Fact]
    public async Task FixAsync_CodeOnlyActiveInLaterTargetFramework_IsFixedOnceInEveryBranch()
    {
        // Arrange: net462 defines NETFRAMEWORK, net8.0 does not; Foo() in the shared code and in both branches (TAAK-223).
        const string code = """
            public class C
            {
                public void M()
                {
                    Foo();
            #if NETFRAMEWORK
                    Foo();
            #else
                    Foo();
                    Foo();
            #endif
                }

                private static void Foo()
                {
                }

                private static void Bar()
                {
                }
            }

            """;
        IPattern pattern = CreatePattern(_ => PatternResult.Fixed(SyntaxFactory.ParseExpression("Bar()")));
        pattern.Find(Arg.Any<SyntaxNode>(), Arg.Any<SemanticModel>())
            .Returns(call => call.Arg<SyntaxNode>().DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => i.Expression.ToString() == "Foo"));
        var fixer = new Fixer([CreateFix(pattern)]);
        Solution solution = CreateMultiTargetSolution(code);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.Count.ShouldBe(4);
        report.Findings.ShouldAllBe(f => f.Result is FixedResult);
        foreach (Document document in report.Solution.Projects.SelectMany(p => p.Documents))
        {
            string text = (await document.GetTextAsync(TestContext.Current.CancellationToken)).ToString();
            text.ShouldNotContain("Foo();");
            (await document.GetSemanticModelAsync(TestContext.Current.CancellationToken))!.GetDiagnostics(cancellationToken: TestContext.Current.CancellationToken).ShouldNotContain(d => d.Severity == DiagnosticSeverity.Error);
        }
    }

    [Fact]
    public async Task FixAsync_ManualInSharedCode_IsReportedOnceAcrossTargetFrameworks()
    {
        // Arrange
        const string code = """
            public class C
            {
                public void M()
                {
                    Foo();
            #if !NETFRAMEWORK
                    Foo();
            #endif
                }

                private static void Foo()
                {
                }
            }

            """;
        var fixer = new Fixer([CreateFix(CreatePattern(_ => PatternResult.Manual("by hand")))]);
        Solution solution = CreateMultiTargetSolution(code);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.Select(f => f.Line).Order().ShouldBe([5, 7]);
    }

    // A multi-target project: the net462 copy (NETFRAMEWORK) first, like MSBuildWorkspace loads them.
    private static Solution CreateMultiTargetSolution(string code)
    {
        Solution solution = new AdhocWorkspace().CurrentSolution;
        foreach ((string name, string[] symbols) in new[] { ("Test(net462)", new[] { "NETFRAMEWORK" }), ("Test(net8.0)", Array.Empty<string>()) })
        {
            var projectId = ProjectId.CreateNewId();
            solution = solution
                .AddProject(projectId, name, "Test", LanguageNames.CSharp)
                .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithProjectParseOptions(projectId, new CSharpParseOptions(preprocessorSymbols: symbols))
                .AddMetadataReferences(projectId, TestReferences.Net)
                .AddDocument(DocumentId.CreateNewId(projectId), TestSolution.FileName, code, filePath: TestSolution.FileName);
        }

        return solution;
    }

    private static Task<FixReport> FixAsync(Fixer fixer, string code) =>
        fixer.FixAsync(TestSolution.Create(code, TestReferences.Net), new FixRequest { FixId = "test" }, TestContext.Current.CancellationToken);

    // A pattern that finds every invocation and rewrites it with the given function.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FixAsync_MarkerInterfaceInOtherFile_FindsHandlerWhateverTheFileOrder(bool markerFirst)
    {
        // Arrange
        const string marker = """
            using MediatR;

            public interface IAppEvent : INotification;

            public interface IAppEventHandler<in TEvent> : INotificationHandler<TEvent>
                where TEvent : IAppEvent;

            """;
        const string handler = """
            using System.Threading;
            using System.Threading.Tasks;

            public record OrderPlaced(int Id) : IAppEvent;

            public class OrderPlacedHandler : IAppEventHandler<OrderPlaced>
            {
                public async Task Handle(OrderPlaced notification, CancellationToken cancellationToken)
                {
                    await Task.Delay(notification.Id, cancellationToken);
                }
            }

            """;
        var projectId = ProjectId.CreateNewId();
        Solution solution = new AdhocWorkspace().CurrentSolution
            .AddProject(projectId, "App", "App", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddMetadataReferences(projectId, TestReferences.MediatR);
        // The Fixer walks the documents in solution order (alphabetical in a real project): the marker file first or last.
        (string Name, string Code)[] files = markerFirst ? [("A.cs", marker), ("M.cs", handler)] : [("M.cs", handler), ("Z.cs", marker)];
        foreach ((string name, string code) in files)
        {
            solution = solution.AddDocument(DocumentId.CreateNewId(projectId), name, code, filePath: name);
        }

        var fixer = new Fixer([new Unshackle.Core.Fixes.MediatR.MediatRFix()]);

        // Act
        FixReport report = await fixer.FixAsync(solution, new FixRequest { FixId = "mediatr" }, TestContext.Current.CancellationToken);

        // Assert
        report.Findings.ShouldContain(f => f.FilePath == "M.cs" && f.Result is FixedResult);
        Document fixedHandler = report.Solution.Projects.Single().Documents.Single(d => d.FilePath == "M.cs");
        (await fixedHandler.GetTextAsync(TestContext.Current.CancellationToken)).ToString().ShouldContain("ValueTask Handle");
    }

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
