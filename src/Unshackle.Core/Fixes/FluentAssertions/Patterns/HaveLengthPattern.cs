using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class HaveLengthPattern : FluentAssertionPattern
{
    public override string Id => "fa.have-length";

    public override string Description => "s.Should().HaveLength(n) → s.Length.ShouldBe(n)";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "HaveLength");

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        SemanticModel model = context.Model;
        InvocationExpressionSyntax assertion = context.Assertion;
        ExpressionSyntax subject = context.Subject;
        if (subject is ConditionalAccessExpressionSyntax)
        {
            return PatternResult.Manual("The subject uses ?. so the length cannot be read directly.");
        }

        if (model.GetSymbolInfo(assertion).Symbol?.ContainingType?.Name != "StringAssertions")
        {
            return PatternResult.Manual("HaveLength(...) on a stream: rewrite to Length.ShouldBe(n) by hand.");
        }

        // A null string fails both: FluentAssertions reports it, ShouldNotBeNull (nullable) or the NullReferenceException does.
        ExpressionSyntax receiver = FluentAssertionsSyntax.NotNullReceiver(subject, model);
        ExpressionSyntax length = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName("Length"));
        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, length, SyntaxFactory.IdentifierName("ShouldBe")),
            assertion.ArgumentList));
    }
}
