using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

internal class GenericRequestUses
{
    public Dictionary<string, IReadOnlyList<ITypeSymbol>> Closed { get; } = new(StringComparer.Ordinal);

    public HashSet<string> Subclassed { get; } = new(StringComparer.Ordinal);

    public HashSet<string> Assemblies { get; } = new(StringComparer.Ordinal);

    public string? Unbound { get; set; }

    public string? OpenCreation { get; set; }
}
