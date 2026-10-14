using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// The member assignments of Expression&lt;Func&lt;A, B&gt;&gt; AToBProjection for a CreateMap&lt;A, B&gt; that a ProjectTo&lt;B&gt; uses
/// (see <see cref="ProjectToPattern"/>), written the way AutoMapper 14's ProjectTo builds its Select (measured, TAAK-252):
/// a member of the same type is read as is, a nested object of a map in the same profile becomes
/// source.X == null ? null : new Y { ... } and a list or array of such elements source.Xs.Select(x => new Y { ... }).ToList() (or ToArray()),
/// without a null check (the database query has none either). AutoMapper's built-in conversions and maps of other profiles are not
/// inlined: an expression tree cannot call ToY() and EF Core cannot translate it.
/// </summary>
public class Projections
{
    private const int MaxDepth = 8;

    private readonly IReadOnlyDictionary<string, (ITypeSymbol Source, ITypeSymbol Destination, IReadOnlyList<ProjectionMember> Members)> _maps;
    private readonly NestedMaps _nested;
    private readonly bool _nullableWarnings;
    private readonly SemanticModel _model;
    private readonly int _position;

    // maps: the maps of one profile by MapIntoPattern.Key; nested: every map a member could use (to refuse one of another profile);
    // position: where the generated class goes (the profile), for the type names.
    public Projections(IReadOnlyDictionary<string, (ITypeSymbol Source, ITypeSymbol Destination, IReadOnlyList<ProjectionMember> Members)> maps,
        NestedMaps nested, SemanticModel model, int position)
    {
        _maps = maps;
        _nested = nested;
        _model = model;
        _position = position;
        _nullableWarnings = model.GetNullableContext(position).WarningsEnabled();
    }

    /// <summary>The name of the projection of CreateMap&lt;A, B&gt;: AToBProjection, so two maps to the same B in one class do not clash.</summary>
    public static string Name(ITypeSymbol source, ITypeSymbol destination) => source.Name + "To" + destination.Name + "Projection";

    /// <summary>Why the map with this key cannot be one expression, or null with its member assignments over the parameter source.</summary>
    public string? Problem(string key, out List<(string Name, string Value)> members)
    {
        members = [];
        (ITypeSymbol source, ITypeSymbol destination, IReadOnlyList<ProjectionMember> mapMembers) = _maps[key];
        foreach (ProjectionMember member in mapMembers)
        {
            if (Value(member.Name, member.Value, member.From, member.To, [key], out string value) is string problem)
            {
                return $"ProjectTo<{destination.Name}> from {source.Name}: {problem}; write the projection by hand";
            }

            members.Add((member.Name, value));
        }

        return null;
    }

    // value: an expression of type from; result: what the projection assigns to a member of type to.
    private string? Value(string what, string value, ITypeSymbol from, ITypeSymbol to, List<string> path, out string result)
    {
        result = value;
        if (path.Count > MaxDepth)
        {
            return $"{what} nests deeper than {MaxDepth} maps";
        }

        bool fromCollection = MapCallPattern.IsCollection(from);
        bool toCollection = MapCallPattern.IsCollection(to);
        if (fromCollection && toCollection && MemberValues.TryElement(from, out ITypeSymbol? fromElement, out _)
            && MemberValues.TryElement(to, out ITypeSymbol? toElement, out bool array))
        {
            if (Map(fromElement, toElement) is { } elementKey)
            {
                if (path.Contains(elementKey))
                {
                    return $"{what} leads back to the map of {toElement.Name}, a cycle";
                }

                string parameter = Parameter(elementKey, value);
                if (Inline(what, elementKey, parameter, path, out string? element) is string elementProblem)
                {
                    return elementProblem;
                }

                result = $"{Receiver(value)}.Select({parameter} => {element})" + (array ? ".ToArray()" : ".ToList()");
                return null;
            }

            if (_nested.Find(fromElement, toElement).Count > 0)
            {
                return $"{what} holds {fromElement.Name} mapped to {toElement.Name} by another profile (an expression cannot call its ToY(); inline it)";
            }
        }

        if (Map(from, to) is { } key)
        {
            if (path.Contains(key))
            {
                return $"{what} leads back to the map of {to.Name}, a cycle";
            }

            if (Inline(what, key, value, path, out string? nested) is string nestedProblem)
            {
                return nestedProblem;
            }

            // AutoMapper writes null into a non-nullable member too; the ! keeps that without a warning, like ToB.
            string none = _nullableWarnings && to.NullableAnnotation == NullableAnnotation.NotAnnotated ? "null!" : "null";
            result = $"{Receiver(value)} == null ? {none} : {nested}";
            return null;
        }

        if (_nested.Find(from, to).Count > 0)
        {
            return $"{what} is {from.Name} mapped to {to.Name} by another profile (an expression cannot call its ToY(); inline it)";
        }

        if (SymbolEqualityComparer.Default.Equals(from, to))
        {
            return null;
        }

        return $"{what} is {from.ToDisplayString()} but {to.ToDisplayString()} in the destination (ProjectTo converts it in the query, this fix does not)";
    }

    // new Y { M = receiver.M, ... } for the map with this key, on one line.
    private string? Inline(string what, string key, string receiver, List<string> path, out string? result)
    {
        result = null;
        (_, ITypeSymbol destination, IReadOnlyList<ProjectionMember> members) = _maps[key];
        List<string> parts = [];
        List<string> inner = [.. path, key];
        foreach (ProjectionMember member in members)
        {
            if (Value(what + "." + member.Name, Substitute(member.Value, receiver), member.From, member.To, inner, out string value) is string problem)
            {
                return problem;
            }

            parts.Add($"{member.Name} = {value}");
        }

        string type = destination.ToMinimalDisplayString(_model, _position);
        result = parts.Count == 0 ? $"new {type}()" : $"new {type} {{ {string.Join(", ", parts)} }}";
        return null;
    }

    // The key of a map of this profile from from to to; only reference types (AutoMapper's null check), no Nullable<T> lifting.
    private string? Map(ITypeSymbol from, ITypeSymbol to)
    {
        if (!from.IsReferenceType || !to.IsReferenceType)
        {
            return null;
        }

        string key = MapIntoPattern.Key(from.WithNullableAnnotation(NullableAnnotation.None), to.WithNullableAnnotation(NullableAnnotation.None));
        return _maps.ContainsKey(key) ? key : null;
    }

    // A lambda parameter that no member of the element map uses as a name already.
    private string Parameter(string key, string receiver)
    {
        HashSet<string> used = [.. _maps[key].Members.Select(m => m.Value).Append(receiver)
            .SelectMany(v => SyntaxFactory.ParseExpression(v).DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.ValueText))];
        string name = "x";
        for (int i = 2; used.Contains(name); i++)
        {
            name = "x" + i;
        }

        return name;
    }

    // value is written over the parameter source; the nested map reads it from receiver instead.
    private static string Substitute(string value, string receiver)
    {
        ExpressionSyntax expression = SyntaxFactory.ParseExpression(value);
        ExpressionSyntax replacement = SyntaxFactory.ParseExpression(Receiver(receiver));
        List<IdentifierNameSyntax> uses = [.. expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
            .Where(n => n.Identifier.ValueText == "source" && n.Parent switch
            {
                MemberAccessExpressionSyntax access => access.Expression == n,
                MemberBindingExpressionSyntax or NameColonSyntax or NameEqualsSyntax => false,
                AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } assignment => assignment.Left != n,
                _ => true,
            })];
        return expression.ReplaceNodes(uses, (original, _) => replacement.WithTriviaFrom(original)).ToString();
    }

    private static string Receiver(string value) =>
        MapCallPattern.NeedsParentheses(SyntaxFactory.ParseExpression(value)) ? $"({value})" : value;
}
