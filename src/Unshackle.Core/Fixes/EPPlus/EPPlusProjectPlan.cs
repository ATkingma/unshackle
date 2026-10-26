using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.EPPlus.Patterns;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus;

// TAAK-326 (council round 13, E17): the EPPlus partial migration is per project, never per file. A project stays on EPPlus when it holds a
// blocking Manual row, shares a file with a kept project, or declares an EPPlus-typed member that a kept project uses, or that a kept project
// gets EPPlus through (EpplusTypeCrossesProject). Every other project that uses EPPlus migrates completely.
internal static class EPPlusProjectPlan
{
    private const string EPPlusAssembly = "EPPlus";

    private static readonly StringComparer s_paths = StringComparer.OrdinalIgnoreCase;

    internal static async Task<PartialPlan?> PlanAsync(
        IFix fix, IPackageAssetsReader? assetsReader, Solution solution, IReadOnlyList<Finding> roots, CancellationToken cancellationToken)
    {
        List<IGrouping<string, Project>> projects =
        [
            .. solution.Projects.Where(p => p.Language == LanguageNames.CSharp && p.FilePath is not null).GroupBy(p => p.FilePath!, s_paths),
        ];
        HashSet<string> usesEPPlus = new(s_paths);
        foreach (IGrouping<string, Project> project in projects)
        {
            foreach (Project copy in project)
            {
                if (await copy.GetCompilationAsync(cancellationToken) is Compilation compilation
                    && compilation.ReferencedAssemblyNames.Any(a => a.Name == EPPlusAssembly))
                {
                    usesEPPlus.Add(project.Key);
                    break;
                }
            }
        }

        var kept = new Dictionary<string, string>(s_paths);
        List<Finding> rows = [];
        foreach (Finding root in roots)
        {
            List<string> owners = [.. projects.Where(p => s_paths.Equals(p.Key, root.FilePath) || p.SelectMany(c => c.Documents).Any(d => s_paths.Equals(d.FilePath, root.FilePath))).Select(p => p.Key)];
            if (owners.Count == 0)
            {
                // A row outside every project (a shared props file): no project can be proven independent of it.
                return null;
            }

            foreach (string owner in owners)
            {
                kept.TryAdd(owner, "manual findings in this project");
            }
        }

        // Closure: keeping a project can force another one to stay (shared files, EPPlus crossing a project reference).
        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (IGrouping<string, Project> candidate in projects.Where(p => usesEPPlus.Contains(p.Key) && !kept.ContainsKey(p.Key)))
            {
                (string Reason, ManualResult? Row)? keep = await KeepReasonAsync(candidate, projects, kept, assetsReader, cancellationToken);
                if (keep is not { } found)
                {
                    continue;
                }

                (string reason, ManualResult? row) = found;

                kept[candidate.Key] = reason;
                if (row is not null)
                {
                    rows.Add(FindingFactory.CreateProjectFinding(candidate.Key, fix, row));
                }

                grew = true;
                break;
            }
        }

        // A project that only sees EPPlus through a kept project (no code of its own names it) has nothing to migrate: it gets no ClosedXML.
        HashSet<string> ownCode = new(s_paths);
        foreach (IGrouping<string, Project> project in projects.Where(p => usesEPPlus.Contains(p.Key)))
        {
            foreach (Document document in project.SelectMany(p => p.Documents))
            {
                if ((await document.GetTextAsync(cancellationToken)).ToString().Contains(EPPlusSyntax.Namespace, StringComparison.Ordinal))
                {
                    ownCode.Add(project.Key);
                    break;
                }
            }
        }

        foreach (IGrouping<string, Project> project in projects.Where(p => usesEPPlus.Contains(p.Key) && !kept.ContainsKey(p.Key) && !ownCode.Contains(p.Key)))
        {
            HashSet<ProjectId> keptIds = [.. projects.Where(k => kept.ContainsKey(k.Key)).SelectMany(k => k).Select(k => k.Id)];
            if (project.Any(p => DependsOn(p, keptIds)))
            {
                kept[project.Key] = "no EPPlus code of its own; it sees EPPlus only through a project that stays on it";
            }
        }

        if (!usesEPPlus.Any(p => !kept.ContainsKey(p) && ownCode.Contains(p)))
        {
            return null;
        }

        // The documents of a kept project go to no pattern: their roots carry the kept annotation.
        Solution planned = solution;
        foreach (Document document in projects.Where(p => kept.ContainsKey(p.Key)).SelectMany(p => p).SelectMany(p => p.Documents))
        {
            if (await document.GetSyntaxRootAsync(cancellationToken) is SyntaxNode syntax)
            {
                planned = planned.WithDocumentSyntaxRoot(document.Id, syntax.WithAdditionalAnnotations(new SyntaxAnnotation(PartialPlan.KeptAnnotationKind)));
            }
        }

        return new PartialPlan { Solution = planned, Findings = [.. roots, .. rows], KeptProjects = kept };
    }

    // Why a project that would migrate has to stay, with the row that reports it (null: the reason is reported elsewhere).
    private static async Task<(string Reason, ManualResult? Row)?> KeepReasonAsync(
        IGrouping<string, Project> candidate,
        IReadOnlyList<IGrouping<string, Project>> projects,
        Dictionary<string, string> kept,
        IPackageAssetsReader? assetsReader,
        CancellationToken cancellationToken)
    {
        HashSet<string> files = new(candidate.SelectMany(p => p.Documents).Select(d => d.FilePath ?? d.Name), s_paths);
        foreach (IGrouping<string, Project> other in projects.Where(p => kept.ContainsKey(p.Key)))
        {
            if (other.SelectMany(p => p.Documents).FirstOrDefault(d => files.Contains(d.FilePath ?? d.Name)) is Document shared)
            {
                return ($"shares {shared.FilePath ?? shared.Name} with {other.Key}", null);
            }
        }

        HashSet<ProjectId> candidateIds = [.. candidate.Select(p => p.Id)];
        foreach (IGrouping<string, Project> other in projects.Where(p => kept.ContainsKey(p.Key)))
        {
            foreach (Project user in other.Where(p => DependsOn(p, candidateIds)))
            {
                if (await CrossingUseAsync(user, candidate, cancellationToken) is string use)
                {
                    return (nameof(EPPlusReason.EpplusTypeCrossesProject), (ManualResult)PatternResult.Manual(EPPlusReason.EpplusTypeCrossesProject,
                        $"{use}, used by {user.Name}, which stays on EPPlus. This project keeps EPPlus too; migrate {user.Name} first or remove that use."));
                }

                // The kept project may get EPPlus only through this project's reference: removing it here would break the kept one.
                if (!DeclaresEPPlus(user, assetsReader))
                {
                    return (nameof(EPPlusReason.EpplusTypeCrossesProject), (ManualResult)PatternResult.Manual(EPPlusReason.EpplusTypeCrossesProject,
                        $"{user.Name} stays on EPPlus and references this project, but it is not proven to declare EPPlus itself (no current restore "
                        + "assets), so removing EPPlus here could break it. Add an EPPlus PackageReference to that project, or migrate it first."));
                }
            }
        }

        return null;
    }

    private static bool DependsOn(Project project, HashSet<ProjectId> targets)
    {
        HashSet<ProjectId> seen = [];
        Stack<ProjectId> pending = new(project.ProjectReferences.Select(r => r.ProjectId));
        while (pending.Count > 0)
        {
            ProjectId id = pending.Pop();
            if (targets.Contains(id))
            {
                return true;
            }

            if (seen.Add(id) && project.Solution.GetProject(id) is Project next)
            {
                foreach (ProjectReference reference in next.ProjectReferences)
                {
                    pending.Push(reference.ProjectId);
                }
            }
        }

        return false;
    }

    private static bool DeclaresEPPlus(Project project, IPackageAssetsReader? assetsReader) =>
        assetsReader is not null && project.FilePath is string path && assetsReader.Read(path) is { State: AssetsState.Current } assets
        && assets.Requirements.Any(r => r.RequiredBy == "project" && string.Equals(r.Package, EPPlusAssembly, StringComparison.OrdinalIgnoreCase));

    // The first symbol of the candidate project that the user's code binds to and whose signature (or base types) names an EPPlus type.
    private static async Task<string?> CrossingUseAsync(Project user, IGrouping<string, Project> candidate, CancellationToken cancellationToken)
    {
        HashSet<string> assemblies = [.. candidate.Select(p => p.AssemblyName)];
        foreach (Document document in user.Documents)
        {
            if (await document.GetSyntaxRootAsync(cancellationToken) is not SyntaxNode root || await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
            {
                continue;
            }

            foreach (SyntaxNode node in root.DescendantNodes().Where(n => n.IsKind(SyntaxKind.IdentifierName) || n.IsKind(SyntaxKind.GenericName)))
            {
                ISymbol? symbol = model.GetSymbolInfo(node, cancellationToken).Symbol;
                if (symbol is not null && symbol.ContainingAssembly is IAssemblySymbol assembly && assemblies.Contains(assembly.Name) && Exposes(symbol))
                {
                    return $"{symbol.ToDisplayString()} names an EPPlus type";
                }
            }
        }

        return null;
    }

    private static bool Exposes(ISymbol symbol) => symbol switch
    {
        IMethodSymbol method => NamesEPPlus(method.ReturnType) || method.Parameters.Any(p => NamesEPPlus(p.Type)) || method.TypeArguments.Any(NamesEPPlus),
        IPropertySymbol property => NamesEPPlus(property.Type) || property.Parameters.Any(p => NamesEPPlus(p.Type)),
        IFieldSymbol field => NamesEPPlus(field.Type),
        IEventSymbol @event => NamesEPPlus(@event.Type),
        INamedTypeSymbol type => (type.BaseType is INamedTypeSymbol baseType && NamesEPPlus(baseType)) || type.AllInterfaces.Any(NamesEPPlus)
            || type.TypeArguments.Any(NamesEPPlus) || (type.DelegateInvokeMethod is IMethodSymbol invoke && Exposes(invoke)),
        _ => false,
    };

    // The type itself or its base chain is (or carries) an EPPlus type.
    private static bool NamesEPPlus(ITypeSymbol type)
    {
        for (ITypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (IsEPPlus(current))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsEPPlus(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => NamesEPPlus(array.ElementType),
        IPointerTypeSymbol pointer => NamesEPPlus(pointer.PointedAtType),
        INamedTypeSymbol named => named.ContainingAssembly?.Name == EPPlusAssembly || named.TypeArguments.Any(IsEPPlus),
        _ => false,
    };
}
