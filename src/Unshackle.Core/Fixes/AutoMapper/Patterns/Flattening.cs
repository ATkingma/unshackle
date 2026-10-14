using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// The source path AutoMapper 14 reads for a destination member without a source member of its exact name (TAAK-256, measured):
/// a member, or a public parameterless method, whose name equals the destination name in any case; else one with the prefix Get
/// (GetTotal() for Total, also a property GetCode for Code); else the name is split into PascalCase words and the shortest prefix
/// that names a member is followed into that member's type, trying longer prefixes when the rest does not match (flattening:
/// CustomerAddressCity ← Customer.Address.City). AutoMapper reads the path null-safe: a null object on the way gives the leaf's default.
/// </summary>
public class Flattening
{
    // AutoMapper's PascalCase split of a destination name.
    private static readonly Regex s_words = new(@"(\p{Lu}+(?=$|\p{Lu}[\p{Ll}0-9])|\p{Lu}?[\p{Ll}0-9]+)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Why the member name of destination cannot be read from source the way AutoMapper does, or null with the path in path
    /// (outermost first); path is empty when AutoMapper finds nothing.
    /// </summary>
    public static string? Problem(ITypeSymbol source, ITypeSymbol destination, string name, out IReadOnlyList<ISymbol> path)
    {
        path = [];
        string[] words = [.. s_words.Matches(name).Select(m => m.Value)];
        if (string.Concat(words) != name)
        {
            // Not PascalCase (an underscore, for one): only a direct match.
            if (Direct(source, name, out ISymbol? single) is string problem)
            {
                return $"{destination.Name}.{name}: {problem}";
            }

            path = single is null ? [] : [single];
            return null;
        }

        List<List<ISymbol>> found = [];
        if (Paths(source, words, 0, [], found) is string pathProblem)
        {
            return $"{destination.Name}.{name}: {pathProblem}";
        }

        if (found.Count > 1)
        {
            return $"{destination.Name}.{name} can be read from {source.Name} as {string.Join(" and as ", found.Select(Text))}; AutoMapper takes the first";
        }

        path = found.Count == 1 ? found[0] : [];
        return null;
    }

    /// <summary>
    /// The C# expression for path read from source the way AutoMapper reads it, and the type of that expression; a problem when
    /// the null-safe value cannot be written for a member of type to.
    /// </summary>
    public static string? Value(IReadOnlyList<ISymbol> path, ITypeSymbol to, bool nullableWarnings, out string value, out ITypeSymbol from)
    {
        value = "source";
        from = Type(path[^1]);
        bool conditional = false;
        for (int i = 0; i < path.Count; i++)
        {
            ITypeSymbol type = Type(path[i]);
            if (type is ITypeParameterSymbol { HasReferenceTypeConstraint: false, HasValueTypeConstraint: false })
            {
                return $"{Text(path)} goes through the type parameter {type.Name}";
            }

            bool nullSafe = i > 0 && !Type(path[i - 1]).IsValueType;
            conditional |= nullSafe;
            value += (nullSafe ? "?." : ".") + path[i].Name + (path[i] is IMethodSymbol ? "()" : "");
        }

        if (!conditional)
        {
            return null;
        }

        if (!from.IsValueType || IsNullable(from, out _))
        {
            // AutoMapper writes null into a non-nullable member too; the ! keeps that without a warning.
            value += nullableWarnings && SymbolEqualityComparer.Default.Equals(from, to) && to.NullableAnnotation == NullableAnnotation.NotAnnotated ? "!" : "";
            return null;
        }

        if (IsNullable(to, out ITypeSymbol? underlying))
        {
            // Measured: a null object on the way gives null in an int? member (but the default in a string member, "0").
            if (!SymbolEqualityComparer.Default.Equals(underlying, from))
            {
                return $"{Text(path)} is {from.Name} and may meet a null object; AutoMapper's value for the {to.ToDisplayString()} member is not measured";
            }

            from = to;
            return null;
        }

        value += " ?? default";
        return null;
    }

    private static string Text(IReadOnlyList<ISymbol> path) => string.Join(".", path.Select(m => m.Name + (m is IMethodSymbol ? "()" : "")));

    // Every complete path for words[start..] in type: a direct match ends the search at this level (measured: CustomerName beats Customer.Name).
    private static string? Paths(ITypeSymbol type, string[] words, int start, List<ISymbol> prefix, List<List<ISymbol>> found)
    {
        if (Direct(type, string.Concat(words[start..]), out ISymbol? direct) is string problem)
        {
            return problem;
        }

        if (direct is not null)
        {
            found.Add([.. prefix, direct]);
            return null;
        }

        for (int end = start + 1; end < words.Length; end++)
        {
            if (Direct(type, string.Concat(words[start..end]), out ISymbol? member) is string memberProblem)
            {
                return memberProblem;
            }

            // A Nullable<T> on the way is not followed (measured: P? P gives no PX).
            if (member is not null && !IsNullable(Type(member), out _)
                && Paths(Type(member), words, end, [.. prefix, member], found) is string deeper)
            {
                return deeper;
            }
        }

        return null;
    }

    // The member of type AutoMapper reads for name: same name in any case, else Get + name; several → a problem.
    private static string? Direct(ITypeSymbol type, string name, out ISymbol? member)
    {
        member = null;
        List<ISymbol> readable = Readable(type);
        foreach (string candidate in new[] { name, "Get" + name })
        {
            List<ISymbol> matches = [.. readable.Where(m => string.Equals(m.Name, candidate, StringComparison.OrdinalIgnoreCase))];
            if (matches.Count > 1)
            {
                return $"{string.Join(" and ", matches.Select(m => m.Name))} of {type.Name} both match {candidate}; AutoMapper takes one of them";
            }

            if (matches is [{ } match])
            {
                member = match;
                return null;
            }
        }

        return null;
    }

    // Public instance properties with a public getter, fields and parameterless methods with a value, up to object (a derived member hides one of the same name).
    private static List<ISymbol> Readable(ITypeSymbol type)
    {
        List<ISymbol> members = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (ITypeSymbol? current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (ISymbol member in current.GetMembers().Where(m => !m.IsStatic && !m.IsImplicitlyDeclared && m.DeclaredAccessibility == Accessibility.Public))
            {
                bool readable = member switch
                {
                    IPropertySymbol p => !p.IsIndexer && p.GetMethod?.DeclaredAccessibility == Accessibility.Public,
                    IFieldSymbol => true,
                    IMethodSymbol { MethodKind: MethodKind.Ordinary, Parameters: [], ReturnsVoid: false, IsGenericMethod: false } => true,
                    _ => false,
                };
                if (readable && seen.Add(member.Name))
                {
                    members.Add(member);
                }
            }
        }

        return members;
    }

    private static ITypeSymbol Type(ISymbol member) => member switch
    {
        IPropertySymbol property => property.Type,
        IFieldSymbol field => field.Type,
        _ => ((IMethodSymbol)member).ReturnType,
    };

    private static bool IsNullable(ITypeSymbol type, out ITypeSymbol? value)
    {
        value = type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [{ } argument] } ? argument : null;
        return value is not null;
    }
}
