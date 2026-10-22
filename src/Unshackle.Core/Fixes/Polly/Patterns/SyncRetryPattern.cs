using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly.Patterns;

/// <summary>
/// <c>Retry</c>, <c>RetryForever</c>, <c>WaitAndRetry</c> and <c>WaitAndRetryForever</c> with their <c>Execute</c> calls → a retry
/// pipeline (queue row 4). The options mapping is the async patterns'; v8 has the same <c>Execute(Action)</c> and
/// <c>Execute&lt;T&gt;(Func&lt;T&gt;)</c>, so the callback stays as written.
/// </summary>
public class SyncRetryPattern : IPattern
{
    private static readonly string[] s_pipelineNamespaces = ["System", "System.Threading.Tasks", "Polly", "Polly.Retry"];

    public string Id => "polly.sync-retry";

    public string Description => "Retry / WaitAndRetry(Forever) + Execute → ResiliencePipelineBuilder().AddRetry(...) with the same Execute call";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => PollyRetry.IsRetryCreation(i, model) && PollyRetry.IsSync(i, model))
            .SelectMany(i => PollyRetry.Targets(i, model))
            .Distinct();

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        PollyPlan plan = node switch
        {
            LocalDeclarationStatementSyntax or FieldDeclarationSyntax => PollyRetry.PlanDeclaration(node, model),
            InvocationExpressionSyntax invocation when PollyRetry.IsRetryCreation(invocation, model) => PollyRetry.PlanCreation(invocation, model),
            InvocationExpressionSyntax execute => PollyRetry.PlanExecute(execute, model),
            _ => PollyPlan.Manual(PollyReason.PolicyNotLocal, "unexpected node."),
        };
        return plan.Node is SyntaxNode replacement
            ? PatternResult.Fixed(replacement, s_pipelineNamespaces)
            : PatternResult.Manual(plan.Code ?? PollyReason.PolicyNotLocal, plan.Reason ?? "No rewrite.");
    }
}
