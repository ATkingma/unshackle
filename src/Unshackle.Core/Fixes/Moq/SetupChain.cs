using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// The steps after a Setup up to its statement: an optional value step (Returns/ReturnsAsync/Throws/ThrowsAsync), an optional Callback
// before or after it, an optional Verifiable(). Statement is null when the chain has another shape.
public class SetupChain
{
    public ExpressionStatementSyntax? Statement { get; init; }

    public InvocationExpressionSyntax? Returns { get; init; }

    public InvocationExpressionSyntax? Callback { get; init; }

    public bool CallbackFirst { get; init; }

    public bool Verifiable { get; init; }
}
