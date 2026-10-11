using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.FluentAssertions;

// What FluentAssertionPattern hands to RewriteAssertion after the shared checks.
public class AssertionContext
{
    // The node from Find: the assertion itself, or the statement or `.Which` around it.
    public required SyntaxNode Node { get; init; }

    public required InvocationExpressionSyntax Assertion { get; init; }

    // The FluentAssertions method name, e.g. "BeNull".
    public required string Name { get; init; }

    // x in `x.Should().Method(...)`.
    public required ExpressionSyntax Subject { get; init; }

    public required SemanticModel Model { get; init; }

    // The value arguments in order, without `because`.
    public IReadOnlyList<ArgumentSyntax> Values { get; init; } = [];

    public ArgumentSyntax? Because { get; init; }
}
