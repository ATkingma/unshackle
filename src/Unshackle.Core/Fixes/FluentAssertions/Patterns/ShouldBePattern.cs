using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Also the base of NotBePattern: the same checks, only the Shouldly name differs.
public class ShouldBePattern : FluentAssertionPattern
{
    public override string Id => "fa.should-be";

    public override string Description => "x.Should().Be(y) → x.ShouldBe(y)";

    protected virtual string ShouldName => "ShouldBe";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "Be");

    // No NotNull(subject): ShouldBe<T> accepts a nullable T, and a null subject passes FluentAssertions' NotBe
    // while ShouldNotBeNull would make it fail.
    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        SemanticModel model = context.Model;
        InvocationExpressionSyntax assertion = context.Assertion;

        // XElement/XDocument and MethodInfoSelector Be(...) do not compare with Equals like ShouldBe does.
        string? assertionsNamespace = model.GetSymbolInfo(assertion).Symbol?.ContainingNamespace.ToDisplayString();
        if (assertionsNamespace is "FluentAssertions.Xml" or "FluentAssertions.Types")
        {
            return PatternResult.Manual($"{context.Name}(...) from {assertionsNamespace} has different equality semantics than {ShouldName}.");
        }

        if (context.Values.Count != 1)
        {
            return PatternResult.Manual($"{context.Name}(...) with a comparer: pass it to {ShouldName} by hand.");
        }

        ExpressionSyntax expected = context.Values[0].Expression;
        ITypeSymbol? subjectType = model.GetTypeInfo(context.Subject).Type;
        if (subjectType is null or IErrorTypeSymbol)
        {
            return PatternResult.Manual("The type of the subject is unknown.");
        }

        ExpressionSyntax? newExpected = FluentAssertionsSyntax.ConvertExpected(expected, subjectType, model, assertion.SpanStart);
        if (newExpected is null)
        {
            return PatternResult.Manual(
                $"Expected type {model.GetTypeInfo(expected).Type} differs from subject type {subjectType}; {ShouldName} cannot infer one type.");
        }

        ArgumentListSyntax arguments = FluentAssertionsSyntax.Arguments(context.Values[0].WithExpression(newExpected), context.Because);
        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                context.Subject.WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName(ShouldName)),
            arguments));
    }
}
