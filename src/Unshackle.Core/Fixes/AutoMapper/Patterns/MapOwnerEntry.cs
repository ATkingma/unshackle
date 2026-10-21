using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

internal class MapOwnerEntry
{
    public Solution? Solution { get; set; }

    public Task<MapOwners>? Plan { get; set; }
}
