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
            return MockCall.Blocked(MoqReason.RefOutOrParams, "the mocked method has a ref, out or params parameter");
        }

        var matchers = new ExpressionSyntax?[list.Arguments.Count];
        for (int i = 0; i < list.Arguments.Count; i++)
        {
            ArgumentSyntax argument = list.Arguments[i];
            if (argument.NameColon is not null)
            {
                return MockCall.Blocked(MoqReason.NamedArgument, "a named argument in a mocked call");
            }

            if (argument.Expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                .Any(id => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(id).Symbol, lambdaParameter)))
            {
                return MockCall.Blocked(MoqReason.ArgumentUsesMock, "an argument refers to the mocked object itself");
            }

            if (Matcher(argument.Expression, method.Parameters[i].Type, model, out MoqBlocker? blocker) is { } matcher)
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
        bool forAny = list.Arguments.All(a => model.GetSymbolInfo(a.Expression).Symbol is IMethodSymbol { Name: "IsAny", ContainingType: { Name: "It", ContainingNamespace.Name: "Moq" } });
        return new() { Node = list.WithArguments(SyntaxFactory.SeparatedList(arguments, list.Arguments.GetSeparators())), ForAnyArguments = forAny };
    }

    // One value (the right side of a VerifySet): a matcher becomes its Arg form, a plain value stays.
    public static MockCall Value(ExpressionSyntax value, ITypeSymbol type, ISymbol lambdaParameter, SemanticModel model)
    {
        if (value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(id => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(id).Symbol, lambdaParameter)))
        {
            return MockCall.Blocked(MoqReason.ArgumentUsesMock, "the value refers to the mocked object itself");
        }

        ExpressionSyntax? matcher = Matcher(value, type, model, out MoqBlocker? blocker);
        return blocker is not null ? MockCall.Blocked(blocker) : MockCall.Done((matcher ?? value).WithoutTrivia());
    }

    // The NSubstitute form of a Moq It.* call, or null (blocker null: a plain value; blocker set: cannot be expressed).
    private static ExpressionSyntax? Matcher(ExpressionSyntax expression, ITypeSymbol parameterType, SemanticModel model, out MoqBlocker? blocker)
    {
        blocker = null;

        // Row 12: `(Func<It.IsAnyType, Exception, string>)It.IsAny<object>()` is how Moq matches ILogger.Log's formatter.
        if (expression is CastExpressionSyntax { Expression: InvocationExpressionSyntax castInner } cast
            && model.GetSymbolInfo(castInner).Symbol is IMethodSymbol { Name: "IsAny", TypeArguments: [{ SpecialType: SpecialType.System_Object }], ContainingType: { Name: "It", ContainingNamespace.Name: "Moq" } }
            && castInner.ArgumentList.Arguments.Count == 0 && AnyTypeText(model.GetTypeInfo(cast.Type).Type, model, expression.SpanStart) is { } formatter)
        {
            return SyntaxFactory.ParseExpression($"Arg.Any<{formatter}>()");
        }

        if (expression is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name: var name } } invocation
            || model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { ContainingType: { Name: "It", ContainingNamespace.Name: "Moq" } } matcher)
        {
            if (expression.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i => model.GetSymbolInfo(i).Symbol is IMethodSymbol
                { ContainingType: { Name: "It", ContainingNamespace.Name: "Moq" } }))
            {
                blocker = MoqBlocker.Of(MoqReason.MatcherNested, "a Moq matcher inside a larger argument expression");
            }

            return null;
        }

        if (matcher.TypeArguments.Length != 1 || !SymbolEqualityComparer.Default.Equals(matcher.TypeArguments[0], parameterType))
        {
            blocker = MoqBlocker.Of(MoqReason.MatcherTypeDiffers, $"It.{matcher.Name} is not of the parameter type {parameterType.ToDisplayString()}");
            return null;
        }

        string type = name is GenericNameSyntax { TypeArgumentList.Arguments: [var written] } ? written.ToString()
            : TypeText(parameterType, model, expression.SpanStart);
        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;

        // Row 12: It.IsAnyType is Arg.AnyType; a predicate over it sees the state as an object (measured equal for ToString()).
        if (parameterType is INamedTypeSymbol { Name: "IsAnyType", ContainingType.Name: "It" })
        {
            switch (matcher.Name)
            {
                case "IsAny" when arguments.Count == 0:
                    return SyntaxFactory.ParseExpression("Arg.Any<Arg.AnyType>()");
                case "Is" when arguments is [{ Expression: LambdaExpressionSyntax lambda }] && AnyTypePredicate(lambda) is { } predicateText:
                    return SyntaxFactory.ParseExpression($"Arg.Is<Arg.AnyType>({predicateText})");
                default:
                    blocker = MoqBlocker.Of(MoqReason.MatcherUnsupported, "It.Is<It.IsAnyType> with a predicate other than one that only calls ToString() on the state");
                    return null;
            }
        }

        switch (matcher.Name)
        {
            case "IsAny" when arguments.Count == 0:
                return SyntaxFactory.ParseExpression($"Arg.Any<{type}>()");
            case "Is" when arguments is [{ Expression: LambdaExpressionSyntax predicate }]:
                return SyntaxFactory.ParseExpression($"Arg.Is<{type}>({predicate.WithoutTrivia()})");
            case "IsNotNull" when arguments.Count == 0 && (!parameterType.IsValueType || parameterType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T):
                return SyntaxFactory.ParseExpression($"Arg.Is<{type}>(a => a != null)");
            default:
                blocker = MoqBlocker.Of(MoqReason.MatcherUnsupported, $"It.{matcher.Name}{(arguments.Count > 0 && matcher.Name == "Is" ? " with a non-literal predicate" : string.Empty)} has no proven NSubstitute form");
                return null;
        }
    }

    // `(v, t) => body` / `v => body` where t is unused and v is only the receiver of ToString() → `(object v) => body`; else null.
    private static string? AnyTypePredicate(LambdaExpressionSyntax lambda)
    {
        IReadOnlyList<ParameterSyntax> parameters = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => [simple.Parameter],
            ParenthesizedLambdaExpressionSyntax parenthesized => [.. parenthesized.ParameterList.Parameters],
            _ => [],
        };
        if (parameters.Count is < 1 or > 2 || parameters.Any(p => p.Type is not null) || lambda.ExpressionBody is not { } body)
        {
            return null;
        }

        string state = parameters[0].Identifier.Text;
        IEnumerable<IdentifierNameSyntax> uses = body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>();
        if (parameters.Count == 2 && uses.Any(i => i.Identifier.Text == parameters[1].Identifier.Text))
        {
            return null;
        }

        bool onlyToString = uses.Where(i => i.Identifier.Text == state).All(i =>
            i.Parent is MemberAccessExpressionSyntax { Name.Identifier.Text: "ToString", Parent: InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } } && i.Parent is { } access
            && ((MemberAccessExpressionSyntax)access).Expression == i);
        return onlyToString ? $"(object {state}) => {body.WithoutTrivia()}" : null;
    }

    // The written type with It.IsAnyType as Arg.AnyType (Func<It.IsAnyType, Exception, string>), or null when it has no IsAnyType or any other Moq type.
    private static string? AnyTypeText(ITypeSymbol? type, SemanticModel model, int position)
    {
        if (type is null)
        {
            return null;
        }

        string text = TypeText(type, model, position);
        return text.Contains("It.IsAnyType", StringComparison.Ordinal) && !text.Replace("Moq.It.IsAnyType", "It.IsAnyType", StringComparison.Ordinal)
            .Replace("It.IsAnyType", string.Empty, StringComparison.Ordinal).Contains("Moq", StringComparison.Ordinal)
            ? text.Replace("Moq.It.IsAnyType", "Arg.AnyType", StringComparison.Ordinal).Replace("It.IsAnyType", "Arg.AnyType", StringComparison.Ordinal) : null;
    }

    private static string TypeText(ITypeSymbol type, SemanticModel model, int position) => type.ToMinimalDisplayString(model, position);
}
