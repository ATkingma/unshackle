using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using Unshackle.Core.Fixes.FluentAssertions.Patterns;
using Unshackle.Core.Models;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Core;

public class TimeSpanExtensionsPatternTests
{
    [Fact]
    public void Rewrite_OwnTimeSpanTypeInScope_QualifiesSystemTimeSpan()
    {
        // Arrange: a type of the user named TimeSpan would capture the simple name.
        const string code = "using FluentAssertions.Extensions;\nnamespace N { class TimeSpan { } class C { object T() => 5.Seconds(); } }";
        (TimeSpanExtensionsPattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);
        SyntaxNode node = pattern.Find(root, model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        FixedResult fixedResult = result.ShouldBeOfType<FixedResult>();
        fixedResult.NewNodes.Single().ToString().ShouldBe("global::System.TimeSpan.FromSeconds(5)");
        fixedResult.RequiredNamespaces.ShouldBeEmpty();
    }

    [Fact]
    public void Find_OwnSecondsExtension_FindsNothing()
    {
        // Arrange: only FluentAssertions.Extensions is migrated.
        const string code = "static class E { public static System.TimeSpan Seconds(this int n) => default; }\nclass C { object T() => 5.Seconds(); }";
        (TimeSpanExtensionsPattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);

        // Act
        IEnumerable<SyntaxNode> found = pattern.Find(root, model);

        // Assert
        found.ShouldBeEmpty();
    }

    [Fact]
    public void Rewrite_DateTimeAt_ConstructorWithDateParts()
    {
        // Arrange
        const string code = "using System; using FluentAssertions.Extensions;\nclass C { DateTime T(DateTime d) => d.At(10, 30); }";
        (TimeSpanExtensionsPattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);
        SyntaxNode node = pattern.Find(root, model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<FixedResult>().NewNodes.Single().ToString().ShouldBe("new DateTime(d.Year, d.Month, d.Day, 10, 30, 0, 0, d.Kind)");
    }

    [Fact]
    public void Rewrite_DateTimeAtMicroseconds_Manual()
    {
        // Arrange
        const string code = "using System; using FluentAssertions.Extensions;\nclass C { DateTime T(DateTime d) => d.At(10, 30, 0, 0, 5); }";
        (TimeSpanExtensionsPattern pattern, SemanticModel model, SyntaxNode root) = Arrange(code);
        SyntaxNode node = pattern.Find(root, model).Single();

        // Act
        PatternResult result = pattern.Rewrite(node, model);

        // Assert
        result.ShouldBeOfType<ManualResult>().Reason.ShouldContain("microseconds");
    }

    private static (TimeSpanExtensionsPattern Pattern, SemanticModel Model, SyntaxNode Root) Arrange(string code)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, cancellationToken: TestContext.Current.CancellationToken);
        SemanticModel model = CSharpCompilation.Create("Check", [tree], TestReferences.FluentAssertions).GetSemanticModel(tree);
        return (new TimeSpanExtensionsPattern(), model, tree.GetRoot(TestContext.Current.CancellationToken));
    }
}
