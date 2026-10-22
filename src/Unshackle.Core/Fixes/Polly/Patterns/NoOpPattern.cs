using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly.Patterns;

/// <summary>
/// <c>Policy.NoOp(Async)()</c> with its <c>Execute</c>/<c>ExecuteAsync</c> calls → <c>ResiliencePipeline.Empty</c> (queue row 11).
/// </summary>
public class NoOpPattern : IPattern
{
    private static readonly string[] s_pipelineNamespaces = ["System", "System.Threading.Tasks", "Polly"];

    public string Id => "polly.no-op";

    public string Description => "Policy.NoOp(Async)() + Execute(Async) → ResiliencePipeline.Empty";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => PollyRetry.IsNoOp(i, model))
            .SelectMany(i => PollyRetry.Targets(i, model))
            .Distinct();

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        PollyPlan plan = node switch
        {
            LocalDeclarationStatementSyntax or FieldDeclarationSyntax => PollyRetry.PlanDeclaration(node, model),
            InvocationExpressionSyntax invocation when PollyRetry.IsNoOp(invocation, model) => PollyRetry.PlanCreation(invocation, model),
            InvocationExpressionSyntax execute => PollyRetry.PlanExecute(execute, model),
            _ => PollyPlan.Manual(PollyReason.PolicyNotLocal, "unexpected node."),
        };
        return plan.Node is SyntaxNode replacement
            ? PatternResult.Fixed(replacement, s_pipelineNamespaces)
            : PatternResult.Manual(plan.Code ?? PollyReason.PolicyNotLocal, plan.Reason ?? "No rewrite.");
    }
}
