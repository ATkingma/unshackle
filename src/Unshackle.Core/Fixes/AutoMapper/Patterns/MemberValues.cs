using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// What a generated ToB method assigns to a member whose type AutoMapper maps itself: a nested object through the map of its type pair
/// (X = source.X?.ToY()), a collection copied element by element into a new one (Xs = source.Xs?.Select(x => x?.ToY()).ToList() ?? []).
/// AutoMapper 14 maps a null object to null and a null collection to an empty one (TAAK-245 has the measured behaviour).
/// </summary>
public class MemberValues
{
    private const string Element = "x";

    private static readonly string[] s_collections = ["List", "IEnumerable", "ICollection", "IList", "IReadOnlyCollection", "IReadOnlyList"];

    private readonly NestedMaps _maps;
    private readonly SemanticModel _model;
    private readonly int _position;
    private readonly string? _namespace;
    private readonly bool _nullableWarnings;
    private readonly bool _collectionExpressions;
    private readonly HashSet<string> _namespaces;

    // position: where the generated class goes (the profile); namespaces collects the usings the values need.
    public MemberValues(NestedMaps maps, SemanticModel model, int position, HashSet<string> namespaces)
    {
        _maps = maps;
        _model = model;
        _position = position;
        ISymbol? enclosing = model.GetEnclosingSymbol(position);
        _namespace = (enclosing as INamespaceSymbol ?? enclosing?.ContainingNamespace) is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : null;
        _nullableWarnings = model.GetNullableContext(position).WarningsEnabled();
        _collectionExpressions = ((CSharpParseOptions)model.SyntaxTree.Options).LanguageVersion >= LanguageVersion.CSharp12;
        _namespaces = namespaces;
    }

    /// <summary>
    /// Why value (an expression of type from) cannot become AutoMapper's value for a member of type to in a map to outer, or null with that
    /// value in result; result null when no rule applies (the types differ and nothing maps them). copies: a nested map or collection copy.
    /// </summary>
    public string? Problem(string what, string value, ITypeSymbol from, ITypeSymbol to, ITypeSymbol outer, out string? result, out bool copies)
    {
        result = null;
        copies = false;
        bool fromCollection = MapCallPattern.IsCollection(from);
        bool toCollection = MapCallPattern.IsCollection(to);
        if (fromCollection != toCollection)
        {
            return null;
        }

        if (toCollection)
        {
            if (!TryElement(from, out ITypeSymbol? fromElement, out _) || !TryElement(to, out ITypeSymbol? toElement, out bool array))
            {
                return $"{what} is a collection of a kind this fix does not copy ({from.ToDisplayString()} to {to.ToDisplayString()}; AutoMapper copies it into a new one)";
            }

            if (Nested(Element, fromElement, toElement, outer, out string? element) is string problem)
            {
                return $"{what}: {problem}";
            }

            string copy;
            if (element is not null)
            {
                copy = $"{Receiver(value)}?.Select({Element} => {element})";
            }
            else if (SymbolEqualityComparer.Default.Equals(fromElement, toElement))
            {
                copy = Receiver(value) + "?";
            }
            else
            {
                return $"{what} holds {fromElement.Name} but the destination holds {toElement.Name} (AutoMapper converts){NoMap(fromElement, toElement)}";
            }

            if (NullSettingProblem(what) is string setting)
            {
                return setting;
            }

            _namespaces.Add("System.Linq");
            result = copy + (array ? ".ToArray()" : ".ToList()") + " ?? " + Empty(toElement, array);
            copies = true;
            return null;
        }

        if (Nested(value, from, to, outer, out string? nested) is string nestedProblem)
        {
            return $"{what}: {nestedProblem}";
        }

        if (nested is not null && NullSettingProblem(what) is string nullSetting)
        {
            return nullSetting;
        }

        if (nested is not null)
        {
            result = nested;
            copies = true;
        }
        else if (SymbolEqualityComparer.Default.Equals(from, to))
        {
            result = value;
        }

        return null;
    }

    private string? NullSettingProblem(string what) => _maps.NullSetting is { } setting
        ? $"{what} is copied by AutoMapper with the null handling that {setting} changes; write the mapping by hand"
        : null;

    // The note for a conversion message when the types are not mapped.
    public static string NoMap(ITypeSymbol from, ITypeSymbol to) =>
        $" and no profile this fix rewrites has a CreateMap<{from.Name}, {to.Name}>";

    // value.ToB() through the map of the type pair, or null when no profile maps it.
    private string? Nested(string value, ITypeSymbol from, ITypeSymbol to, ITypeSymbol outer, out string? call)
    {
        call = null;
        bool lifted = IsNullable(from, out ITypeSymbol? fromValue) & IsNullable(to, out ITypeSymbol? toValue);
        ITypeSymbol source = lifted ? fromValue! : from;
        ITypeSymbol destination = lifted ? toValue! : to;
        if (!lifted && (IsNullable(from, out _) || IsNullable(to, out _)))
        {
            return null;
        }

        IReadOnlyList<NestedMap> maps = _maps.Find(source, destination);
        if (maps.Count == 0)
        {
            return null;
        }

        if (maps.Count > 1)
        {
            return $"several profiles map {source.Name} to {destination.Name}; choose the mapping method by hand";
        }

        NestedMap map = maps[0];
        if (map.IsInternal && map.Project is not null)
        {
            return $"the mapping method {map.Method} becomes internal in another project; make the types public or write the mapping by hand";
        }

        if (_model.LookupSymbols(_position, source, map.Method).Any())
        {
            return $"{source.Name} already has a member {map.Method}, so the extension method would not be called";
        }

        if (Reaches(destination, outer))
        {
            return $"{destination.Name} can lead back to {outer.Name}, a cycle (AutoMapper keeps the references, a mapping method would recurse)";
        }

        if (map.Namespace is { } ns && ns != _namespace && !(_namespace?.StartsWith(ns + ".", StringComparison.Ordinal) ?? false))
        {
            _namespaces.Add(ns);
        }

        string receiver = Receiver(value);
        call = (lifted, source.IsValueType, destination.IsValueType) switch
        {
            (true, _, _) => $"{receiver}?.{map.Method}()",
            (false, true, _) => $"{receiver}.{map.Method}()",
            // AutoMapper gives a null source the default struct (not one made by its parameterless constructor).
            (false, false, true) => $"{receiver}?.{map.Method}() ?? default",
            // AutoMapper writes null into a non-nullable member too; the ! keeps that without a warning.
            _ => $"{receiver}?.{map.Method}()" + (_nullableWarnings && to.NullableAnnotation == NullableAnnotation.NotAnnotated ? "!" : ""),
        };
        return null;
    }

    // An array, List<T> or one of the interfaces AutoMapper fills with a List<T>; other collections (sets, dictionaries, own types) are not copied.
    private static bool TryElement(ITypeSymbol type, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out ITypeSymbol? element, out bool array)
    {
        element = null;
        array = type is IArrayTypeSymbol;
        if (type is IArrayTypeSymbol { Rank: 1 } arrayType)
        {
            element = arrayType.ElementType;
        }
        else if (type is INamedTypeSymbol { TypeArguments: [{ } argument] } named && s_collections.Contains(named.Name)
            && named.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic")
        {
            element = argument;
        }

        return element is not null;
    }

    private static bool IsNullable(ITypeSymbol type, out ITypeSymbol? value)
    {
        value = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [{ } argument] } ? argument : null;
        return value is not null;
    }

    private string Empty(ITypeSymbol element, bool array)
    {
        if (_collectionExpressions)
        {
            return "[]";
        }

        string name = element.ToMinimalDisplayString(_model, _position);
        if (array)
        {
            return $"global::System.Array.Empty<{name}>()";
        }

        _namespaces.Add("System.Collections.Generic");
        return $"new List<{name}>()";
    }

    private static string Receiver(string value) =>
        MapCallPattern.NeedsParentheses(SyntaxFactory.ParseExpression(value)) ? $"({value})" : value;

    // Whether target is reachable from type through public members, type arguments and array elements: a map from type would
    // then call the map to target again. Types of the framework are not searched (they hold no DTOs).
    private static bool Reaches(ITypeSymbol type, ITypeSymbol target)
    {
        HashSet<ITypeSymbol> seen = new(SymbolEqualityComparer.Default);
        Queue<ITypeSymbol> queue = new([type]);
        while (queue.TryDequeue(out ITypeSymbol? current))
        {
            if (!seen.Add(current))
            {
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(current, target) || seen.Count > 10_000)
            {
                return true;
            }

            switch (current)
            {
                case IArrayTypeSymbol array:
                    queue.Enqueue(array.ElementType);
                    break;
                case INamedTypeSymbol named:
                    foreach (ITypeSymbol argument in named.TypeArguments)
                    {
                        queue.Enqueue(argument);
                    }

                    if (named.ContainingNamespace?.ToDisplayString() is { } ns && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal)))
                    {
                        break;
                    }

                    for (ITypeSymbol? level = named; level is not null && level.SpecialType != SpecialType.System_Object; level = level.BaseType)
                    {
                        foreach (ISymbol member in level.GetMembers().Where(m => !m.IsStatic && m.DeclaredAccessibility == Accessibility.Public))
                        {
                            if (member is IPropertySymbol { IsIndexer: false } property)
                            {
                                queue.Enqueue(property.Type);
                            }
                            else if (member is IFieldSymbol field)
                            {
                                queue.Enqueue(field.Type);
                            }
                        }
                    }

                    break;
            }
        }

        return false;
    }
}
