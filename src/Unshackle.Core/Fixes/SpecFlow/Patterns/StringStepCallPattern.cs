using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.SpecFlow.Patterns;

/// <summary>
/// <c>Given("...")</c>, <c>When</c>, <c>Then</c>, <c>And</c>, <c>But</c> called on a <c>Steps</c> base class: SpecFlow v4, and so Reqnroll,
/// removed these string step calls. Always Manual: the replacement (calling the step method, or a driver) is a design choice.
/// </summary>
public class StringStepCallPattern : IPattern
{
    private static readonly HashSet<string> s_methods = new(["Given", "When", "Then", "And", "But"], StringComparer.Ordinal);

    public string Id => "specflow.string-step-call";

    public string Description => "Steps.Given(\"...\") and friends → Manual (removed in Reqnroll)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => model.GetSymbolInfo(i).Symbol is IMethodSymbol method
                && s_methods.Contains(method.Name)
                && SpecFlowNames.FullName(method.ContainingType) == SpecFlowNames.OldRoot + ".Steps");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual("String step calls (Given/When/Then/And/But on a Steps base class) were removed in Reqnroll. Call the step " +
            "definition method directly or move the shared code to a driver class, then run the fix again.");
}
