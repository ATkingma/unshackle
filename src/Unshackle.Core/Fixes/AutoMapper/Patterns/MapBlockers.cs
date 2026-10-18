using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// Why a Map, MapInto or ProjectTo call finds no generated mapping method for A → B, when the solution does hold a CreateMap&lt;A, B&gt;:
/// the profile around it stays Manual (its own reason is the real blocker), or the CreateMap sits outside a profile constructor
/// (an IMapFrom&lt;T&gt;.Mapping(Profile) method, a static configuration). Null when no such CreateMap is found.
/// </summary>
internal static class MapBlockers
{
    private const string AutoMapperAssembly = "AutoMapper";

    /// <summary>
    /// Why a type pair with several CreateMap definitions in different profiles is left to the user. Measured (AutoMapper 14.0.0, TAAK-265): Map and ProjectTo do not
    /// throw, the definition registered last replaces the earlier ones whole (their members are not merged; with captured parameters too), and
    /// AssertConfigurationIsValid throws DuplicateTypeMapConfigurationException. Which one is last depends on the order the profiles are added
    /// (assembly scanning included), which this fix cannot read, so the generated method of one profile could be the wrong one.
    /// </summary>
    public static string Several(ITypeSymbol source, ITypeSymbol destination) =>
        $"Several profiles map {source.Name} to {destination.Name}; AutoMapper uses the one registered last (its order is not known to this fix, and AssertConfigurationIsValid rejects the duplicate); keep one definition and run the fix again.";

    public static async Task<string?> ReasonAsync(Solution solution, SemanticModel model, ITypeSymbol source, ITypeSymbol destination, CancellationToken cancellationToken)
    {
        string map = $"CreateMap<{source.Name}, {destination.Name}>";
        if (source is ITypeParameterSymbol || destination is ITypeParameterSymbol)
        {
            return $"{map} maps a type parameter: the map depends on the type argument at run time; migrate the call by hand.";
        }

        string sourceName = source.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string destinationName = destination.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        IEnumerable<Project> projects = solution.GetDocument(model.SyntaxTree)?.Project is { } callProject
            ? solution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(callProject.Id).Append(callProject.Id).Select(solution.GetProject).OfType<Project>()
            : solution.Projects;
        string? outside = null;
        foreach (Document document in projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!(await document.GetTextAsync(cancellationToken)).ToString().Contains("CreateMap", StringComparison.Ordinal)
                || await document.GetSyntaxRootAsync(cancellationToken) is not { } root || await document.GetSemanticModelAsync(cancellationToken) is not { } mapModel)
            {
                continue;
            }

            foreach (InvocationExpressionSyntax createMap in root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(i => Maps(i, mapModel, sourceName, destinationName, cancellationToken)))
            {
                ClassDeclarationSyntax? profile = createMap.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
                if (profile is not null && CreateMapPattern.IsProfileWithMaps(profile, mapModel) && CreateMapPattern.IsInMapStatements(createMap, profile, mapModel))
                {
                    if (await CreateMapPattern.RewriteMethodsAsync(profile, mapModel, solution, cancellationToken) is ManualResult { Reason: var reason })
                    {
                        return $"{map} is in profile {profile.Identifier.ValueText}, which this fix does not rewrite: {reason}";
                    }

                    continue;
                }

                outside ??= $"{map} is in {Where(createMap)}, outside a Profile constructor; this fix only rewrites the maps of a profile's constructor. "
                    + "Move the map into a profile (or write ToB by hand) and run the fix again.";
            }
        }

        return outside;
    }

    // CreateMap<A, B>(), CreateMap(typeof(A), typeof(B)), or CreateMap<B, A>().ReverseMap() of AutoMapper.
    private static bool Maps(InvocationExpressionSyntax invocation, SemanticModel model, string source, string destination, CancellationToken cancellationToken)
    {
        if (invocation.Expression is not (GenericNameSyntax { Identifier.ValueText: "CreateMap" } or IdentifierNameSyntax { Identifier.ValueText: "CreateMap" }
            or MemberAccessExpressionSyntax { Name.Identifier.ValueText: "CreateMap" })
            || model.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol { Name: "CreateMap" } method
            || method.ContainingAssembly?.Name != AutoMapperAssembly)
        {
            return false;
        }

        List<ITypeSymbol> types = method.TypeArguments.Length == 2
            ? [.. method.TypeArguments]
            : [.. invocation.ArgumentList.Arguments.Take(2).Select(a => a.Expression is TypeOfExpressionSyntax t ? model.GetTypeInfo(t.Type, cancellationToken).Type : null).OfType<ITypeSymbol>()];
        if (types is not [{ } from, { } to])
        {
            return false;
        }

        string fromName = from.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string toName = to.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return (fromName == source && toName == destination) || (fromName == destination && toName == source && CreateMapPattern.HasReverseMap(invocation));
    }

    private static string Where(SyntaxNode node)
    {
        string? type = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText;
        string? member = node.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault() switch
        {
            MethodDeclarationSyntax m => m.Identifier.ValueText,
            ConstructorDeclarationSyntax => "its constructor",
            PropertyDeclarationSyntax p => p.Identifier.ValueText,
            _ => null,
        };
        return type is null ? "top-level code" : member is null ? type : member == "its constructor" ? $"the constructor of {type}" : $"{type}.{member}";
    }
}
