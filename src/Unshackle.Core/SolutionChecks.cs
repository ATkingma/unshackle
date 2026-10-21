using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core;

// Checks and steps over the whole solution around the per-file passes: files that are not migrated, and the global usings.
internal static class SolutionChecks
{
    // A global using applies to the whole project (every target): it is swapped only when no file still needs it.
    // Otherwise the added namespaces are imported next to it, so the migrated files still compile.
    internal static async Task<Solution> SwapGlobalUsingsAsync(
        Solution solution,
        IFix fix,
        HashSet<ProjectId> globalProjects,
        HashSet<string> swappedTypes,
        HashSet<string> notUtf8,
        List<Finding> findings,
        CancellationToken cancellationToken)
    {
        foreach (IGrouping<string, ProjectId> project in globalProjects.GroupBy(id => solution.GetProject(id)!.FilePath ?? id.Id.ToString(), StringComparer.Ordinal))
        {
            bool keep = false;
            foreach (Document document in project.SelectMany(id => solution.GetProject(id)!.Documents))
            {
                keep = keep || await UsingDirectives.MayUseRemovedNamespaceAsync(document, fix.RemovedNamespaces, swappedTypes, cancellationToken);
            }

            foreach (DocumentId documentId in project.SelectMany(id => solution.GetProject(id)!.DocumentIds).ToList())
            {
                if (notUtf8.Contains(solution.GetDocument(documentId)!.FilePath ?? documentId.Id.ToString()))
                {
                    continue;
                }

                solution = await UsingDirectives.QualifyClashingAsync(
                    solution.GetDocument(documentId)!, fix.AddedNamespaces, fix.RemovedNamespaces, fix.AddedTypes, cancellationToken);
            }

            HashSet<string> reported = new(StringComparer.Ordinal);
            Dictionary<ProjectId, HashSet<string>> imported = [];
            foreach (Document document in project.SelectMany(id => solution.GetProject(id)!.Documents).Where(d => !notUtf8.Contains(d.FilePath ?? d.Id.Id.ToString())))
            {
                SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
                List<UsingDirectiveSyntax> globals = [.. UsingDirectives.RemovedGlobalUsings(root, document.FilePath, fix.RemovedNamespaces)];
                if (globals.Count == 0)
                {
                    continue;
                }

                if (!imported.TryGetValue(document.Project.Id, out HashSet<string>? projectImports))
                {
                    Compilation compilation = (await document.Project.GetCompilationAsync(cancellationToken))!;
                    projectImports = [.. UsingDirectives.GlobalUsings(compilation, cancellationToken).Where(u => u.Alias is null && u.StaticKeyword.IsKind(SyntaxKind.None))
                        .Select(u => u.Name!.ToString())];
                    imported[document.Project.Id] = projectImports;
                }

                solution = solution.WithDocumentSyntaxRoot(
                    document.Id, UsingDirectives.SwapGlobalUsings((CompilationUnitSyntax)root, globals, fix.AddedNamespaces, keep, projectImports));
                string filePath = document.FilePath ?? document.Name;
                if (keep && reported.Add(filePath))
                {
                    findings.AddRange(globals.Select(g => FindingFactory.CreateUsingsFinding(filePath, g.GetLocation().GetLineSpan().StartLinePosition.Line + 1, fix,
                        PatternResult.Manual($"{Fixer.KeptGlobalUsingReason}{g.Name} (or has inactive #if code), so its global using stays next to the new one. " +
                            "Fix the manual findings and run the fix again."))));
                }
            }
        }

        return solution;
    }

    // Roslyn reads a file with a BOM or valid UTF-8 as UTF-8, and anything else (Latin-1, UTF-16) with that other encoding.
    // Text without an encoding (made in memory) is written as UTF-8.
    // Files that one project can migrate (it references the old library) but another project compiles without any reference to the old
    // or the new one (TAAK-281, serilogmem): its project file gets no new package, so the migrated file would not build there.
    internal static async Task<HashSet<string>> UnreachableFilesAsync(Solution solution, IFix fix, CancellationToken cancellationToken)
    {
        if (fix.Package is not PackageMigration package)
        {
            return [];
        }

        HashSet<string> old = [package.RemovedPackage, .. package.AlsoRemovedPackages];
        HashSet<string> added = [.. package.AddedPackages.Select(p => p.Name)];
        var reach = new Dictionary<ProjectId, (bool Old, bool Neither)>();
        foreach (Project project in solution.Projects.Where(p => p.Language == LanguageNames.CSharp))
        {
            Compilation? compilation = await project.GetCompilationAsync(cancellationToken);
            string[] names = compilation is null ? [] : [.. compilation.ReferencedAssemblyNames.Select(a => a.Name)];
            reach[project.Id] = (names.Any(old.Contains), !names.Any(old.Contains) && !names.Any(added.Contains));
        }

        return [.. solution.Projects.Where(p => p.Language == LanguageNames.CSharp)
            .SelectMany(p => p.Documents.Where(d => d.FilePath is not null).Select(d => (Path: d.FilePath!, Project: p.Id)))
            .GroupBy(d => d.Path, StringComparer.Ordinal)
            .Where(g => g.Any(d => reach[d.Project].Old) && g.Any(d => reach[d.Project].Neither))
            .Select(g => g.Key)];
    }

    internal static async Task<HashSet<string>> NotUtf8FilesAsync(Solution solution, CancellationToken cancellationToken)
    {
        HashSet<string> paths = new(StringComparer.Ordinal);
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            if (document.FilePath is not null && (await document.GetTextAsync(cancellationToken)).Encoding is { } encoding and not UTF8Encoding && encoding.CodePage != Encoding.Latin1.CodePage)
            {
                paths.Add(document.FilePath);
            }
        }

        return paths;
    }
}
