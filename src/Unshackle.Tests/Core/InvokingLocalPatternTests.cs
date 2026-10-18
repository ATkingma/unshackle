using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class InvokingLocalPatternTests
{
    [Fact]
    public void Find_InvokingNotStoredInALocal_FindsNothing()
    {
        // Arrange
        const string code = "using System; using FluentAssertions;\nclass C { void F() {} void T(C c) { c.Invoking(x => x.F()).Should().NotThrow(); Action a = c.F; } }";
        (InvokingLocalPattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);

        // Act
        IEnumerable<SyntaxNode> found = pattern.Find(root, model);

        // Assert
        found.ShouldBeEmpty();
    }

    [Fact]
    public void Rewrite_VarLocal_WritesTheReturnedDelegateType()
    {
        // Arrange
        const string code = "using FluentAssertions;\nclass C { int G() => 1; void T(C c) { var act = c.Invoking(x => x.G()); } }";
        (InvokingLocalPattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);
        SyntaxNode node = pattern.Find(root, model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes.Single().ToString().ShouldBe("System.Func<int> act = () => c.G();");
    }

    [Fact]
    public void Rewrite_ReceiverWithSideEffects_IsManual()
    {
        // Arrange
        const string code = "using FluentAssertions;\nclass C { void F() {} C Make() => this; void T() { var act = Make().Invoking(x => x.F()); } }";
        (InvokingLocalPattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);
        SyntaxNode node = pattern.Find(root, model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("side effects");
    }

    private static (InvokingLocalPattern Pattern, SemanticModel Model, SyntaxNode Root) Arrange(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.FluentAssertions).GetSemanticModel(tree);
        return (new InvokingLocalPattern(), model, tree.GetRoot(TestContext.Current.CancellationToken));
    }
}
