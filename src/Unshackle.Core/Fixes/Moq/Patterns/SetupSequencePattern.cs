using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq.Patterns;

// `m.SetupSequence(x => x.M(..)).Returns(a).Returns(b);` → `m.Object.M(..).Returns(a, b);` with a warning (council round 8, M2), only when the
// whole mock converts (MockUsage); moq.create then drops the .Object.
public class SetupSequencePattern : IPattern
{
    public string Id => "moq.setup-sequence";

    public string Description => "m.SetupSequence(x => x.M(..)).Returns(a).Returns(b) → m.M(..).Returns(a, b), warning: NSubstitute repeats the last value";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => SetupSequenceSyntax.IsSequence(i, model) && SetupSequenceSyntax.Statement(i) is not null)
            .Select(i => (SyntaxNode)SetupSequenceSyntax.Statement(i)!);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        InvocationExpressionSyntax sequence = node.DescendantNodes().OfType<InvocationExpressionSyntax>().First(i => SetupSequenceSyntax.IsSequence(i, model));
        if (sequence.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } || MockUsage.Holder(receiver, model) is not { } holder)
        {
            return PatternResult.Manual(MoqReason.MockPassedAround, "the SetupSequence is on a mock that is not a local or private field of this file");
        }

        MockAnalysis analysis = MockUsage.Analyze(holder, model, pendingUses: true);
        if (analysis.Blocker is not null)
        {
            return analysis.Blocker.ToResult();
        }

        MockCall plan = SetupSequenceSyntax.Plan(sequence, model);
        if (plan.Blocker is not null)
        {
            return plan.Blocker.ToResult();
        }

        string[] namespaces = plan.Type is null ? ["NSubstitute"] : ["NSubstitute", SetupReturnsSyntax.TasksNamespace];
        return (FixedResult)PatternResult.Fixed(plan.Node!, namespaces) with { Warning = SetupSequenceSyntax.Warning };
    }
}
