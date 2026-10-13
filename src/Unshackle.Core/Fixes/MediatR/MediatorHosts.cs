using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Fixes.MediatR.Patterns;

namespace Unshackle.Core.Fixes.MediatR;

/// <summary>
/// Projects that get Mediator's source generator (an AddMediatR call or an Autofac module scanning MediatR), and what a host needs
/// when it references another host (TAAK-213). Each host generates its own Mediator, AddMediator and Mediator.Internals types:
/// a referenced host makes them internal (GenerateTypesAsInternal), the referencing host limits its scan to the assemblies that
/// are no host (Assemblies), so it never names the internal handlers of the other host and needs no InternalsVisibleTo from it.
/// Measured on Mediator 3.0.2: with InternalsVisibleTo from host to host no option helps (CS0121 on AddMediator, CS0436).
/// </summary>
public static class MediatorHosts
{
    private static readonly string[] s_messageInterfaces =
        ["IRequest", "IRequest`1", "INotification", "IStreamRequest`1", "IRequestHandler`1", "IRequestHandler`2", "INotificationHandler`1", "IStreamRequestHandler`2"];

    /// <summary>
    /// Assembly names of the C# projects with an AddMediatR call or an Autofac module that scans MediatR, sorted; an AddMediator call counts
    /// too, because the Fixer passes the solution with the current file already partly rewritten.
    /// </summary>
    /// <param name="solution">The solution of the file being fixed.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    public static async Task<IReadOnlySet<string>> AssemblyNamesAsync(Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solution);
        var addMediatR = new AddMediatRPattern();
        var autofac = new AutofacMediatorModulePattern();
        SortedSet<string> hosts = new(StringComparer.Ordinal);
        foreach (Project project in solution.Projects.Where(p => p.Language == LanguageNames.CSharp))
        {
            foreach (Document document in project.Documents)
            {
                string text = (await document.GetTextAsync(cancellationToken)).ToString();
                bool mayCall = text.Contains("AddMediatR", StringComparison.Ordinal);
                bool mayBeFixed = text.Contains("AddMediator(", StringComparison.Ordinal);
                bool mayScan = text.Contains("RegisterAssemblyTypes", StringComparison.Ordinal);
                if (!(mayCall || mayScan || mayBeFixed)
                    || await document.GetSyntaxRootAsync(cancellationToken) is not SyntaxNode root
                    || await document.GetSemanticModelAsync(cancellationToken) is not SemanticModel model)
                {
                    continue;
                }

                if ((mayCall && addMediatR.Find(root, model).Any()) || (mayScan && autofac.Find(root, model).Any()) || (mayBeFixed && CallsAddMediator(root)))
                {
                    hosts.Add(project.AssemblyName);
                    break;
                }
            }
        }

        return hosts;
    }

    /// <summary>
    /// The AddMediator settings this host needs next to the other hosts of the solution: none when it is the only one in its
    /// references, otherwise GenerateTypesAsInternal and/or Assemblies; or why it cannot be done (<see cref="MediatorHostSettings.ManualReason"/>).
    /// </summary>
    /// <param name="node">The node with the AddMediatR call or the Autofac scan, in <paramref name="model"/>'s tree.</param>
    /// <param name="model">The semantic model of the host's document.</param>
    /// <param name="solution">The original solution.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    public static async Task<MediatorHostSettings> SettingsAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(solution);
        IAssemblySymbol self = model.Compilation.Assembly;
        if (solution.Projects.FirstOrDefault(p => p.Language == LanguageNames.CSharp && p.AssemblyName == self.Name) is not Project project)
        {
            return MediatorHostSettings.None;
        }

        IReadOnlySet<string> hosts = await AssemblyNamesAsync(solution, cancellationToken);
        ProjectDependencyGraph graph = solution.GetProjectDependencyGraph();
        List<string> referencedBy = [.. graph.GetProjectsThatTransitivelyDependOnThisProject(project.Id)
            .Select(id => solution.GetProject(id)!.AssemblyName)
            .Where(hosts.Contains)
            .Order(StringComparer.Ordinal)];
        HashSet<string> referenced = [.. graph.GetProjectsThatThisProjectTransitivelyDependsOn(project.Id)
            .Select(id => solution.GetProject(id)!.AssemblyName)
            .Where(hosts.Contains)];
        List<(string Property, string Value)> assignments = [];
        if (referencedBy.Count > 0)
        {
            // InternalsVisibleTo to a host that generates its own Mediator: the internal generated types collide there anyway.
            if (referencedBy.FirstOrDefault(h => Friends(self).Contains(h)) is string friend)
            {
                return new MediatorHostSettings
                {
                    Assignments = [],
                    ManualReason = $"{self.Name} grants InternalsVisibleTo to {friend}, which gets Mediator's source generator too: their generated Mediator types "
                        + "would collide there (CS0436, CS0121 on AddMediator). Remove that InternalsVisibleTo, or call AddMediator in only one of the two projects.",
                };
            }

            assignments.Add(("GenerateTypesAsInternal", "true"));
        }

        if (referenced.Count > 0)
        {
            List<string> types = [];
            if (OwnType(node, model) is not INamedTypeSymbol own)
            {
                return Manual($"{self.Name} references the Mediator host {referenced.Order(StringComparer.Ordinal).First()} and has no non-generic type to list itself in AddMediator's Assemblies; set Assemblies by hand.");
            }

            types.Add(own.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            foreach (IAssemblySymbol assembly in model.Compilation.SourceModule.ReferencedAssemblySymbols
                .Where(a => !referenced.Contains(a.Name) && !hosts.Contains(a.Name) && HasMessages(a))
                .OrderBy(a => a.Name, StringComparer.Ordinal))
            {
                if (Representative(assembly, model.Compilation) is not INamedTypeSymbol type)
                {
                    return Manual($"{self.Name} references the Mediator host {referenced.Order(StringComparer.Ordinal).First()}, so AddMediator lists its assemblies, "
                        + $"but {assembly.Name} has no non-generic type {self.Name} can name; set Assemblies by hand.");
                }

                types.Add(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            assignments.Add(("Assemblies", $"[{string.Join(", ", types.Select(t => $"typeof({t})"))}]"));
        }

        return new MediatorHostSettings { Assignments = assignments };
    }

    // A file the fixer already rewrote passes the working solution, where AddMediator is unbound (no generator ran): by name.
    private static bool CallsAddMediator(SyntaxNode root) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Any(i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddMediator" });

    private static MediatorHostSettings Manual(string reason) => new() { Assignments = [], ManualReason = reason };

    private static HashSet<string> Friends(IAssemblySymbol assembly) =>
        [.. assembly.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.InternalsVisibleToAttribute")
            .Select(a => (a.ConstructorArguments.FirstOrDefault().Value as string)?.Split(',')[0].Trim())
            .OfType<string>()];

    // The type around the call, or else the first non-generic type of the project: Mediator's generator reads typeof(T) from source.
    private static INamedTypeSymbol? OwnType(SyntaxNode node, SemanticModel model)
    {
        INamedTypeSymbol? around = model.GetEnclosingSymbol(node.SpanStart)?.ContainingType;
        return around is not null && IsNameable(around)
            ? around
            : AddMediatRPattern.Types(model.Compilation.Assembly.GlobalNamespace)
                .Where(IsNameable)
                .OrderBy(t => t.ToDisplayString(), StringComparer.Ordinal)
                .FirstOrDefault();
    }

    // A referenced assembly with MediatR (or Mediator) messages or handlers: the ones Mediator's generator would have scanned.
    private static bool HasMessages(IAssemblySymbol assembly) =>
        assembly.Name is not ("MediatR" or "MediatR.Contracts") && !assembly.Name.StartsWith("Mediator", StringComparison.Ordinal)
        && assembly.Modules.Any(m => m.ReferencedAssemblies.Any(r => r.Name is "MediatR" or "MediatR.Contracts" or "Mediator.Abstractions"))
        && AddMediatRPattern.Types(assembly.GlobalNamespace).Any(IsMessage);

    private static bool IsMessage(INamedTypeSymbol type) =>
        type.AllInterfaces.Any(i => s_messageInterfaces.Contains(i.MetadataName) && i.ContainingNamespace.ToDisplayString() is "MediatR" or "Mediator");

    // A type of the assembly this project can name: a message or handler first, else any; sorted by name.
    private static INamedTypeSymbol? Representative(IAssemblySymbol assembly, Compilation compilation)
    {
        List<INamedTypeSymbol> nameable = [.. AddMediatRPattern.Types(assembly.GlobalNamespace)
            .Where(t => IsNameable(t) && AddMediatRPattern.IsVisibleOutside(t) && compilation.IsSymbolAccessibleWithin(t, compilation.Assembly))
            .OrderBy(t => t.ToDisplayString(), StringComparer.Ordinal)];
        return nameable.FirstOrDefault(IsMessage) ?? nameable.FirstOrDefault();
    }

    private static bool IsNameable(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
        {
            if (current.TypeParameters.Length > 0 || current.IsAnonymousType || current.IsImplicitlyDeclared || !current.CanBeReferencedByName)
            {
                return false;
            }
        }

        return true;
    }
}
