using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class DiscardAssignmentPatternTests
{
    [Fact]
    public void Find_DiscardOfNonAssertionOrRealLocal_FindsNothing()
    {
        // Arrange: a discard of other calls and values, and a local named `_` that holds the constraint.
        const string code = "using FluentAssertions; using FluentAssertions.Primitives;\n"
            + "class C { int F() => 1; void T(object a, int[] xs) { _ = F(); _ = a; _ = xs.Length; "
            + "{ AndConstraint<ObjectAssertions> _; _ = a.Should().NotBeNull(); } } }";
        (DiscardAssignmentPattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);

        // Act
        IEnumerable<SyntaxNode> found = pattern.Find(root, model);

        // Assert
        found.ShouldBeEmpty();
    }

    [Fact]
    public void Rewrite_DiscardedAssertion_KeepsOnlyTheAssertion()
    {
        // Arrange
        const string code = "using FluentAssertions;\nclass C { void T(int a) { _ = a.Should().Be(3); } }";
        (DiscardAssignmentPattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);
        SyntaxNode node = pattern.Find(root, model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes.Single().ToString().ShouldBe("a.Should().Be(3);");
    }

    private static (DiscardAssignmentPattern Pattern, SemanticModel Model, SyntaxNode Root) Arrange(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.FluentAssertions).GetSemanticModel(tree);
        return (new DiscardAssignmentPattern(), model, tree.GetRoot(TestContext.Current.CancellationToken));
    }
}
