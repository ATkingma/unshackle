using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core;

// Usings of removed namespaces are swapped or removed wherever they are: top-level or inside a namespace declaration.
// Global usings are only top-level (the compiler requires that).
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

        HashSet<string> imported = [.. AllUsings(root).Concat(GlobalUsings(model.Compilation, cancellationToken))
            .Where(IsPlainUsing)
            .Select(u => u.Name!.ToString())];
        List<string> missing = [.. addedNamespaces.Distinct().Where(ns => !imported.Contains(ns)).Order(StringComparer.Ordinal)];

        HashSet<string> used = UsedNamespaces(root, model, swappedTypes, cancellationToken);
        List<UsingDirectiveSyntax> unused = HasInactiveCode(root)
            ? []
            : [.. AllUsings(root).Where(d => IsPlainUsing(d) && d.GlobalKeyword.IsKind(SyntaxKind.None)
                && removedNamespaces.Contains(d.Name!.ToString()) && !used.Contains(d.Name!.ToString()))];

        // Swap in place, so position and surrounding trivia stay as the user had them; the other unused ones go.
        int swapCount = Math.Min(unused.Count, missing.Count);
        CompilationUnitSyntax updated = root.TrackNodes(unused);
        updated = updated.RemoveNodes(unused.Skip(swapCount).Select(u => updated.GetCurrentNode(u)!), SyntaxRemoveOptions.KeepNoTrivia)!;
        for (int i = 0; i < swapCount; i++)
        {
            UsingDirectiveSyntax current = updated.GetCurrentNode(unused[i])!;
            updated = updated.ReplaceNode(current, current.WithName(SyntaxFactory.ParseName(missing[i]).WithTriviaFrom(current.Name!)));
        }

        missing.RemoveRange(0, swapCount);
        var usings = new List<UsingDirectiveSyntax>(updated.Usings);

        SyntaxTrivia endOfLine = root.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        if (endOfLine.IsKind(SyntaxKind.None))
        {
            endOfLine = SyntaxFactory.LineFeed;
        }

        // shortcut: new usings go after the existing ones (or on top of the file), not sorted. Fine for the CLI diff.
        usings.AddRange(missing.Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns).WithLeadingTrivia(SyntaxFactory.Space))
            .WithTrailingTrivia(root.Usings.Count == 0 ? SyntaxFactory.TriviaList(endOfLine, endOfLine) : SyntaxFactory.TriviaList(endOfLine))));

        return document.WithSyntaxRoot(updated.WithUsings([.. usings])).Project.Solution;
    }

    // True when the document has a top-level using of a removed namespace that nothing (but swapped types) binds to anymore.
    public static async Task<bool> HasUnusedRemovedUsingAsync(
        Document document,
        IEnumerable<string> removedNamespaces,
        IReadOnlyCollection<string> swappedTypes,
        CancellationToken cancellationToken)
    {
        if (await document.GetSyntaxRootAsync(cancellationToken) is not CompilationUnitSyntax root)
        {
            return false;
        }

        List<string> candidates = [.. AllUsings(root)
            .Where(u => IsPlainUsing(u) && u.GlobalKeyword.IsKind(SyntaxKind.None))
            .Select(u => u.Name!.ToString())
            .Where(removedNamespaces.Contains)];
        // Binding every name is the expensive part: only files that import a removed namespace get that far.
        return candidates.Count > 0
            && await document.GetSemanticModelAsync(cancellationToken) is SemanticModel model
            && candidates.Except(UsedNamespaces(root, model, swappedTypes, cancellationToken)).Any();
    }

    // Code in an inactive #if branch is only trivia: nothing binds there, so a removed using may still be needed by it.
    public static bool HasInactiveCode(SyntaxNode root) =>
        root.DescendantTrivia().Any(t => t.IsKind(SyntaxKind.DisabledTextTrivia));

    // A top-level using of a removed namespace that stays because inactive code may still need it.
    public static bool KeepsRemovedUsing(SyntaxNode root, IEnumerable<string> removedNamespaces) =>
        root is CompilationUnitSyntax unit && HasInactiveCode(unit) && AllUsings(unit)
            .Any(u => IsPlainUsing(u) && u.GlobalKeyword.IsKind(SyntaxKind.None) && removedNamespaces.Contains(u.Name!.ToString()));

    // Top-level `global using` directives of removed namespaces in the user's own files (not MSBuild's generated *.g.cs).
    public static IEnumerable<UsingDirectiveSyntax> RemovedGlobalUsings(SyntaxNode root, string? filePath, IEnumerable<string> removedNamespaces) =>
        root is CompilationUnitSyntax unit && filePath?.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) != true
            ? unit.Usings.Where(u => IsPlainUsing(u) && !u.GlobalKeyword.IsKind(SyntaxKind.None) && removedNamespaces.Contains(u.Name!.ToString()))
            : [];

    // Swaps each global using for the added namespaces in place, or (keep) puts them right after it.
    public static CompilationUnitSyntax SwapGlobalUsings(
        CompilationUnitSyntax root, IReadOnlyCollection<UsingDirectiveSyntax> globals, IReadOnlyList<string> addedNamespaces, bool keep)
    {
        var usings = new List<UsingDirectiveSyntax>();
        foreach (UsingDirectiveSyntax directive in root.Usings)
        {
            if (!globals.Contains(directive))
            {
                usings.Add(directive);
                continue;
            }

            if (keep)
            {
                usings.Add(directive);
            }

            // Only the first replacement keeps the leading trivia, so comments are not duplicated.
            usings.AddRange(addedNamespaces.Select((ns, i) =>
            {
                UsingDirectiveSyntax swapped = directive.WithName(SyntaxFactory.ParseName(ns).WithTriviaFrom(directive.Name!));
                return keep || i > 0 ? swapped.WithLeadingTrivia() : swapped;
            }));
        }

        // shortcut: two global usings of removed namespaces would import the added ones twice (a warning); fine until seen in real code.
        return root.WithUsings([.. usings]);
    }

    // True when the document binds a name to a removed namespace (swapped types aside), or has inactive code that may.
    public static async Task<bool> MayUseRemovedNamespaceAsync(
        Document document, IEnumerable<string> removedNamespaces, IReadOnlyCollection<string> swappedTypes, CancellationToken cancellationToken)
    {
        if (await document.GetSyntaxRootAsync(cancellationToken) is not CompilationUnitSyntax root)
        {
            return false;
        }

        return HasInactiveCode(root)
            || await document.GetSemanticModelAsync(cancellationToken) is SemanticModel model
            && UsedNamespaces(root, model, swappedTypes, cancellationToken).Overlaps(removedNamespaces);
    }

    // A swapped type, or a member of one: its twin in the added namespace takes over after the using swap.
    public static bool IsSwapped(ISymbol symbol, IReadOnlyCollection<string> swappedTypes)
    {
        INamedTypeSymbol? type = (symbol as INamedTypeSymbol ?? symbol.ContainingType)?.OriginalDefinition;
        return type is not null && swappedTypes.Contains($"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}");
    }

    // The file's usings and those of its namespace declarations (block or file-scoped).
    private static IEnumerable<UsingDirectiveSyntax> AllUsings(CompilationUnitSyntax root) =>
        root.DescendantNodesAndSelf(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .SelectMany(n => n switch
            {
                CompilationUnitSyntax unit => unit.Usings,
                BaseNamespaceDeclarationSyntax ns => ns.Usings,
                _ => [],
            });

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
