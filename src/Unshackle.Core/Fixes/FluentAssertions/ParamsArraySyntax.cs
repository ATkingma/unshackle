using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.FluentAssertions;

// The array expression FluentAssertions builds for a call to a `params T[]` overload, written out so ShouldBe infers the element type.
public static class ParamsArraySyntax
{
    // `new T[] { a, b }` for the arguments of the assertion. One argument per line (or comments between them): the user's lines
    // and comments are kept, braces on their own lines at the statement's indentation and a trailing comma (StyleCop
    // SA1500/SA1413), instead of one long line (TAAK-153).
    public static ExpressionSyntax Create(InvocationExpressionSyntax assertion, IEnumerable<ExpressionSyntax> values, ITypeSymbol arrayType, SemanticModel model)
    {
        string elements = string.Join(", ", values.Select(v => v.WithoutTrivia().ToString()));
        string type = arrayType.ToMinimalDisplayString(model, assertion.SpanStart);
        string text = $"new {type} {{ {elements}{(elements.Length == 0 ? "" : " ")}}}";
        ArgumentListSyntax list = assertion.ArgumentList;
        if (list.Arguments is not [{ Expression: CollectionExpressionSyntax }] && list.Arguments.Count > 0
            && list.Arguments.GetWithSeparators().Any(n => n.ToFullString().Contains('\n', StringComparison.Ordinal)))
        {
            string source = list.SyntaxTree.GetText().ToString();
            string indentation = string.Concat(assertion.FirstAncestorOrSelf<StatementSyntax>()?.GetLeadingTrivia()
                .Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Select(t => t.ToString()) ?? []);
            string newLine = list.OpenParenToken.TrailingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString() is { Length: > 0 } eol ? eol : "\n";
            string lines = source[list.OpenParenToken.Span.End..list.Arguments[^1].Span.End].Trim(' ', '\t').TrimStart('\r', '\n');
            string tail = source[list.Arguments[^1].Span.End..list.CloseParenToken.SpanStart].TrimEnd();
            text = $"new {type}{newLine}{indentation}{{{newLine}{lines},{tail}{newLine}{indentation}}}";
        }

        return SyntaxFactory.ParseExpression(text);
    }
}
