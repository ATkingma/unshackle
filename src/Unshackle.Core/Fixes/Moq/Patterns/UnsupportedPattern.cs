using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq.Patterns;

// moq-queue.md row 13: what has no proven NSubstitute form is reported Manual with a reason code. Uses of a mock (Protected, As, Invocations,
// Reset, Raise, strict behaviour) get their code in MockUseRules / MoqSyntax, reported through moq.create; this pattern owns the rest:
// `new AutoMocker()` (Moq.AutoMock, council round 8, M5: NSubstitute has no auto-mocking container in the package set).
public class UnsupportedPattern : IPattern
{
    public string Id => "moq.unsupported";

    public string Description => "new AutoMocker() (AutoMoq) → Manual AutoMocker; Protected, As, Invocations, Reset, Raise, Strict get their own reason on the mock";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>().Where(c => model.GetTypeInfo(c).Type is { Name: "AutoMocker", ContainingNamespace: { Name: "AutoMock", ContainingNamespace.Name: "Moq" } });

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual(MoqReason.AutoMocker, "AutoMocker creates mocks on demand; NSubstitute has no such container (council round 8, M5): register the substitutes yourself");
}
