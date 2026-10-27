using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.SpecFlow.Patterns;

/// <summary><c>scenarioInfo.ScenarioAndFeatureTags</c> → <c>scenarioInfo.CombinedTags</c> (renamed in SpecFlow v4, so in Reqnroll).</summary>
public class CombinedTagsPattern : IPattern
{
    public string Id => "specflow.combined-tags";

    public string Description => "ScenarioInfo.ScenarioAndFeatureTags → ScenarioInfo.CombinedTags";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(n => n.Identifier.ValueText == "ScenarioAndFeatureTags"
                && model.GetSymbolInfo(n).Symbol is IPropertySymbol property
                && SpecFlowNames.FullName(property.ContainingType) == SpecFlowNames.OldRoot + ".ScenarioInfo");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Fixed(((IdentifierNameSyntax)node).WithIdentifier(SyntaxFactory.Identifier("CombinedTags")));
}
