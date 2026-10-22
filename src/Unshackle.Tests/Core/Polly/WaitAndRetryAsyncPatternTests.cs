using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Fixes.Polly.Patterns;
using Unshackle.Core.Models;
using Xunit;

namespace Unshackle.Tests.Core.Polly;

public class WaitAndRetryAsyncPatternTests
{
    [Fact]
    public void Find_LocalWithTwoExecuteCalls_FindsDeclarationAndBothCalls()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse(
            "var p = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, a => TimeSpan.Zero); await p.ExecuteAsync(() => Task.CompletedTask); await p.ExecuteAsync(() => Task.CompletedTask);");

        // Act
        List<SyntaxNode> found = [.. new WaitAndRetryAsyncPattern().Find(root, model)];

        // Assert
        found.Count.ShouldBe(3);
        found[0].ShouldBeOfType<LocalDeclarationStatementSyntax>();
    }

    [Fact]
    public void Find_InlineExecute_FindsOnlyTheOuterCall()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("await Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, a => TimeSpan.Zero).ExecuteAsync(() => Task.CompletedTask);");

        // Act
        List<SyntaxNode> found = [.. new WaitAndRetryAsyncPattern().Find(root, model)];

        // Assert
        found.ShouldHaveSingleItem().ToString().ShouldEndWith("ExecuteAsync(() => Task.CompletedTask)");
    }

    [Fact]
    public void Find_OwnWaitAndRetryAsync_FindsNothing()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("await Own.WaitAndRetryAsync(2, a => TimeSpan.Zero);");

        // Act
        List<SyntaxNode> found = [.. new WaitAndRetryAsyncPattern().Find(root, model)];

        // Assert
        found.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("var p = Policy.Handle<TimeoutException>().WaitAndRetryAsync(0, a => TimeSpan.Zero); await p.ExecuteAsync(() => Task.CompletedTask);", nameof(PollyReason.RetryCountNotLiteral))]
    [InlineData("var p = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, Delay); await p.ExecuteAsync(() => Task.CompletedTask);", nameof(PollyReason.DelayNotLambda))]
    [InlineData("var p = Policy.HandleInner<TimeoutException>().WaitAndRetryAsync(2, a => TimeSpan.Zero); await p.ExecuteAsync(() => Task.CompletedTask);", nameof(PollyReason.ExceptionFilterNotSupported))]
    [InlineData("var p = Policy.HandleResult<int>(r => r < 0).WaitAndRetryAsync(2, a => TimeSpan.Zero, (o, t) => Keep(o)); await p.ExecuteAsync(() => Task.FromResult(1));", nameof(PollyReason.OnRetryNotMappable))]
    [InlineData("var p = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, (a, ctx) => TimeSpan.Zero, (e, t, ctx) => { }); await p.ExecuteAsync(() => Task.CompletedTask);", nameof(PollyReason.OverloadNotSupported))]
    [InlineData("var p = Policy.HandleResult<int>(1).WaitAndRetryAsync(2, a => TimeSpan.Zero); await p.ExecuteAsync(() => Task.FromResult(1));", nameof(PollyReason.ExceptionFilterNotSupported))]
    [InlineData("var p = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, a => TimeSpan.Zero); await p.ExecuteAsync(ct => Task.CompletedTask, CancellationToken.None);", nameof(PollyReason.ExecuteOverloadNotSupported))]
    [InlineData("var p = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, a => TimeSpan.Zero); await p.ExecuteAsync(Work);", nameof(PollyReason.ExecuteNotLambda))]
    [InlineData("var p = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, a => TimeSpan.Zero); await p.ExecuteAsync(() => Task.CompletedTask); Keep(p);", nameof(PollyReason.PolicyUsedBeyondExecute))]
    public void Rewrite_UnsupportedShape_EveryFindingManualWithCode(string statements, string code)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse(statements);
        var pattern = new WaitAndRetryAsyncPattern();

        // Act
        List<PatternResult> results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(r => r is ManualResult && ((ManualResult)r).Code == code);
    }

    [Fact]
    public void Rewrite_Manual_RendersCodePrefixAsBefore()
    {
        // Arrange (council round 10, C1: the rendered text stays "Code: reason")
        (SyntaxNode root, SemanticModel model) = Parse("var p = Policy.Handle<TimeoutException>().WaitAndRetryAsync(0, a => TimeSpan.Zero); await p.ExecuteAsync(() => Task.CompletedTask);");
        var pattern = new WaitAndRetryAsyncPattern();

        // Act
        var manual = (ManualResult)pattern.Rewrite(pattern.Find(root, model).First(), model);

        // Assert
        KeptReasonText.Rendered(manual).ShouldBe(
            "RetryCountNotLiteral: the retry count '0' is not an integer literal of at least 1 (MaxRetryAttempts must be 1 or more).");
    }

    [Fact]
    public void Rewrite_NameTakenInScope_UsesFreshName()
    {
        // Arrange (council P10)
        (SyntaxNode root, SemanticModel model) = Parse("var args = 1; var p = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, a => TimeSpan.FromSeconds(args)); await p.ExecuteAsync(() => Task.CompletedTask);");
        var pattern = new WaitAndRetryAsyncPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).First(), model);

        // Assert
        string text = result.ShouldBeOfType<FixedResult>().NewNodes.ShouldHaveSingleItem().ToString();
        text.ShouldContain("DelayGenerator = args1 =>");
        text.ShouldContain("SleepDuration(args1.AttemptNumber + 1)");
    }

    [Fact]
    public void Rewrite_GenericResultFromTaskExpression_WrapsInTypedValueTask()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("var p = Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, a => TimeSpan.Zero); int r = await p.ExecuteAsync(() => Task.FromResult(1));");
        var pattern = new WaitAndRetryAsyncPattern();
        SyntaxNode execute = pattern.Find(root, model).Last();

        // Act
        PatternResult result = pattern.Rewrite(execute, model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes.ShouldHaveSingleItem().ToString().ShouldBe("p.ExecuteAsync(_ => new ValueTask<int>(Task.FromResult(1)))");
    }

    private static (SyntaxNode Root, SemanticModel Model) Parse(string statements, params string[] preprocessorSymbols)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "using System;\nusing System.Threading;\nusing System.Threading.Tasks;\nusing Polly;\n"
            + "public static class Own { public static Task WaitAndRetryAsync(int n, Func<int, TimeSpan> f) => Task.CompletedTask; }\n"
            + "public class T { static TimeSpan Delay(int a) => TimeSpan.Zero; static Task Work() => Task.CompletedTask; static void Keep(object o) { }\n"
            + "public async Task M() { " + statements + " } }",
            new CSharpParseOptions(preprocessorSymbols: preprocessorSymbols),
            cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], PollyReferences.Before).GetSemanticModel(tree);
        return (tree.GetRoot(TestContext.Current.CancellationToken), model);
    }
}
