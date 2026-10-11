using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Shared by ThrowPattern and ThrowAsyncPattern: the Shouldly form of FluentAssertions' WithMessage wildcard.
internal static class ThrowMessage
{
    // `thrown.Message.ShouldX(literal, option[, because])` for the wildcard pattern `[*]literal[*]`; null for other wildcards.
    // FluentAssertions matches the whole message against a case-insensitive wildcard pattern (* and ?).
    public static ExpressionSyntax? Check(ExpressionSyntax thrown, string pattern, ArgumentSyntax? because)
    {
        bool start = pattern.StartsWith('*');
        bool end = pattern.EndsWith('*') && pattern.Length > 1;
        string literal = pattern.Trim('*');
        if (literal.Length == 0 || literal.AsSpan().IndexOfAny('*', '?') >= 0)
        {
            return null;
        }

        ExpressionSyntax text = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(literal));
        (string method, string option) = (start, end) switch
        {
            (true, true) => ("ShouldContain", "Shouldly.Case.Insensitive"),
            (false, true) => ("ShouldStartWith", "Shouldly.Case.Insensitive"),
            (true, false) => ("ShouldEndWith", "Shouldly.Case.Insensitive"),
            _ => ("ShouldBe", "Shouldly.StringCompareShould.IgnoreCase"),
        };

        ArgumentSyntax optionArgument = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(option));
        ArgumentSyntax[] messageArguments = method == "ShouldBe"
            ? [SyntaxFactory.Argument(text), .. because is null ? Array.Empty<ArgumentSyntax>() : [because], optionArgument]
            : [SyntaxFactory.Argument(text), optionArgument, .. because is null ? Array.Empty<ArgumentSyntax>() : [because]];

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, thrown, SyntaxFactory.IdentifierName("Message")),
                SyntaxFactory.IdentifierName(method)),
            Arguments(messageArguments, null));
    }

    // Arguments separated by ", ": every argument after the first gets a leading space.
    public static ArgumentListSyntax Arguments(ArgumentSyntax[] first, ArgumentSyntax? because) =>
        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            (because is null ? first : [.. first, because])
                .Select((a, i) => i == 0 ? a : a.WithLeadingTrivia(SyntaxFactory.Space))));
}
