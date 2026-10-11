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

    [Theory]
    [InlineData("Action act = () => M(1); act.Should().Throw<AssertionFailedException>();", true)]
    [InlineData("Action act = () => { if (true) { M(1); } }; act.Should().ThrowExactly<InvalidOperationException>();", true)]
    [InlineData("((Action)(() => M(1))).Should().Throw<AssertionFailedException>();", true)]
    [InlineData("this.Invoking(c => c.M(1)).Should().Throw<AssertionFailedException>();", true)]
    [InlineData("FluentActions.Invoking(() => M(1)).Should().Throw<AssertionFailedException>();", true)]
    [InlineData("Func<Task> act = async () => { await Task.Yield(); M(1); }; _ = act.Should().ThrowAsync<AssertionFailedException>();", true)]
    [InlineData("Should.Throw<AssertionFailedException>(() => M(1));", true)]
    [InlineData("Action act = () => M(1); act.Should().Throw<Exception>();", false)]
    [InlineData("Should.Throw<ShouldAssertException>(() => M(1));", false)]
    [InlineData("Action act = () => M(1); act();", false)]
    [InlineData("M(1);", false)]
    public void IsInsideExpectedFailure_DependsOnExpectedExceptionType(string body, bool expected)
    {
        // Arrange
        string code = "using System; using System.Threading.Tasks; using FluentAssertions; using FluentAssertions.Execution; using Shouldly;\n"
            + "class C { public void M(int i) { } void T() { " + body + " } }";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], [.. TestReferences.FluentAssertions, .. TestReferences.ShouldlyAssembly])
            .GetSemanticModel(tree);
        InvocationExpressionSyntax call = tree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression is IdentifierNameSyntax { Identifier.ValueText: "M" } or MemberAccessExpressionSyntax { Name.Identifier.ValueText: "M" });

        // Act
        bool result = FluentAssertionsSyntax.IsInsideExpectedFailure(call, model);

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
