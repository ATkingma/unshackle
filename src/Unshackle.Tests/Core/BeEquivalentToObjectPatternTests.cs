using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class BeEquivalentToObjectPatternTests
{
    [Theory]
    [InlineData("class Dto { public int Id { get; set; } }", "void T(Dto a, Dto b) { a.Should().BeEquivalentTo(b); }", "not sealed: Dto")]
    [InlineData("record Dto(int Id);", "void T(Dto a, Dto b) { a.Should().BeEquivalentTo(b); }", "not sealed: Dto")]
    [InlineData("class In { public int X { get; set; } } sealed class Dto { public In In { get; set; } }", "void T(Dto a, Dto b) { a.Should().BeEquivalentTo(b); }", "not sealed: In")]
    [InlineData("sealed class Dto { public int Id { get; set; } }", "void T(Dto a, Dto b) { a.Should().BeEquivalentTo(b); }", null)]
    [InlineData("struct Dto { public int Id { get; set; } }", "void T(Dto a, Dto b) { a.Should().BeEquivalentTo(b); }", null)]
    [InlineData("class Dto { public int Id { get; set; } }", "void T() { new Dto().Should().BeEquivalentTo(new Dto()); }", null)]
    public void Rewrite_DerivableType_FixedWithWarning(string types, string method, string? expected)
    {
        // Arrange
        string code = "using FluentAssertions;\n" + types + "\nclass C { " + method + " }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.FluentAssertions).GetSemanticModel(tree);
        var pattern = new BeEquivalentToObjectPattern();
        SyntaxNode node = pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        if (expected is null)
        {
            fixedResult.Warning.ShouldBeNull();
        }
        else
        {
            fixedResult.Warning.ShouldNotBeNull().ShouldContain(expected);
            fixedResult.Warning.ShouldContain("ShouldBeEquivalentTo fails when the runtime type differs");
        }
    }
}
