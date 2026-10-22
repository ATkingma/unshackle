using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq.Patterns;

// `m.Setup(x => x.M(It.IsAny<int>())).Returns(v);` → `m.Object.M(Arg.Any<int>()).Returns(v);` only when the whole mock converts
// (MockUsage): a NSubstitute Returns on a mock that stays Moq would throw at run time. moq.create then drops the .Object.
public class SetupReturnsPattern : IPattern
{
    public virtual string Id => "moq.setup-returns";

    public virtual string Description => "m.Setup(x => x.M(..)).Returns(v) → m.Object.M(..).Returns(v), ReturnsAsync(v) → Returns(Task.FromResult(v))";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => SetupReturnsSyntax.IsSetup(i, model) && SetupReturnsSyntax.Statement(i) is not null && SetupReturnsSyntax.Kind(i) == Finds)
            .Select(i => (SyntaxNode)SetupReturnsSyntax.Statement(i)!);

    // moq.throws and moq.returns-lambda reuse this pattern for their kind of Setup.
    protected virtual SetupKind Finds => SetupKind.Returns;

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
        if (plan.Blocker is not null)
        {
            return plan.Blocker.ToResult();
        }

        if (plan.Removed)
        {
            return PatternResult.Fixed(Array.Empty<SyntaxNode>());
        }

        List<string> namespaces = ["NSubstitute"];
        if (plan.Throws)
        {
            namespaces.Add(SetupReturnsSyntax.ExceptionsNamespace);
        }

        if (plan.Type is not null)
        {
            namespaces.Add(SetupReturnsSyntax.TasksNamespace);
        }

        if (plan.Configured)
        {
            namespaces.Add(SetupReturnsSyntax.ConfigureNamespace);
        }

        return PatternResult.Fixed(plan.Node!, [.. namespaces]);
    }
}
