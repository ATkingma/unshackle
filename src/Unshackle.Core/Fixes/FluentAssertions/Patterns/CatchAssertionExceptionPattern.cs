using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Own code that catches or filters the assertion exception (`catch (AssertionFailedException)`, `e is XunitException`), for
// example a retry helper, stops catching once the assertions are Shouldly's. The catch site also gets ShouldAssertException;
// the old type stays, so not yet migrated assertions are still caught.
public class CatchAssertionExceptionPattern : IPattern
{
    private const string ShouldlyException = "ShouldAssertException";

    private static readonly string[] s_assertionExceptions = ["FluentAssertions.Execution.AssertionFailedException", "Xunit.Sdk.XunitException"];

    // Literal for check-pattern-docs.sh.
    public string Id => "fa.catch-assertion-exception";

    public string Description => "catch (AssertionFailedException) / e is XunitException → also catches ShouldAssertException";

    // One node per catch site: the first matching catch of a try, the first matching `is` of a condition. A site that
    // already names ShouldAssertException is done (second run).
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        IEnumerable<SyntaxNode> catches = root.DescendantNodes()
            .OfType<TryStatementSyntax>()
            .Where(t => !t.Catches.Any(c => NamesShouldly(c.Declaration)))
            .Select(t => t.Catches.FirstOrDefault(c => c.Declaration is not null && IsAssertionException(c.Declaration.Type, model)))
            .OfType<CatchClauseSyntax>();
        IEnumerable<SyntaxNode> checks = root.DescendantNodes()
            .Where(n => n is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.IsExpression, Right: TypeSyntax type } && IsAssertionException(type, model)
                || n is IsPatternExpressionSyntax pattern && pattern.Pattern.DescendantNodes().OfType<TypeSyntax>().Any(t => IsAssertionException(t, model)))
            .GroupBy(Condition)
            .Where(g => !NamesShouldly(g.Key))
            .Select(g => g.First());
        return catches.Concat(checks);
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        switch (node)
        {
            case CatchClauseSyntax clause:
                if (clause.Declaration!.Identifier.ValueText is { Length: > 0 } name
                    && clause.DescendantNodes().OfType<IdentifierNameSyntax>().Any(i => i.Identifier.ValueText == name))
                {
                    return PatternResult.Manual(
                        $"catch ({clause.Declaration.Type} {name}) uses the exception: add a catch for Shouldly.ShouldAssertException by hand, "
                        + "Shouldly's assertions throw that.");
                }

                CatchClauseSyntax shouldly = clause.WithDeclaration(clause.Declaration.WithType(SyntaxFactory.IdentifierName(ShouldlyException)));
                return PatternResult.Fixed([clause, shouldly], "Shouldly");

            case BinaryExpressionSyntax { Left: IdentifierNameSyntax subject } check:
                // `e is T || e is ShouldAssertException`; parenthesised only inside another operator than `||`.
                string either = $"{check.WithoutTrivia()} || {subject.Identifier.ValueText} is {ShouldlyException}";
                bool parenthesise = check.Parent is ExpressionSyntax and not ParenthesizedExpressionSyntax
                    and not BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalOrExpression };
                return PatternResult.Fixed(SyntaxFactory.ParseExpression(parenthesise ? $"({either})" : either), "Shouldly");

            default:
                return PatternResult.Manual(
                    $"`{node}` checks for the FluentAssertions/xunit assertion exception: also accept Shouldly.ShouldAssertException by hand, "
                    + "Shouldly's assertions throw that.");
        }
    }

    private static bool IsAssertionException(TypeSyntax type, SemanticModel model) =>
        model.GetSymbolInfo(type).Symbol is ITypeSymbol symbol && s_assertionExceptions.Contains(symbol.ToDisplayString(), StringComparer.Ordinal);

    private static bool NamesShouldly(SyntaxNode? node) =>
        node is not null && node.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(i => i.Identifier.ValueText == ShouldlyException);

    // The whole boolean expression a check belongs to, such as `!(e is A || e is B)`.
    private static SyntaxNode Condition(SyntaxNode check)
    {
        SyntaxNode current = check;
        while (current.Parent is ParenthesizedExpressionSyntax or PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression }
            or BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalOrExpression or (int)SyntaxKind.LogicalAndExpression })
        {
            current = current.Parent;
        }

        return current;
    }
}
