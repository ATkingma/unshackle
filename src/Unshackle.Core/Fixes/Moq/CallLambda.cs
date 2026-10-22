using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// A Moq Returns/Callback function (moq-queue.md rows 7-8) as an NSubstitute CallInfo function: `(int id, string name) => body` becomes
// `ci => body'` where each parameter is `ci.ArgAt<T>(i)`. Moq passes the call's arguments in order and NSubstitute's ArgAt reads them,
// so both see the same values; a parameter that is assigned, passed by ref or captured by an async lambda is Manual.
public static class CallLambda
{
    public static MockCall Convert(ExpressionSyntax function, IReadOnlyList<IParameterSymbol> mockedParameters, SemanticModel model)
    {
        if (function is not LambdaExpressionSyntax lambda || lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword))
        {
            return MockCall.Blocked(MoqReason.ReturnsLambda, "a method group, anonymous method or async function; only a plain lambda is rewritten");
        }

        IReadOnlyList<ParameterSyntax> parameters = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => [simple.Parameter],
            ParenthesizedLambdaExpressionSyntax parenthesized => [.. parenthesized.ParameterList.Parameters],
            _ => [],
        };
        if (parameters.Count != 0 && parameters.Count != mockedParameters.Count)
        {
            return MockCall.Blocked(MoqReason.ReturnsLambda, $"the function takes {parameters.Count} parameter(s), the mocked member {mockedParameters.Count}");
        }

        string ci = Name(lambda);
        var replacements = new Dictionary<SyntaxNode, SyntaxNode>();
        for (int i = 0; i < parameters.Count; i++)
        {
            if (model.GetDeclaredSymbol(parameters[i]) is not IParameterSymbol symbol)
            {
                return MockCall.Blocked(MoqReason.ReturnsLambda, "a function parameter without a symbol");
            }

            string type = symbol.Type.ToMinimalDisplayString(model, lambda.SpanStart);
            foreach (IdentifierNameSyntax use in lambda.Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                .Where(id => id.Identifier.Text == symbol.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(id).Symbol, symbol)))
            {
                if (use.Parent is AssignmentExpressionSyntax assignment && assignment.Left == use
                    || use.Parent is PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax
                    || use.Parent is ArgumentSyntax { RefKindKeyword.RawKind: not 0 })
                {
                    return MockCall.Blocked(MoqReason.ReturnsLambda, $"the function changes its parameter {symbol.Name}");
                }

                replacements[use] = SyntaxFactory.ParseExpression($"{ci}.ArgAt<{type}>({i})").WithTriviaFrom(use);
            }
        }

        CSharpSyntaxNode body = lambda.Body.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]);
        return MockCall.Done(SyntaxFactory.SimpleLambdaExpression(SyntaxFactory.Parameter(SyntaxFactory.Identifier(ci)), body.WithoutTrivia())
            .WithArrowToken(SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space)));
    }

    // `ci`, unless the body already uses that name.
    private static string Name(LambdaExpressionSyntax lambda)
    {
        HashSet<string> used = [.. lambda.Body.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.Text)];
        string name = "ci";
        for (int i = 1; used.Contains(name); i++)
        {
            name = "callInfo" + (i == 1 ? string.Empty : i.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        return name;
    }
}
