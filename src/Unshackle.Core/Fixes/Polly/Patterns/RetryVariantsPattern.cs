using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly.Patterns;

/// <summary>
/// <c>RetryAsync(n)</c>, <c>RetryForeverAsync()</c>, <c>WaitAndRetryForeverAsync(attempt =&gt; delay)</c> and
/// <c>WaitAndRetryAsync(new[] { ... })</c> with their awaited <c>ExecuteAsync</c> calls → a retry pipeline (queue row 7). Same
/// filter, onRetry and local/inline rules as <see cref="WaitAndRetryAsyncPattern"/>.
/// </summary>
public class RetryVariantsPattern : IPattern
{
    private static readonly string[] s_pipelineNamespaces = ["System", "System.Threading.Tasks", "Polly", "Polly.Retry"];

    public string Id => "polly.retry-variants";

    public string Description => "RetryAsync / RetryForeverAsync / WaitAndRetryForeverAsync / WaitAndRetryAsync(durations) + ExecuteAsync → ResiliencePipelineBuilder().AddRetry(...)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => PollyRetry.IsRetryCreation(i, model) && PollyRetry.IsVariant(i, model))
            .SelectMany(i => PollyRetry.Targets(i, model))
            .Distinct();

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        PollyPlan plan = node switch
        {
            LocalDeclarationStatementSyntax statement => PollyRetry.PlanDeclaration(statement, model),
            InvocationExpressionSyntax invocation when PollyRetry.IsRetryCreation(invocation, model) =>
                PollyPlan.Manual(PollyReason.PolicyNotLocal,
                    "the policy is stored or passed on (field, property, return value or argument); only a local or inline policy used by awaited ExecuteAsync calls is rewritten so far."),
            InvocationExpressionSyntax execute => PollyRetry.PlanExecute(execute, model),
            _ => PollyPlan.Manual(PollyReason.PolicyNotLocal, "unexpected node."),
        };
        return plan.Node is SyntaxNode replacement
            ? PatternResult.Fixed(replacement, s_pipelineNamespaces)
            : PatternResult.Manual(plan.Code ?? PollyReason.PolicyNotLocal, plan.Reason ?? "No rewrite.");
    }
}
