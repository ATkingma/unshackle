using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class StringStartEndPattern : FluentAssertionPattern
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

    public override string Id => "fa.string-start-end";

    public override string Description => "s.Should().StartWith(x) → s.ShouldStartWith(x, Case.Sensitive), NotBeNullOrEmpty() → ShouldNotBeNullOrEmpty()";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, [.. s_affixes.Keys, .. s_nullOr.Keys]);

    // Before the argument split: the collection overloads may take params.
    protected override PatternResult? RewriteBeforeArguments(AssertionContext context) =>
        context.Model.GetSymbolInfo(context.Assertion).Symbol?.ContainingType?.Name != "StringAssertions"
            ? PatternResult.Manual($"{context.Name}(...) on a non-string subject (e.g. a collection) has no Shouldly string equivalent.")
            : null;

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        string name = context.Name;
        IReadOnlyList<ArgumentSyntax> values = context.Values;
        ArgumentSyntax? because = context.Because;
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

        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(context.Subject.WithoutTrailingTrivia(), shouldName, arguments));
    }
}
