using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Shouldly;
using Unshackle.Core.Fixes.FluentAssertions;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class FluentAssertionsSyntaxTests
{
    [Theory]
    [InlineData("#nullable enable\n", "string? s", "s.ShouldNotBeNull()")]
    [InlineData("#nullable enable\n", "string s", "s")]
    [InlineData("", "string s", "s")]
    public void NotNull_DependsOnNullableFlowState(string directive, string parameter, string expected)
    {
        // Arrange
        string code = directive + "class C { void M(" + parameter + ") { _ = s; } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.Shouldly).GetSemanticModel(tree);
        IdentifierNameSyntax subject = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<IdentifierNameSyntax>().Last();

        // Act
        ExpressionSyntax result = FluentAssertionsSyntax.NotNull(subject, model);

        // Assert
        result.ToString().ShouldBe(expected);
    }

    [Fact]
    public void CompileErrors_ReportsNullableWarningOfRewrite()
    {
        // Arrange
        const string code = "#nullable enable\nusing Shouldly;\nclass C { void M(string? s) { s.ShouldContain(\"a\"); } }";

        // Act
        IReadOnlyList<Diagnostic> errors = TestSolution.CompileErrors(code, TestReferences.Shouldly);

        // Assert
        errors.ShouldContain(d => d.Id == "CS8604");
    }
}
