using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Unshackle.Core.Fixes.MediatR.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class SendAsTaskPatternTests
{
    [Fact]
    public void Find_NSubstituteReturnsSetup_FindsTheSetupNotTheSend()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("m.Send(new Q(), default).Returns(Task.FromResult(1));");

        // Act
        List<SyntaxNode> found = [.. new SendAsTaskPattern().Find(root, model)];

        // Assert
        found.ShouldHaveSingleItem().ToString().ShouldBe("m.Send(new Q(), default).Returns(Task.FromResult(1))");
    }

    [Fact]
    public void Find_NSubstituteThrowsSetupAndReceivedCheck_FindsNothing()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("m.Send(new Q(), default).Throws(new System.Exception()); m.Received().Send(new Q(), default);");

        // Act
        List<SyntaxNode> found = [.. new SendAsTaskPattern().Find(root, model)];

        // Assert
        found.ShouldBeEmpty();
    }

    [Fact]
    public void Find_SendInsideExpressionTree_FindsNothing()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("System.Linq.Expressions.Expression<System.Func<IMediator, Task<int>>> e = x => x.Send(new Q(), default);");

        // Act
        List<SyntaxNode> found = [.. new SendAsTaskPattern().Find(root, model)];

        // Assert
        found.ShouldBeEmpty();
    }

    [Fact]
    public void Rewrite_ReturnsWithVariable_Manual()
    {
        // Arrange
        (SyntaxNode root, SemanticModel model) = Parse("var t = Task.FromResult(1); m.Send(new Q(), default).Returns(t);");
        var pattern = new SendAsTaskPattern();

        // Act
        PatternResult result = pattern.Rewrite(pattern.Find(root, model).Single(), model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("NSubstitute mock");
    }

    private static (SyntaxNode Root, SemanticModel Model) Parse(string statements)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "using System.Threading.Tasks;\nusing MediatR;\nusing NSubstitute;\nusing NSubstitute.ExceptionExtensions;\n"
            + "public record Q : IRequest<int>;\n"
            + "public class T { public void M() { var m = Substitute.For<IMediator>(); " + statements + " } }",
            cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.MediatR).GetSemanticModel(tree);
        return (tree.GetRoot(TestContext.Current.CancellationToken), model);
    }
}
