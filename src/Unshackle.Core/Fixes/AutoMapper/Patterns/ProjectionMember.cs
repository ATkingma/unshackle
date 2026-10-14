using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// A destination member of a CreateMap as ProjectTo sees it: the source expression AutoMapper reads (over the parameter source:
/// source.X, or the body of a MapFrom), before any ToB-only rewrite such as source.X?.ToY().
/// </summary>
public class ProjectionMember
{
    public required string Name { get; init; }

    public required string Value { get; init; }

    public required ITypeSymbol From { get; init; }

    public required ITypeSymbol To { get; init; }
}
