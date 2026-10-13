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

    [Theory]
    [InlineData("Action a = () => Helper.Check(true); a.Should().Throw<AssertionFailedException>();", true)]
    [InlineData("Action a = () => Wrap(); a.Should().Throw<AssertionFailedException>();", true)]
    [InlineData("Action a = () => Helper.Check(true); a.Should().Throw<InvalidOperationException>();", false)]
    [InlineData("Action a = () => Helper.Check(true); a.Should().Throw<Exception>();", false)]
    [InlineData("Helper.Check(true);", false)]
    public void IsInsideExpectedFailure_HelperInOtherFile_DependsOnCallerInAnyFile(string body, bool expected)
    {
        // Arrange
        const string helper = "using FluentAssertions;\nstatic class Helper { public static void Check(bool b) { b.Should().BeTrue(); } }";
        string test = "using System; using FluentAssertions; using FluentAssertions.Execution;\n"
            + "class T { static void Wrap() => Helper.Check(false); void Run() { " + body + " } }";
        SyntaxTree helperTree = CSharpSyntaxTree.ParseText(helper, cancellationToken: TestContext.Current.CancellationToken);
        SyntaxTree testTree = CSharpSyntaxTree.ParseText(test, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [helperTree, testTree], TestReferences.FluentAssertions).GetSemanticModel(helperTree);
        InvocationExpressionSyntax assertion = helperTree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>().First();

        // Act
        bool result = FluentAssertionsSyntax.IsInsideExpectedFailure(assertion, model);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData("AssertionFailedException", true)]
    [InlineData("InvalidOperationException", false)]
    public void ExpectsAssertionFailure_FollowsHelperInOtherFile(string thrown, bool expected)
    {
        // Arrange
        const string helper = "using FluentAssertions;\nstatic class Helper { public static void Check(bool b) { b.Should().BeTrue(); } }";
        string test = "using System; using FluentAssertions; using FluentAssertions.Execution;\n"
            + "class T { void Run() { Action a = () => Helper.Check(false); a.Should().Throw<" + thrown + ">(); } }";
        SyntaxTree helperTree = CSharpSyntaxTree.ParseText(helper, cancellationToken: TestContext.Current.CancellationToken);
        SyntaxTree testTree = CSharpSyntaxTree.ParseText(test, cancellationToken: TestContext.Current.CancellationToken);
        var compilation = CSharpCompilation.Create("Check", [helperTree, testTree], TestReferences.FluentAssertions);
        SemanticModel model = compilation.GetSemanticModel(testTree);
        InvocationExpressionSyntax throwCall = testTree.GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(i => i.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "Throw" } });
        ExpressionSyntax subject = FluentAssertionsSyntax.GetSubject(throwCall)!;
        ITypeSymbol? type = model.GetTypeInfo(((GenericNameSyntax)((MemberAccessExpressionSyntax)throwCall.Expression).Name).TypeArgumentList.Arguments[0], TestContext.Current.CancellationToken).Type;

        // Act
        bool result = FluentAssertionsSyntax.ExpectsAssertionFailure(subject, type, model);

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
