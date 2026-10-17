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
        // A rewrite can leave an old call unbound until a namespace it needs is imported: `new DateTime(..).At(..)` without
        // `using System;` (TAAK-242). What the file uses with the missing usings added counts too.
        if (missing.Count > 0)
        {
            Document withMissing = document.WithSyntaxRoot(
                root.AddUsings([.. missing.Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns)))]));
            if (await withMissing.GetSyntaxRootAsync(cancellationToken) is CompilationUnitSyntax withMissingRoot
                && await withMissing.GetSemanticModelAsync(cancellationToken) is SemanticModel withMissingModel)
            {
                used.UnionWith(UsedRemovedNamespaces(withMissingRoot, withMissingModel, removedNamespaces, swappedTypes, cancellationToken));
            }
        }

        if (UnboundCalls(root, model, removedExtensionMethods, cancellationToken).Any())
        {
            used.UnionWith(removedNamespaces);
        }

        List<UsingDirectiveSyntax> unused = InactiveCodeMayUse(root, model.Compilation, removedNamespaces)
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

        if (swapCount > 0)
        {
            updated = Resort(root, updated);
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
            return document.WithSyntaxRoot(Resort(root, updated.ReplaceNode(target, target.WithUsings(target.Usings.AddRange(added))), originalMinimum: 2)).Project.Solution;
        }

        // New usings go after the existing ones (or on top of the file); a list that was sorted is sorted again (SA1210, TAAK-212).
        usings.AddRange(missing.Select(ns => SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(ns).WithLeadingTrivia(SyntaxFactory.Space))
            .WithTrailingTrivia(root.Usings.Count == 0 ? SyntaxFactory.TriviaList(endOfLine, endOfLine) : SyntaxFactory.TriviaList(endOfLine))));

        return document.WithSyntaxRoot(Resort(root, updated.WithUsings([.. usings]), originalMinimum: 2)).Project.Solution;
    }

    // A swapped name may land out of order (using Shouldly; before using NUnit.Framework;, SA1210): each using list that was sorted
    // (System first or not) is sorted again; an unsorted one is left as the user had it (TAAK-152). For added usings the list must have had
    // at least originalMinimum usings: one using says nothing about the order the user keeps (TAAK-212).
    private static CompilationUnitSyntax Resort(CompilationUnitSyntax before, CompilationUnitSyntax after, int originalMinimum = 0)
    {
        List<SyntaxNode> containers = [.. Containers(before)];
        List<SyntaxNode> targets = [.. Containers(after)];
        return after.ReplaceNodes(targets, (original, rewritten) =>
        {
            SyntaxNode source = containers[targets.IndexOf(original)];
            SyntaxList<UsingDirectiveSyntax> was = UsingsOf(source);
            SyntaxList<UsingDirectiveSyntax> now = UsingsOf(original);
            List<int> plain = [.. Enumerable.Range(0, now.Count).Where(i => IsPlainUsing(now[i]) && now[i].GlobalKeyword.IsKind(SyntaxKind.None))];
            List<string> oldNames = [.. was.Where(u => IsPlainUsing(u) && u.GlobalKeyword.IsKind(SyntaxKind.None)).Select(u => u.Name!.ToString())];
            if (plain.Count < 2 || oldNames.Count < originalMinimum)
            {
                return rewritten;
            }

            foreach (bool systemFirst in (bool[])[true, false])
            {
                Func<string, (int, string)> key = n => (systemFirst && (n == "System" || n.StartsWith("System.", StringComparison.Ordinal)) ? 0 : 1, n);
                IComparer<(int, string)> order = Comparer<(int, string)>.Create((a, b) => a.Item1 != b.Item1
                    ? a.Item1.CompareTo(b.Item1)
                    : StringComparer.OrdinalIgnoreCase.Compare(a.Item2, b.Item2));
                if (!oldNames.Select(key).SequenceEqual(oldNames.Select(key).Order(order)))
                {
                    continue;
                }

                List<UsingDirectiveSyntax> sorted = [.. plain.Select(i => now[i]).OrderBy(u => key(u.Name!.ToString()), order)];
                var result = new List<UsingDirectiveSyntax>(now);
                for (int j = 0; j < plain.Count; j++)
                {
                    // Trivia stays by position: a leading comment or blank line belongs to the place, not to the name.
                    result[plain[j]] = sorted[j].WithTriviaFrom(now[plain[j]]);
                }

                return rewritten switch
                {
                    CompilationUnitSyntax unit => unit.WithUsings([.. result]),
                    BaseNamespaceDeclarationSyntax ns => ns.WithUsings([.. result]),
                    _ => rewritten,
                };
            }

            return rewritten;
        });
    }

    private static IEnumerable<SyntaxNode> Containers(CompilationUnitSyntax root) =>
        root.DescendantNodesAndSelf(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax)
            .Where(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax);

    private static SyntaxList<UsingDirectiveSyntax> UsingsOf(SyntaxNode container) => container switch
    {
        CompilationUnitSyntax unit => unit.Usings,
        BaseNamespaceDeclarationSyntax ns => ns.Usings,
        _ => [],
    };

    // Own types named like a type of an added namespace (an own ICommand next to Mediator.ICommand): once the using is in,
    // the name is ambiguous (CS0104) or binds to the added type, so these references get their full name (TAAK-144).
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

    // Same for a global using of the added namespaces (SwapGlobalUsings): it reaches every file of the project (TAAK-147).
    public static async Task<Solution> QualifyClashingAsync(
        Document document, IReadOnlyList<string> added, IEnumerable<string> removed, IReadOnlyCollection<string> addedTypes, CancellationToken cancellationToken)
    {
        if (await document.GetSyntaxRootAsync(cancellationToken) is not CompilationUnitSyntax root
            || await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
        {
            return document.Project.Solution;
        }

        List<SimpleNameSyntax> clashing = Clashing(root, model, [.. added], removed, addedTypes, cancellationToken);
        return clashing.Count == 0
            ? document.Project.Solution
            : document.WithSyntaxRoot(Qualify(root.TrackNodes(clashing), clashing, model)).Project.Solution;
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
        && (InactiveCodeMayUse(root, model.Compilation, removedNamespaces) || UsedNamespaces(root, model, [], cancellationToken).Overlaps(removedNamespaces));

    // Code in an inactive #if branch is only trivia: nothing binds there, so we tokenise it and look for a name of the removed
    // namespaces (their types, their extension methods, the namespace itself). Inactive code without one needs no using (TAAK-182).
    public static bool InactiveCodeMayUse(SyntaxNode root, Compilation compilation, IEnumerable<string> removedNamespaces)
    {
        List<SyntaxTrivia> inactive = [.. root.DescendantTrivia().Where(t => t.IsKind(SyntaxKind.DisabledTextTrivia))];
        if (inactive.Count == 0)
        {
            return false;
        }

        HashSet<string> names = RemovedNames(compilation, [.. removedNamespaces]);
        return inactive.Any(t => SyntaxFactory.ParseTokens(t.ToString()).Any(token => token.IsKind(SyntaxKind.IdentifierToken) && names.Contains(token.ValueText)));
    }

    private static HashSet<string> RemovedNames(Compilation compilation, IReadOnlyCollection<string> removedNamespaces)
    {
        HashSet<string> names = [.. removedNamespaces.Select(ns => ns.Split('.')[0])];
        foreach (string ns in removedNamespaces)
        {
            INamespaceSymbol? scope = compilation.GlobalNamespace;
            foreach (string part in ns.Split('.'))
            {
                scope = scope?.GetNamespaceMembers().FirstOrDefault(m => m.Name == part);
            }

            foreach (INamedTypeSymbol type in scope?.GetTypeMembers().Where(t => t.DeclaredAccessibility == Accessibility.Public) ?? [])
            {
                names.Add(type.Name);
                names.UnionWith(type.GetMembers().OfType<IMethodSymbol>().Where(m => m.IsExtensionMethod).Select(m => m.Name));
            }
        }

        return names;
    }

    // A top-level using of a removed namespace that stays because inactive code may still need it.
    // The first call that an added namespace made ambiguous (CS0121) with a method that is not from an added namespace,
    // e.g. Shouldly's ShouldBe next to Akka.TestKit's own (TAAK-174); null when there is none.
    public static async Task<(IMethodSymbol Added, IMethodSymbol Existing, int Line)?> AmbiguityWithAddedAsync(
        Document document, IReadOnlyCollection<string> addedNamespaces, CancellationToken cancellationToken)
    {
        if (await document.GetSyntaxRootAsync(cancellationToken) is not SyntaxNode root
            || await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
        {
            return null;
        }

        foreach ((IMethodSymbol added, IMethodSymbol existing, int line) in Ambiguities(root, model, addedNamespaces, cancellationToken))
        {
            return (added, existing, line);
        }

        return null;
    }

    private static IEnumerable<(IMethodSymbol Added, IMethodSymbol Existing, int Line)> Ambiguities(
        SyntaxNode root, SemanticModel model, IReadOnlyCollection<string> addedNamespaces, CancellationToken cancellationToken)
    {
        foreach (Diagnostic diagnostic in model.GetDiagnostics(cancellationToken: cancellationToken).Where(d => d.Id == "CS0121"))
        {
            SyntaxNode node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            if (node.FirstAncestorOrSelf<InvocationExpressionSyntax>() is not InvocationExpressionSyntax invocation)
            {
                continue;
            }

            IMethodSymbol[] candidates = [.. model.GetSymbolInfo(invocation, cancellationToken).CandidateSymbols.OfType<IMethodSymbol>()];
            IMethodSymbol? added = candidates.FirstOrDefault(m => IsIn(m, addedNamespaces));
            IMethodSymbol? existing = candidates.FirstOrDefault(m => !IsIn(m, addedNamespaces));
            if (added is not null && existing is not null)
            {
                yield return (added, existing, diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1);
            }
        }
    }

    private static bool IsIn(ISymbol symbol, IReadOnlyCollection<string> namespaces) =>
        namespaces.Contains(symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty);

    // The first call to a method named like one of the added namespaces (e.g. ShouldBe) that binds elsewhere, and that the original
    // file did not already have: an extension in the enclosing namespace wins over an imported one without ambiguity, e.g.
    // Akka.TestKit.AkkaSpecExtensions.ShouldBe in a file in namespace Akka.TestKit (TAAK-199); null when there is none.
    public static async Task<(IMethodSymbol Bound, int Line)?> MisboundAddedCallAsync(
        Document original, Document updated, IReadOnlyCollection<string> addedNamespaces, CancellationToken cancellationToken)
    {
        if (await updated.GetSyntaxRootAsync(cancellationToken) is not SyntaxNode root
            || await updated.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
        {
            return null;
        }

        List<(IMethodSymbol Bound, int Line)> misbound = await MisboundAsync(original, root, model, addedNamespaces, cancellationToken);
        return misbound.Count == 0 ? null : misbound[0];
    }

    // Per method name, the last call that binds elsewhere when the updated file has more of them than the original.
    private static async Task<List<(IMethodSymbol Bound, int Line)>> MisboundAsync(
        Document original, SyntaxNode root, SemanticModel model, IReadOnlyCollection<string> addedNamespaces, CancellationToken cancellationToken)
    {
        HashSet<string> names = AddedMethodNames(model.Compilation, addedNamespaces);
        List<(IMethodSymbol Bound, InvocationExpressionSyntax Invocation)> misbound = Misbound(root, model, names, addedNamespaces, cancellationToken);
        if (misbound.Count == 0)
        {
            return [];
        }

        Dictionary<string, int> before = [];
        if (await original.GetSyntaxRootAsync(cancellationToken) is SyntaxNode originalRoot
            && await original.GetSemanticModelAsync(cancellationToken) is SemanticModel originalModel)
        {
            foreach ((IMethodSymbol bound, _) in Misbound(originalRoot, originalModel, names, addedNamespaces, cancellationToken))
            {
                before[bound.Name] = before.GetValueOrDefault(bound.Name) + 1;
            }
        }

        return [.. misbound.GroupBy(m => m.Bound.Name)
            .Where(group => group.Count() > before.GetValueOrDefault(group.Key))
            .Select(group => (group.Last().Bound, group.Last().Invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1))];

        static List<(IMethodSymbol, InvocationExpressionSyntax)> Misbound(
            SyntaxNode root, SemanticModel model, HashSet<string> names, IReadOnlyCollection<string> addedNamespaces, CancellationToken cancellationToken) =>
            [.. root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(i => i.Expression switch
                {
                    MemberAccessExpressionSyntax member => names.Contains(member.Name.Identifier.ValueText),
                    SimpleNameSyntax name => names.Contains(name.Identifier.ValueText),
                    _ => false,
                })
                .Select(i => (Bound: model.GetSymbolInfo(i, cancellationToken).Symbol as IMethodSymbol, Invocation: i))
                .Where(m => m.Bound is not null && !addedNamespaces.Contains(m.Bound.ContainingNamespace?.ToDisplayString() ?? string.Empty))
                .Select(m => (m.Bound!, m.Invocation))];
    }

    // Every call in the candidate (a file with targeted imports, TAAK-198) that an added namespace breaks: ambiguous (CS0121)
    // or a rewritten call that binds to an own method (TAAK-199). The reasons are per call: the rest of the file is migrated.
    public static async Task<IReadOnlyList<AddedConflict>> ConflictsWithAddedAsync(
        Document original, Document candidate, IReadOnlyCollection<string> addedNamespaces, CancellationToken cancellationToken)
    {
        if (await candidate.GetSyntaxRootAsync(cancellationToken) is not SyntaxNode root
            || await candidate.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
        {
            return [];
        }

        List<AddedConflict> conflicts = [.. Ambiguities(root, model, addedNamespaces, cancellationToken).Select(a => new AddedConflict
        {
            Name = a.Added.Name,
            AddedType = a.Added.ContainingType.OriginalDefinition.ToDisplayString(),
            Reason = $"{a.Added.ContainingType.ToDisplayString()}.{a.Added.Name} is ambiguous with {a.Existing.ContainingType.ToDisplayString()}.{a.Existing.Name} "
                + $"(CS0121), so {a.Added.ContainingType.ToDisplayString()} is not imported here and this call is not migrated. "
                + "Rename or remove the conflicting extension, or migrate this call by hand.",
            Line = a.Line,
        })];
        conflicts.AddRange((await MisboundAsync(original, root, model, addedNamespaces, cancellationToken)).Select(m => new AddedConflict
        {
            Name = m.Bound.Name,
            AddedType = null,
            Reason = $"a rewritten {m.Bound.Name}() binds to {m.Bound.ContainingType.ToDisplayString()}.{m.Bound.Name} instead of "
                + $"{string.Join(", ", addedNamespaces)} (an own method in the enclosing namespace or type wins over an imported extension), "
                + "so this call is not migrated. Rename or remove the conflicting method, or migrate this call by hand.",
            Line = m.Line,
        }));
        return conflicts;
    }

    // The extension methods (with their static class) and the types of the added namespaces the rewritten file uses. The model
    // imports the added namespaces globally (Fixer.AddReferences), so an ambiguous call counts with its added candidates.
    // Null when a type cannot get an alias (generic) or the file already imports an added namespace itself.
    public static async Task<AddedUsage?> AddedUsageAsync(Document document, IReadOnlyCollection<string> addedNamespaces, CancellationToken cancellationToken)
    {
        if (await document.GetSyntaxRootAsync(cancellationToken) is not CompilationUnitSyntax root
            || await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel model
            || AllUsings(root).Any(u => u.Name is not null && addedNamespaces.Contains(u.Name.ToString())))
        {
            return null;
        }

        List<(string Name, string Type)> calls = [];
        HashSet<string> types = new(StringComparer.Ordinal);
        foreach (SimpleNameSyntax name in root.DescendantNodes(n => n is not UsingDirectiveSyntax).OfType<SimpleNameSyntax>())
        {
            SymbolInfo info = model.GetSymbolInfo(name, cancellationToken);
            foreach (ISymbol symbol in info.Symbol is ISymbol bound ? [bound] : info.CandidateSymbols)
            {
                if (symbol is IMethodSymbol method && (method.ReducedFrom ?? method).IsExtensionMethod && IsIn(method, addedNamespaces))
                {
                    calls.Add((method.Name, method.ContainingType.OriginalDefinition.ToDisplayString()));
                }
                else if (symbol is INamedTypeSymbol type && type.ContainingType is null && IsIn(type, addedNamespaces) && !IsMemberName(name))
                {
                    if (type.IsGenericType)
                    {
                        return null;
                    }

                    types.Add(type.ToDisplayString());
                }
            }
        }

        return new AddedUsage { Calls = calls, Types = [.. types.Order(StringComparer.Ordinal)] };

        static bool IsMemberName(SimpleNameSyntax name) => name.Parent switch
        {
            QualifiedNameSyntax q => q.Right == name,
            MemberAccessExpressionSyntax m => m.Name == name,
            AliasQualifiedNameSyntax a => a.Name == name,
            _ => false,
        };
    }

    // Instead of `using Shouldly;`: `using static` for each class whose extensions the file calls, and an alias for each type it
    // names (using Should = Shouldly.Should;), after the existing usings, so a conflicting class stays out (TAAK-198).
    public static CompilationUnitSyntax AddTargetedImports(CompilationUnitSyntax root, IEnumerable<string> staticClasses, IEnumerable<string> types)
    {
        List<string> lines =
        [
            .. staticClasses.Distinct().Order(StringComparer.Ordinal).Select(c => $"using static {c};"),
            .. types.Distinct().Order(StringComparer.Ordinal).Select(t => $"using {t[(t.LastIndexOf('.') + 1)..]} = {t};"),
        ];
        if (lines.Count == 0)
        {
            return root;
        }

        SyntaxTrivia endOfLine = root.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        if (endOfLine.IsKind(SyntaxKind.None))
        {
            endOfLine = SyntaxFactory.LineFeed;
        }

        if (root.Usings.Count == 0
            && root.Members.OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0) is BaseNamespaceDeclarationSyntax target)
        {
            SyntaxTriviaList indentation = [.. target.Usings.Last().GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia))];
            return root.ReplaceNode(target, target.WithUsings(target.Usings.AddRange(
                lines.Select(l => SyntaxFactory.ParseCompilationUnit(l).Usings[0].WithLeadingTrivia(indentation).WithTrailingTrivia(endOfLine)))));
        }

        SyntaxTriviaList last = root.Usings.Count == 0 ? SyntaxFactory.TriviaList(endOfLine, endOfLine) : SyntaxFactory.TriviaList(endOfLine);
        return root.WithUsings(root.Usings.AddRange(lines.Select((l, i) =>
            SyntaxFactory.ParseCompilationUnit(l).Usings[0].WithTrailingTrivia(i == lines.Count - 1 ? last : SyntaxFactory.TriviaList(endOfLine)))));
    }

    public static async Task<int> ErrorCountAsync(Document document, CancellationToken cancellationToken) =>
        await document.GetSemanticModelAsync(cancellationToken) is SemanticModel model
            ? model.GetDiagnostics(cancellationToken: cancellationToken).Count(d => d.Severity == DiagnosticSeverity.Error)
            : 0;

    // Names of the extension methods of the public static types in the added namespaces.
    private static HashSet<string> AddedMethodNames(Compilation compilation, IEnumerable<string> addedNamespaces)
    {
        HashSet<string> names = [];
        foreach (string ns in addedNamespaces)
        {
            INamespaceSymbol? scope = compilation.GlobalNamespace;
            foreach (string part in ns.Split('.'))
            {
                scope = scope?.GetNamespaceMembers().FirstOrDefault(m => m.Name == part);
            }

            foreach (INamedTypeSymbol type in scope?.GetTypeMembers().Where(t => t.DeclaredAccessibility == Accessibility.Public && t.IsStatic) ?? [])
            {
                names.UnionWith(type.GetMembers().OfType<IMethodSymbol>().Where(m => m.IsExtensionMethod).Select(m => m.Name));
            }
        }

        return names;
    }

    public static bool KeepsRemovedUsing(SyntaxNode root, Compilation compilation, IEnumerable<string> removedNamespaces) =>
        root is CompilationUnitSyntax unit && InactiveCodeMayUse(unit, compilation, removedNamespaces) && AllUsings(unit)
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

        return await document.GetSemanticModelAsync(cancellationToken) is SemanticModel model
            && (InactiveCodeMayUse(root, model.Compilation, removedNamespaces)
                || UsedNamespaces(root, model, swappedTypes, cancellationToken).Overlaps(removedNamespaces));
    }

    // A name written from global:: (global::MediatR.IMediator, global::MediatR.Unit.Value): it needs no using, and a using swap
    // does not move it. A partial migration pins the code that stays on the old library that way (TAAK-274).
    public static bool IsGlobalQualified(SimpleNameSyntax name)
    {
        SyntaxNode? left = name.Parent switch
        {
            QualifiedNameSyntax qualified when qualified.Right == name => qualified.Left,
            MemberAccessExpressionSyntax member when member.Name == name => member.Expression,
            _ => null,
        };
        while (left is QualifiedNameSyntax or MemberAccessExpressionSyntax)
        {
            left = left is QualifiedNameSyntax q ? q.Left : ((MemberAccessExpressionSyntax)left).Expression;
        }

        return left is AliasQualifiedNameSyntax { Alias.Identifier.ValueText: "global" };
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
            .Where(n => !IsGlobalQualified(n))
            .Select(n => model.GetSymbolInfo(n, cancellationToken).Symbol)
            .Where(s => s is not null and not INamespaceSymbol && !IsSwapped(s, swappedTypes))
            .Select(s => s!.ContainingNamespace?.ToDisplayString() ?? string.Empty)];
}
