using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly.Patterns;

/// <summary>
/// <c>Policy.Handle&lt;E&gt;().WaitAndRetryAsync(n, attempt =&gt; delay)</c> in a local or inline, with its awaited
/// <c>ExecuteAsync</c> calls → a <c>ResiliencePipelineBuilder().AddRetry(...)</c> pipeline; the delay lambda becomes the
/// <c>DelayGenerator</c> (council round 8, P2).
/// </summary>
public class WaitAndRetryAsyncPattern : IPattern
{
    private static readonly string[] s_pipelineNamespaces = ["System", "System.Threading.Tasks", "Polly", "Polly.Retry"];

    public string Id => "polly.wait-and-retry-async";

    public string Description => "Policy.Handle<E>().WaitAndRetryAsync(n, attempt => delay) + ExecuteAsync → new ResiliencePipelineBuilder().AddRetry(...).Build()";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => PollyRetry.IsRetryCreation(i, model) && !PollyRetry.IsVariant(i, model))
            .SelectMany(i => PollyRetry.Targets(i, model))
            .Distinct();

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        PollyPlan plan = node switch
        {
            LocalDeclarationStatementSyntax or FieldDeclarationSyntax => PollyRetry.PlanDeclaration(node, model),
            InvocationExpressionSyntax invocation when PollyRetry.IsRetryCreation(invocation, model) =>
                PollyPlan.Manual(PollyReason.PolicyNotLocal,
                    "the policy is stored or passed on (property, return value, argument, or a field assigned outside its declaration); only a local, a private field or an inline policy used by awaited ExecuteAsync calls is rewritten so far."),
            InvocationExpressionSyntax execute => PollyRetry.PlanExecute(execute, model),
            _ => PollyPlan.Manual(PollyReason.PolicyNotLocal, "unexpected node."),
        };
        return plan.Node is SyntaxNode replacement
            ? PatternResult.Fixed(replacement, s_pipelineNamespaces)
            : PatternResult.Manual(plan.Code ?? PollyReason.PolicyNotLocal, plan.Reason ?? "No rewrite.");
    }
}
