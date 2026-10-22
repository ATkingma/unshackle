using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly.Patterns;

/// <summary>
/// <c>Policy.Timeout(Async)(timeout)</c> with its <c>Execute</c>/<c>ExecuteAsync</c> calls → <c>new ResiliencePipelineBuilder().AddTimeout(timeout).Build()</c>
/// (queue row 9). Only optimistic timeouts with a literal inside v8's validated range; <c>ExecuteAsync(async ct =&gt; ..., token)</c> keeps its shape.
/// </summary>
public class TimeoutPattern : IPattern
{
    private static readonly string[] s_pipelineNamespaces = ["System", "System.Threading.Tasks", "Polly"];

    public string Id => "polly.timeout";

    public string Description => "Policy.Timeout(Async)(literal) + Execute(Async) → ResiliencePipelineBuilder().AddTimeout(...).Build()";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => PollyRetry.IsTimeout(i, model))
            .SelectMany(i => PollyRetry.Targets(i, model))
            .Distinct();

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        PollyPlan plan = node switch
        {
            LocalDeclarationStatementSyntax or FieldDeclarationSyntax => PollyRetry.PlanDeclaration(node, model),
            InvocationExpressionSyntax invocation when PollyRetry.IsTimeout(invocation, model) => PollyRetry.PlanCreation(invocation, model),
            InvocationExpressionSyntax execute => PollyRetry.PlanExecute(execute, model),
            _ => PollyPlan.Manual(PollyReason.PolicyNotLocal, "unexpected node."),
        };
        return plan.Node is SyntaxNode replacement
            ? PatternResult.Fixed(replacement, s_pipelineNamespaces)
            : PatternResult.Manual(plan.Code ?? PollyReason.PolicyNotLocal, plan.Reason ?? "No rewrite.");
    }
}
