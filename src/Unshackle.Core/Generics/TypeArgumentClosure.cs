using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using static Unshackle.Core.Generics.GenericUses;

namespace Unshackle.Core.Generics;

/// <summary>
/// Closes type parameters over the solution (council round 13, A11): the closed type arguments a generic method or type gets from its
/// callers, its uses and its derived types, followed transitively through generic code up to <see cref="MaxDepth"/> levels. The set is only
/// proven when every caller is in the solution: a member visible outside a packable project (R1, IsPackable from the project properties when given, else guessed as
/// AssemblyResolver does without project properties; R2: internal stays closed unless InternalsVisibleTo names an assembly outside the
/// solution), a call through a base or interface member, a method group, a dynamic call, <c>MakeGenericMethod</c>/<c>MakeGenericType</c>
/// or an unbound <c>typeof(G&lt;&gt;)</c> (an open generic DI registration) leave it open, with the place named.
/// Constraints only filter the closed set.
/// </summary>
public class TypeArgumentClosure
{
    public const int MaxDepth = 3;

    private static readonly HashSet<string> s_testFrameworks =
        new(["Microsoft.TestPlatform.CoreUtilities", "Microsoft.VisualStudio.TestPlatform.TestFramework", "xunit.core", "xunit.v3.core", "nunit.framework"], StringComparer.OrdinalIgnoreCase);

    private readonly IReadOnlyList<Compilation> _compilations;
    private readonly HashSet<string> _assemblies;
    private readonly Solution? _solution;
    private readonly IProjectProperties? _projectProperties;

    public TypeArgumentClosure(IReadOnlyList<Compilation> compilations)
    {
        ArgumentNullException.ThrowIfNull(compilations);
        _compilations = compilations;
        _assemblies = new([.. compilations.Select(c => c.AssemblyName ?? "")], StringComparer.Ordinal);
    }

    // TAAK-324: with the project properties IsPackable is read as MSBuild sees it (council round 10, R1), so a public member of a library with
    // IsPackable=false is closed too; without them (or for a project without a file) it is guessed from the compilation.
    public TypeArgumentClosure(IReadOnlyList<Compilation> compilations, Solution solution, IProjectProperties? projectProperties)
        : this(compilations)
    {
        _solution = solution;
        _projectProperties = projectProperties;
    }

    /// <summary>
    /// The closed sets of the type parameters, which all belong to one generic method and/or the generic type that declares it.
    /// </summary>
    public ClosedSets Close(IReadOnlyList<ITypeParameterSymbol> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var failure = new List<string>();
        List<Dictionary<ITypeParameterSymbol, ITypeSymbol>>? sets = Close(parameters, 1, failure);
        if (sets is null)
        {
            return new ClosedSets { Sets = [], Unresolved = failure.FirstOrDefault() ?? "the closed set is not known" };
        }

        List<Dictionary<ITypeParameterSymbol, ITypeSymbol>> kept = [.. sets.Where(s => parameters.All(p => Satisfies(s[p], p)))];
        return kept.Count == 0
            ? new ClosedSets { Sets = [], Unresolved = $"no caller in the solution closes {string.Join(", ", parameters.Select(p => p.Name))}" }
            : new ClosedSets { Sets = kept, Unresolved = null };
    }

    private List<Dictionary<ITypeParameterSymbol, ITypeSymbol>>? Close(IReadOnlyList<ITypeParameterSymbol> parameters, int depth, List<string> failure)
    {
        string names = string.Join(", ", parameters.Select(p => p.Name));
        if (depth > MaxDepth)
        {
            return Fail(failure, $"closing {names} goes through more than {MaxDepth} levels of generic code");
        }

        List<IMethodSymbol> methods = [.. parameters.Select(p => p.DeclaringMethod?.OriginalDefinition).OfType<IMethodSymbol>().Distinct(SymbolEqualityComparer.Default).OfType<IMethodSymbol>()];
        List<INamedTypeSymbol> types = [.. parameters.Select(p => p.DeclaringType?.OriginalDefinition).OfType<INamedTypeSymbol>().Distinct(SymbolEqualityComparer.Default).OfType<INamedTypeSymbol>()];
        if (methods.Count > 1 || types.Count > 1
            || (methods.Count == 1 && types.Count == 1 && !SymbolEqualityComparer.Default.Equals(methods[0].ContainingType.OriginalDefinition, types[0])))
        {
            return Fail(failure, $"{names} belong to more than one generic method or type");
        }

        return methods.Count == 1 ? CloseByCalls(methods[0], parameters, depth, failure) : CloseByUses(types[0], parameters, depth, failure);
    }

    // A generic method: the type arguments of every call (and of the type the called method is a member of).
    private List<Dictionary<ITypeParameterSymbol, ITypeSymbol>>? CloseByCalls(IMethodSymbol method, IReadOnlyList<ITypeParameterSymbol> parameters, int depth, List<string> failure)
    {
        string name = $"{method.ContainingType.Name}.{method.Name}";
        if (method.MethodKind is not MethodKind.Ordinary)
        {
            return Fail(failure, $"{name} is a local function, lambda or accessor; its type arguments are not followed");
        }

        if (ReachableOutside(method) is string outside)
        {
            return Fail(failure, $"{name} {outside}");
        }

        if (method.IsVirtual || method.IsAbstract || method.IsOverride || !method.ExplicitInterfaceImplementations.IsEmpty || ImplementsInterface(method))
        {
            return Fail(failure, $"{name} is virtual, an override or an interface member: callers through the base or interface are not followed");
        }

        if (Reflection("MakeGenericMethod", null) is string reflection)
        {
            return Fail(failure, $"MakeGenericMethod is called at {reflection}; a closed {name} may be made by reflection");
        }

        string id = Id(method);
        var sets = new List<Dictionary<ITypeParameterSymbol, ITypeSymbol>>();
        foreach (Compilation compilation in _compilations)
        {
            foreach (SimpleNameSyntax reference in Names(compilation, method.Name))
            {
                if (InNameof(reference))
                {
                    continue;
                }

                SemanticModel model = compilation.GetSemanticModel(reference.SyntaxTree);
                SymbolInfo info = model.GetSymbolInfo(reference);
                if (info.Symbol is not IMethodSymbol called)
                {
                    if (info.CandidateSymbols.OfType<IMethodSymbol>().Any(c => Id(c) == id)
                        || (reference.Parent is MemberAccessExpressionSyntax { Expression: var receiver } && model.GetTypeInfo(receiver).Type is { TypeKind: TypeKind.Dynamic }))
                    {
                        return Fail(failure, $"{name} may be called dynamically at {Where(reference)}");
                    }

                    continue;
                }

                // TAAK-325: query.ProjectToPage<T, TResult>(...) binds to the reduced extension method, whose id differs from the declaration's; it was
                // skipped silently. Its constructed unreduced form carries the same type arguments, so it is followed like a static call.
                if (called.ReducedFrom is not null && called.GetConstructedReducedFrom() is { } unreduced)
                {
                    called = unreduced;
                }

                if (Id(called) != id)
                {
                    continue;
                }

                if (!IsInvoked(reference))
                {
                    return Fail(failure, $"{name} is used as a method group at {Where(reference)}; its type arguments are not followed");
                }

                List<ITypeSymbol> arguments = [.. parameters.Select(p => p.DeclaringMethod is not null ? called.TypeArguments[p.Ordinal] : called.ContainingType.TypeArguments[p.Ordinal])];
                if (Resolve(arguments, parameters, reference, model, depth, failure) is not { } resolved)
                {
                    return null;
                }

                sets.AddRange(resolved);
            }
        }

        return Distinct(sets, parameters);
    }

    // A generic type: the type arguments of every written use (new, field and parameter types, base classes of derived types).
    private List<Dictionary<ITypeParameterSymbol, ITypeSymbol>>? CloseByUses(INamedTypeSymbol type, IReadOnlyList<ITypeParameterSymbol> parameters, int depth, List<string> failure)
    {
        if (type.ContainingType is not null)
        {
            return Fail(failure, $"{type.Name} is a nested generic type; its type arguments are not followed");
        }

        if (ReachableOutside(type) is string outside)
        {
            return Fail(failure, $"{type.Name} {outside}");
        }

        if (Reflection("MakeGenericType", type) is string reflection)
        {
            return Fail(failure, $"MakeGenericType is called at {reflection}; a closed {type.Name} may be made by reflection");
        }

        var sets = new List<Dictionary<ITypeParameterSymbol, ITypeSymbol>>();
        foreach (GenericUse use in Find(type, _compilations, true))
        {
            if (use.Kind == GenericUseKind.Unbound)
            {
                return Fail(failure, $"typeof({type.Name}<>) is used at {use.Where} (an open generic registration or reflection); its closed types are not known");
            }

            List<ITypeSymbol> arguments = [.. parameters.Select(p => use.Constructed.TypeArguments[p.Ordinal])];
            if (Resolve(arguments, parameters, use.Node, use.Model, depth, failure) is not { } resolved)
            {
                return null;
            }

            sets.AddRange(resolved);
        }

        return Distinct(sets, parameters);
    }

    // The closed sets one place gives: its arguments, with the type parameters of the code around it closed in turn.
    private List<Dictionary<ITypeParameterSymbol, ITypeSymbol>>? Resolve(List<ITypeSymbol> arguments, IReadOnlyList<ITypeParameterSymbol> parameters, SyntaxNode node,
        SemanticModel model, int depth, List<string> failure)
    {
        if (arguments.Zip(parameters).All(p => SymbolEqualityComparer.Default.Equals(p.First, p.Second)))
        {
            // The member uses itself with its own type parameters (recursion, this-calls): nothing new.
            return [];
        }

        if (!arguments.Any(ContainsTypeParameter))
        {
            return [Set(parameters, arguments)];
        }

        if (arguments.FirstOrDefault(a => a is not ITypeParameterSymbol && ContainsTypeParameter(a)) is ITypeSymbol nested)
        {
            return Fail(failure, $"{nested.ToDisplayString()} at {Where(node)} closes a type parameter through another generic type; this is not followed");
        }

        List<ITypeParameterSymbol> free = [.. arguments.OfType<ITypeParameterSymbol>().Distinct(SymbolEqualityComparer.Default).OfType<ITypeParameterSymbol>()];
        if (free.Any(f => f.DeclaringMethod is { MethodKind: not MethodKind.Ordinary }))
        {
            return Fail(failure, $"the type arguments at {Where(node)} come from a local function; they are not followed");
        }

        if (Close(free, depth + 1, failure) is not { } outer)
        {
            if (failure.Count > 0 && !failure[^1].Contains(Where(node), StringComparison.Ordinal))
            {
                failure[^1] += $" (via {Where(node)})";
            }

            return null;
        }

        return [.. outer.Select(o => Set(parameters, [.. arguments.Select(a => a is ITypeParameterSymbol p ? o[p] : a)]))];
    }

    private static Dictionary<ITypeParameterSymbol, ITypeSymbol> Set(IReadOnlyList<ITypeParameterSymbol> parameters, List<ITypeSymbol> arguments)
    {
        var set = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        for (int i = 0; i < parameters.Count; i++)
        {
            set[parameters[i]] = arguments[i];
        }

        return set;
    }

    private static List<Dictionary<ITypeParameterSymbol, ITypeSymbol>> Distinct(List<Dictionary<ITypeParameterSymbol, ITypeSymbol>> sets, IReadOnlyList<ITypeParameterSymbol> parameters) =>
        [.. sets.GroupBy(s => string.Join("|", parameters.Select(p => s[p].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))), StringComparer.Ordinal).Select(g => g.First())];

    private static List<Dictionary<ITypeParameterSymbol, ITypeSymbol>>? Fail(List<string> failure, string reason)
    {
        failure.Add(reason);
        return null;
    }

    // Constraints filter the closed set (council A11, option B): class/struct and base or interface constraints without type parameters.
    private static bool Satisfies(ITypeSymbol type, ITypeParameterSymbol parameter)
    {
        if ((parameter.HasReferenceTypeConstraint && !type.IsReferenceType) || (parameter.HasValueTypeConstraint && !type.IsValueType))
        {
            return false;
        }

        foreach (INamedTypeSymbol constraint in parameter.ConstraintTypes.OfType<INamedTypeSymbol>().Where(c => !ContainsTypeParameter(c)))
        {
            string key = MetadataKey(constraint.OriginalDefinition) + "<" + ArgumentsKey(constraint) + ">";
            if (!Bases(type).Any(b => MetadataKey(b.OriginalDefinition) + "<" + ArgumentsKey(b) + ">" == key))
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<INamedTypeSymbol> Bases(ITypeSymbol type)
    {
        for (ITypeSymbol? current = type; current is INamedTypeSymbol named; current = named.BaseType)
        {
            yield return named;
        }

        foreach (INamedTypeSymbol contract in type.AllInterfaces)
        {
            yield return contract;
        }
    }

    private static string Id(IMethodSymbol method) =>
        (method.ContainingAssembly?.Name ?? "") + ":" + (DocumentationCommentId.CreateDeclarationId(method.OriginalDefinition) ?? method.OriginalDefinition.ToDisplayString());

    private static bool ImplementsInterface(IMethodSymbol method) =>
        method.ContainingType.AllInterfaces.SelectMany(i => i.GetMembers(method.Name).OfType<IMethodSymbol>())
            .Any(m => SymbolEqualityComparer.Default.Equals(method.ContainingType.FindImplementationForInterfaceMember(m)?.OriginalDefinition, method));

    private static bool InNameof(SyntaxNode node) =>
        node.Ancestors().OfType<InvocationExpressionSyntax>().Any(i => i.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" });

    // M(...), x.M(...) or x?.M(...).
    private static bool IsInvoked(SimpleNameSyntax name)
    {
        SyntaxNode called = name.Parent is MemberAccessExpressionSyntax access && access.Name == name ? access
            : name.Parent is MemberBindingExpressionSyntax binding ? binding
            : name;
        return called.Parent is InvocationExpressionSyntax invocation && invocation.Expression == called;
    }

    // Where a reflection method is called; for MakeGenericType not when called on typeof of another generic type.
    private string? Reflection(string method, INamedTypeSymbol? type)
    {
        foreach (Compilation compilation in _compilations)
        {
            foreach (SimpleNameSyntax name in Names(compilation, method))
            {
                if (type is not null && name.Parent is MemberAccessExpressionSyntax { Expression: TypeOfExpressionSyntax { Type: var written } }
                    && compilation.GetSemanticModel(name.SyntaxTree).GetTypeInfo(written).Type is INamedTypeSymbol other
                    && MetadataKey(other.OriginalDefinition) != MetadataKey(type))
                {
                    continue;
                }

                if (compilation.GetSemanticModel(name.SyntaxTree).GetSymbolInfo(name).Symbol is IMethodSymbol { ContainingNamespace: var ns }
                    && ns.ToDisplayString() is "System" or "System.Reflection")
                {
                    return Where(name);
                }
            }
        }

        return null;
    }

    // R1/R2: why callers outside the solution may close the member, or null.
    private string? ReachableOutside(ISymbol symbol)
    {
        Compilation? compilation = _compilations.FirstOrDefault(c => c.AssemblyName == symbol.ContainingAssembly?.Name);
        if (compilation is null)
        {
            return "is not declared in the solution";
        }

        if (IsVisibleOutside(symbol))
        {
            // A13 (council round 15): the reason names the one-line fix; a re-run after it closes the set.
            return IsPackable(compilation, _solution, _projectProperties)
                ? $"is public in a packable project ({compilation.AssemblyName}), so callers outside the solution may close it with other types. "
                    + $"Set `<IsPackable>false</IsPackable>` in {ProjectName(compilation)} if this library is not published"
                : null;
        }

        string? friend = compilation.Assembly.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.InternalsVisibleToAttribute")
            .Select(a => a.ConstructorArguments.FirstOrDefault().Value as string)
            .OfType<string>()
            .Select(n => n.Split(',')[0].Trim())
            .FirstOrDefault(n => !_assemblies.Contains(n));
        return friend is not null ? $"is internal, but InternalsVisibleTo names {friend}, which is not in the solution" : null;
    }

    // The project file that builds the assembly (its file name), or the assembly name without one.
    private string ProjectName(Compilation compilation) =>
        _solution?.Projects.Where(p => p.AssemblyName == compilation.AssemblyName).Select(p => p.FilePath).OfType<string>().Select(Path.GetFileName).FirstOrDefault()
            ?? compilation.AssemblyName
            ?? "the project";

    private static bool IsVisibleOutside(ISymbol symbol)
    {
        for (ISymbol? current = symbol; current is not null and not INamespaceSymbol; current = current.ContainingSymbol)
        {
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
            {
                return false;
            }
        }

        return true;
    }

    // Every project file that builds the assembly (one per target framework) is asked; any packable one makes it packable.
    private static bool IsPackable(Compilation compilation, Solution? solution, IProjectProperties? projectProperties)
    {
        List<string> files = solution is null || projectProperties is null
            ? []
            : [.. solution.Projects.Where(p => p.AssemblyName == compilation.AssemblyName).Select(p => p.FilePath).OfType<string>().Distinct(StringComparer.OrdinalIgnoreCase)];
        return files.Count == 0 ? IsPackable(compilation) : files.Any(projectProperties!.IsPackable);
    }

    // Same guess as MediatR's AssemblyResolver without project properties: a library is packable unless it references a test framework.
    private static bool IsPackable(Compilation compilation) =>
        compilation.Options.OutputKind == OutputKind.DynamicallyLinkedLibrary
        && !compilation.References.OfType<PortableExecutableReference>()
            .Any(r => s_testFrameworks.Contains(Path.GetFileNameWithoutExtension(r.FilePath ?? r.Display ?? string.Empty)));
}
