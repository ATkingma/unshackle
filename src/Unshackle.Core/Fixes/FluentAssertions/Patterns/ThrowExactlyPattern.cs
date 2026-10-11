using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// ThrowExactly<T>/ThrowExactlyAsync<T>; ThrowPattern and ThrowAsyncPattern do the rewrite and add .ShouldBeOfType<T>().
public class ThrowExactlyPattern : IPattern
{
    public string Id => "fa.throw-exactly";

    public string Description => "act.Should().ThrowExactly<T>() → Should.Throw<T>(act).ShouldBeOfType<T>() (and the async form)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        ThrowPattern.Find(root, model, t => IsExactly(t) && !InvokingPattern.IsInvoking(t, model))
            .Concat(ThrowAsyncPattern.Find(root, model, t => IsExactly(t) && !InvokingPattern.IsInvoking(t, model)));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => InvokingPattern.RewriteAssertion(node, model);

    internal static bool IsExactly(InvocationExpressionSyntax assertion) =>
        ((MemberAccessExpressionSyntax)assertion.Expression).Name.Identifier.ValueText.StartsWith("ThrowExactly", StringComparison.Ordinal);
}
