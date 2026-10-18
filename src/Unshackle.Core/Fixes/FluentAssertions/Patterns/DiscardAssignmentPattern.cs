using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// `_ = x.Should().Be(3);` (a discard against the "unused result" analyzer) becomes `x.Should().Be(3);`: a discard has no
// runtime effect, so pass/fail stays the same, and the assertion patterns then see a plain statement (Shouldly returns void,
// which a discard cannot take). Only invocations and awaits of a FluentAssertions method: both are valid statements.
public class DiscardAssignmentPattern : IPattern
{
    // Literal for check-pattern-docs.sh.
    public string Id => "fa.discard-assignment";

    public string Description => "_ = x.Should().Be(y); → x.Should().Be(y); (then rewritten by the assertion patterns)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes()
            .OfType<ExpressionStatementSyntax>()
            .Where(s => s.Expression is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment
                && model.GetSymbolInfo(assignment.Left).Symbol is IDiscardSymbol
                && Call(assignment.Right) is { } call
                && FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(call).Symbol));

    // The Fixer copies the statement's trivia onto the result.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var statement = (ExpressionStatementSyntax)node;
        return PatternResult.Fixed(statement.WithExpression(((AssignmentExpressionSyntax)statement.Expression).Right.WithoutTrivia()));
    }

    // The invocation of `x.Should().M(...)` or `await x.Should().MAsync(...)`; anything else is no statement on its own.
    private static InvocationExpressionSyntax? Call(ExpressionSyntax right) => right switch
    {
        AwaitExpressionSyntax { Expression: InvocationExpressionSyntax awaited } => awaited,
        InvocationExpressionSyntax invocation => invocation,
        _ => null,
    };
}
