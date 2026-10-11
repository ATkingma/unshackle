using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class EqualSequencePattern : FluentAssertionPattern
{
    public override string Id => "fa.equal-sequence";

    public override string Description => "x.Should().Equal(seq) / Equal(1, 2) → x.ShouldBe(seq) / x.ShouldBe(new[] { 1, 2 }) (order-sensitive)";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "Equal", "NotEqual");

    // Before the argument split: Equal(params T[] elements) would be reported as becauseArgs.
    protected override PatternResult? RewriteBeforeArguments(AssertionContext context)
    {
        SemanticModel model = context.Model;
        InvocationExpressionSyntax assertion = context.Assertion;
        if (model.GetSymbolInfo(assertion).Symbol is not IMethodSymbol { ContainingType.Name: "GenericCollectionAssertions" or "StringCollectionAssertions" } method)
        {
            return PatternResult.Manual($"{context.Name}(...) on a dictionary compares without order; check ShouldBe or ShouldBeEquivalentTo by hand.");
        }

        if (context.Name == "NotEqual")
        {
            return PatternResult.Manual("NotEqual(...): Shouldly has no ShouldNotBe for sequences; e.g. x.SequenceEqual(y).ShouldBeFalse().");
        }

        // Equal(params T[] elements): build the array FluentAssertions builds, typed so ShouldBe infers the element type.
        if (method.Parameters is [{ IsParams: true, Type: IArrayTypeSymbol arrayType }]
            && assertion.ArgumentList.Arguments.All(a => a.NameColon is null)
            && !(assertion.ArgumentList.Arguments.Count == 1
                && SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(assertion.ArgumentList.Arguments[0].Expression).ConvertedType, arrayType)
                && model.GetTypeInfo(assertion.ArgumentList.Arguments[0].Expression).Type is IArrayTypeSymbol))
        {
            // Equal(["a", "b"]): the collection expression is the array itself, not one element of it (TAAK-113).
            IEnumerable<ExpressionSyntax> values = assertion.ArgumentList.Arguments.Select(a => a.Expression);
            if (assertion.ArgumentList.Arguments is [{ Expression: CollectionExpressionSyntax collection }])
            {
                if (!collection.Elements.All(e => e is ExpressionElementSyntax))
                {
                    return PatternResult.Manual("Equal([...]) with a spread element: write the expected sequence as an array by hand.");
                }

                values = collection.Elements.Cast<ExpressionElementSyntax>().Select(e => e.Expression);
            }

            string elements = string.Join(", ", values.Select(v => v.WithoutTrivia().ToString()));
            string type = arrayType.ToMinimalDisplayString(model, assertion.SpanStart);
            string text = $"new {type} {{ {elements}{(elements.Length == 0 ? "" : " ")}}}";
            ArgumentListSyntax list = assertion.ArgumentList;
            if (list.Arguments is not [{ Expression: CollectionExpressionSyntax }] && list.Arguments.Count > 0
                && list.Arguments.GetWithSeparators().Any(n => n.ToFullString().Contains('\n', StringComparison.Ordinal)))
            {
                // One argument per line (or comments between them): keep the user's lines and comments, braces on their own lines at the
                // statement's indentation and a trailing comma (StyleCop SA1500/SA1413), instead of one long line (TAAK-153).
                string source = list.SyntaxTree.GetText().ToString();
                string indentation = string.Concat(assertion.FirstAncestorOrSelf<StatementSyntax>()?.GetLeadingTrivia()
                    .Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Select(t => t.ToString()) ?? []);
                string newLine = list.OpenParenToken.TrailingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString() is { Length: > 0 } eol ? eol : "\n";
                string lines = source[list.OpenParenToken.Span.End..list.Arguments[^1].Span.End].Trim(' ', '\t').TrimStart('\r', '\n');
                string tail = source[list.Arguments[^1].Span.End..list.CloseParenToken.SpanStart].TrimEnd();
                text = $"new {type}{newLine}{indentation}{{{newLine}{lines},{tail}{newLine}{indentation}}}";
            }

            ExpressionSyntax array = SyntaxFactory.ParseExpression(text);
            return Fixed(context, SyntaxFactory.Argument(array));
        }

        if (method.Parameters.Any(p => p.Type is INamedTypeSymbol { DelegateInvokeMethod: not null }))
        {
            return PatternResult.Manual("Equal(...) with a predicate: pass an IEqualityComparer to ShouldBe by hand.");
        }

        return null;
    }

    // The only sequence ShouldBe with a message is (expected, ignoreOrder, customMessage).
    protected override PatternResult RewriteAssertion(AssertionContext context) =>
        context.Because is null
            ? Fixed(context, context.Values[0])
            : Fixed(context, context.Values[0], SyntaxFactory.ParseArgumentList("(ignoreOrder: false)").Arguments[0], context.Because);

    private static PatternResult Fixed(AssertionContext context, params IEnumerable<ArgumentSyntax> arguments) =>
        PatternResult.Fixed(FluentAssertionsSyntax.Invoke(context.Subject.WithoutTrailingTrivia(), "ShouldBe", arguments));
}
