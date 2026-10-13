using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

// TAAK-200: FluentAssertions calls actual.Equals(expected), Shouldly expected.Equals(actual); an asymmetric Equals gets a warning.
public class ShouldBeEqualsOrderTests
{
    private const string Types = """
        class Own { public override bool Equals(object o) => true; public override int GetHashCode() => 0; }
        class Derived : Own { }
        record Rec(int X);
        interface IThing { }
        """;

    [Theory]
    [InlineData("Own a, Own b", "a.Should().Be(b)", "check symmetry of Own.Equals")]
    [InlineData("Derived a, Derived b", "a.Should().NotBe(b)", "check symmetry of Own.Equals")]
    [InlineData("object a, Own b", "a.Should().Be(b)", "check symmetry of Own.Equals")]
    [InlineData("object a, object b", "a.Should().Be(b)", "the runtime types are unknown here")]
    [InlineData("IThing a, IThing b", "a.Should().Be(b)", "the runtime types are unknown here")]
    public void Rewrite_EqualsMayBeAsymmetric_FixedWithWarning(string parameters, string assertion, string warning)
    {
        // Act
        FixedResult result = Rewrite(parameters, assertion).ShouldBeOfType<FixedResult>();

        // Assert
        result.Warning.ShouldNotBeNull().ShouldContain("Shouldly compares expected.Equals(actual)");
        result.Warning.ShouldContain(warning);
    }

    [Theory]
    [InlineData("int a, int b", "a.Should().Be(b)")]
    [InlineData("string a, string b", "a.Should().Be(b)")]
    [InlineData("Rec a, Rec b", "a.Should().Be(b)")]
    [InlineData("object a, int b", "a.Should().Be(b)")]
    [InlineData("System.DateTime a, System.DateTime b", "a.Should().NotBe(b)")]
    public void Rewrite_EqualsSymmetricOrBcl_FixedWithoutWarning(string parameters, string assertion)
    {
        // Act
        FixedResult result = Rewrite(parameters, assertion).ShouldBeOfType<FixedResult>();

        // Assert
        result.Warning.ShouldBeNull();
    }

    private static PatternResult Rewrite(string parameters, string assertion)
    {
        string code = $"using FluentAssertions;\n{Types}\nclass C {{ void T({parameters}) {{ {assertion}; }} }}";
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.FluentAssertions).GetSemanticModel(tree);
        ShouldBePattern pattern = assertion.Contains("NotBe", StringComparison.Ordinal) ? new NotBePattern() : new ShouldBePattern();
        SyntaxNode node = pattern.Find(tree.GetRoot(TestContext.Current.CancellationToken), model).Single();
        return pattern.Rewrite(node, model);
    }
}
