using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeOfTypePattern : IPattern
{
    private static readonly Dictionary<string, string> s_methods = new(StringComparer.Ordinal)
    {
        ["BeOfType"] = "ShouldBeOfType",
        ["BeAssignableTo"] = "ShouldBeAssignableTo",
        ["NotBeOfType"] = "ShouldNotBeOfType",
        ["NotBeAssignableTo"] = "ShouldNotBeAssignableTo",
    };

    public string Id => "fa.be-of-type";

    public string Description => "x.Should().BeOfType<T>()[.Which] → x.ShouldBeOfType<T>()";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, [.. s_methods.Keys])
            .Select<InvocationExpressionSyntax, SyntaxNode>(i => i.Parent is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Which" or "Subject",
                Parent: EqualsValueClauseSyntax,
            } which
                ? which
                : i);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        // `var dto = x.Should().BeOfType<T>().Which;`: Shouldly's method returns the cast value itself.
        var which = node as MemberAccessExpressionSyntax;
        var assertion = (InvocationExpressionSyntax)(which?.Expression ?? node);
        var access = (MemberAccessExpressionSyntax)assertion.Expression;
        string name = access.Name.Identifier.ValueText;
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual($"{name}(...) is not in the form x.Should().{name}(...), e.g. after .And.");
        }

        if (which is null
            ? !FluentAssertionsSyntax.IsResultDiscarded(assertion, model)
            : which.Parent is not EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Variables.Count: 1 } } })
        {
            return PatternResult.Manual($"The result of {name}(...) is used beyond `var x = ….Which;` (.And/.Which chain): rewrite it by hand.");
        }

        if (model.GetSymbolInfo(assertion).Symbol?.ContainingNamespace.ToDisplayString() == "FluentAssertions.Types")
        {
            return PatternResult.Manual($"{name}(...) on a Type asserts the type itself; Shouldly has no equivalent.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because))
        {
            return PatternResult.Manual($"{name}(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        if (values.Count != (access.Name is GenericNameSyntax ? 0 : 1))
        {
            return PatternResult.Manual($"{name}(...) with unexpected arguments: rewrite it by hand.");
        }

        SimpleNameSyntax shouldName = access.Name is GenericNameSyntax generic
            ? generic.WithIdentifier(SyntaxFactory.Identifier(s_methods[name]))
            : SyntaxFactory.IdentifierName(s_methods[name]);
        ArgumentSyntax[] arguments = [.. values, .. because is null ? [] : new[] { because }];
        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, subject.WithoutTrailingTrivia(), shouldName.WithoutTrivia()),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                arguments.Select((a, i) => i == 0 ? a.WithoutTrivia() : a.WithoutTrivia().WithLeadingTrivia(SyntaxFactory.Space))))));
    }
}
