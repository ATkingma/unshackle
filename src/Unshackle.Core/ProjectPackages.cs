using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// Which projects follow the package migration, and whether their code still needs the removed package.
internal static class ProjectPackages
{
    // AddedPackage.OnlyWithCallTo entry for typeof(T).Assembly with T of the removed package: an Autofac scan that registers
    // MediatR's own classes is the hand-written AddMediatR (TAAK-196).
    public const string TypeofAssembly = "typeof().Assembly";

    public static async Task<IReadOnlyList<ProjectMigration>> PlanAsync(
        Solution original,
        Solution solution,
        IFix fix,
        PackageMigration package,
        IReadOnlyCollection<string> swappedTypes,
        CancellationToken cancellationToken)
    {
        var migrations = new List<ProjectMigration>();
        foreach (Project project in solution.Projects.Where(p => p.Language == LanguageNames.CSharp && p.FilePath is not null))
        {
            // The references do not change during a fix: the original's compilation is already built at load (TAAK-298).
            Compilation? compilation = await (original.GetProject(project.Id) ?? project).GetCompilationAsync(cancellationToken);
            // shortcut: package id = assembly name (true for FluentAssertions and MediatR); other packages need a mapping.
            HashSet<string> removed = [package.RemovedPackage, .. package.AlsoRemovedPackages];
            if (compilation is null || !compilation.ReferencedAssemblyNames.Any(a => removed.Contains(a.Name)))
            {
                continue;
            }

            migrations.Add(new ProjectMigration
            {
                ProjectPath = project.FilePath!,
                Package = await ForProjectAsync(original.GetProject(project.Id) ?? project, removed, package, cancellationToken),
                KeepRemovedPackage = await UsesAssemblyAsync(project, removed, fix, swappedTypes, cancellationToken),
                RemovedNamespaces = fix.RemovedNamespaces,
                AddedNamespaces = fix.AddedNamespaces,
            });
        }

        // A multi-target project is loaded once per target framework; it keeps the package when any of them needs it.
        return
        [
            .. migrations.GroupBy(m => m.ProjectPath, StringComparer.Ordinal).Select(g => g.FirstOrDefault(m => m.KeepRemovedPackage) ?? g.First()),
        ];
    }

    // The package migration without the added packages this project's code has no call for (AddedPackage.OnlyWithCallTo).
    // Semantic, on the original code: only a call that binds to the removed package counts, not an own method with that name (TAAK-147).
    private static async Task<PackageMigration> ForProjectAsync(
        Project original, HashSet<string> removed, PackageMigration package, CancellationToken cancellationToken)
    {
        var called = new HashSet<string>(StringComparer.Ordinal);
        foreach (Document document in original.Documents)
        {
            if (await document.GetSyntaxRootAsync(cancellationToken) is not SyntaxNode root
                || await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
            {
                continue;
            }

            called.UnionWith(root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Select(i => model.GetSymbolInfo(i, cancellationToken).Symbol)
                .Where(s => s?.ContainingAssembly?.Name is string name && removed.Contains(name))
                .Select(s => s!.Name));
            if (root.DescendantNodes().OfType<TypeOfExpressionSyntax>().Any(t => IsAssemblyOf(t)
                && model.GetTypeInfo(t.Type, cancellationToken).Type?.ContainingAssembly?.Name is string name && removed.Contains(name)))
            {
                called.Add(TypeofAssembly);
            }
        }

        List<AddedPackage> added = [.. package.AddedPackages.Where(p => p.OnlyWithCallTo.Count == 0 || p.OnlyWithCallTo.Any(called.Contains))];
        return added.Count == package.AddedPackages.Count
            ? package
            : new PackageMigration { RemovedPackage = package.RemovedPackage, AlsoRemovedPackages = package.AlsoRemovedPackages, AddedPackages = added };
    }

    // typeof(T).Assembly or typeof(T).GetTypeInfo().Assembly.
    private static bool IsAssemblyOf(TypeOfExpressionSyntax typeOf) => typeOf.Parent switch
    {
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Assembly" } => true,
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetTypeInfo", Parent: InvocationExpressionSyntax { Parent: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Assembly" } } } => true,
        _ => false,
    };

    // True when any document binds a name to a type or member of the assemblies, swapped types aside (unless written from global::, TAAK-274). Using directives do not count:
    // the global usings MSBuild generates from <Using Include> would otherwise always keep the package. Except a using that
    // inactive #if code may need, a global using the fixer kept, or an unbound x.Should() (TAAK-110): that code would break without the package.
    private static async Task<bool> UsesAssemblyAsync(
        Project project, HashSet<string> assemblyNames, IFix fix, IReadOnlyCollection<string> swappedTypes,
        CancellationToken cancellationToken)
    {
        foreach (Document document in project.Documents)
        {
            SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
            SemanticModel? model = await document.GetSemanticModelAsync(cancellationToken);
            if (root is null || model is null)
            {
                continue;
            }

            bool uses = RemovedAssemblyUses(root, model, assemblyNames, swappedTypes, cancellationToken).Any();
            if (uses || UsingDirectives.KeepsRemovedUsing(root, model.Compilation, fix.RemovedNamespaces)
                || UsingDirectives.RemovedGlobalUsings(root, document.FilePath, fix.RemovedNamespaces).Any()
                || UsingDirectives.UnboundCalls(root, model, fix.RemovedExtensionMethods, cancellationToken).Any())
            {
                return true;
            }
        }

        return false;
    }

    // Where the code of these projects still names a type or member of the removed package (swapped types aside): the places no pattern migrated.
    public static async Task<IReadOnlyList<(string FilePath, int Line, string Symbol)>> RemainingUsesAsync(
        Solution solution,
        string projectPath,
        IEnumerable<string> assemblyNames,
        IReadOnlyCollection<string> swappedTypes,
        CancellationToken cancellationToken)
    {
        HashSet<string> names = [.. assemblyNames];
        var uses = new List<(string FilePath, int Line, string Symbol)>();
        foreach (Document document in solution.Projects.Where(p => p.FilePath == projectPath).SelectMany(p => p.Documents))
        {
            SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
            SemanticModel? model = await document.GetSemanticModelAsync(cancellationToken);
            if (root is null || model is null)
            {
                continue;
            }

            uses.AddRange(RemovedAssemblyUses(root, model, names, swappedTypes, cancellationToken)
                .Select(u => (document.FilePath ?? document.Name, u.Name.GetLocation().GetLineSpan().StartLinePosition.Line + 1, u.Symbol.ToDisplayString())));
        }

        return [.. uses.DistinctBy(u => (u.FilePath, u.Line))];
    }

    private static IEnumerable<(SimpleNameSyntax Name, ISymbol Symbol)> RemovedAssemblyUses(
        SyntaxNode root, SemanticModel model, HashSet<string> assemblyNames, IReadOnlyCollection<string> swappedTypes, CancellationToken cancellationToken) =>
        root.DescendantNodes(n => n is not UsingDirectiveSyntax)
            .OfType<SimpleNameSyntax>()
            .Select(n => (Name: n, Symbol: model.GetSymbolInfo(n, cancellationToken).Symbol))
            .Where(u => u.Symbol is not null and not INamespaceSymbol && u.Symbol.ContainingAssembly?.Name is string name && assemblyNames.Contains(name)
                && (!UsingDirectives.IsSwapped(u.Symbol, swappedTypes) || UsingDirectives.IsGlobalQualified(u.Name)))
            .Select(u => (u.Name, u.Symbol!));
}
