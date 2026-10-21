using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>A node the cleanup removes or trims, with a warning for the report when it may change behaviour.</summary>
internal class MapperCleanupRemoval
{
    public MapperCleanupRemoval(SyntaxNode node, string? warning)
    {
        Node = node;
        Warning = warning;
    }

    public SyntaxNode Node { get; }

    public string? Warning { get; }
}
