using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class ContainKeyPatternTests
{
    private const string Code = "using FluentAssertions;\nusing System.Collections.Generic;\n"
        + "class C { void T(Dictionary<string, int> x) { x.Should().ContainKey(\"a\"); } }";

    [Theory]
    [InlineData(LanguageVersion.CSharp12, "x.ContainsKey(\"a\").ShouldBeTrue()")]
    [InlineData(LanguageVersion.CSharp13, "x.ShouldContainKey(\"a\")")]
    public void Rewrite_DictionaryByLanguageVersion_AvoidsAmbiguousOverloadBeforeCSharp13(LanguageVersion version, string expected)
    {
        // Arrange
        SyntaxTree tree = CSharpSyntaxTree.ParseText(Code, new CSharpParseOptions(version), cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.FluentAssertions).GetSemanticModel(tree);
        var pattern = new ContainKeyPattern();
        SyntaxNode node = pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes.Single().ToString().ShouldBe(expected);
    }
}
