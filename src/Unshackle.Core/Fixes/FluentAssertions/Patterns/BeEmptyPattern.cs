using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeEmptyPattern : FluentAssertionPattern
{
    public override string Id => "fa.be-empty";

    public override string Description =>
        "x.Should().BeEmpty() → x.ShouldBeEmpty(), NotBeEmpty() → ShouldNotBeEmpty(); on a Guid → ShouldBe(Guid.Empty) / ShouldNotBe(Guid.Empty)";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeEmpty", "NotBeEmpty");

    // Shouldly has ShouldBeEmpty for string and IEnumerable<T> (also dictionaries), but not for Guid (TAAK-114).
    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        bool empty = context.Name == "BeEmpty";
        ITypeSymbol? type = context.Model.GetTypeInfo(context.Subject).Type;
        bool nullable = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
        ITypeSymbol? guid = nullable ? ((INamedTypeSymbol)type!).TypeArguments[0] : type;
        if (guid?.ToDisplayString() != "System.Guid")
        {
            return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(
                context.Subject.WithoutTrailingTrivia(), empty ? "ShouldBeEmpty" : "ShouldNotBeEmpty", context.Because));
        }

        if (nullable && !empty)
        {
            return PatternResult.Manual(
                "NotBeEmpty() on a Guid? also fails on null, ShouldNotBe(Guid.Empty) does not; e.g. x.ShouldNotBeNull().ShouldNotBe(Guid.Empty).");
        }

        ArgumentSyntax guidEmpty = SyntaxFactory.Argument(
            SyntaxFactory.ParseExpression($"{guid.ToMinimalDisplayString(context.Model, context.Assertion.SpanStart)}.Empty"));
        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(
            context.Subject.WithoutTrailingTrivia(), empty ? "ShouldBe" : "ShouldNotBe", guidEmpty, context.Because));
    }
}
