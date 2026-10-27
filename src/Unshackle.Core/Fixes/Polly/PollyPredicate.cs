using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Polly;

// The v7 exception/result filter chain (Policy.Handle<E>(), Policy.HandleResult<T>(...), Policy<T>.Handle<E>(), .Or<E>(),
// .OrResult(...)) → a v8 PredicateBuilder(<T>) chain. Both match a clause the same way (`is E e && predicate(e)`), and a
// chain of clauses is an OR in both, so the clauses carry over in order with their arguments verbatim (queue row 2).
internal static class PollyPredicate
{
    // The lambda parameter of the written-out transient-error filter; FreshName picks response1.. when taken.
    public const string ResponseName = "response";

    public static PollyPlan Plan(ExpressionSyntax receiver, string? resultType, SemanticModel model, string response)
    {
        var clauses = new List<string>();
        ExpressionSyntax current = receiver;
        while (true)
        {
            // The PolicyBuilder<HttpResponseMessage> parameter of AddTransientHttpErrorPolicy(p => ...) is HandleTransientHttpError().
            if (current is IdentifierNameSyntax && model.GetSymbolInfo(current).Symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } lambda }
                && IsTransientPolicyLambda(lambda))
            {
                return PollyPlan.OfText($"new PredicateBuilder<{resultType}>()" + Concat([.. TransientClauses(response), .. clauses], response));
            }

            if (current is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } invocation
                || model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { ContainingAssembly.Name: PollyRetry.PollyAssembly or HttpExtensionsAssembly } method)
            {
                return Unsupported();
            }

            // Polly.Extensions.Http 3.0.0: HttpRequestException, or a status code of 500 and up, or 408.
            if (method.ContainingType.Name == "HttpPolicyExtensions" && invocation.ArgumentList.Arguments.Count == 0)
            {
                switch (method.Name)
                {
                    case "HandleTransientHttpError" when method.IsStatic && method.Parameters.Length == 0:
                        return PollyPlan.OfText($"new PredicateBuilder<{resultType}>()" + Concat([.. TransientClauses(response), .. clauses], response));
                    case "OrTransientHttpError":
                        clauses.InsertRange(0, TransientClauses(response));
                        current = access.Expression;
                        continue;
                    case "OrTransientHttpStatusCode":
                        clauses.Insert(0, TransientStatus(response));
                        current = access.Expression;
                        continue;
                    default:
                        return Unsupported();
                }
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

    private const string HttpExtensionsAssembly = "Polly.Extensions.Http";

    private static string[] TransientClauses(string response) => [".Handle<HttpRequestException>()", TransientStatus(response)];

    // A clause the transient filter already wrote (OrTransientHttpStatusCode after AddTransientHttpErrorPolicy's
    // HandleTransientHttpError, for example) is written once: OR with an identical, side-effect-free clause changes nothing.
    // Only these generated clauses are deduplicated; user clauses carry over verbatim.
    private static string Concat(List<string> clauses, string response)
    {
        string[] generated = TransientClauses(response);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return string.Concat(clauses.Where(c => !generated.Contains(c, StringComparer.Ordinal) || seen.Add(c)));
    }

    private static string TransientStatus(string response) =>
        $".HandleResult({response} => (int){response}.StatusCode >= 500 || {response}.StatusCode == HttpStatusCode.RequestTimeout)";

    private static bool IsTransientPolicyLambda(IMethodSymbol lambda) =>
        lambda.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax() is { Parent: ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddTransientHttpErrorPolicy" } } } };

    private static PollyPlan Unsupported() => PollyPlan.Manual(PollyReason.ExceptionFilterNotSupported,
        "the filter is not a chain of Policy.Handle<E>/HandleResult and Or<E>/OrResult with at most a predicate; HandleInner, OrInner and HandleResult(value) stay Manual.");
}
