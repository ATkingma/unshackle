using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class NotBePattern : IPattern
{
    public string Id => "fa.not-be";

    public string Description => "x.Should().NotBe(y) → x.ShouldNotBe(y)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "NotBe");

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
            return PatternResult.Manual("NotBe(...) is not in the form x.Should().NotBe(y), e.g. after .And.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual("The result of NotBe(...) is used (.And/.Which or a value); ShouldNotBe returns nothing.");
        }

        string? assertionsNamespace = model.GetSymbolInfo(assertion).Symbol?.ContainingNamespace.ToDisplayString();
        if (assertionsNamespace is "FluentAssertions.Xml" or "FluentAssertions.Types")
        {
            return PatternResult.Manual($"NotBe(...) from {assertionsNamespace} has different equality semantics than ShouldNotBe.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out _))
        {
            return PatternResult.Manual("NotBe(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        if (values.Count != 1)
        {
            return PatternResult.Manual("NotBe(...) with a comparer: pass it to ShouldNotBe by hand.");
        }

        ExpressionSyntax unexpected = values[0].Expression;
        ITypeSymbol? subjectType = model.GetTypeInfo(subject).Type;
        if (subjectType is null or IErrorTypeSymbol)
        {
            return PatternResult.Manual("The type of the subject is unknown.");
        }

        // No NotNull(subject): a null subject passes FluentAssertions' NotBe, ShouldNotBeNull would make it fail.
        ExpressionSyntax? newUnexpected = ShouldBePattern.ConvertExpected(unexpected, subjectType, model, assertion.SpanStart);
        if (newUnexpected is null)
        {
            return PatternResult.Manual(
                $"Unexpected type {model.GetTypeInfo(unexpected).Type} differs from subject type {subjectType}; ShouldNotBe cannot infer one type.");
        }

        ArgumentListSyntax arguments = newUnexpected == unexpected
            ? assertion.ArgumentList
            : assertion.ArgumentList.ReplaceNode(unexpected, newUnexpected.WithTriviaFrom(unexpected));
        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                subject.WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName("ShouldNotBe")),
            arguments));
    }
}
