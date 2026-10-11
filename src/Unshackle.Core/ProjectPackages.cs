using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// Which projects follow the package migration, and whether their code still needs the removed package.
internal static class ProjectPackages
{
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
            Compilation? compilation = await project.GetCompilationAsync(cancellationToken);
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
        }

        List<AddedPackage> added = [.. package.AddedPackages.Where(p => p.OnlyWithCallTo.Count == 0 || p.OnlyWithCallTo.Any(called.Contains))];
        return added.Count == package.AddedPackages.Count
            ? package
            : new PackageMigration { RemovedPackage = package.RemovedPackage, AlsoRemovedPackages = package.AlsoRemovedPackages, AddedPackages = added };
    }

    // True when any document binds a name to a type or member of the assemblies, swapped types aside. Using directives do not count:
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

            bool uses = root.DescendantNodes(n => n is not UsingDirectiveSyntax)
                .OfType<SimpleNameSyntax>()
                .Select(n => model.GetSymbolInfo(n, cancellationToken).Symbol)
                .Any(s => s is not null and not INamespaceSymbol && s.ContainingAssembly?.Name is string name && assemblyNames.Contains(name)
                    && !UsingDirectives.IsSwapped(s, swappedTypes));
            if (uses || UsingDirectives.KeepsRemovedUsing(root, fix.RemovedNamespaces)
                || UsingDirectives.RemovedGlobalUsings(root, document.FilePath, fix.RemovedNamespaces).Any()
                || UsingDirectives.UnboundCalls(root, model, fix.RemovedExtensionMethods, cancellationToken).Any())
            {
                return true;
            }
        }

        return false;
    }
}
