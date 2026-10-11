using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeOfTypePattern : FluentAssertionPattern
{
    private static readonly Dictionary<string, string> s_methods = new(StringComparer.Ordinal)
    {
        ["BeOfType"] = "ShouldBeOfType",
        ["BeAssignableTo"] = "ShouldBeAssignableTo",
        ["NotBeOfType"] = "ShouldNotBeOfType",
        ["NotBeAssignableTo"] = "ShouldNotBeAssignableTo",
    };

    public override string Id => "fa.be-of-type";

    public override string Description => "x.Should().BeOfType<T>()[.Which] → x.ShouldBeOfType<T>()";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, [.. s_methods.Keys]).Select(FluentAssertionsSyntax.WithWhich);

    // `var dto = x.Should().BeOfType<T>().Which;`: Shouldly's method returns the cast value itself.
    protected override InvocationExpressionSyntax GetAssertion(SyntaxNode node) => FluentAssertionsSyntax.AssertionOfWhich(node);

    protected override bool IsResultDiscarded(AssertionContext context) => FluentAssertionsSyntax.IsResultDiscardedOrWhich(context);

    protected override string ResultUsedReason(string name) => FluentAssertionsSyntax.WhichUsedReason(name);

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        string name = context.Name;
        if (context.Model.GetSymbolInfo(context.Assertion).Symbol?.ContainingNamespace.ToDisplayString() == "FluentAssertions.Types")
        {
            return PatternResult.Manual($"{name}(...) on a Type asserts the type itself; Shouldly has no equivalent.");
        }

        var access = (MemberAccessExpressionSyntax)context.Assertion.Expression;
        if (context.Values.Count != (access.Name is GenericNameSyntax ? 0 : 1))
        {
            return PatternResult.Manual($"{name}(...) with unexpected arguments: rewrite it by hand.");
        }

        SimpleNameSyntax shouldName = access.Name is GenericNameSyntax generic
            ? generic.WithIdentifier(SyntaxFactory.Identifier(s_methods[name]))
            : SyntaxFactory.IdentifierName(s_methods[name]);
        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, context.Subject.WithoutTrailingTrivia(), shouldName.WithoutTrivia()),
            FluentAssertionsSyntax.Arguments([.. context.Values, context.Because])));
    }
}
