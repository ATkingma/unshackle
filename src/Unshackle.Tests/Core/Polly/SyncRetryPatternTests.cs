using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Fixes.Polly.Patterns;
using Unshackle.Core.Models;
using Xunit;

namespace Unshackle.Tests.Core.Polly;

public class SyncRetryPatternTests
{
    [Theory]
    [InlineData("Policy.Handle<TimeoutException>().WaitAndRetry(2, a => TimeSpan.Zero).Execute(() => { });", 1, 0)]
    [InlineData("Policy.Handle<TimeoutException>().Retry(2).Execute(() => { });", 1, 0)]
    [InlineData("Policy.Handle<TimeoutException>().WaitAndRetryForever(a => TimeSpan.Zero).Execute(() => { });", 1, 0)]
    [InlineData("await Policy.Handle<TimeoutException>().RetryAsync(2).ExecuteAsync(() => Task.CompletedTask);", 0, 1)]
    public void Find_SyncAndAsyncRetryCallsBelongToDifferentPatterns(string statements, int sync, int variants)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse(statements);

        // Act
        int syncFound = new SyncRetryPattern().Find(root, model).Count();
        int asyncFound = new RetryVariantsPattern().Find(root, model).Count() + new WaitAndRetryAsyncPattern().Find(root, model).Count();

        // Assert
        syncFound.ShouldBe(sync);
        asyncFound.ShouldBe(variants);
    }

    [Fact]
    public void Rewrite_InlineRetryAndExecute_ExecuteCallKept()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("Policy.Handle<TimeoutException>().Retry(2).Execute(() => { });");
        var pattern = new SyncRetryPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).Single(), model);

        // Assert
        string text = result.ShouldBeOfType<FixedResult>().NewNodes.ShouldHaveSingleItem().ToString();
        text.ShouldStartWith("new ResiliencePipelineBuilder()");
        text.ShouldEndWith(".Build().Execute(() => { })");
    }

    [Theory]
    [InlineData("var p = Policy.Handle<TimeoutException>().Retry(1); p.Execute(c => { }, new Context(\"x\"));", nameof(PollyReason.ExecuteOverloadNotSupported))]
    [InlineData("var p = Policy.Handle<TimeoutException>().Retry(1); p.Execute(() => { }); p.Reset();", nameof(PollyReason.PolicyUsedBeyondExecute))]
    [InlineData("var n = 2; var p = Policy.Handle<TimeoutException>().Retry(n); p.Execute(() => { });", nameof(PollyReason.RetryCountNotLiteral))]
    [InlineData("var p = Policy.Handle<TimeoutException>().WaitAndRetry(1, (a, c) => TimeSpan.Zero); p.Execute(() => { });", nameof(PollyReason.OverloadNotSupported))]
    public void Rewrite_UnsupportedShape_EveryFindingManualWithCode(string statements, string code)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse(statements);
        var pattern = new SyncRetryPattern();

        // Act
        List<PatternResult> results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(r => r is ManualResult && ((ManualResult)r).Code == code);
    }

    private static (SyntaxNode Root, SemanticModel Model) Parse(string statements)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "using System;\nusing System.Threading.Tasks;\nusing Polly;\npublic class T { public async Task M() { " + statements + " } }",
            cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], PollyReferences.Before).GetSemanticModel(tree);
        return (tree.GetRoot(TestContext.Current.CancellationToken), model);
    }
}
