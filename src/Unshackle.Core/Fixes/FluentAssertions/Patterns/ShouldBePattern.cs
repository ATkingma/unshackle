using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class ShouldBePattern : IPattern
{
    public string Id => "fa.should-be";

    public string Description => "x.Should().Be(y) → x.ShouldBe(y)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "Be");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        var assertion = (InvocationExpressionSyntax)node;
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual("Be(...) is not in the form x.Should().Be(y), e.g. after .And.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual("The result of Be(...) is used (.And/.Which or a value); ShouldBe returns nothing.");
        }

        // XElement/XDocument and MethodInfoSelector Be(...) do not compare with Equals like ShouldBe does.
        string? assertionsNamespace = model.GetSymbolInfo(assertion).Symbol?.ContainingNamespace.ToDisplayString();
        if (assertionsNamespace is "FluentAssertions.Xml" or "FluentAssertions.Types")
        {
            return PatternResult.Manual($"Be(...) from {assertionsNamespace} has different equality semantics than ShouldBe.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out _))
        {
            return PatternResult.Manual("Be(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        if (values.Count != 1)
        {
            return PatternResult.Manual("Be(...) with a comparer: pass it to ShouldBe by hand.");
        }

        ExpressionSyntax expected = values[0].Expression;
        ITypeSymbol? subjectType = model.GetTypeInfo(subject).Type;
        if (subjectType is null or IErrorTypeSymbol)
        {
            return PatternResult.Manual("The type of the subject is unknown.");
        }

        ExpressionSyntax? newExpected = ConvertExpected(expected, subjectType, model, assertion.SpanStart);
        if (newExpected is null)
        {
            return PatternResult.Manual(
                $"Expected type {model.GetTypeInfo(expected).Type} differs from subject type {subjectType}; ShouldBe cannot infer one type.");
        }

        ArgumentListSyntax arguments = newExpected == expected
            ? assertion.ArgumentList
            : assertion.ArgumentList.ReplaceNode(expected, newExpected.WithTriviaFrom(expected));
        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                subject.WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName("ShouldBe")),
            arguments));
    }

    // ShouldBe<T>(this T actual, T expected) infers T from both sides, and the receiver allows no numeric or nullable
    // conversion. FluentAssertions converted the argument to the subject type instead, e.g. byte b; b.Should().Be(1).
    // Returns the expected expression (cast when needed), or null when no safe form exists.
    internal static ExpressionSyntax? ConvertExpected(ExpressionSyntax expected, ITypeSymbol subjectType, SemanticModel model, int position)
    {
        ITypeSymbol? expectedType = model.GetTypeInfo(expected).Type;
        if (expectedType is null || SymbolEqualityComparer.Default.Equals(expectedType, subjectType)
            || model.Compilation.ClassifyConversion(expectedType, subjectType).IsImplicit)
        {
            return expected;
        }

        Conversion toExpected = model.Compilation.ClassifyConversion(subjectType, expectedType);
        if (toExpected.IsImplicit && (toExpected.IsReference || toExpected.IsBoxing))
        {
            return expected;
        }

        if (!model.ClassifyConversion(expected, subjectType).IsImplicit)
        {
            return null;
        }

        ExpressionSyntax operand = expected is LiteralExpressionSyntax or IdentifierNameSyntax or MemberAccessExpressionSyntax
            ? expected.WithoutTrivia()
            : SyntaxFactory.ParenthesizedExpression(expected.WithoutTrivia());
        return SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(subjectType.ToMinimalDisplayString(model, position)), operand);
    }
}
