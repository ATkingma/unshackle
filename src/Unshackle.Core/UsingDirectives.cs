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
        IReadOnlyCollection<string> removedExtensionMethods,
        IReadOnlyCollection<string> addedTypes,
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

        HashSet<string> used = UsedRemovedNamespaces(root, model, removedNamespaces, swappedTypes, cancellationToken);
        if (UnboundCalls(root, model, removedExtensionMethods, cancellationToken).Any())
        {
            used.UnionWith(removedNamespaces);
        }

        List<UsingDirectiveSyntax> unused = HasInactiveCode(root)
            ? []
            : [.. AllUsings(root).Where(d => IsPlainUsing(d) && d.GlobalKeyword.IsKind(SyntaxKind.None)
                && removedNamespaces.Contains(d.Name!.ToString()) && !used.Contains(d.Name!.ToString()))];

        List<SimpleNameSyntax> clashing = Clashing(root, model, missing, removedNamespaces, addedTypes, cancellationToken);

        // Swap in place, so position and surrounding trivia stay as the user had them; the other unused ones go.
        int swapCount = Math.Min(unused.Count, missing.Count);
        CompilationUnitSyntax updated = Qualify(root.TrackNodes(unused.Cast<SyntaxNode>().Concat(clashing)), clashing, model);
        updated = updated.RemoveNodes(unused.Skip(swapCount).Select(u => updated.GetCurrentNode(u)!), SyntaxRemoveOptions.KeepNoTrivia)!;
        for (int i = 0; i < swapCount; i++)
        {
            UsingDirectiveSyntax current = updated.GetCurrentNode(unused[i])!;
            string name = IsShadowed(unused[i], missing[i], model) ? "global::" + missing[i] : missing[i];
            updated = updated.ReplaceNode(current, current.WithName(SyntaxFactory.ParseName(name).WithTriviaFrom(current.Name!)));
        }

        missing.RemoveRange(0, swapCount);
        var usings = new List<UsingDirectiveSyntax>(updated.Usings);

        SyntaxTrivia endOfLine = root.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        if (endOfLine.IsKind(SyntaxKind.None))
        {
            endOfLine = SyntaxFactory.LineFeed;
        }

        // The file keeps its usings inside the namespace (IDE0065): the new ones go there too, with the same indentation (TAAK-115).
        // Not when a name inside it would shadow the new namespace (an own *.Mediator namespace, TAAK-131): then on top.
        if (missing.Count > 0 && usings.Count == 0
            && updated.Members.OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0) is BaseNamespaceDeclarationSyntax target
            && root.Members[updated.Members.IndexOf(target)] is BaseNamespaceDeclarationSyntax original
            && !missing.Any(ns => model.LookupNamespacesAndTypes(original.Members.FirstOrDefault()?.SpanStart ?? original.Span.End, name: ns.Split('.')[0])
                .Any(s => s is not INamespaceSymbol { ContainingNamespace.IsGlobalNamespace: true })))
        {
            SyntaxTriviaList indentation = [.. target.Usings.Last().GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia))];
            IEnumerable<UsingDirectiveSyntax> added = missing.Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns).WithLeadingTrivia(SyntaxFactory.Space))
                .WithLeadingTrivia(indentation)
                .WithTrailingTrivia(endOfLine));
            return document.WithSyntaxRoot(updated.ReplaceNode(target, target.WithUsings(target.Usings.AddRange(added)))).Project.Solution;
        }

        // shortcut: new usings go after the existing ones (or on top of the file), not sorted. Fine for the CLI diff.
        usings.AddRange(missing.Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns).WithLeadingTrivia(SyntaxFactory.Space))
            .WithTrailingTrivia(root.Usings.Count == 0 ? SyntaxFactory.TriviaList(endOfLine, endOfLine) : SyntaxFactory.TriviaList(endOfLine))));

        return document.WithSyntaxRoot(updated.WithUsings([.. usings])).Project.Solution;
    }

    // Own types named like a type of an added namespace (an own ICommand next to Mediator.ICommand): once the using is in,
    // the name is ambiguous (CS0104) or binds to the added type, so these references get their full name (TAAK-144).
    // shortcut: a global using of the added namespace (SwapGlobalUsings) is not checked; add that when seen in real code.
    private static List<SimpleNameSyntax> Clashing(
        CompilationUnitSyntax root, SemanticModel model, List<string> added, IEnumerable<string> removed, IReadOnlyCollection<string> addedTypes,
        CancellationToken cancellationToken)
    {
        HashSet<string> taken = [.. addedTypes.Where(t => added.Contains(t[..t.LastIndexOf('.')])).Select(t => t[(t.LastIndexOf('.') + 1)..])];
        return taken.Count == 0
            ? []
            : [.. root.DescendantNodes(n => n is not UsingDirectiveSyntax)
                .OfType<SimpleNameSyntax>()
                .Where(n => n.Parent switch
                {
                    QualifiedNameSyntax q => q.Right != n,
                    MemberAccessExpressionSyntax m => m.Name != n,
                    AliasQualifiedNameSyntax a => a.Name != n,
                    _ => true,
                })
                .Where(n => model.GetSymbolInfo(n, cancellationToken).Symbol is INamedTypeSymbol type
                    && taken.Contains(type.MetadataName)
                    && n.Identifier.ValueText == type.Name // not through an alias
                    && type.ContainingSymbol is not INamespaceSymbol { IsGlobalNamespace: true } // shortcut: would need global::Name, not seen yet
                    && !added.Concat(removed).Contains(type.ContainingNamespace.ToDisplayString()))];
    }

    private static CompilationUnitSyntax Qualify(CompilationUnitSyntax root, List<SimpleNameSyntax> clashing, SemanticModel model) =>
        root.ReplaceNodes(clashing.Select(n => root.GetCurrentNode(n)!), (original, current) =>
        {
            ISymbol container = model.GetSymbolInfo(clashing.First(n => n.Span == original.Span)).Symbol!.ContainingSymbol;
            return SyntaxFactory.QualifiedName(
                SyntaxFactory.ParseName(container.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
                current.WithoutTrivia()).WithTriviaFrom(current);
        });

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
            && candidates.Except(UsedRemovedNamespaces(root, model, removedNamespaces, swappedTypes, cancellationToken)).Any();
    }

    // `x.Should()` of a removed namespace that binds to nothing: a rewrite earlier in the file left the type of its receiver unknown
    // (e.g. inside a lambda argument, TAAK-110), so no pattern saw it and it still needs the removed using.
    public static IEnumerable<SimpleNameSyntax> UnboundCalls(
        SyntaxNode root, SemanticModel model, IReadOnlyCollection<string> extensionMethods, CancellationToken cancellationToken) =>
        root.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(m => m.Parent is InvocationExpressionSyntax && extensionMethods.Contains(m.Name.Identifier.ValueText)
                && model.GetSymbolInfo(m.Name, cancellationToken).Symbol is null)
            .Select(m => m.Name);

    // True when the document names a type or member of a removed namespace (swapped types too), or has inactive code that may:
    // only then does it need the added ones.
    public static async Task<bool> NamesRemovedNamespaceAsync(Document document, IEnumerable<string> removedNamespaces, CancellationToken cancellationToken) =>
        await document.GetSyntaxRootAsync(cancellationToken) is CompilationUnitSyntax root
        && await document.GetSemanticModelAsync(cancellationToken) is SemanticModel model
        && (HasInactiveCode(root) || UsedNamespaces(root, model, [], cancellationToken).Overlaps(removedNamespaces));

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

    // A using inside `namespace A.B` resolves `Mediator` against A.B and A first: an own A.Mediator would win (CS0246).
    private static bool IsShadowed(UsingDirectiveSyntax directive, string ns, SemanticModel model)
    {
        string first = ns.Split('.')[0];
        for (INamespaceSymbol? scope = directive.Parent is BaseNamespaceDeclarationSyntax declaration ? model.GetDeclaredSymbol(declaration) as INamespaceSymbol : null;
            scope is { IsGlobalNamespace: false };
            scope = scope.ContainingNamespace)
        {
            if (scope.GetMembers(first).Any())
            {
                return true;
            }
        }

        return false;
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

    // The Fixer's own document (see Fixer.AddReferences) imports for the model only: it is no using the files have (TAAK-116).
    public const string AddedUsingsPath = "Unshackle.AddedUsings.g.cs";

    private static IEnumerable<UsingDirectiveSyntax> GlobalUsings(Compilation compilation, CancellationToken cancellationToken) =>
        compilation.SyntaxTrees
            .Where(t => t.FilePath != AddedUsingsPath)
            .Select(t => t.GetRoot(cancellationToken))
            .OfType<CompilationUnitSyntax>()
            .SelectMany(r => r.Usings)
            .Where(u => !u.GlobalKeyword.IsKind(SyntaxKind.None));

    // One removed using that stays keeps them all: swapping the others would import a twin of what it brings (e.g. MediatR.NotificationPublishers
    // still used next to using Mediator: TaskWhenAllPublisher is ambiguous, CS0104).
    private static HashSet<string> UsedRemovedNamespaces(
        CompilationUnitSyntax root, SemanticModel model, IEnumerable<string> removedNamespaces, IReadOnlyCollection<string> swappedTypes,
        CancellationToken cancellationToken)
    {
        HashSet<string> used = UsedNamespaces(root, model, swappedTypes, cancellationToken);
        if (used.Overlaps(removedNamespaces))
        {
            used.UnionWith(removedNamespaces);
        }

        return used;
    }

    // Namespaces the document still needs a using for: those of the types and members it binds to by simple name.
    private static HashSet<string> UsedNamespaces(
        CompilationUnitSyntax root, SemanticModel model, IReadOnlyCollection<string> swappedTypes, CancellationToken cancellationToken) =>
        [.. root.DescendantNodes(n => n is not UsingDirectiveSyntax)
            .OfType<SimpleNameSyntax>()
            .Select(n => model.GetSymbolInfo(n, cancellationToken).Symbol)
            .Where(s => s is not null and not INamespaceSymbol && !IsSwapped(s, swappedTypes))
            .Select(s => s!.ContainingNamespace?.ToDisplayString() ?? string.Empty)];
}
