using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Polly;

// The v7 exception/result filter chain (Policy.Handle<E>(), Policy.HandleResult<T>(...), Policy<T>.Handle<E>(), .Or<E>(),
// .OrResult(...)) → a v8 PredicateBuilder(<T>) chain. Both match a clause the same way (`is E e && predicate(e)`), and a
// chain of clauses is an OR in both, so the clauses carry over in order with their arguments verbatim (queue row 2).
internal static class PollyPredicate
{
    public static PollyPlan Plan(ExpressionSyntax receiver, string? resultType, SemanticModel model)
    {
        var clauses = new List<string>();
        ExpressionSyntax current = receiver;
        while (true)
        {
            if (current is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } invocation
                || model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { ContainingAssembly.Name: PollyRetry.PollyAssembly } method)
            {
                return Unsupported();
            }

            string arguments = invocation.ArgumentList.ToString();
            if (invocation.ArgumentList.Arguments.Count > 1
                || (invocation.ArgumentList.Arguments.Count == 1 && method.Parameters[0].Type.TypeKind != TypeKind.Delegate))
            {
                // HandleResult(T value) compares with Equals in v7 and an IEqualityComparer in v8: not proven equal.
                return Unsupported();
            }

            switch (method.Name)
            {
                case "Handle" or "Or" when access.Name is GenericNameSyntax { TypeArgumentList.Arguments: [TypeSyntax exception] }:
                    clauses.Insert(0, $".Handle<{exception}>{arguments}");
                    break;
                case "HandleResult" or "OrResult":
                    clauses.Insert(0, $".HandleResult{arguments}");
                    break;
                default:
                    return Unsupported();
            }

            if (method.Name is "Handle" or "HandleResult")
            {
                // The start of the chain: Policy.X or Policy<T>.X, static on the v7 Policy class.
                return method.IsStatic && method.ContainingType is { Name: "Policy" }
                    ? PollyPlan.OfText((resultType is null ? "new PredicateBuilder()" : $"new PredicateBuilder<{resultType}>()") + string.Concat(clauses))
                    : Unsupported();
            }

            current = access.Expression;
        }
    }

    private static PollyPlan Unsupported() => PollyPlan.Manual(PollyReason.ExceptionFilterNotSupported,
        "the filter is not a chain of Policy.Handle<E>/HandleResult and Or<E>/OrResult with at most a predicate; HandleInner, OrInner and HandleResult(value) stay Manual.");
}
