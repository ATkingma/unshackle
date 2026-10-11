using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Models;

public record FixedResult(IReadOnlyList<SyntaxNode> NewNodes, IReadOnlyList<string> RequiredNamespaces) : PatternResult
{
    // A behaviour difference the rewrite may introduce, for the report; null when the rewrite is equivalent.
    public string? Warning { get; init; }
}
