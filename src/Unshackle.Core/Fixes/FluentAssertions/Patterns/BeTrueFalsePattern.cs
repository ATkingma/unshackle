using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeTrueFalsePattern : FluentAssertionPattern
{
    public override string Id => "fa.be-true-false";

    public override string Description => "x.Should().BeTrue() → x.ShouldBeTrue() (bool? → x.ShouldBe(true))";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeTrue", "BeFalse");

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        ITypeSymbol? type = context.Model.GetTypeInfo(context.Subject).Type;
        bool isBool = type?.SpecialType == SpecialType.System_Boolean;
        bool isNullableBool = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [{ SpecialType: SpecialType.System_Boolean }] };
        if (!isBool && !isNullableBool)
        {
            return PatternResult.Manual("The type of the subject is unknown or not bool/bool?.");
        }

        bool value = context.Name == "BeTrue";
        ExpressionSyntax receiver = context.Subject.WithoutTrailingTrivia();
        // Shouldly has no bool? overload for ShouldBeTrue/ShouldBeFalse; ShouldBe(true) also fails on null, like FluentAssertions.
        if (isNullableBool)
        {
            ArgumentSyntax expected = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression));
            return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(receiver, "ShouldBe", expected, context.Because));
        }

        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(receiver, value ? "ShouldBeTrue" : "ShouldBeFalse", context.Because));
    }
}
