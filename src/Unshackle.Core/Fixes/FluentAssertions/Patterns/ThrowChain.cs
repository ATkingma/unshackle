using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// A Throw<T>()/ThrowAsync<T>() assertion with the FluentAssertions calls that follow it, e.g.
// `(await act.Should().ThrowAsync<T>().WithMessage("m")).And`, or the same for a local that holds such a chain (`ex.And`).
public class ThrowChain
{
    // The Throw/ThrowExactly/ThrowAsync/ThrowExactlyAsync invocation, or a reference to a local declared by another chain.
    public required ExpressionSyntax Origin { get; init; }

    // WithMessage/WithParameterName/Where/WithInnerException/WithInnerExceptionExactly calls in order, before and after the await.
    public required IReadOnlyList<InvocationExpressionSyntax> Steps { get; init; }

    public InvocationExpressionSyntax? ConfigureAwait { get; init; }

    public AwaitExpressionSyntax? Await { get; init; }

    // `.And` or `.Which` at the end: the exception itself, which Should.Throw returns.
    public MemberAccessExpressionSyntax? Tail { get; init; }

    // The outermost node of the chain: the tail, the last step, the await or the origin.
    public required ExpressionSyntax Top { get; init; }
}
