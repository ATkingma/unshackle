using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq.Patterns;

// `m.Verify(x => x.M(It.IsAny<int>()), Times.Once());` → `m.Object.Received(1).M(Arg.Any<int>());` only when the whole mock
// converts (MockUsage); moq.create then drops the .Object.
public class VerifyPattern : IPattern
{
    public virtual string Id => "moq.verify";

    public virtual string Description => "m.Verify(x => x.M(..), Times.Once/Never/Exactly(n)/AtLeastOnce) → m.Received(1)/DidNotReceive()/Received(n)/Received().M(..)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => VerifySyntax.IsVerify(i, model) && !VerifySyntax.IsPropertyVerify(i) && IsLog(i, model) == Logs).Select(i => i.Parent!);

    // moq.logger-verify (row 12) reuses this pattern for the Verify of ILogger.Log.
    protected virtual bool Logs => false;

    private static bool IsLog(InvocationExpressionSyntax verify, SemanticModel model) =>
        verify.ArgumentList.Arguments[0].Expression is SimpleLambdaExpressionSyntax { ExpressionBody: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.Text: "Log" } } call }
        && model.GetSymbolInfo(call).Symbol is IMethodSymbol { ContainingType: { Name: "ILogger", ContainingNamespace: { Name: "Logging", ContainingNamespace.Name: "Extensions" } } };

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

        MockCall plan = VerifySyntax.Plan(verify, model);
        return plan.Blocker is not null ? plan.Blocker.ToResult() : PatternResult.Fixed(plan.Node!, "NSubstitute");
    }
}
