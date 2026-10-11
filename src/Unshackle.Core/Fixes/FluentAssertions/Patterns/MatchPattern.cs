using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Probed against FluentAssertions 7.2.2: Match's wildcards are case-sensitive, `*` and `?` also match line breaks, and the
// end may be followed by one trailing line break; `(?s)\A…$` gives the same verdict on every probed case. MatchRegex
// searches like ShouldMatch (Regex.IsMatch), but fails on an empty pattern where ShouldMatch passes.
public class MatchPattern : FluentAssertionPattern
{
    private static readonly Dictionary<string, string> s_methods = new(StringComparer.Ordinal)
    {
        ["Match"] = "ShouldMatch",
        ["NotMatch"] = "ShouldNotMatch",
        ["MatchRegex"] = "ShouldMatch",
        ["NotMatchRegex"] = "ShouldNotMatch",
    };

    public override string Id => "fa.match";

    public override string Description => "s.Should().MatchRegex(r) → s.ShouldMatch(r), Match(\"SO-*\") → s.ShouldMatch(@\"(?s)\\ASO-.*$\")";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, [.. s_methods.Keys]);

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        string name = context.Name;
        if (context.Model.GetTypeInfo(context.Subject).Type?.SpecialType != SpecialType.System_String)
        {
            return PatternResult.Manual($"{name}(...) on a non-string subject (e.g. a predicate on an object): rewrite it by hand.");
        }

        if (context.Values.Count != 1 || context.Model.GetConstantValue(context.Values[0].Expression) is not { Value: string { Length: > 0 } pattern })
        {
            return PatternResult.Manual($"{name}(...) without one non-empty string constant (a Regex, a variable or an occurrence constraint): FluentAssertions fails on an empty pattern, Shouldly does not.");
        }

        string regex = name.EndsWith("Regex", StringComparison.Ordinal)
            ? pattern
            : @"(?s)\A" + Regex.Escape(pattern).Replace(@"\*", ".*", StringComparison.Ordinal).Replace(@"\?", ".", StringComparison.Ordinal) + "$";
        ArgumentSyntax value = regex == pattern
            ? context.Values[0]
            : SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(regex)));
        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(
            FluentAssertionsSyntax.NotNull(context.Subject, context.Model).WithoutTrivia(), s_methods[name], value, context.Because));
    }
}
