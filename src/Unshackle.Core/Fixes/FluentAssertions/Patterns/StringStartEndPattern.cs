using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class StringStartEndPattern : IPattern
{
    // Shouldly's StartWith/EndWith family defaults to Case.Insensitive; FluentAssertions' plain names are case-sensitive.
    private static readonly Dictionary<string, (string Shouldly, bool CaseSensitive)> s_affixes = new(StringComparer.Ordinal)
    {
        ["StartWith"] = ("ShouldStartWith", true),
        ["EndWith"] = ("ShouldEndWith", true),
        ["NotStartWith"] = ("ShouldNotStartWith", true),
        ["NotEndWith"] = ("ShouldNotEndWith", true),
        ["StartWithEquivalentOf"] = ("ShouldStartWith", false),
        ["EndWithEquivalentOf"] = ("ShouldEndWith", false),
        ["NotStartWithEquivalentOf"] = ("ShouldNotStartWith", false),
        ["NotEndWithEquivalentOf"] = ("ShouldNotEndWith", false),
    };

    private static readonly Dictionary<string, string> s_nullOr = new(StringComparer.Ordinal)
    {
        ["BeNullOrEmpty"] = "ShouldBeNullOrEmpty",
        ["NotBeNullOrEmpty"] = "ShouldNotBeNullOrEmpty",
        ["BeNullOrWhiteSpace"] = "ShouldBeNullOrWhiteSpace",
        ["NotBeNullOrWhiteSpace"] = "ShouldNotBeNullOrWhiteSpace",
    };

    public string Id => "fa.string-start-end";

    public string Description => "s.Should().StartWith(x) → s.ShouldStartWith(x, Case.Sensitive), NotBeNullOrEmpty() → ShouldNotBeNullOrEmpty()";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, [.. s_affixes.Keys, .. s_nullOr.Keys]);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        var assertion = (InvocationExpressionSyntax)node;
        string name = ((MemberAccessExpressionSyntax)assertion.Expression).Name.Identifier.ValueText;
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual($"{name}(...) is not in the form s.Should().{name}(...), e.g. after .And.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual($"The result of {name}(...) is used (.And or a value); Shouldly's {name} returns nothing.");
        }

        if (model.GetSymbolInfo(assertion).Symbol?.ContainingType?.Name != "StringAssertions")
        {
            return PatternResult.Manual($"{name}(...) on a non-string subject (e.g. a collection) has no Shouldly string equivalent.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because))
        {
            return PatternResult.Manual($"{name}(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        var arguments = new List<ArgumentSyntax>();
        string shouldName;
        if (s_nullOr.TryGetValue(name, out string? nullOrName))
        {
            shouldName = nullOrName;
        }
        else if (values.Count != 1)
        {
            return PatternResult.Manual($"{name}(...) with equivalency options: Shouldly only knows Case.Insensitive.");
        }
        else
        {
            (shouldName, bool caseSensitive) = s_affixes[name];
            // Qualified: user code may have its own type named Case.
            ArgumentSyntax caseArgument = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(
                caseSensitive ? "Shouldly.Case.Sensitive" : "Shouldly.Case.Insensitive"));
            arguments.Add(values[0]);
            if (shouldName == "ShouldNotEndWith")
            {
                // Only ShouldNotEndWith takes (expected, customMessage, caseSensitivity).
                arguments.AddRange([.. because is null ? [] : new[] { because }, .. caseSensitive ? new[] { caseArgument } : []]);
                because = null;
            }
            else if (caseSensitive || because is not null)
            {
                arguments.Add(caseArgument);
            }
        }

        if (because is not null)
        {
            arguments.Add(because);
        }

        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, subject.WithoutTrailingTrivia(), SyntaxFactory.IdentifierName(shouldName)),
            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                arguments.Select((a, i) => i == 0 ? a.WithoutTrivia() : a.WithoutTrivia().WithLeadingTrivia(SyntaxFactory.Space))))));
    }
}
