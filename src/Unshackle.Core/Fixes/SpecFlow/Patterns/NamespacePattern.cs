using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.SpecFlow.Patterns;

/// <summary>
/// <c>using TechTalk.SpecFlow[.X];</c> → <c>using Reqnroll[.X];</c>, <c>using BoDi;</c> → <c>using Reqnroll.BoDi;</c>, and the same swap for
/// names written out in code (<c>TechTalk.SpecFlow.Table</c> → <c>Reqnroll.Table</c>).
/// </summary>
public class NamespacePattern : IPattern
{
    public string Id => "specflow.namespace";

    public string Description => "using TechTalk.SpecFlow[.X] / BoDi → using Reqnroll[.X] / Reqnroll.BoDi";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        foreach (UsingDirectiveSyntax directive in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
        {
            if (directive.Name is NameSyntax name && OldTarget(name, model) is not null)
            {
                yield return directive;
            }
        }

        foreach (ExpressionSyntax node in root.DescendantNodes().OfType<ExpressionSyntax>()
            .Where(n => n is QualifiedNameSyntax or MemberAccessExpressionSyntax or IdentifierNameSyntax or AliasQualifiedNameSyntax))
        {
            if (node.Ancestors().Any(a => a is UsingDirectiveSyntax) || InNamespaceDeclarationName(node) || !IsOldNamespace(node, model)
                || Outer(node) is ExpressionSyntax outer && IsOldNamespace(outer, model))
            {
                continue;
            }

            // A renamed or removed type written out in full is rewritten as a whole: its new namespace may differ.
            yield return Outer(node) is ExpressionSyntax typeName && TypeOf(typeName, model) is INamedTypeSymbol type && IsSpecialType(type)
                ? typeName
                : node;
        }
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is UsingDirectiveSyntax directive)
        {
            return RewriteUsing(directive, model);
        }

        var expression = (ExpressionSyntax)node;
        if (TypeOf(expression, model) is INamedTypeSymbol type)
        {
            string fullName = SpecFlowNames.FullName(type.OriginalDefinition);
            if (!SpecFlowNames.RenamedTypes.TryGetValue(fullName, out string? newName))
            {
                return PatternResult.Manual(RemovedReason(fullName));
            }

            return PatternResult.Fixed(Replace(expression, Global(expression) + newName));
        }

        string oldNamespace = ((INamespaceSymbol)model.GetSymbolInfo(expression).Symbol!).ToDisplayString();
        return SpecFlowNames.IsRemovedNamespace(oldNamespace)
            ? PatternResult.Manual(RemovedReason(oldNamespace))
            : PatternResult.Fixed(Replace(expression, Global(expression) + SpecFlowNames.NewNamespace(oldNamespace)));
    }

    private static PatternResult RewriteUsing(UsingDirectiveSyntax directive, SemanticModel model)
    {
        string target = OldTarget(directive.Name!, model)!;
        string? newName = null;
        bool isType = model.GetSymbolInfo(directive.Name!).Symbol is INamedTypeSymbol;
        if (isType)
        {
            if (SpecFlowNames.RemovedTypes.Contains(target))
            {
                return PatternResult.Manual(RemovedReason(target));
            }

            newName = SpecFlowNames.RenamedTypes.TryGetValue(target, out string? renamed)
                ? renamed
                : SpecFlowNames.NewNamespace(SpecFlowNames.NamespaceOf(target)) + "." + SpecFlowNames.SimpleNameOf(target);
        }
        else if (SpecFlowNames.IsRemovedNamespace(target))
        {
            return PatternResult.Manual(RemovedReason(target));
        }
        else
        {
            newName = SpecFlowNames.NewNamespace(target)!;
        }

        if (newName.Contains('+', StringComparison.Ordinal) || newName.Contains('`', StringComparison.Ordinal))
        {
            return PatternResult.Manual($"using {directive.Name} names a nested or generic type; swap it to Reqnroll by hand.");
        }

        UsingDirectiveSyntax swapped = directive.WithName(SyntaxFactory.ParseName(Global(directive.Name!) + newName).WithTriviaFrom(directive.Name!));
        // TechTalk.SpecFlow.Assist's extension methods (CreateInstance, CreateSet, ...) moved to namespace Reqnroll.
        return !isType && target == SpecFlowNames.OldRoot + ".Assist"
            ? PatternResult.Fixed(swapped, SpecFlowNames.NewRoot)
            : PatternResult.Fixed(swapped);
    }

    // The SpecFlow namespace or type (full metadata name) a using directive names; null when it names neither.
    private static string? OldTarget(NameSyntax name, SemanticModel model) =>
        model.GetSymbolInfo(name).Symbol switch
        {
            INamespaceSymbol ns when SpecFlowNames.IsOldNamespace(ns.ToDisplayString()) => ns.ToDisplayString(),
            INamedTypeSymbol type when SpecFlowNames.IsOldType(type) => SpecFlowNames.FullName(type.OriginalDefinition),
            _ => null,
        };

    // The name of a namespace declaration (namespace TechTalk.SpecFlow.Extensions { ... }) is the user's own namespace, not a use of SpecFlow's.
    private static bool InNamespaceDeclarationName(SyntaxNode node) =>
        node.AncestorsAndSelf().Any(a => a.Parent is BaseNamespaceDeclarationSyntax declaration && declaration.Name == a);

    private static bool IsOldNamespace(ExpressionSyntax node, SemanticModel model) =>
        model.GetSymbolInfo(node).Symbol is INamespaceSymbol ns && SpecFlowNames.IsOldNamespace(ns.ToDisplayString());

    private static bool IsSpecialType(INamedTypeSymbol type)
    {
        string fullName = SpecFlowNames.FullName(type.OriginalDefinition);
        return SpecFlowNames.RenamedTypes.ContainsKey(fullName) || SpecFlowNames.RemovedTypes.Contains(fullName);
    }

    // The qualified name or member access that continues this node (it is its left side), if any.
    internal static ExpressionSyntax? Outer(ExpressionSyntax node) => node.Parent switch
    {
        QualifiedNameSyntax qualified when qualified.Left == node => qualified,
        MemberAccessExpressionSyntax access when access.Expression == node => access,
        _ => null,
    };

    private static INamedTypeSymbol? TypeOf(ExpressionSyntax node, SemanticModel model) =>
        model.GetSymbolInfo(node).Symbol is INamedTypeSymbol type && SpecFlowNames.IsOldType(type) ? type : null;

    private static string Global(ExpressionSyntax node) => node.ToString().StartsWith("global::", StringComparison.Ordinal) ? "global::" : "";

    private static ExpressionSyntax Replace(ExpressionSyntax node, string text) =>
        node is NameSyntax ? SyntaxFactory.ParseName(text) : SyntaxFactory.ParseExpression(text);

    private static string RemovedReason(string name) =>
        $"{name} has no Reqnroll counterpart (App.config configuration and the synchronous binding invoker are gone; App.config only works " +
        "through Reqnroll.SpecFlowCompatibility). Migrate this code by hand.";
}
