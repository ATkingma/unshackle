using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Shared by ThrowPattern and ThrowAsyncPattern: the Shouldly form of FluentAssertions' WithMessage wildcard.
internal static class ThrowMessage
{
    // `thrown.Message.ShouldX(literal, option[, because])` for the wildcard pattern `[*]literal[*]`; Manual otherwise.
    // FluentAssertions matches the whole message against a case-insensitive wildcard pattern (* and ?) and ignores
    // line-ending differences (\r\n vs \n); of the Shouldly forms only ShouldBe can ignore line endings too.
    public static PatternResult Check(ExpressionSyntax thrown, string pattern, ArgumentSyntax? because)
    {
        bool start = pattern.StartsWith('*');
        bool end = pattern.EndsWith('*') && pattern.Length > 1;
        string literal = pattern.Trim('*');
        if (literal.Length == 0 || literal.AsSpan().IndexOfAny('*', '?') >= 0)
        {
            return PatternResult.Manual("WithMessage(...) pattern has wildcards other than a leading or trailing *: use ShouldMatch with a regex by hand.");
        }

        bool lineBreak = literal.AsSpan().IndexOfAny('\r', '\n') >= 0;
        if (lineBreak && (start || end))
        {
            return PatternResult.Manual("WithMessage(...) pattern contains a line break: FluentAssertions ignores line-ending differences, Shouldly's ShouldContain/ShouldStartWith/ShouldEndWith do not.");
        }

        ExpressionSyntax text = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(literal));
        (string method, string option) = (start, end) switch
        {
            (true, true) => ("ShouldContain", "Shouldly.Case.Insensitive"),
            (false, true) => ("ShouldStartWith", "Shouldly.Case.Insensitive"),
            (true, false) => ("ShouldEndWith", "Shouldly.Case.Insensitive"),
            _ when lineBreak => ("ShouldBe", "Shouldly.StringCompareShould.IgnoreCase | Shouldly.StringCompareShould.IgnoreLineEndings"),
            _ => ("ShouldBe", "Shouldly.StringCompareShould.IgnoreCase"),
        };

        ArgumentSyntax optionArgument = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(option));
        ArgumentSyntax[] messageArguments = method == "ShouldBe"
            ? [SyntaxFactory.Argument(text), .. because is null ? Array.Empty<ArgumentSyntax>() : [because], optionArgument]
            : [SyntaxFactory.Argument(text), optionArgument, .. because is null ? Array.Empty<ArgumentSyntax>() : [because]];

        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, thrown, SyntaxFactory.IdentifierName("Message")),
                SyntaxFactory.IdentifierName(method)),
            Arguments(messageArguments, null)));
    }

    // Arguments separated by ", ": every argument after the first gets a leading space.
    public static ArgumentListSyntax Arguments(ArgumentSyntax[] first, ArgumentSyntax? because) =>
        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            (because is null ? first : [.. first, because])
                .Select((a, i) => i == 0 ? a : a.WithLeadingTrivia(SyntaxFactory.Space))));
}
