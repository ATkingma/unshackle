using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

internal class MapperCleanupEntry
{
    public Solution? Solution { get; set; }

    public string? FilePath { get; set; }

    public Task<MapperCleanup>? Plan { get; set; }
}
