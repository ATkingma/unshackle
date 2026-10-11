using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class CatchAssertionExceptionPatternTests
{
    private static readonly IReadOnlyList<MetadataReference> s_references =
        [.. TestReferences.FluentAssertions, MetadataReference.CreateFromFile(typeof(Xunit.Sdk.XunitException).Assembly.Location)];

    [Fact]
    public void Rewrite_FilterOnFluentAssertionsAndXunit_AddsShouldlyOnce()
    {
        // Arrange: the shape of Polly's RateLimitSpecsBase.Within.
        const string code = "using System; using FluentAssertions.Execution; using Xunit.Sdk;\n"
            + "class C { void T(Exception e) { if (!(e is AssertionFailedException || e is XunitException)) throw e; } }";
        (CatchAssertionExceptionPattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);
        SyntaxNode node = pattern.Find(root, model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes.Single().ToString().ShouldBe("e is AssertionFailedException || e is ShouldAssertException");
    }

    [Fact]
    public void Find_SiteAlreadyCatchesShouldly_FindsNothing()
    {
        // Arrange: the code after a first run.
        const string code = "using System; using FluentAssertions.Execution; using Shouldly;\n"
            + "class C { bool T(Exception e, Action a) { try { a(); } catch (AssertionFailedException) { } catch (ShouldAssertException) { } "
            + "return e is AssertionFailedException || e is ShouldAssertException; } }";
        (CatchAssertionExceptionPattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);

        // Act
        IEnumerable<SyntaxNode> found = pattern.Find(root, model);

        // Assert
        found.ShouldBeEmpty();
    }

    [Fact]
    public void Rewrite_IsPatternWithDesignation_Manual()
    {
        // Arrange
        const string code = "using System; using Xunit.Sdk;\nclass C { string T(Exception e) => e is XunitException x ? x.Message : \"\"; }";
        (CatchAssertionExceptionPattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);
        SyntaxNode node = pattern.Find(root, model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("Shouldly.ShouldAssertException");
    }

    private static (CatchAssertionExceptionPattern Pattern, SemanticModel Model, SyntaxNode Root) Arrange(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], [.. s_references, .. TestReferences.ShouldlyAssembly]).GetSemanticModel(tree);
        return (new CatchAssertionExceptionPattern(), model, tree.GetRoot(TestContext.Current.CancellationToken));
    }
}
