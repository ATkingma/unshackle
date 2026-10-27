using Microsoft.CodeAnalysis;
using Unshackle.Core.Abstractions;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

internal class MapOwnerEntry
{
    public Solution? Solution { get; set; }

    public IProjectProperties? ProjectProperties { get; set; }

    public Task<MapOwners>? Plan { get; set; }
}
