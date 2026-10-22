using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Fixes.Polly.Patterns;
using Unshackle.Core.Models;
using Xunit;

namespace Unshackle.Tests.Core.Polly;

public class RetryVariantsPatternTests
{
    [Theory]
    [InlineData("await Policy.Handle<TimeoutException>().WaitAndRetryAsync(2, a => TimeSpan.Zero).ExecuteAsync(() => Task.CompletedTask);", 1, 0)]
    [InlineData("await Policy.Handle<TimeoutException>().RetryAsync(2).ExecuteAsync(() => Task.CompletedTask);", 0, 1)]
    [InlineData("await Policy.Handle<TimeoutException>().WaitAndRetryAsync(new[] { TimeSpan.Zero }).ExecuteAsync(() => Task.CompletedTask);", 0, 1)]
    [InlineData("await Policy.Handle<TimeoutException>().WaitAndRetryForeverAsync(a => TimeSpan.Zero).ExecuteAsync(() => Task.CompletedTask);", 0, 1)]
    public void Find_EachRetryCallBelongsToExactlyOnePattern(string statements, int waitAndRetry, int variants)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse(statements);

        // Act
        int first = new WaitAndRetryAsyncPattern().Find(root, model).Count();
        int second = new RetryVariantsPattern().Find(root, model).Count();

        // Assert
        first.ShouldBe(waitAndRetry);
        second.ShouldBe(variants);
    }

    [Theory]
    [InlineData("var d = TimeSpan.Zero; var p = Policy.Handle<TimeoutException>().WaitAndRetryAsync(new[] { d }); await p.ExecuteAsync(() => Task.CompletedTask);", nameof(PollyReason.SleepDurationsNotLiteral))]
    [InlineData("var n = 2; var p = Policy.Handle<TimeoutException>().RetryAsync(n); await p.ExecuteAsync(() => Task.CompletedTask);", nameof(PollyReason.RetryCountNotLiteral))]
    [InlineData("var p = Policy.Handle<TimeoutException>().RetryForeverAsync((e, ctx) => Console.WriteLine(ctx.OperationKey)); await p.ExecuteAsync(() => Task.CompletedTask);", nameof(PollyReason.OnRetryUsesContext))]
    public void Rewrite_UnsupportedShape_EveryFindingManualWithCode(string statements, string code)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse(statements);
        var pattern = new RetryVariantsPattern();

        // Act
        List<PatternResult> results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(r => r is ManualResult && ((ManualResult)r).Code == code);
    }

    [Theory]
    [InlineData("public partial class W { private readonly IAsyncPolicy _p = Policy.Handle<TimeoutException>().RetryAsync(1); public Task M() => _p.ExecuteAsync(() => Task.CompletedTask); } public partial class W { }", nameof(PollyReason.PolicyMemberNotPrivate))]
    [InlineData("public class W { private readonly IAsyncPolicy _p; public W() { _p = Policy.Handle<TimeoutException>().RetryAsync(1); } }", nameof(PollyReason.PolicyNotLocal))]
    public void Rewrite_FieldNotProvablyLocal_Manual(string type, string code)
    {
        // Arrange (queue row 5)
        SyntaxTree tree = CSharpSyntaxTree.ParseText("using System;\nusing System.Threading.Tasks;\nusing Polly;\n" + type, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], PollyReferences.Before).GetSemanticModel(tree);
        var pattern = new RetryVariantsPattern();

        // Act
        List<PatternResult> results = [.. pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).Select(n => pattern.Rewrite(n, model))];

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
