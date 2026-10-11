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
    [InlineData("services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers));", "handlers")]
    [InlineData("services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(handlers, typeof(C).Assembly));", "handlers")]
    [InlineData("services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(handlers)); _ = handlers.FullName;", null)]
    [InlineData("services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(C).Assembly));", null)]
    public void Rewrite_DroppedAssemblyParameter_FixedWithWarning(string body, string? parameter)
    {
        // Arrange
        string code = "using System.Reflection;\nusing Microsoft.Extensions.DependencyInjection;\n"
            + "class C { void T(IServiceCollection services, Assembly handlers) { " + body + " } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.MediatR).GetSemanticModel(tree);
        var pattern = new AddMediatRPattern();
        SyntaxNode node = pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).Single();

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
}
