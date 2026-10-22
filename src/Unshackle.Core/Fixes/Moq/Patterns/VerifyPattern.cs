using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq.Patterns;

// `m.Verify(x => x.M(It.IsAny<int>()), Times.Once());` → `m.Object.Received(1).M(Arg.Any<int>());` only when the whole mock
// converts (MockUsage); moq.create then drops the .Object.
public class VerifyPattern : IPattern
{
    public string Id => "moq.verify";

    public string Description => "m.Verify(x => x.M(..), Times.Once/Never/Exactly(n)/AtLeastOnce) → m.Received(1)/DidNotReceive()/Received(n)/Received().M(..)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => VerifySyntax.IsVerify(i, model)).Select(i => i.Parent!);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var verify = (InvocationExpressionSyntax)((ExpressionStatementSyntax)node).Expression;
        if (verify.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } || MockUsage.Holder(receiver, model) is not { } holder)
        {
            return PatternResult.Manual("MockPassedAround: the Verify is on a mock that is not a local or private field of this file");
        }

        MockAnalysis analysis = MockUsage.Analyze(holder, model, pendingUses: true);
        if (analysis.Blocker is not null)
        {
            return PatternResult.Manual(analysis.Blocker);
        }

        MockCall plan = VerifySyntax.Plan(verify, model);
        return plan.Blocker is not null ? PatternResult.Manual(plan.Blocker) : PatternResult.Fixed(plan.Node!, "NSubstitute");
    }
}
