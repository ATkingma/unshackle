using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions;

// Template Method for `x.Should().Method(...)` patterns: Rewrite runs the checks every assertion shares (assertion scope,
// subject form, discarded result, becauseArgs) with one reason text each, then hands over to RewriteAssertion.
public abstract class FluentAssertionPattern : IPattern
{
    public abstract string Id { get; }

    public abstract string Description { get; }

    public abstract IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        InvocationExpressionSyntax assertion = GetAssertion(node);
        string name = ((MemberAccessExpressionSyntax)assertion.Expression).Name.Identifier.ValueText;
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual($"{name}(...) is not in the form x.Should().{name}(...), e.g. after .And.");
        }

        var context = new AssertionContext { Node = node, Assertion = assertion, Name = name, Subject = subject, Model = model };
        if (!IsResultDiscarded(context))
        {
            return PatternResult.Manual(ResultUsedReason(name));
        }

        if (RewriteBeforeArguments(context) is { } early)
        {
            return early;
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because))
        {
            return PatternResult.Manual($"{name}(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        return RewriteAssertion(new AssertionContext
        {
            Node = node,
            Assertion = assertion,
            Name = name,
            Subject = subject,
            Model = model,
            Values = values,
            Because = because,
        });
    }

    // The node from Find is the assertion invocation; patterns that find a statement or a `.Which` override this.
    protected virtual InvocationExpressionSyntax GetAssertion(SyntaxNode node) => (InvocationExpressionSyntax)node;

    protected virtual bool IsResultDiscarded(AssertionContext context) =>
        FluentAssertionsSyntax.IsResultDiscarded(context.Assertion, context.Model);

    protected virtual string ResultUsedReason(string name) =>
        $"The result of {name}(...) is used (.And/.Which or a value); Shouldly's assertions return nothing.";

    // For overloads the argument split rejects (params): a result here skips the becauseArgs check. Values is still empty.
    protected virtual PatternResult? RewriteBeforeArguments(AssertionContext context) => null;

    protected abstract PatternResult RewriteAssertion(AssertionContext context);
}
