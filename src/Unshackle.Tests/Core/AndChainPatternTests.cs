using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.FluentAssertions;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class AndChainPatternTests
{
    [Fact]
    public void Rewrite_LinkWithWarning_KeepsWarning()
    {
        // Arrange
        const string code = "using FluentAssertions;\nclass Dto { public int Id { get; set; } }\n"
            + "class C { void T(Dto a, Dto b) { a.Should().NotBeNull().And.BeEquivalentTo(b); } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.FluentAssertions).GetSemanticModel(tree);
        var pattern = new AndChainPattern([new BeNullPattern(), new BeEquivalentToObjectPattern()]);
        SyntaxNode node = pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        fixedResult.NewNodes.Count.ShouldBe(2);
        fixedResult.Warning.ShouldNotBeNull().ShouldContain("not sealed: Dto");
    }

    [Fact]
    public void Rewrite_LinkWithSeveralStatements_KeepsAll()
    {
        // Arrange
        const string code = "using FluentAssertions;\nusing System.Collections.Generic;\n"
            + "class C { void T(List<int> x) { x.Should().HaveCount(2).And.OnlyContain(i => i > 0); } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.FluentAssertions).GetSemanticModel(tree);
        var pattern = new AndChainPattern(new FluentAssertionsFix().Patterns.Where(p => p is not AndChainPattern).ToList());
        SyntaxNode node = pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        fixedResult.NewNodes.Select(n => n.ToString()).ShouldBe(["x.Count.ShouldBe(2);", "x.ShouldNotBeEmpty();", "x.ShouldAllBe(i => i > 0);"]);
    }
}
