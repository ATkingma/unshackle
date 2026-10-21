using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

// One CreateMap statement of a profile constructor, with the types AutoMapper may meet while it runs that map.
internal class MapNode
{
    private readonly HashSet<string> _reached = new(StringComparer.Ordinal);

    public MapNode(SyntaxNode statement, SemanticModel model, ITypeSymbol from, ITypeSymbol to, bool reverse)
    {
        Statement = statement;
        From = from;
        To = to;
        Reverse = reverse;
        FromKey = MapOwners.Key(from);
        ToKey = MapOwners.Key(to);
        // Over-approximated (safe: more maps stay): member and constructor parameter types of both sides, their elements, and every type the
        // statement names or computes (MapFrom results, Include<C, D>).
        foreach (ITypeSymbol type in new[] { from, to })
        {
            Add(type);
            foreach (ISymbol member in CreateMapPattern.AllMembers(type))
            {
                Add(member switch { IPropertySymbol p => p.Type, IFieldSymbol f => f.Type, _ => null });
            }

            foreach (IMethodSymbol constructor in (type as INamedTypeSymbol)?.InstanceConstructors ?? [])
            {
                constructor.Parameters.ToList().ForEach(p => Add(p.Type));
            }
        }

        foreach (SyntaxNode node in statement.DescendantNodes())
        {
            Add(node is ExpressionSyntax or TypeSyntax ? model.GetTypeInfo(node).Type : null);
        }
    }

    public SyntaxNode Statement { get; }

    public ITypeSymbol From { get; }

    public ITypeSymbol To { get; }

    public bool Reverse { get; }

    public string FromKey { get; }

    public string ToKey { get; }

    public bool Reaches(MapNode other) => _reached.Contains(other.FromKey) && _reached.Contains(other.ToKey);

    private void Add(ITypeSymbol? type)
    {
        if (type is null || type.TypeKind == TypeKind.Error || !_reached.Add(MapOwners.Key(type)))
        {
            return;
        }

        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [{ } value] })
        {
            Add(value);
        }

        if (MemberValues.TryElement(type, out ITypeSymbol? element, out _))
        {
            Add(element);
        }
    }
}
