using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeNullPattern : FluentAssertionPattern
{
    public override string Id => "fa.be-null";

    public override string Description => "x.Should().BeNull() → x.ShouldBeNull(), NotBeNull() → ShouldNotBeNull()";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeNull", "NotBeNull");

    // The same extension works for reference types and Nullable<T>, so no type switch is needed. A non-nullable struct
    // (FA boxes it, so NotBeNull always passes and BeNull always fails) is boxed too: Shouldly's extension needs a class (CS0452).
    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        ExpressionSyntax subject = context.Subject.WithoutTrailingTrivia();
        if (context.Model.GetTypeInfo(context.Subject).Type is { IsValueType: true, OriginalDefinition.SpecialType: not SpecialType.System_Nullable_T })
        {
            subject = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), subject.WithoutTrivia())).WithTriviaFrom(subject);
        }

        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(subject, context.Name == "BeNull" ? "ShouldBeNull" : "ShouldNotBeNull", context.Because));
    }
}
