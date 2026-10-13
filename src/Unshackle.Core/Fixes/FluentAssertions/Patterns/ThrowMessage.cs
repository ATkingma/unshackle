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

    public const string AggregateReason = "The action blocks on a task or runs Parallel.* (Wait/Result/WaitAll/Parallel.For/ForEach/Invoke), which throws AggregateException: FluentAssertions unwraps AggregateException; Shouldly does not.";

    public const string AggregateUsedReason = "The action blocks on a task or runs Parallel.* (Wait/Result/WaitAll/Parallel.For/ForEach/Invoke), which throws AggregateException, and the exception is checked further (And/Which/WithMessage/...): FluentAssertions hands over the unwrapped inner exception, Shouldly the AggregateException.";

    private const int MethodDepth = 2;

    // Heuristic: true when the action (a lambda, a local delegate initialised with one, or a method group), or a method of
    // the solution it calls (two levels deep), visibly calls a member that wraps exceptions in AggregateException, and
    // FluentAssertions would unwrap it: exceptionType is no AggregateException, and when it is a base of AggregateException
    // (Exception) only if the chain uses the exception (FluentAssertions returns the inner one, Shouldly the aggregate; probed).
    // An action that cannot be followed (e.g. a parameter) counts as false. GetAwaiter().GetResult() and WaitAny do not wrap.
    public static bool MayThrowAggregate(ExpressionSyntax action, TypeSyntax exceptionType, bool usesException, SemanticModel model)
    {
        if (model.GetTypeInfo(exceptionType).Type is not ITypeSymbol expected
            || model.Compilation.GetTypeByMetadataName("System.AggregateException") is not { } aggregate
            || model.Compilation.ClassifyConversion(expected, aggregate).IsImplicit
            || model.Compilation.ClassifyConversion(aggregate, expected).IsImplicit && !usesException)
        {
            return false;
        }

        SyntaxNode? body = action is AnonymousFunctionExpressionSyntax ? action : null;
        if (body is null)
        {
            ISymbol? symbol = model.GetSymbolInfo(action).Symbol;
            SyntaxNode? declaration = symbol is ILocalSymbol or IMethodSymbol && symbol.DeclaringSyntaxReferences is [var reference]
                ? reference.GetSyntax()
                : null;
            if (declaration is VariableDeclaratorSyntax { Initializer.Value: var value })
            {
                // A local's initialiser can never refer back to itself, so this recursion ends.
                return MayThrowAggregate(value, exceptionType, usesException, model);
            }

            body = declaration switch
            {
                BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax => declaration,
                _ => null,
            };
        }

        return body is not null && Blocks(body, model, MethodDepth, new HashSet<SyntaxNode>());
    }

    // True when body, or a method with source it calls (up to depth levels), calls a wrapping member (TAAK-220).
    private static bool Blocks(SyntaxNode body, SemanticModel model, int depth, HashSet<SyntaxNode> seen)
    {
        if (!seen.Add(body) || !model.Compilation.ContainsSyntaxTree(body.SyntaxTree))
        {
            return false;
        }

        SemanticModel bodyModel = body.SyntaxTree == model.SyntaxTree ? model : model.Compilation.GetSemanticModel(body.SyntaxTree);
        var called = new List<SyntaxNode>();
        foreach (SimpleNameSyntax name in body.DescendantNodes().OfType<SimpleNameSyntax>())
        {
            ISymbol? symbol = bodyModel.GetSymbolInfo(name).Symbol;
            if (Wraps(symbol))
            {
                return true;
            }

            if (depth > 0
                && symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.LocalFunction or MethodKind.ReducedExtension } method
                && (method.ReducedFrom ?? method).OriginalDefinition.DeclaringSyntaxReferences is [var reference]
                && reference.GetSyntax() is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax)
            {
                called.Add(reference.GetSyntax());
            }
        }

        return called.Any(c => Blocks(c, model, depth - 1, seen));
    }

    private static bool Wraps(ISymbol? symbol) =>
        symbol?.ContainingType?.OriginalDefinition is { ContainingNamespace: { Name: "Tasks", ContainingNamespace: { Name: "Threading", ContainingNamespace.Name: "System" } } } type
        && (type.Name, symbol) switch
        {
            ("Task", IMethodSymbol { Name: "Wait" or "WaitAll" }) => true,
            ("Task", IPropertySymbol { Name: "Result" }) => true,
            ("Parallel", IMethodSymbol { Name: "For" or "ForEach" or "Invoke" }) => true,
            _ => false,
        };

    // `thrown.ShouldBeOfType<T>()`: Should.Throw<T> also accepts derived types, ThrowExactly<T> does not.
    public static ExpressionSyntax OfType(ExpressionSyntax thrown, GenericNameSyntax throwName) =>
        SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            thrown,
            SyntaxFactory.GenericName(SyntaxFactory.Identifier("ShouldBeOfType"), throwName.TypeArgumentList)));

    // Arguments separated by ", ": every argument after the first gets a leading space.
    public static ArgumentListSyntax Arguments(ArgumentSyntax[] first, ArgumentSyntax? because) =>
        SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            (because is null ? first : [.. first, because])
                .Select((a, i) => i == 0 ? a : a.WithLeadingTrivia(SyntaxFactory.Space))));
}
