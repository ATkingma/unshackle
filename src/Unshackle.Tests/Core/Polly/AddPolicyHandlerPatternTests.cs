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

    private static (SyntaxNode Root, SemanticModel Model) Parse(string statements)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "using System;\nusing System.Net.Http;\nusing Microsoft.Extensions.DependencyInjection;\nusing Polly;\nusing Polly.Extensions.Http;\n"
            + "public class T { public void M(IHttpClientBuilder b, int n) { " + statements + " } }",
            cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], PollyReferences.Before).GetSemanticModel(tree);
        return (tree.GetRoot(TestContext.Current.CancellationToken), model);
    }
}
