using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Models;

public abstract record PatternResult
{
    // The report row id when it differs from the pattern's own Id (council round 13, M8); null: the pattern's Id.
    public string? PatternId { get; init; }

    // requiredNamespaces: usings the new code needs, e.g. "System.Linq" for Count(); the Fixer adds them when missing.
    public static PatternResult Fixed(SyntaxNode newNode, params IReadOnlyList<string> requiredNamespaces) =>
        new FixedResult([newNode], requiredNamespaces);

    // Several nodes replace one node in a list, for example one statement split into two.
    public static PatternResult Fixed(IEnumerable<SyntaxNode> newNodes, params IReadOnlyList<string> requiredNamespaces) =>
        new FixedResult([.. newNodes], requiredNamespaces);

    public static PatternResult Manual(string reason) => new ManualResult(reason);

    // A Manual result with a typed reason code (council round 10, C1): each fix keeps its own enum of codes; the report prints "Code: reason".
    public static PatternResult Manual<TCode>(TCode code, string reason)
        where TCode : struct, Enum => new ManualResult(reason) { Code = code.ToString() };

    // A note that needs no change (council round 8, S2), e.g. a row of a report-only inventory.
    public static PatternResult Note(string text) => new InfoResult(text);

    // A finding that stays on the old library while the rest of the fix goes ahead (council round 3, Q2); the renderer adds the prefix.
    public static PatternResult KeptManual(KeptReason code, string reason) => new ManualResult(reason) { Kept = code };
}
