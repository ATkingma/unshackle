using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeTrueFalsePattern : IPattern
{
    public string Id => "fa.be-true-false";

    public string Description => "x.Should().BeTrue() → x.ShouldBeTrue() (bool? → x.ShouldBe(true))";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeTrue", "BeFalse");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var assertion = (InvocationExpressionSyntax)node;
        string name = ((MemberAccessExpressionSyntax)assertion.Expression).Name.Identifier.ValueText;
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual($"{name}() is not in the form x.Should().{name}(), e.g. after .And.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual($"The result of {name}() is used (.And/.Which or a value); Shouldly returns nothing.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out _, out ArgumentSyntax? because))
        {
            return PatternResult.Manual($"{name}(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        ITypeSymbol? type = model.GetTypeInfo(subject).Type;
        bool isBool = type?.SpecialType == SpecialType.System_Boolean;
        bool isNullableBool = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [{ SpecialType: SpecialType.System_Boolean }] };
        if (!isBool && !isNullableBool)
        {
            return PatternResult.Manual("The type of the subject is unknown or not bool/bool?.");
        }

        bool value = name == "BeTrue";
        ExpressionSyntax receiver = subject.WithoutTrailingTrivia();
        // Shouldly has no bool? overload for ShouldBeTrue/ShouldBeFalse; ShouldBe(true) also fails on null, like FluentAssertions.
        IEnumerable<ArgumentSyntax> arguments = isNullableBool
            ? new[] { SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression)) }
            : [];
        if (because is not null)
        {
            arguments = arguments.Append(isNullableBool ? because.WithLeadingTrivia(SyntaxFactory.Space) : because);
        }

        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                receiver,
                SyntaxFactory.IdentifierName(isNullableBool ? "ShouldBe" : value ? "ShouldBeTrue" : "ShouldBeFalse")),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))));
    }
}
