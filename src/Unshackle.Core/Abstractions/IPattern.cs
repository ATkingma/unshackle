using Microsoft.CodeAnalysis;
using Unshackle.Core.Models;

namespace Unshackle.Core.Abstractions;

/// <summary>
/// Finds one kind of code and rewrites it (Strategy). A pattern never throws: when it cannot rewrite a node it returns
/// <see cref="PatternResult.Manual(string)"/>.
/// </summary>
public interface IPattern
{
    /// <summary>Stable identifier in the form <c>&lt;fix&gt;.&lt;kebab-case&gt;</c>, for example <c>fa.should-be</c>.</summary>
    string Id { get; }

    /// <summary>Short human-readable description, for example <c>x.Should().Be(y) → x.ShouldBe(y)</c>.</summary>
    string Description { get; }

    /// <summary>
    /// Returns every node this pattern is responsible for, including the ones it can only report as manual work.
    /// Each returned node is passed to <see cref="Rewrite"/> and appears in the report.
    /// </summary>
    /// <param name="root">The syntax root of the document.</param>
    /// <param name="model">The semantic model of the same document, for type-dependent decisions.</param>
    IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model);

    /// <summary>
    /// Rewrites one node returned by <see cref="Find"/>. Return <see cref="PatternResult.Fixed(SyntaxNode, IReadOnlyList{string})"/> with the
    /// replacement, <see cref="PatternResult.Fixed(IEnumerable{SyntaxNode}, IReadOnlyList{string})"/> to replace a statement with several
    /// statements (for example to split an <c>.And</c> chain), or <see cref="PatternResult.Manual(string)"/>.
    /// The returned nodes need no trivia: the Fixer copies it from <paramref name="node"/>. Namespaces passed to
    /// <c>Fixed</c> are added as <c>using</c> directives when the document does not import them yet (also not globally).
    /// </summary>
    /// <param name="node">A node from <see cref="Find"/>, still part of the original tree.</param>
    /// <param name="model">The semantic model of the original tree.</param>
    PatternResult Rewrite(SyntaxNode node, SemanticModel model);
}
