using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>A CreateMap&lt;A, B&gt; that a profile rewrite turns into B ToB(this A source): what a nested member of another map calls.</summary>
public class NestedMap
{
    public required string Method { get; init; }

    // Null: the global namespace.
    public required string? Namespace { get; init; }

    public required bool IsInternal { get; init; }

    // Null: the project of the profile that calls it.
    public required ProjectId? Project { get; init; }

    // A ConvertUsing map: AutoMapper calls the converter with a null source too, so a member calls it without ?. (TAAK-253).
    public required bool Converts { get; init; }
}
