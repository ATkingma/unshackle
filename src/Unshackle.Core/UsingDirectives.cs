using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core;

// shortcut: only top-level usings are touched; usings inside namespace blocks stay. Upgrade when real code needs it.
internal static class UsingDirectives
{
    public static async Task<Solution> UpdateAsync(
        Document document,
        IEnumerable<string> addedNamespaces,
        IEnumerable<string> removedNamespaces,
        IReadOnlyCollection<string> swappedTypes,
        CancellationToken cancellationToken)
    {
        if (await document.GetSyntaxRootAsync(cancellationToken) is not CompilationUnitSyntax root
            || await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
        {
            return document.Project.Solution;
        }

        HashSet<string> imported = [.. root.Usings.Concat(GlobalUsings(model.Compilation, cancellationToken))
            .Where(IsPlainUsing)
            .Select(u => u.Name!.ToString())];
        List<string> missing = [.. addedNamespaces.Distinct().Where(ns => !imported.Contains(ns)).Order(StringComparer.Ordinal)];

        HashSet<string> used = UsedNamespaces(root, model, swappedTypes, cancellationToken);
        var usings = new List<UsingDirectiveSyntax>();
        foreach (UsingDirectiveSyntax directive in root.Usings)
        {
            bool unused = IsPlainUsing(directive) && directive.GlobalKeyword.IsKind(SyntaxKind.None)
                && removedNamespaces.Contains(directive.Name!.ToString()) && !used.Contains(directive.Name!.ToString());
            if (!unused)
            {
                usings.Add(directive);
            }
            else if (missing.Count > 0)
            {
                // Swap in place, so position and surrounding trivia stay as the user had them.
                usings.Add(directive.WithName(SyntaxFactory.ParseName(missing[0]).WithTriviaFrom(directive.Name!)));
                missing.RemoveAt(0);
            }
        }

        SyntaxTrivia endOfLine = root.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        if (endOfLine.IsKind(SyntaxKind.None))
        {
            endOfLine = SyntaxFactory.LineFeed;
        }

        // shortcut: new usings go after the existing ones (or on top of the file), not sorted. Fine for the CLI diff.
        usings.AddRange(missing.Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns).WithLeadingTrivia(SyntaxFactory.Space))
            .WithTrailingTrivia(root.Usings.Count == 0 ? SyntaxFactory.TriviaList(endOfLine, endOfLine) : SyntaxFactory.TriviaList(endOfLine))));

        return document.WithSyntaxRoot(root.WithUsings([.. usings])).Project.Solution;
    }

    // True when the document has a top-level using of a removed namespace that nothing (but swapped types) binds to anymore.
    public static async Task<bool> HasUnusedRemovedUsingAsync(
        Document document,
        IEnumerable<string> removedNamespaces,
        IReadOnlyCollection<string> swappedTypes,
        CancellationToken cancellationToken)
    {
        if (await document.GetSyntaxRootAsync(cancellationToken) is not CompilationUnitSyntax root
            || await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
        {
            return false;
        }

        List<string> candidates = [.. root.Usings
            .Where(u => IsPlainUsing(u) && u.GlobalKeyword.IsKind(SyntaxKind.None))
            .Select(u => u.Name!.ToString())
            .Where(removedNamespaces.Contains)];
        return candidates.Count > 0 && candidates.Except(UsedNamespaces(root, model, swappedTypes, cancellationToken)).Any();
    }

    // A swapped type, or a member of one: its twin in the added namespace takes over after the using swap.
    public static bool IsSwapped(ISymbol symbol, IReadOnlyCollection<string> swappedTypes)
    {
        INamedTypeSymbol? type = (symbol as INamedTypeSymbol ?? symbol.ContainingType)?.OriginalDefinition;
        return type is not null && swappedTypes.Contains($"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}");
    }

    private static bool IsPlainUsing(UsingDirectiveSyntax directive) =>
        directive.Alias is null && directive.StaticKeyword.IsKind(SyntaxKind.None) && directive.Name is not null;

    private static IEnumerable<UsingDirectiveSyntax> GlobalUsings(Compilation compilation, CancellationToken cancellationToken) =>
        compilation.SyntaxTrees
            .Select(t => t.GetRoot(cancellationToken))
            .OfType<CompilationUnitSyntax>()
            .SelectMany(r => r.Usings)
            .Where(u => !u.GlobalKeyword.IsKind(SyntaxKind.None));

    // Namespaces the document still needs a using for: those of the types and members it binds to by simple name.
    private static HashSet<string> UsedNamespaces(
        CompilationUnitSyntax root, SemanticModel model, IReadOnlyCollection<string> swappedTypes, CancellationToken cancellationToken) =>
        [.. root.DescendantNodes(n => n is not UsingDirectiveSyntax)
            .OfType<SimpleNameSyntax>()
            .Select(n => model.GetSymbolInfo(n, cancellationToken).Symbol)
            .Where(s => s is not null and not INamespaceSymbol && !IsSwapped(s, swappedTypes))
            .Select(s => s!.ContainingNamespace?.ToDisplayString() ?? string.Empty)];
}
