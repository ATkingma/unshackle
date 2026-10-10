using Microsoft.CodeAnalysis;

namespace Unshackle.Core.Models;

public record FixedResult(IReadOnlyList<SyntaxNode> NewNodes, IReadOnlyList<string> RequiredNamespaces) : PatternResult;
