using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Models;

public record FixedResult(IReadOnlyList<SyntaxNode> NewNodes, IReadOnlyList<string> RequiredNamespaces) : PatternResult
{
    // A behaviour difference the rewrite may introduce, for the report; null when the rewrite is equivalent.
    public string? Warning { get; init; }

    // A note for the report that is not a behaviour difference, e.g. a type the rewrite left empty (TAAK-292); null when there is none.
    public string? Info { get; init; }
}
