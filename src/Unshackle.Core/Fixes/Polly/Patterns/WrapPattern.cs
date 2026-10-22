using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly.Patterns;

/// <summary>
/// <c>Policy.WrapAsync(a, b)</c> / <c>a.WrapAsync(b)</c> of retry policies, inline or in a local, with awaited <c>ExecuteAsync</c> →
/// one <c>ResiliencePipelineBuilder</c> with an <c>AddRetry</c> per operand, outermost first (council round 8, P4; queue row 10).
/// </summary>
public class WrapPattern : IPattern
{
    private static readonly string[] s_pipelineNamespaces = ["System", "System.Threading.Tasks", "Polly", "Polly.Retry"];

    public string Id => "polly.wrap";

    public string Description => "Policy.WrapAsync(a, b) / a.WrapAsync(b) of retries + ExecuteAsync → new ResiliencePipelineBuilder().AddRetry(a).AddRetry(b).Build()";

    // The outermost wrap of a chain, its use (inline call or holder) and the operand locals it inlines.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => PollyWrap.IsWrap(i, model) && !IsInnerWrap(i, model))
            .SelectMany(w => PollyRetry.Targets(w, model).Concat(PollyWrap.OperandLocals(w, model)))
            .Distinct();

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is LocalDeclarationStatementSyntax { Declaration.Variables: [{ Initializer.Value: InvocationExpressionSyntax value }] } operandLocal
            && PollyRetry.IsRetryCreation(value, model))
        {
            PollyPlan removal = PollyWrap.RemoveOperandLocal(operandLocal, model);
            return removal.Text is not null ? PatternResult.Fixed([]) : PatternResult.Manual(removal.Code ?? PollyReason.WrapOrderNotProvable, removal.Reason ?? "No rewrite.");
        }

        PollyPlan plan = node switch
        {
            LocalDeclarationStatementSyntax or FieldDeclarationSyntax => PollyRetry.PlanDeclaration(node, model),
            InvocationExpressionSyntax invocation when PollyWrap.IsWrap(invocation, model) => PollyRetry.PlanCreation(invocation, model),
            InvocationExpressionSyntax execute => PollyRetry.PlanExecute(execute, model),
            _ => PollyPlan.Manual(PollyReason.PolicyNotLocal, "unexpected node."),
        };
        return plan.Node is SyntaxNode replacement
            ? PatternResult.Fixed(replacement, s_pipelineNamespaces)
            : PatternResult.Manual(plan.Code ?? PollyReason.PolicyNotLocal, plan.Reason ?? "No rewrite.");
    }

    private static bool IsInnerWrap(InvocationExpressionSyntax wrap, SemanticModel model) => wrap.Parent switch
    {
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: "WrapAsync", Parent: InvocationExpressionSyntax outer } => PollyWrap.IsWrap(outer, model),
        ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax outer } => PollyWrap.IsWrap(outer, model),
        _ => false,
    };
}
