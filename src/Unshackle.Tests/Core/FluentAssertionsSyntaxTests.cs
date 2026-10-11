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

    [Theory]
    [InlineData("using (new AssertionScope()) { M(1); }", true)]
    [InlineData("using (var s = new AssertionScope(\"ctx\")) { if (true) { M(1); } }", true)]
    [InlineData("using var s = new AssertionScope(); M(1);", true)]
    [InlineData("using var s = new AssertionScope(); { { M(1); } }", true)]
    [InlineData("using (new AssertionScope()) { using (new AssertionScope()) { M(1); } }", true)]
    [InlineData("M(1); using var s = new AssertionScope();", false)]
    [InlineData("using (new AssertionScope()) { } M(1);", false)]
    [InlineData("using (new System.IO.MemoryStream()) { M(1); }", false)]
    [InlineData("M(1);", false)]
    public void IsInsideAssertionScope_DependsOnEnclosingScope(string body, bool expected)
    {
        // Arrange
        string code = "using FluentAssertions.Execution;\nclass C { void M(int i) { } void T() { " + body + " } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.FluentAssertions).GetSemanticModel(tree);
        InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().Single();

        // Act
        bool result = FluentAssertionsSyntax.IsInsideAssertionScope(call, model);

        // Assert
        result.ShouldBe(expected);
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
