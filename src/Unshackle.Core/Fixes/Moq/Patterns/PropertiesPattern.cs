using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq.Patterns;

// moq-queue.md row 5 under its own id (council round 12, M7, a rename only): SetupGet statements (rewritten as moq.setup-returns,
// moq.throws or moq.callback would) and VerifyGet/VerifySet statements (as moq.verify would). SetupProperty and SetupAllProperties stay
// with moq.create, which must rewrite them in the same pass as its own analysis.
public class PropertiesPattern : IPattern
{
    private readonly SetupReturnsPattern _setups = new();

    private readonly VerifyPattern _verifies = new();

    public string Id => "moq.properties";

    public string Description => "m.SetupGet(x => x.P).Returns(v) → m.P.Returns(v); VerifyGet/VerifySet → m.Received(n).P";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        foreach (InvocationExpressionSyntax invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (SetupReturnsSyntax.IsSetup(invocation, model) && SetupReturnsSyntax.IsSetupGet(invocation) && SetupReturnsSyntax.Statement(invocation) is { } statement)
            {
                yield return statement;
            }
            else if (VerifySyntax.IsVerify(invocation, model) && VerifySyntax.IsPropertyVerify(invocation))
            {
                yield return invocation.Parent!;
            }
        }
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        node is ExpressionStatementSyntax { Expression: InvocationExpressionSyntax verify } && VerifySyntax.IsVerify(verify, model)
            ? _verifies.Rewrite(node, model)
            : _setups.Rewrite(node, model);
}
