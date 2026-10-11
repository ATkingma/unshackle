using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class HaveLengthPattern : IPattern
{
    public string Id => "fa.have-length";

    public string Description => "s.Should().HaveLength(n) → s.Length.ShouldBe(n)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "HaveLength");

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
            return PatternResult.Manual("HaveLength(...) is not in the form s.Should().HaveLength(n), e.g. after .And.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual("The result of HaveLength(...) is used (.And or a value); ShouldBe returns nothing.");
        }

        if (subject is ConditionalAccessExpressionSyntax)
        {
            return PatternResult.Manual("The subject uses ?. so the length cannot be read directly.");
        }

        if (model.GetSymbolInfo(assertion).Symbol?.ContainingType?.Name != "StringAssertions")
        {
            return PatternResult.Manual("HaveLength(...) on a stream: rewrite to Length.ShouldBe(n) by hand.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out _, out _))
        {
            return PatternResult.Manual("HaveLength(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        // A null string fails both: FluentAssertions reports it, ShouldNotBeNull (nullable) or the NullReferenceException does.
        ExpressionSyntax receiver = HaveCountPattern.IsSafeReceiver(subject)
            ? FluentAssertionsSyntax.NotNull(subject, model).WithoutTrailingTrivia()
            : SyntaxFactory.ParenthesizedExpression(subject.WithoutTrivia());
        ExpressionSyntax length = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName("Length"));
        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, length, SyntaxFactory.IdentifierName("ShouldBe")),
            assertion.ArgumentList));
    }
}
