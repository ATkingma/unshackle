using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class BehaviorDiRegistrationPatternTests
{
    [Theory]
    [InlineData("services.AddTransient(typeof(IPipelineBehavior<,>), typeof(B<,>));", 1)]
    [InlineData("services.AddScoped(typeof(IPipelineBehavior<,>), typeof(B<,>));", 1)]
    [InlineData("services.AddSingleton(typeof(MediatR.IPipelineBehavior<,>), typeof(B<,>));", 1)]
    [InlineData("services.AddTransient(typeof(IStreamPipelineBehavior<,>), typeof(B<,>));", 0)]
    [InlineData("services.AddTransient(typeof(B<,>), typeof(B<,>));", 0)]
    [InlineData("services.AddTransient(typeof(IPipelineBehavior<,>), _ => new object());", 0)]
    [InlineData("services.AddMediatR(cfg => cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(B<,>)));", 0)]
    public void Find_Registration_FindsOnlyDiRegistrationOfMediatRBehavior(string body, int expected)
    {
        // Arrange
        (BehaviorDiRegistrationPattern pattern, SyntaxNode root, SemanticModel model) = Arrange(body);

        // Act
        List<SyntaxNode> found = [.. pattern.Find(root, model)];

        // Assert
        found.Count.ShouldBe(expected);
    }

    [Fact]
    public void Rewrite_Registration_KeepsNodeWithoutWarning()
    {
        // Arrange
        (BehaviorDiRegistrationPattern pattern, SyntaxNode root, SemanticModel model) = Arrange("services.AddScoped(typeof(IPipelineBehavior<,>), typeof(B<,>));");
        SyntaxNode node = pattern.Find(root, model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        fixedResult.Warning.ShouldBeNull();
        fixedResult.NewNodes.ShouldBe([node]);
    }

    private static (BehaviorDiRegistrationPattern Pattern, SyntaxNode Root, SemanticModel Model) Arrange(string body)
    {
        string code = "using MediatR;\nusing Microsoft.Extensions.DependencyInjection;\n"
            + "class B<T1, T2>; class C { void T(IServiceCollection services) { " + body + " } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.MediatR).GetSemanticModel(tree);
        return (new BehaviorDiRegistrationPattern(), tree.GetRoot(TestContext.Current.CancellationToken), model);
    }
}
