using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// The arguments of a mocked call in a Setup or Verify lambda (moq-queue.md row 2): It.IsAny<T>() → Arg.Any<T>(),
// It.Is<T>(pred) → Arg.Is<T>(pred), It.IsNotNull<T>() → Arg.Is<T>(a => a != null). Once one argument is a matcher, a plain value
// becomes Arg.Is<T>(value): NSubstitute cannot tell a mix of specs and values apart reliably (AmbiguousArgumentsException).
public static class ArgMatchers
{
    public static MockCall Rewrite(ArgumentListSyntax list, IMethodSymbol method, ISymbol lambdaParameter, SemanticModel model)
    {
        if (method.Parameters.Any(p => p.IsParams || p.RefKind != RefKind.None))
        {
            return MockCall.Blocked("RefOutOrParams: the mocked method has a ref, out or params parameter");
        }

        var matchers = new ExpressionSyntax?[list.Arguments.Count];
        for (int i = 0; i < list.Arguments.Count; i++)
        {
            ArgumentSyntax argument = list.Arguments[i];
            if (argument.NameColon is not null)
            {
                return MockCall.Blocked("NamedArgument: a named argument in a mocked call");
            }

            if (argument.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                .Any(id => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(id).Symbol, lambdaParameter)))
            {
                return MockCall.Blocked("ArgumentUsesMock: an argument refers to the mocked object itself");
            }

            if (Matcher(argument.Expression, method.Parameters[i].Type, model, out string? blocker) is { } matcher)
            {
                matchers[i] = matcher;
            }
            else if (blocker is not null)
            {
                return MockCall.Blocked(blocker);
            }
        }

        bool anyMatcher = matchers.Any(m => m is not null);
        IEnumerable<ArgumentSyntax> arguments = list.Arguments.Select((a, i) => matchers[i] is { } m ? a.WithExpression(m.WithTriviaFrom(a.Expression))
            : anyMatcher ? a.WithExpression(SyntaxFactory.ParseExpression(
                $"Arg.Is<{TypeText(method.Parameters[i].Type, model, a.SpanStart)}>({a.Expression.WithoutTrivia()})").WithTriviaFrom(a.Expression))
            : a);
        return MockCall.Done(list.WithArguments(SyntaxFactory.SeparatedList(arguments, list.Arguments.GetSeparators())));
    }

    // The NSubstitute form of a Moq It.* call, or null (blocker null: a plain value; blocker set: cannot be expressed).
    private static ExpressionSyntax? Matcher(ExpressionSyntax expression, ITypeSymbol parameterType, SemanticModel model, out string? blocker)
    {
        blocker = null;
        if (expression is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: var name } } invocation
            || model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { ContainingType: { Name: "It", ContainingNamespace.Name: "Moq" } } matcher)
        {
            if (expression.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i => model.GetSymbolInfo(i).Symbol is IMethodSymbol
                { ContainingType: { Name: "It", ContainingNamespace.Name: "Moq" } }))
            {
                blocker = "MatcherNested: a Moq matcher inside a larger argument expression";
            }

            return null;
        }

        if (matcher.TypeArguments.Length != 1 || !SymbolEqualityComparer.Default.Equals(matcher.TypeArguments[0], parameterType))
        {
            blocker = $"MatcherTypeDiffers: It.{matcher.Name} is not of the parameter type {parameterType.ToDisplayString()}";
            return null;
        }

        string type = name is GenericNameSyntax { TypeArgumentList.Arguments: [var written] } ? written.ToString()
            : TypeText(parameterType, model, expression.SpanStart);
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
        switch (matcher.Name)
        {
            case "IsAny" when arguments.Count == 0:
                return SyntaxFactory.ParseExpression($"Arg.Any<{type}>()");
            case "Is" when arguments is [{ Expression: LambdaExpressionSyntax predicate }]:
                return SyntaxFactory.ParseExpression($"Arg.Is<{type}>({predicate.WithoutTrivia()})");
            case "IsNotNull" when arguments.Count == 0 && (!parameterType.IsValueType || parameterType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T):
                return SyntaxFactory.ParseExpression($"Arg.Is<{type}>(a => a != null)");
            default:
                blocker = $"MatcherUnsupported: It.{matcher.Name}{(arguments.Count > 0 && matcher.Name == "Is" ? " with a non-literal predicate" : string.Empty)} has no proven NSubstitute form";
                return null;
        }
    }

    private static string TypeText(ITypeSymbol type, SemanticModel model, int position) => type.ToMinimalDisplayString(model, position);
}
