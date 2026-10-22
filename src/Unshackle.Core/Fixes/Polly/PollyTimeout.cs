using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Polly;

// Policy.Timeout(Async)(timeout) → AddTimeout(timeout) (queue row 9). v7's default is optimistic, as v8's only mode: both cancel the token
// handed to the callback and throw Polly.Timeout.TimeoutRejectedException when the callback honours it, and neither interrupts a
// callback that ignores it (probe, TAAK-308). v8 rejects timeouts outside 100 ms..1 day in the probes (5 ms and 30 days failed validation
// at Build()), so only a literal inside that range is Fixed; pessimistic, providers and onTimeout callbacks are Manual.
internal static class PollyTimeout
{
    private static readonly TimeSpan s_min = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan s_max = TimeSpan.FromDays(1);

    public static PollyPlan Plan(InvocationExpressionSyntax creation, SemanticModel model)
    {
        SeparatedSyntaxList<ArgumentSyntax> arguments = creation.ArgumentList.Arguments;
        if (arguments.Count is < 1 or > 2 || arguments.Any(a => a.NameColon is not null))
        {
            return Manual(PollyReason.OverloadNotSupported, "only Timeout(timeout) and Timeout(timeout, TimeoutStrategy.Optimistic) are rewritten; a provider or an onTimeout callback is Manual.");
        }

        if (arguments.Count == 2)
        {
            if (arguments[1].Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: string strategy }
                || model.GetTypeInfo(arguments[1].Expression).Type is not { Name: "TimeoutStrategy" })
            {
                return Manual(PollyReason.OverloadNotSupported, "the second argument is not a TimeoutStrategy member (an onTimeout callback is Manual).");
            }

            if (strategy != "Optimistic")
            {
                return Manual(PollyReason.TimeoutStrategyNotOptimistic, "pessimistic timeouts abandon a callback that ignores the token; v8 only has the optimistic mode.");
            }
        }

        ExpressionSyntax value = arguments[0].Expression;
        string? text = Literal(value, model) is TimeSpan span && span >= s_min && span <= s_max
            ? model.GetConstantValue(value).Value is int ? $"TimeSpan.FromSeconds({value})" : value.ToString()
            : null;
        if (text is null)
        {
            return Manual(PollyReason.TimeoutNotProvable, "the timeout is not a literal (int seconds or TimeSpan.FromX(literal)) between 100 ms and 1 day; v8 validates the range when the pipeline is built.");
        }

        string generic = PollyRetry.ResultType(creation, model) is string result ? $"<{result}>" : string.Empty;
        return PollyPlan.Of(SyntaxFactory.ParseExpression($"new ResiliencePipelineBuilder{generic}().AddTimeout({text}).Build()"));
    }

    private static TimeSpan? Literal(ExpressionSyntax value, SemanticModel model)
    {
        if (model.GetConstantValue(value).Value is int seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        if (value is InvocationExpressionSyntax { ArgumentList.Arguments: [{ Expression: LiteralExpressionSyntax number }] } call
            && model.GetSymbolInfo(call).Symbol is IMethodSymbol { ContainingType: { Name: "TimeSpan", ContainingNamespace.Name: "System" }, Name: string name }
            && model.GetConstantValue(number).Value is int or double
            && Convert.ToDouble(model.GetConstantValue(number).Value, System.Globalization.CultureInfo.InvariantCulture) is double amount)
        {
            return name switch
            {
                "FromSeconds" => TimeSpan.FromSeconds(amount),
                "FromMilliseconds" => TimeSpan.FromMilliseconds(amount),
                "FromMinutes" => TimeSpan.FromMinutes(amount),
                "FromHours" => TimeSpan.FromHours(amount),
                _ => null,
            };
        }

        return null;
    }

    private static PollyPlan Manual(PollyReason code, string text) => PollyPlan.Manual(code, text);
}
