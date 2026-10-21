using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

internal class TypePairComparer : IEqualityComparer<(ITypeSymbol, ITypeSymbol)>
{
    public bool Equals((ITypeSymbol, ITypeSymbol) x, (ITypeSymbol, ITypeSymbol) y) =>
        SymbolEqualityComparer.Default.Equals(x.Item1, y.Item1) && SymbolEqualityComparer.Default.Equals(x.Item2, y.Item2);

    public int GetHashCode((ITypeSymbol, ITypeSymbol) obj) =>
        HashCode.Combine(SymbolEqualityComparer.Default.GetHashCode(obj.Item1), SymbolEqualityComparer.Default.GetHashCode(obj.Item2));
}
