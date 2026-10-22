using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq.Patterns;

// `m.Setup(x => x.M(It.IsAny<int>())).Returns(v);` → `m.Object.M(Arg.Any<int>()).Returns(v);` only when the whole mock converts
// (MockUsage): a NSubstitute Returns on a mock that stays Moq would throw at run time. moq.create then drops the .Object.
public class SetupReturnsPattern : IPattern
{
    public string Id => "moq.setup-returns";

    public string Description => "m.Setup(x => x.M(..)).Returns(v) → m.Object.M(..).Returns(v), ReturnsAsync(v) → Returns(Task.FromResult(v))";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => SetupReturnsSyntax.IsSetup(i, model) && SetupReturnsSyntax.Statement(i) is not null)
            .Select(i => (SyntaxNode)SetupReturnsSyntax.Statement(i)!);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var statement = (ExpressionStatementSyntax)node;
        InvocationExpressionSyntax setup = statement.DescendantNodes().OfType<InvocationExpressionSyntax>().First(i => SetupReturnsSyntax.IsSetup(i, model));
        if (setup.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } || MockUsage.Holder(receiver, model) is not { } holder)
        {
            return PatternResult.Manual(MoqReason.MockPassedAround, "the Setup is on a mock that is not a local or private field of this file");
        }

        MockAnalysis analysis = MockUsage.Analyze(holder, model, pendingUses: true);
        if (analysis.Blocker is not null)
        {
            return analysis.Blocker.ToResult();
        }

        MockCall plan = SetupReturnsSyntax.Plan(setup, model);
        return plan.Blocker is not null ? plan.Blocker.ToResult()
            : plan.Removed ? PatternResult.Fixed(Array.Empty<SyntaxNode>())
            : plan.Type is not null ? PatternResult.Fixed(plan.Node!, "NSubstitute", SetupReturnsSyntax.TasksNamespace)
            : PatternResult.Fixed(plan.Node!, "NSubstitute");
    }
}
