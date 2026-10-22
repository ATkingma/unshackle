using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Fixes.Polly.Patterns;
using Unshackle.Core.Models;
using Xunit;

namespace Unshackle.Tests.Core.Polly;

public class TimeoutNoOpUnsupportedPatternTests
{
    [Theory]
    [InlineData("var p = Policy.TimeoutAsync(TimeSpan.FromHours(2147483647)); await p.ExecuteAsync(() => Task.CompletedTask);", nameof(PollyReason.TimeoutNotProvable))]
    [InlineData("var p = Policy.TimeoutAsync(TimeSpan.FromDays(30)); await p.ExecuteAsync(() => Task.CompletedTask);", nameof(PollyReason.TimeoutNotProvable))]
    [InlineData("var p = Policy.TimeoutAsync(-1); await p.ExecuteAsync(() => Task.CompletedTask);", nameof(PollyReason.TimeoutNotProvable))]
    [InlineData("var p = Policy.TimeoutAsync(() => TimeSpan.FromSeconds(1)); await p.ExecuteAsync(() => Task.CompletedTask);", nameof(PollyReason.TimeoutNotProvable))]
    [InlineData("var p = Policy.TimeoutAsync(2); await p.ExecuteAsync(ct => Task.CompletedTask, CancellationToken.None); p.ExecuteAsync(() => Task.CompletedTask);", nameof(PollyReason.ExecuteNotLambda))]
    public void Timeout_Rewrite_UnprovableShapeIsManualWithCode(string statements, string code)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse(statements);
        var pattern = new TimeoutPattern();

        // Act
        List<PatternResult> results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldContain(r => r is ManualResult && ((ManualResult)r).Code == code);
    }

    [Fact]
    public void Timeout_Find_ValueInsideWrapIsLeftToTheWrapPattern()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("await Policy.WrapAsync(Policy.TimeoutAsync(2), Policy.Handle<TimeoutException>().RetryAsync(1)).ExecuteAsync(() => Task.CompletedTask);");

        // Act
        int timeouts = new TimeoutPattern().Find(root, model).Count();
        List<PatternResult> wraps = [.. new WrapPattern().Find(root, model).Select(n => new WrapPattern().Rewrite(n, model))];

        // Assert
        timeouts.ShouldBe(0);
        wraps.ShouldAllBe(r => r is ManualResult);
    }

    [Fact]
    public void NoOp_Rewrite_ReturnedPolicyIsManual()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("IAsyncPolicy p = Policy.NoOpAsync(); p.ToString();");
        var pattern = new NoOpPattern();

        // Act
        List<PatternResult> results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(r => r is ManualResult);
    }

    [Theory]
    [InlineData("var a = Policy.Handle<Exception>().CircuitBreakerAsync(2, TimeSpan.FromSeconds(1));", 1)]
    [InlineData("var a = Policy.Handle<Exception>().AdvancedCircuitBreaker(0.5, TimeSpan.FromSeconds(5), 4, TimeSpan.FromSeconds(1));", 1)]
    [InlineData("var a = Policy.Handle<Exception>().RetryAsync(2);", 0)]
    public void CircuitBreaker_Find_OnlyBreakerCalls(string statements, int expected)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse(statements);

        // Act
        var pattern = new CircuitBreakerPattern();
        List<PatternResult> results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.Count.ShouldBe(expected);
        results.ShouldAllBe(r => r is ManualResult && ((ManualResult)r).Code == nameof(PollyReason.CircuitBreakerNotEquivalent));
    }

    [Fact]
    public void Unsupported_Find_PlainRetryIsNotReported()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("await Policy.Handle<Exception>().RetryAsync(2).ExecuteAsync(() => Task.CompletedTask);");

        // Act
        int found = new UnsupportedPattern().Find(root, model).Count();

        // Assert
        found.ShouldBe(0);
    }

    private static (SyntaxNode Root, SemanticModel Model) Parse(string statements)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "using System;\nusing System.Threading;\nusing System.Threading.Tasks;\nusing Polly;\npublic class T { public async Task M() { " + statements + " } }",
            cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], PollyReferences.Before).GetSemanticModel(tree);
        return (tree.GetRoot(TestContext.Current.CancellationToken), model);
    }
}
