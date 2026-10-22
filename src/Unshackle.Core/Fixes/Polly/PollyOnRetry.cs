using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Polly;

// A v7 onRetry/onRetryAsync lambda → v8 OnRetry (council P3, queue row 3). The body is kept verbatim in a local function whose
// parameters map 1:1 onto OnRetryArguments: Exception → Outcome.Exception, DelegateResult<T> → Outcome (only .Result and
// .Exception used, the members both share), TimeSpan → RetryDelay, the 1-based retry count → AttemptNumber + 1. A Context
// parameter must be unused; a body that reads it is Manual.
internal static class PollyOnRetry
{
    public const string CallbackName = "OnRetryCallback";

    public static PollyPlan Plan(
        ExpressionSyntax onRetry, IParameterSymbol parameter, string? resultType, string args, string callback, string inner, string newLine, SemanticModel model)
    {
        if (parameter.Type is not INamedTypeSymbol { DelegateInvokeMethod: IMethodSymbol invoke }
            || onRetry is not LambdaExpressionSyntax lambda
            || Parameters(lambda) is not { } parameters
            || parameters.Count != invoke.Parameters.Length
            || lambda.Modifiers.Any(m => !m.IsKind(SyntaxKind.AsyncKeyword)))
        {
            return Unsupported("the onRetry argument is not a lambda with one parameter per callback argument.");
        }

        bool returnsTask = !invoke.ReturnsVoid;
        bool isAsync = lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
        if (returnsTask && invoke.ReturnType is not INamedTypeSymbol { Name: "Task", Arity: 0 })
        {
            return Unsupported("the onRetryAsync callback does not return Task.");
        }

        var declared = new List<string>();
        var passed = new List<string>();
        for (int i = 0; i < parameters.Count; i++)
        {
            ITypeSymbol type = invoke.Parameters[i].Type;
            string name = parameters[i].Identifier.ValueText;
            List<IdentifierNameSyntax> uses = [.. Uses(lambda, parameters[i], model)];
            string declaredName = uses.Count == 0 ? $"unused{i}" : name;
            switch (type)
            {
                case { Name: "Context", ContainingAssembly.Name: PollyRetry.PollyAssembly }:
                    if (uses.Count > 0)
                    {
                        return PollyPlan.Manual(PollyReason.OnRetryUsesContext, "the onRetry body reads the Polly Context, which has no 1:1 v8 counterpart (council P3).");
                    }

                    continue;
                case { Name: "Exception", ContainingNamespace.Name: "System" }:
                    declared.Add($"Exception {declaredName}");
                    passed.Add($"{args}.Outcome.Exception!");
                    break;
                case INamedTypeSymbol { Name: "DelegateResult", ContainingAssembly.Name: PollyRetry.PollyAssembly } when resultType is not null:
                    if (uses.Any(u => u.Parent is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Result" or "Exception" } access || access.Expression != u))
                    {
                        return Unsupported("the onRetry body uses the DelegateResult for more than .Result and .Exception.");
                    }

                    declared.Add($"Outcome<{resultType}> {declaredName}");
                    passed.Add($"{args}.Outcome");
                    break;
                case { Name: "TimeSpan", ContainingNamespace.Name: "System" }:
                    declared.Add($"TimeSpan {declaredName}");
                    passed.Add($"{args}.RetryDelay");
                    break;
                case { SpecialType: SpecialType.System_Int32 }:
                    declared.Add($"int {declaredName}");
                    passed.Add($"{args}.AttemptNumber + 1");
                    break;
                default:
                    return Unsupported($"the onRetry parameter type {type.Name} has no 1:1 v8 counterpart.");
            }
        }

        string returnType = (isAsync ? "async " : string.Empty) + (returnsTask ? "Task" : "void");
        string signature = $"{returnType} {callback}({string.Join(", ", declared)})";
        string function = lambda.Body switch
        {
            BlockSyntax block => $"{signature}{newLine}{inner}    {PollyRetry.Reindent(block, inner + "    ", newLine)}",
            ExpressionSyntax expression => $"{signature} => {expression};",
            _ => throw new System.Diagnostics.UnreachableException(),
        };
        string call = $"{callback}({string.Join(", ", passed)})";
        string indent = inner[..^8];
        string tail = returnsTask
            ? $"{inner}    return new ValueTask({call});{newLine}"
            : $"{inner}    {call};{newLine}{inner}    return default;{newLine}";
        return PollyPlan.OfText(
            $"{indent}        OnRetry = {args} =>{newLine}"
            + $"{indent}        {{{newLine}"
            + $"{inner}    {function}{newLine}"
            + tail
            + $"{indent}        }},{newLine}");
    }

    private static IReadOnlyList<ParameterSyntax>? Parameters(LambdaExpressionSyntax lambda) => lambda switch
    {
        SimpleLambdaExpressionSyntax simple => [simple.Parameter],
        ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters,
        _ => null,
    };

    private static IEnumerable<IdentifierNameSyntax> Uses(LambdaExpressionSyntax lambda, ParameterSyntax parameter, SemanticModel model)
    {
        IParameterSymbol? symbol = model.GetDeclaredSymbol(parameter);
        return symbol is null || parameter.Identifier.ValueText == "_"
            && lambda is ParenthesizedLambdaExpressionSyntax parenthesized && parenthesized.ParameterList.Parameters.Count(p => p.Identifier.ValueText == "_") > 1
            ? []
            : lambda.Body.DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(n => n.Identifier.ValueText == symbol.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, symbol));
    }

    private static PollyPlan Unsupported(string text) => PollyPlan.Manual(PollyReason.OnRetryNotMappable, text);
}
