using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq.Patterns;

// `m.Verify();` / `m.VerifyAll();` → one `m.Object.Received().M(..);` per covered setup (council round 8, M4), with a warning. Runs
// before moq.setup-returns, which drops the .Verifiable() marks this pattern reads.
public class VerifyAllPattern : IPattern
{
    public string Id => "moq.verify-all";

    public string Description => "m.Verify() / m.VerifyAll() → m.Received().M(..) per covered setup; VerifyNoOtherCalls Manual";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => VerifyAllSyntax.IsVerifyAll(i, model)).Select(i => i.Parent!);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var verify = (InvocationExpressionSyntax)((ExpressionStatementSyntax)node).Expression;
        if (verify.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } || MockUsage.Holder(receiver, model) is not { } holder)
        {
            return PatternResult.Manual(MoqReason.MockPassedAround, "the Verify is on a mock that is not a local or private field of this file");
        }

        MockAnalysis analysis = MockUsage.Analyze(holder, model, pendingUses: true);
        if (analysis.Blocker is not null)
        {
            return analysis.Blocker.ToResult();
        }

        VerifyAllPlan plan = VerifyAllSyntax.Plan(verify, model);
        return plan.Blocker is not null ? plan.Blocker.ToResult()
            : (FixedResult)PatternResult.Fixed(plan.Statements, "NSubstitute") with { Warning = plan.Warning };
    }
}
