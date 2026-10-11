using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class AddMediatRPatternTests
{
    [Theory]
    [InlineData("services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers));", "_ = handlers;")]
    [InlineData("services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(handlers, typeof(C).Assembly));", "_ = handlers;")]
    [InlineData("services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers)); _ = handlers.FullName;", null)]
    [InlineData("services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(C).Assembly));", null)]
    public void Rewrite_DroppedAssemblyParameterInStatement_DiscardsParameter(string body, string? discard)
    {
        // Arrange
        (AddMediatRPattern pattern, SyntaxNode node, SemanticModel model) = Arrange(body);

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        fixedResult.Warning.ShouldBeNull();
        List<string> statements = [.. fixedResult.NewNodes.Select(n => n.ToString())];
        if (discard is null)
        {
            statements.Count.ShouldBe(1);
        }
        else
        {
            statements.ShouldBe([discard, statements[1]]);
            statements[1].ShouldStartWith("services.AddMediator(");
        }
    }

    [Theory]
    [InlineData("var s = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers));", "handlers")]
    [InlineData("var s = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(C).Assembly));", null)]
    public void Rewrite_DroppedAssemblyParameterInExpression_FixedWithWarning(string body, string? parameter)
    {
        // Arrange
        (AddMediatRPattern pattern, SyntaxNode node, SemanticModel model) = Arrange(body);

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        if (parameter is null)
        {
            fixedResult.Warning.ShouldBeNull();
        }
        else
        {
            fixedResult.Warning.ShouldNotBeNull().ShouldContain($"Parameter '{parameter}' is no longer used");
        }
    }

    private static (AddMediatRPattern Pattern, SyntaxNode Node, SemanticModel Model) Arrange(string body)
    {
        string code = "using System.Reflection;\nusing Microsoft.Extensions.DependencyInjection;\n"
            + "class C { void T(IServiceCollection services, Assembly handlers) { " + body + " } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.MediatR).GetSemanticModel(tree);
        var pattern = new AddMediatRPattern();
        return (pattern, pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).Single(), model);
    }
}
