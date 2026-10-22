using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// The uses of a mock other than .Object that a pattern of this fix rewrites (one rule per queue row). Null: the use is rewritten;
// otherwise why the mock stays Moq. A rule adds the members it sets up for every argument to setUp (no pin for them).
public static class MockUseRules
{
    public static string? Blocker(ExpressionSyntax use, SemanticModel model, List<ISymbol> setUp)
    {
        string shown = (use.Parent as MemberAccessExpressionSyntax)?.ToString() ?? use.Parent?.ToString() ?? use.ToString();
        return $"MockUsedBeyondObject: {use} is used as a Mock<T> (`{Shorten(shown)}`); that use needs a later moq-queue.md row";
    }

    private static string Shorten(string text)
    {
        string line = text.ReplaceLineEndings(" ");
        return line.Length <= 60 ? line : line[..57] + "...";
    }
}
