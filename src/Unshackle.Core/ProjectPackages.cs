using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// Which projects follow the package migration, and whether their code still needs the removed package.
internal static class ProjectPackages
{
    public static async Task<IReadOnlyList<ProjectMigration>> PlanAsync(
        Solution solution,
        IFix fix,
        PackageMigration package,
        CancellationToken cancellationToken)
    {
        var migrations = new List<ProjectMigration>();
        foreach (Project project in solution.Projects.Where(p => p.Language == LanguageNames.CSharp && p.FilePath is not null))
        {
            Compilation? compilation = await project.GetCompilationAsync(cancellationToken);
            // shortcut: package id = assembly name (true for FluentAssertions); a package with other assembly names needs a list.
            if (compilation is null || !compilation.ReferencedAssemblyNames.Any(a => a.Name == package.RemovedPackage))
            {
                continue;
            }

            migrations.Add(new ProjectMigration
            {
                ProjectPath = project.FilePath!,
                Package = package,
                KeepRemovedPackage = await UsesAssemblyAsync(project, package.RemovedPackage, cancellationToken),
                RemovedNamespaces = fix.RemovedNamespaces,
                AddedNamespaces = fix.AddedNamespaces,
            });
        }

        return migrations;
    }

    // True when any document binds a name to a type or member of the assembly. Using directives do not count:
    // the global usings MSBuild generates from <Using Include> would otherwise always keep the package.
    private static async Task<bool> UsesAssemblyAsync(Project project, string assemblyName, CancellationToken cancellationToken)
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
                .Any(s => s is not null and not INamespaceSymbol && s.ContainingAssembly?.Name == assemblyName);
            if (uses)
            {
                return true;
            }
        }

        return false;
    }
}
