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
        string[] required = !isType && target == SpecFlowNames.OldRoot + ".Assist" ? [SpecFlowNames.NewRoot] : [];
        // Council round 18, S7: a swapped namespace using to which nothing in the file binds afterwards is dropped.
        return !isType && Droppable(directive, target, newName, model)
            ? PatternResult.Fixed(Array.Empty<SyntaxNode>(), required)
            : PatternResult.Fixed(swapped, required);
    }

    // A plain (not global, alias or static) using of an old namespace, with nothing but whitespace in front of it, through which no use in the
    // file binds to a type that stays in the swapped namespace. Implicit uses that are not checked (query expressions, deconstruction) keep it.
    private static bool Droppable(UsingDirectiveSyntax directive, string oldNamespace, string newNamespace, SemanticModel model)
    {
        // The only using of its block stays: removing it leaves the blank line after the block behind the usings the Fixer adds.
        SyntaxList<UsingDirectiveSyntax> block = directive.Parent switch
        {
            CompilationUnitSyntax unit => unit.Usings,
            BaseNamespaceDeclarationSyntax ns => ns.Usings,
            _ => default,
        };
        if (block.Count < 2 || directive.GlobalKeyword != default || directive.Alias is not null || directive.StaticKeyword != default
            || directive.GetLeadingTrivia().Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia)))
        {
            return false;
        }

        SyntaxNode root = directive.SyntaxTree.GetRoot();
        if (root.DescendantNodes().Any(n => n is QueryExpressionSyntax or AssignmentExpressionSyntax { Left: TupleExpressionSyntax or DeclarationExpressionSyntax }
            or ForEachVariableStatementSyntax))
        {
            return false;
        }

        IEnumerable<ISymbol?> used = root.DescendantNodes()
            .Where(n => !n.Ancestors().Any(a => a is UsingDirectiveSyntax))
            .SelectMany(n => n switch
            {
                SimpleNameSyntax name => [model.GetSymbolInfo(name).Symbol, .. model.GetSymbolInfo(name).CandidateSymbols],
                ForEachStatementSyntax loop => [model.GetForEachStatementInfo(loop).GetEnumeratorMethod],
                AwaitExpressionSyntax wait => [model.GetAwaitExpressionInfo(wait).GetAwaiterMethod],
                ExpressionSyntax element when element.Parent is InitializerExpressionSyntax { RawKind: (int)SyntaxKind.CollectionInitializerExpression }
                    => [model.GetCollectionInitializerSymbolInfo(element).Symbol],
                _ => Array.Empty<ISymbol?>(),
            });
        return !used.Any(symbol => NewNamespaceOf(symbol, oldNamespace) == newNamespace);
    }

    // The namespace a use of a symbol of oldNamespace binds to after the swap; null when the symbol is not in oldNamespace.
    private static string? NewNamespaceOf(ISymbol? symbol, string oldNamespace)
    {
        if (symbol is INamespaceSymbol ns)
        {
            return ns.ContainingNamespace?.ToDisplayString() == oldNamespace ? SpecFlowNames.NewNamespace(oldNamespace) : null;
        }

        INamedTypeSymbol? type = symbol is IMethodSymbol { ReducedFrom: IMethodSymbol reduced } ? reduced.ContainingType
            : symbol as INamedTypeSymbol ?? symbol?.ContainingType;
        while (type?.ContainingType is INamedTypeSymbol outer)
        {
            type = outer;
        }

        if (type is null || type.OriginalDefinition.ContainingNamespace.ToDisplayString() != oldNamespace)
        {
            return null;
        }

        string fullName = SpecFlowNames.FullName(type.OriginalDefinition);
        return SpecFlowNames.RenamedTypes.TryGetValue(fullName, out string? renamed) ? SpecFlowNames.NamespaceOf(renamed) : SpecFlowNames.NewNamespace(oldNamespace);
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
