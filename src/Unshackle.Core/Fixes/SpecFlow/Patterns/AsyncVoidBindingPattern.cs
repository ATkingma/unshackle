using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.SpecFlow.Patterns;

/// <summary>
/// An <c>async void</c> step definition or hook: Reqnroll refuses it at run time. Manual (council question): SpecFlow 3.9 did not wait for
/// it, so turning it into <c>async Task</c> makes the scenario wait for work it used to run in the background, which can change outcomes.
/// </summary>
public class AsyncVoidBindingPattern : IPattern
{
    public string Id => "specflow.async-void-binding";

    public string Description => "async void step definition or hook → Manual (Reqnroll refuses it)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(SyntaxKind.AsyncKeyword)
                && m.ReturnType is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword }
                && m.AttributeLists.SelectMany(l => l.Attributes).Any(a => IsBindingAttribute(a, model)));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        // Council round 18, S3: the reason names the hand route.
        PatternResult.Manual("async void step definition or hook: Reqnroll awaits this; change it to async Task by hand and check ordering " +
            "(SpecFlow did not wait for it, so work that ran in the background now runs before the next step).");

    private static bool IsBindingAttribute(AttributeSyntax attribute, SemanticModel model)
    {
        INamedTypeSymbol? type = (model.GetSymbolInfo(attribute).Symbol as IMethodSymbol)?.ContainingType;
        for (; type is not null; type = type.BaseType)
        {
            string fullName = SpecFlowNames.FullName(type);
            if (fullName is "TechTalk.SpecFlow.StepDefinitionBaseAttribute" or "TechTalk.SpecFlow.HookAttribute")
            {
                return true;
            }
        }

        return false;
    }
}
