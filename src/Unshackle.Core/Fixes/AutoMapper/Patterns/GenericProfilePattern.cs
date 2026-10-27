using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Generics;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// A generic Profile (class P&lt;T&gt; : Profile { P() { CreateMap&lt;T, T&gt;(); } }) → one B ToB(this A source) per closed map (council round 15, A14).
/// The closed constructions of P come from <see cref="TypeArgumentClosure"/> over the class type parameters: closed subclasses
/// (class RoleProfile : P&lt;Role&gt;), new P&lt;Role&gt;() and AddProfile&lt;P&lt;Role&gt;&gt;(), followed through generic code up to depth 3 with the
/// same packability rule as A11. Each closed pair is written like a CreateMap&lt;A, B&gt;() without options. Fixed only when every pair is;
/// an open use (typeof(P&lt;&gt;), a public P in a packable library, ...) or a pair that cannot be written is Manual GenericProfileOpenSet.
/// Map calls on the closed pairs are not routed to the generated methods yet; such a call stays Manual (and so does the fix).
/// </summary>
public class GenericProfilePattern : ISolutionPattern
{
    private readonly IProjectProperties? _projectProperties;

    // Without project properties (tests, a host without MSBuild) packability is guessed from the compilation (TypeArgumentClosure).
    public GenericProfilePattern()
        : this(null)
    {
    }

    // IsPackable as MSBuild sees it decides whether a public generic profile closes its type arguments (council round 10, R1).
    public GenericProfilePattern(IProjectProperties? projectProperties)
    {
        _projectProperties = projectProperties;
    }

    public string Id => "automapper.generic-profile";

    public string Description =>
        "Generic Profile P<T> with CreateMap<T, T>() → B ToB(this A source) per closed type argument (closed subclasses, new P<X>(), AddProfile<P<X>>); open sets are Manual";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => IsGenericProfile(c, model));

    // A top-level generic Profile with maps; a profile nested in a generic type stays with CreateMapPattern (Manual).
    internal static bool IsGenericProfile(ClassDeclarationSyntax profile, SemanticModel model) =>
        profile.TypeParameterList is not null
        && profile.Parent is not TypeDeclarationSyntax
        && model.GetDeclaredSymbol(profile) is INamedTypeSymbol type
        && ProfileSyntax.IsProfile(type)
        && ProfileSyntax.IsProfileWithMaps(profile, model);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        Open("the closed type arguments of a generic profile are only known with the whole solution");

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        var profile = (ClassDeclarationSyntax)node;
        if (model.GetDeclaredSymbol(profile, cancellationToken) is not INamedTypeSymbol profileType)
        {
            return Open($"Profile {profile.Identifier.ValueText} has no symbol");
        }

        string name = $"Profile {profile.Identifier.ValueText}<{string.Join(", ", profileType.TypeParameters.Select(p => p.Name))}>";
        if (profile.Modifiers.Any(SyntaxKind.PartialKeyword) || profile.Members.OfType<ConstructorDeclarationSyntax>().ToList() is not [{ } constructor])
        {
            return Open($"{name} is partial or has several constructors; write its mapping methods by hand");
        }

        // Only CreateMap<A, B>(); statements: options and other settings are not closed per type argument.
        List<IMethodSymbol> createMaps = [];
        foreach (SyntaxNode statement in ProfileSyntax.MapStatements(constructor))
        {
            if (statement is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { Expression: GenericNameSyntax { Identifier.ValueText: "CreateMap", TypeArgumentList.Arguments.Count: 2 }, ArgumentList.Arguments.Count: 0 } invocation }
                || model.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol { ContainingAssembly.Name: CreateMapPattern.AutoMapperAssembly } method)
            {
                string written = string.Join(' ', statement.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                return Open($"'{written}' in {name} is not a CreateMap<A, B>() without options; write its mapping methods by hand");
            }

            createMaps.Add(method);
        }

        IReadOnlyList<Compilation> compilations = await GenericUses.CompilationsAsync(solution, cancellationToken);
        ClosedSets closed = new TypeArgumentClosure(compilations, solution, _projectProperties).Close(profileType.TypeParameters);
        if (closed.Unresolved is string why)
        {
            return Open($"{name}: {why}");
        }

        IReadOnlySet<string> mapInto = await MapIntoPattern.TargetsAsync(solution, cancellationToken);
        IReadOnlySet<string> projectTo = await ProjectToPattern.TargetsAsync(solution, cancellationToken);
        NestedMaps nested = await NestedMaps.FromAsync(model, solution, _projectProperties, cancellationToken);
        HashSet<string> namespaces = new(StringComparer.Ordinal);
        var values = new MemberValues(nested, model, profile.SpanStart, namespaces);
        List<(TypeSyntax Source, TypeSyntax Destination, List<(string Name, string Value)> Members, bool MapInto, string Creation)> maps = [];
        HashSet<string> pairs = new(StringComparer.Ordinal);
        bool isInternal = false;
        foreach (IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> set in closed.Sets)
        {
            foreach (IMethodSymbol createMap in createMaps)
            {
                if (Closed(createMap.TypeArguments[0], set, model.Compilation) is not ITypeSymbol source
                    || Closed(createMap.TypeArguments[1], set, model.Compilation) is not ITypeSymbol destination)
                {
                    return Open($"{name}: CreateMap<{createMap.TypeArguments[0].ToDisplayString()}, {createMap.TypeArguments[1].ToDisplayString()}> is not closed to plain types visible from {model.Compilation.AssemblyName} by {Describe(set)}");
                }

                string pair = $"CreateMap<{source.Name}, {destination.Name}> (for {Describe(set)})";
                string key = MapIntoPattern.Key(source, destination);
                if (mapInto.Contains(key) || projectTo.Contains(key))
                {
                    return Open($"{name}: {pair} is used by Map(source, destination) or ProjectTo, which are not migrated for a generic profile");
                }

                if (!pairs.Add(key))
                {
                    continue;
                }

                if (CreateMapProblems.Problem(source, destination, [], model, profileType, values, out List<(string Name, string Value)> members, out _, out string creation) is string problem)
                {
                    return Open($"{name}: {pair}: {problem}");
                }

                Accessibility access = MapAccessibility.Weakest(MapAccessibility.Weakest(MapAccessibility.Effective(source), MapAccessibility.Effective(destination)), Accessibility.Public);
                if (access is not (Accessibility.Public or Accessibility.Internal))
                {
                    return Open($"{name}: {pair}: {source.Name} or {destination.Name} is private or protected; an extension method cannot use it");
                }

                isInternal |= access == Accessibility.Internal;
                maps.Add((Written(source, model, profile.SpanStart), Written(destination, model, profile.SpanStart), members, false, creation));
            }
        }

        if (maps.GroupBy(m => (MapMethodWriter.MethodName(m.Destination), m.Source.ToString())).Any(g => g.Count() > 1))
        {
            return Open($"{name}: two closed maps would get the same mapping method; write them by hand");
        }

        string className = MapMethodWriter.ExtensionsName(profile);
        if (model.Compilation.GetSymbolsWithName(className, SymbolFilter.Type, cancellationToken).Any())
        {
            return Open($"A type {className} already exists; write the mapping methods of {name} by hand");
        }

        if (ProfileDirectives.Repair(MapMethodWriter.Emptied(constructor), out ConstructorDeclarationSyntax emptied) is string directiveProblem)
        {
            return Open(directiveProblem);
        }

        return PatternResult.Fixed([
            profile.ReplaceNode(constructor, emptied),
            MapMethodWriter.Extensions(profile, className, isInternal ? "internal" : "public", maps, [], [])], [.. namespaces.Order(StringComparer.Ordinal)]);
    }

    // The type argument with the type parameter of the profile replaced by its closed type, as the profile's compilation sees it; null when
    // it is not a plain (non-generic) type visible there.
    private static ITypeSymbol? Closed(ITypeSymbol written, IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> set, Compilation compilation)
    {
        ITypeSymbol type = written is ITypeParameterSymbol parameter && set.TryGetValue(parameter, out ITypeSymbol? argument) ? argument : written;
        if (GenericUses.ContainsTypeParameter(type))
        {
            return null;
        }

        if (type.SpecialType != SpecialType.None && type is INamedTypeSymbol { IsGenericType: false })
        {
            return compilation.GetSpecialType(type.SpecialType);
        }

        return type is INamedTypeSymbol { IsGenericType: false, TypeKind: not TypeKind.Error } named
            && compilation.GetTypeByMetadataName(GenericUses.MetadataKey(named)) is INamedTypeSymbol local
            && compilation.IsSymbolAccessibleWithin(local, compilation.Assembly)
                ? local
                : null;
    }

    private static TypeSyntax Written(ITypeSymbol type, SemanticModel model, int position) =>
        SyntaxFactory.ParseTypeName(type.ToMinimalDisplayString(model, position));

    private static string Describe(IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> set) =>
        string.Join(", ", set.OrderBy(p => p.Key.Ordinal).Select(p => $"{p.Key.Name} = {p.Value.Name}"));

    private static PatternResult Open(string reason) =>
        PatternResult.Manual(GenericProfileCode.GenericProfileOpenSet, reason.EndsWith('.') ? reason : reason + ".");
}
