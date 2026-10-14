using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// Every CreateMap&lt;A, B&gt; in a profile the profile patterns rewrite, in the project of a profile and (with the solution) the projects it
/// depends on, by type pair. The fix is all-or-nothing, so a map found here is either written as ToB or nothing is applied at all.
/// </summary>
public class NestedMaps
{
    private static readonly string[] s_nullSettings = ["AllowNullCollections", "AllowNullDestinationValues"];

    // A Solution never changes: every profile rewrite and Map call on the same solution shares the index of a project.
    private static readonly ConditionalWeakTable<Solution, ConcurrentDictionary<ProjectId, Task<NestedMaps>>> s_cache = new();

    private readonly Dictionary<(string Source, string Destination), List<NestedMap>> _maps;

    // Every CreateMap<A, B> type pair in the solution (also outside profiles, e.g. cfg.CreateMap<string, DateTime>().ConvertUsing(...)), with where it is.
    private readonly Dictionary<(string Source, string Destination), string> _configured;

    private NestedMaps(Dictionary<(string Source, string Destination), List<NestedMap>> maps, Dictionary<(string Source, string Destination), string> configured,
        string? nullSetting)
    {
        _maps = maps;
        _configured = configured;
        NullSetting = nullSetting;
    }

    /// <summary>
    /// Where AllowNullCollections or AllowNullDestinationValues is set ("name at file:line"), or null: those settings change what AutoMapper
    /// does with a null nested object or collection, so a copy written for the defaults would differ.
    /// </summary>
    public string? NullSetting { get; }

    // Only the project the model belongs to.
    public static NestedMaps From(SemanticModel model)
    {
        Dictionary<(string Source, string Destination), List<NestedMap>> maps = [];
        AddCompilation(maps, model.Compilation, project: null);
        Dictionary<(string Source, string Destination), string> configured = [];
        foreach (SyntaxTree tree in model.Compilation.SyntaxTrees)
        {
            AddConfigured(configured, tree, model.Compilation.GetSemanticModel(tree));
        }

        return new NestedMaps(maps, configured,
            model.Compilation.SyntaxTrees.Select(t => NullSettingIn(t, model.Compilation.GetSemanticModel(t))).FirstOrDefault(s => s is not null));
    }

    public static Task<NestedMaps> FromAsync(SemanticModel model, Solution solution, CancellationToken cancellationToken) =>
        solution.GetDocument(model.SyntaxTree)?.Project is { } own
            ? s_cache.GetValue(solution, _ => new()).GetOrAdd(own.Id, _ => BuildAsync(model.Compilation, solution, own.Id, cancellationToken))
            : Task.FromResult(From(model));

    public IReadOnlyList<NestedMap> Find(ITypeSymbol source, ITypeSymbol destination) =>
        _maps.TryGetValue((Key(source), Key(destination)), out List<NestedMap>? found) ? found : [];

    /// <summary>
    /// Where a CreateMap&lt;source, destination&gt; is ("file:line"), anywhere in the solution, or null. Such a map (typically with
    /// ConvertUsing) replaces AutoMapper's built-in conversion of the pair.
    /// </summary>
    public string? ConfiguredAt(ITypeSymbol source, ITypeSymbol destination) =>
        _configured.TryGetValue((Key(source), Key(destination)), out string? where) ? where : null;

    private static async Task<NestedMaps> BuildAsync(Compilation own, Solution solution, ProjectId ownId, CancellationToken cancellationToken)
    {
        Dictionary<(string Source, string Destination), List<NestedMap>> maps = [];
        AddCompilation(maps, own, project: null);
        foreach (ProjectId id in solution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(ownId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (solution.GetProject(id) is { Language: LanguageNames.CSharp } project && await project.GetCompilationAsync(cancellationToken) is { } compilation)
            {
                AddCompilation(maps, compilation, id);
            }
        }

        // The settings and global maps can be anywhere, typically in the host project that calls AddAutoMapper (which depends on the
        // profiles, not the reverse).
        Dictionary<(string Source, string Destination), string> configured = [];
        string? nullSetting = null;
        foreach (SyntaxTree ownTree in own.SyntaxTrees)
        {
            SemanticModel ownModel = own.GetSemanticModel(ownTree);
            nullSetting ??= NullSettingIn(ownTree, ownModel);
            AddConfigured(configured, ownTree, ownModel);
        }

        foreach (Document document in solution.Projects.Where(p => p.Id != ownId && p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            string text = (await document.GetTextAsync(cancellationToken)).ToString();
            bool hasSetting = nullSetting is null && s_nullSettings.Any(text.Contains);
            if ((hasSetting || text.Contains("CreateMap", StringComparison.Ordinal))
                && await document.GetSyntaxTreeAsync(cancellationToken) is { } tree && await document.GetSemanticModelAsync(cancellationToken) is { } documentModel)
            {
                nullSetting ??= hasSetting ? NullSettingIn(tree, documentModel) : null;
                AddConfigured(configured, tree, documentModel);
            }
        }

        return new NestedMaps(maps, configured, nullSetting);
    }

    // project: null for the profile's own project.
    private static void AddCompilation(Dictionary<(string Source, string Destination), List<NestedMap>> maps, Compilation compilation, ProjectId? project)
    {
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            string text = tree.GetText().ToString();
            bool hasMaps = text.Contains("CreateMap", StringComparison.Ordinal);
            if (!hasMaps && !text.Contains("Extensions", StringComparison.Ordinal))
            {
                continue;
            }

            SemanticModel model = compilation.GetSemanticModel(tree);
            SyntaxNode root = tree.GetRoot();
            if (hasMaps)
            {
                AddProfiles(maps, root, model, project);
            }

            AddRewritten(maps, root, model, project);
        }
    }

    private static void AddProfiles(Dictionary<(string Source, string Destination), List<NestedMap>> maps, SyntaxNode root, SemanticModel model, ProjectId? project)
    {
        foreach (ClassDeclarationSyntax profile in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => CreateMapPattern.IsProfileWithMaps(c, model)))
        {
            List<(TypeSyntax Destination, ITypeSymbol From, ITypeSymbol To)> found = [];
            foreach (InvocationExpressionSyntax createMap in profile.Members.OfType<ConstructorDeclarationSyntax>().SelectMany(c => c.DescendantNodes())
                .OfType<InvocationExpressionSyntax>())
            {
                if (createMap.Expression is GenericNameSyntax { Identifier.ValueText: "CreateMap", TypeArgumentList.Arguments: [_, { } destination] }
                    && model.GetSymbolInfo(createMap).Symbol is IMethodSymbol { ContainingAssembly.Name: "AutoMapper", TypeArguments: [{ } from, { } to] })
                {
                    found.Add((destination, from, to));
                }
            }

            // The generated class is internal when one of its maps uses an internal type (see CreateMapPattern.Rewrite).
            bool isInternal = found.Any(m => CreateMapPattern.Weakest(CreateMapPattern.Effective(m.From), CreateMapPattern.Effective(m.To)) != Accessibility.Public);
            string? ns = Namespace(model.GetDeclaredSymbol(profile)?.ContainingNamespace);
            foreach ((TypeSyntax destination, ITypeSymbol from, ITypeSymbol to) in found)
            {
                Add(maps, from, to, new NestedMap { Method = CreateMapPattern.MethodName(destination), Namespace = ns, IsInternal = isInternal, Project = project });
            }
        }
    }

    // A profile that a profile pattern already rewrote (earlier in this run on the same file, or in an earlier run) has only its
    // <Profile>Extensions class next to it left.
    private static void AddRewritten(Dictionary<(string Source, string Destination), List<NestedMap>> maps, SyntaxNode root, SemanticModel model, ProjectId? project)
    {
        HashSet<string> extensions = [.. root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => model.GetDeclaredSymbol(c) is INamedTypeSymbol type && CreateMapPattern.IsProfile(type)).Select(c => c.Identifier.ValueText + "Extensions")];
        if (extensions.Count == 0)
        {
            return;
        }

        foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.Identifier.ValueText.StartsWith("To", StringComparison.Ordinal) && m.ParameterList.Parameters.Count == 1
                && m.Parent is ClassDeclarationSyntax { Identifier.ValueText: var name } && extensions.Contains(name)))
        {
            if (model.GetDeclaredSymbol(method) is IMethodSymbol { IsExtensionMethod: true, Parameters: [{ } self] } symbol)
            {
                Add(maps, self.Type, symbol.ReturnType, new NestedMap
                {
                    Method = symbol.Name,
                    Namespace = Namespace(symbol.ContainingNamespace),
                    IsInternal = symbol.ContainingType.DeclaredAccessibility != Accessibility.Public,
                    Project = project,
                });
            }
        }
    }

    private static void Add(Dictionary<(string Source, string Destination), List<NestedMap>> maps, ITypeSymbol from, ITypeSymbol to, NestedMap map)
    {
        (string, string) key = (Key(from), Key(to));
        if (!maps.TryGetValue(key, out List<NestedMap>? list))
        {
            maps[key] = list = [];
        }

        list.Add(map);
    }

    private static void AddConfigured(Dictionary<(string Source, string Destination), string> configured, SyntaxTree tree, SemanticModel model)
    {
        if (!tree.GetText().ToString().Contains("CreateMap", StringComparison.Ordinal))
        {
            return;
        }

        foreach (InvocationExpressionSyntax createMap in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if ((createMap.Expression is GenericNameSyntax { Identifier.ValueText: "CreateMap" }
                    || createMap.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "CreateMap" } })
                && model.GetSymbolInfo(createMap).Symbol is IMethodSymbol { ContainingAssembly.Name: "AutoMapper", TypeArguments: [{ } from, { } to] })
            {
                configured.TryAdd((Key(from), Key(to)), $"{tree.FilePath}:{createMap.GetLocation().GetLineSpan().StartLinePosition.Line + 1}");
            }
        }
    }

    // An assignment to AutoMapper's AllowNullCollections or AllowNullDestinationValues in the tree.
    private static string? NullSettingIn(SyntaxTree tree, SemanticModel model)
    {
        if (!s_nullSettings.Any(tree.GetText().ToString().Contains))
        {
            return null;
        }

        IdentifierNameSyntax? setting = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
            .FirstOrDefault(n => s_nullSettings.Contains(n.Identifier.ValueText)
                && n.Parent is MemberAccessExpressionSyntax { Parent: AssignmentExpressionSyntax assignment } access && assignment.Left == access
                && model.GetSymbolInfo(n).Symbol is IPropertySymbol { ContainingAssembly.Name: "AutoMapper" });
        return setting is null
            ? null
            : $"{setting.Identifier.ValueText} at {tree.FilePath}:{setting.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
    }

    private static string? Namespace(INamespaceSymbol? ns) => ns is null or { IsGlobalNamespace: true } ? null : ns.ToDisplayString();

    // Across compilations a type is the same by its fully qualified name; nullable annotations do not count.
    private static string Key(ITypeSymbol type) => type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
