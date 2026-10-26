using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Generics;

/// <summary>
/// The result of <see cref="TypeArgumentClosure"/>: every proven assignment of closed types to the type parameters, or why the set is open.
/// </summary>
public class ClosedSets
{
    /// <summary>One entry per distinct assignment; each maps every requested type parameter to a closed type.</summary>
    public required IReadOnlyList<IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol>> Sets { get; init; }

    /// <summary>Why the set could not be proven (naming the unresolved caller or use), or null when <see cref="Sets"/> is complete.</summary>
    public required string? Unresolved { get; init; }
}
