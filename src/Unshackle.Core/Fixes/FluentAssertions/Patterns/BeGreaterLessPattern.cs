using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class BeGreaterLessPattern : IPattern
{
    // FluentAssertions name → (Shouldly name, number of value arguments).
    private static readonly Dictionary<string, (string Should, int Values)> s_methods = new(StringComparer.Ordinal)
    {
        ["BeGreaterThan"] = ("ShouldBeGreaterThan", 1),
        ["BeGreaterThanOrEqualTo"] = ("ShouldBeGreaterThanOrEqualTo", 1),
        ["BeGreaterOrEqualTo"] = ("ShouldBeGreaterThanOrEqualTo", 1),
        ["BeLessThan"] = ("ShouldBeLessThan", 1),
        ["BeLessThanOrEqualTo"] = ("ShouldBeLessThanOrEqualTo", 1),
        ["BeLessOrEqualTo"] = ("ShouldBeLessThanOrEqualTo", 1),
        ["BeInRange"] = ("ShouldBeInRange", 2),
        ["NotBeInRange"] = ("ShouldNotBeInRange", 2),
        ["BePositive"] = ("ShouldBePositive", 0),
        ["BeNegative"] = ("ShouldBeNegative", 0),
    };

    // The numeric types Shouldly has ShouldBePositive/ShouldBeNegative for.
    private static readonly SpecialType[] s_signTypes =
        [SpecialType.System_Int16, SpecialType.System_Int32, SpecialType.System_Int64, SpecialType.System_Decimal, SpecialType.System_Double, SpecialType.System_Single];

    public string Id => "fa.be-greater-less";

    public string Description => "x.Should().BeGreaterThan(n)[.And.BeLessThan(m)] → x.ShouldBeGreaterThan(n); x.ShouldBeLessThan(m);";

    // The node is the invocation, or for an `.And` chain the whole statement, which is split into one statement per link.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, [.. s_methods.Keys])
            .Select<InvocationExpressionSyntax, SyntaxNode>(i =>
            {
                InvocationExpressionSyntax top = i;
                while (top.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "And" } and
                    && and.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax next } call
                    && next.Expression == call)
                {
                    top = next;
                }

                bool chained = top != i || FluentAssertionsSyntax.GetSubject(top) is null;
                return chained && top.Parent is ExpressionStatementSyntax statement ? statement : top;
            })
            .Distinct();

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var statement = node as ExpressionStatementSyntax;
        var top = (InvocationExpressionSyntax)(statement?.Expression ?? node);

        // Walk down the `.And` chain to `x.Should()`; links ends up in source order.
        var links = new List<InvocationExpressionSyntax>();
        ExpressionSyntax? subject = null;
        for (InvocationExpressionSyntax? current = top; current is not null;)
        {
            links.Insert(0, current);
            subject = FluentAssertionsSyntax.GetSubject(current);
            current = subject is null && current.Expression is MemberAccessExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "And", Expression: InvocationExpressionSyntax previous },
            }
                ? previous
                : null;
        }

        if (subject is null)
        {
            return PatternResult.Manual("The assertion is not in the form x.Should().Method(...), e.g. after .And.");
        }

        if (links.Any(l => l.Expression is not MemberAccessExpressionSyntax access || !s_methods.ContainsKey(access.Name.Identifier.ValueText)))
        {
            return PatternResult.Manual("The .And chain contains an assertion that is not a comparison: fix it with its own pattern and run again.");
        }

        if (links.Count == 1
            ? !FluentAssertionsSyntax.IsResultDiscarded(top, model)
            : statement is null)
        {
            return PatternResult.Manual("The result of the assertion is used (.And/.Which or a value); Shouldly's methods return nothing.");
        }

        if (links.Count > 1)
        {
            if (statement!.Parent is not (BlockSyntax or SwitchSectionSyntax))
            {
                return PatternResult.Manual("The .And chain cannot be split: the statement is not directly in a block.");
            }

            if (!IsSimple(subject))
            {
                return PatternResult.Manual("The .And chain subject is not a plain name or member access, so splitting would evaluate it twice.");
            }
        }

        ITypeSymbol? type = model.GetTypeInfo(subject).Type;
        if (type is null or IErrorTypeSymbol || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return PatternResult.Manual("The subject is nullable or has an unknown type: Shouldly's comparisons need a non-nullable IComparable<T>.");
        }

        if (!type.AllInterfaces.Any(i => i.OriginalDefinition.SpecialType == SpecialType.None
                && i.OriginalDefinition.Name == "IComparable" && i.TypeArguments.Length == 1
                && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], type)))
        {
            return PatternResult.Manual("The subject type does not implement IComparable<T> of itself, which Shouldly requires.");
        }

        var invocations = new List<InvocationExpressionSyntax>();
        foreach (InvocationExpressionSyntax link in links)
        {
            (string shouldName, int count) = s_methods[((MemberAccessExpressionSyntax)link.Expression).Name.Identifier.ValueText];
            if (!FluentAssertionsSyntax.TryGetArguments(link, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because)
                || values.Count != count)
            {
                return PatternResult.Manual("An assertion with becauseArgs or named arguments: Shouldly cannot format the message.");
            }

            if (count == 0 && !s_signTypes.Contains(type.SpecialType))
            {
                return PatternResult.Manual($"{shouldName} only exists for int, long, short, decimal, double and float.");
            }

            ArgumentSyntax[] arguments = [.. values, .. because is null ? [] : new[] { because }];
            invocations.Add(SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    subject.WithoutTrivia(),
                    SyntaxFactory.IdentifierName(shouldName)),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
                    arguments.Select((a, i) => i == 0 ? a.WithoutTrivia() : a.WithoutTrivia().WithLeadingTrivia(SyntaxFactory.Space))))));
        }

        return links.Count == 1
            ? PatternResult.Fixed(invocations[0])
            : PatternResult.Fixed(invocations.Select(i => SyntaxFactory.ExpressionStatement(i)));
    }

    // Names, `this` and member accesses on those: safe to evaluate twice (shortcut: property getters are assumed pure).
    private static bool IsSimple(ExpressionSyntax e) => e switch
    {
        IdentifierNameSyntax or ThisExpressionSyntax => true,
        MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression, Name: IdentifierNameSyntax } access => IsSimple(access.Expression),
        _ => false,
    };
}
