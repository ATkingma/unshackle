using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.Polly;
using Unshackle.Core.Fixes.Polly.Patterns;
using Unshackle.Core.Models;
using Xunit;

namespace Unshackle.Tests.Core.Polly;

public class AddPolicyHandlerPatternTests
{
    [Fact]
    public void Find_HandlerPolicy_OnlyTheHandlerPatternReportsIt()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("b.AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(2, a => TimeSpan.Zero));");

        // Act
        int handler = new AddPolicyHandlerPattern().Find(root, model).Count();
        int retry = new WaitAndRetryAsyncPattern().Find(root, model).Count();

        // Assert
        handler.ShouldBe(1);
        retry.ShouldBe(0);
    }

    [Theory]
    [InlineData("b.AddPolicyHandler(request => HttpPolicyExtensions.HandleTransientHttpError().RetryAsync(1));", nameof(PollyReason.HandlerOverloadNotSupported))]
    [InlineData("b.AddPolicyHandler(Policy.NoOpAsync<HttpResponseMessage>());", nameof(PollyReason.HandlerPolicyNotInline))]
    [InlineData("b.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(n, a => TimeSpan.Zero));", nameof(PollyReason.RetryCountNotLiteral))]
    [InlineData("var c = b.AddTransientHttpErrorPolicy(p => p.RetryAsync(1));", nameof(PollyReason.HandlerResultUsed))]
    [InlineData("b.AddTransientHttpErrorPolicy(p => p.RetryAsync(1)).SetHandlerLifetime(TimeSpan.Zero);", nameof(PollyReason.HandlerResultUsed))]
    [InlineData("Func<IHttpClientBuilder> f = () => b.AddTransientHttpErrorPolicy(p => p.RetryAsync(1));", nameof(PollyReason.HandlerResultUsed))]
    public void Rewrite_UnsupportedShape_ManualWithCode(string statements, string code)
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse(statements);
        var pattern = new AddPolicyHandlerPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).Single(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Code.ShouldBe(code);
    }

    [Fact]
    public void Rewrite_VoidLambdaBody_Fixed()
    {
        // Arrange (a void `=>` body discards the builder like a statement)
        (SyntaxNode root, SemanticModel model) = Parse("Action a = () => b.AddTransientHttpErrorPolicy(p => p.RetryAsync(1));");
        var pattern = new AddPolicyHandlerPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).Single(), model);

        // Assert
        result.ShouldBeOfType<FixedResult>();
    }

    [Theory]
    [InlineData("b.AddTransientHttpErrorPolicy(p => p.OrTransientHttpStatusCode().RetryAsync(1));")]
    [InlineData("b.AddTransientHttpErrorPolicy(p => p.OrTransientHttpError().RetryAsync(1));")]
    [InlineData("b.AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError().OrTransientHttpStatusCode().RetryAsync(1));")]
    public void Rewrite_RedundantTransientClause_PredicateWrittenOnce(string statements)
    {
        // Arrange (TAAK-335: the transient filter already covers OrTransientHttpStatusCode / OrTransientHttpError)
        (SyntaxNode root, SemanticModel model) = Parse(statements);
        var pattern = new AddPolicyHandlerPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).Single(), model);

        // Assert
        string text = string.Concat(result.ShouldBeOfType<FixedResult>().NewNodes.Select(n => n.ToFullString()));
        CountOf(text, "StatusCode >= 500").ShouldBe(1);
        CountOf(text, "Handle<HttpRequestException>()").ShouldBe(1);
    }

    [Fact]
    public void Rewrite_TransientWithUserClause_UserClauseKept()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("b.AddTransientHttpErrorPolicy(p => p.OrTransientHttpStatusCode().OrResult(r => r.StatusCode == System.Net.HttpStatusCode.NotFound).RetryAsync(1));");
        var pattern = new AddPolicyHandlerPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).Single(), model);

        // Assert
        string text = string.Concat(result.ShouldBeOfType<FixedResult>().NewNodes.Select(n => n.ToFullString()));
        CountOf(text, "StatusCode >= 500").ShouldBe(1);
        text.ShouldContain(".HandleResult(r => r.StatusCode == System.Net.HttpStatusCode.NotFound)");
    }

    private static int CountOf(string text, string value) => (text.Length - text.Replace(value, "", StringComparison.Ordinal).Length) / value.Length;

    [Fact]
    public void Rewrite_PolicyMethodAlsoUsedElsewhere_HandlerAndMethodManual()
    {
        // Arrange (row 6, slice 2: the method goes only when every use is a handler)
        (SyntaxNode root, SemanticModel model) = Parse("b.AddPolicyHandler(GetPolicy()); var kept = GetPolicy();");
        var pattern = new AddPolicyHandlerPattern();

        // Act
        List<PatternResult> results = [.. pattern.Find(root, model).Select(n => pattern.Rewrite(n, model))];

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldAllBe(r => r is ManualResult && ((ManualResult)r).Code == nameof(PollyReason.HandlerPolicyNotInline));
    }

    private static (SyntaxNode Root, SemanticModel Model) Parse(string statements)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "using System;\nusing System.Net.Http;\nusing Microsoft.Extensions.DependencyInjection;\nusing Polly;\nusing Polly.Extensions.Http;\n"
            + "public class T { private static IAsyncPolicy<HttpResponseMessage> GetPolicy() => HttpPolicyExtensions.HandleTransientHttpError().RetryAsync(1); "
            + "public void M(IHttpClientBuilder b, int n) { " + statements + " } }",
            cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], PollyReferences.Before).GetSemanticModel(tree);
        return (tree.GetRoot(TestContext.Current.CancellationToken), model);
    }
}
