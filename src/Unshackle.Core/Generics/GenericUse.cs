using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Generics;

/// <summary>One written use of a generic type in the solution (TAAK-193, shared since TAAK-324).</summary>
public class GenericUse
{
    /// <summary>The type as written there (constructed; for an unbound use the unbound type).</summary>
    public required INamedTypeSymbol Constructed { get; init; }

    public required GenericNameSyntax Node { get; init; }

    public required SemanticModel Model { get; init; }

    public required GenericUseKind Kind { get; init; }

    /// <summary>file:line of the use, for a report reason.</summary>
    public required string Where { get; init; }

    public required string AssemblyName { get; init; }
}
