using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Models;

public abstract record PatternResult
{
    // requiredNamespaces: usings the new code needs, e.g. "System.Linq" for Count(); the Fixer adds them when missing.
    public static PatternResult Fixed(SyntaxNode newNode, params IReadOnlyList<string> requiredNamespaces) =>
        new FixedResult([newNode], requiredNamespaces);

    // Several nodes replace one node in a list, for example one statement split into two.
    public static PatternResult Fixed(IEnumerable<SyntaxNode> newNodes, params IReadOnlyList<string> requiredNamespaces) =>
        new FixedResult([.. newNodes], requiredNamespaces);

    public static PatternResult Manual(string reason) => new ManualResult(reason);
}
