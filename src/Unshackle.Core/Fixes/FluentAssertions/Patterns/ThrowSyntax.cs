using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Shared by the Throw, ThrowAsync, ThrowExactly, NotThrow and Invoking patterns: finding their nodes (each pattern
// passes which ones it owns), the ThrowExactly and Invoking checks, and the Invoking subject as a lambda.
internal static class ThrowSyntax
{
    // The node is the Throw call, the whole `.WithMessage(...)` call when that directly follows it, or `.Which` after the call
    // (not `.Subject`: that is all thrown exceptions, Should.Throw returns one).
    public static IEnumerable<SyntaxNode> FindThrow(SyntaxNode root, SemanticModel model, Func<InvocationExpressionSyntax, bool> owns) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "Throw", "ThrowExactly")
            .Where(owns)
            .Select<InvocationExpressionSyntax, SyntaxNode>(t => t.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "WithMessage" } access
                && access.Parent is InvocationExpressionSyntax { } withMessage
                && withMessage.Expression == access
                    ? withMessage
                    : t.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Which" } which && which.Expression == t
                        ? which
                        : t);

    // The node is the `await` around the ThrowAsync call (or around the `.WithMessage(...)` call that directly follows it);
    // without an await it is the invocation itself. A `.ConfigureAwait(...)` between the call and the await is kept.
    public static IEnumerable<SyntaxNode> FindThrowAsync(SyntaxNode root, SemanticModel model, Func<InvocationExpressionSyntax, bool> owns) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "ThrowAsync", "ThrowExactlyAsync")
            .Where(owns)
            .Select(t => Follow(t, "WithMessage"))
            .Select(i => Follow(i, "ConfigureAwait"))
            .Select(i => i.Parent is AwaitExpressionSyntax await ? (SyntaxNode)await : i);

    // `call.method(...)` when that directly follows call, else call.
    public static InvocationExpressionSyntax Follow(InvocationExpressionSyntax call, string method) =>
        call.Parent is MemberAccessExpressionSyntax access
        && access.Name.Identifier.ValueText == method
        && access.Parent is InvocationExpressionSyntax next
        && next.Expression == access
            ? next
            : call;

    public static IEnumerable<SyntaxNode> FindNotThrow(SyntaxNode root, SemanticModel model, Func<InvocationExpressionSyntax, bool> owns) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "NotThrow", "NotThrowAfter").Where(owns);

    public static bool IsExactly(InvocationExpressionSyntax assertion) =>
        ((MemberAccessExpressionSyntax)assertion.Expression).Name.Identifier.ValueText.StartsWith("ThrowExactly", StringComparison.Ordinal);

    // Hands the node to the pattern of its assertion method; those patterns handle ThrowExactly and Invoking subjects too.
    public static PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        string method = node.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Select(i => ((i.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.ValueText) ?? string.Empty)
            .First(n => n.StartsWith("Throw", StringComparison.Ordinal) || n.StartsWith("NotThrow", StringComparison.Ordinal));
        IPattern pattern = method switch
        {
            _ when method.StartsWith("NotThrow", StringComparison.Ordinal) => new NotThrowPattern(),
            _ when method.EndsWith("Async", StringComparison.Ordinal) => new ThrowAsyncPattern(),
            _ => new ThrowPattern(),
        };
        return pattern.Rewrite(node, model);
    }

    // True when the subject of the assertion is a FluentAssertions Invoking(...) or Awaiting(...) call.
    public static bool IsInvoking(InvocationExpressionSyntax assertion, SemanticModel model) =>
        FluentAssertionsSyntax.GetSubject(assertion) is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Invoking" or "Awaiting" },
        } call
        && FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(call).Symbol);

    // For `x.Invoking(s => body)`: action = `() => body[s := x]` and lambda = the original lambda (to inspect its body).
    // False with a reason when x may have side effects or is a value type (Invoking works on a copy), or the
    // argument is not a one-parameter lambda.
    public static bool TryGetAction(
        ExpressionSyntax subject,
        SemanticModel model,
        out ExpressionSyntax action,
        out AnonymousFunctionExpressionSyntax lambda,
        out string reason)
    {
        action = subject;
        lambda = null!;
        reason = string.Empty;
        var call = (InvocationExpressionSyntax)subject;
        ExpressionSyntax receiver = ((MemberAccessExpressionSyntax)call.Expression).Expression;
        if (!IsStable(receiver, model))
        {
            reason = "The receiver of Invoking/Awaiting is not a local, parameter, field or this: it may have side effects, which Invoking runs once outside the action.";
            return false;
        }

        if (model.GetTypeInfo(receiver).Type is not { IsReferenceType: true })
        {
            reason = "The receiver of Invoking/Awaiting is a value type: Invoking acts on a copy, a lambda on the variable itself.";
            return false;
        }

        if (call.ArgumentList.Arguments is not [{ NameColon: null, Expression: AnonymousFunctionExpressionSyntax function }]
            || model.GetSymbolInfo(function).Symbol is not IMethodSymbol { Parameters: [var parameter] })
        {
            reason = "Invoking/Awaiting without a one-parameter lambda (e.g. a method group): pass the delegate by hand.";
            return false;
        }

        lambda = function;
        SyntaxNode body = function.Body.ReplaceNodes(
            function.Body.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                .Where(i => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(i).Symbol, parameter)),
            (original, _) => receiver.WithoutTrivia().WithTriviaFrom(original));
        string prefix = function.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword) ? "async () => " : "() => ";
        action = SyntaxFactory.ParseExpression(prefix + body.WithoutLeadingTrivia().ToFullString().TrimEnd());
        return true;
    }

    // shortcut: properties and other members are refused, even side-effect-free ones; widen when real code needs it.
    private static bool IsStable(ExpressionSyntax receiver, SemanticModel model) => receiver switch
    {
        ThisExpressionSyntax => true,
        IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } =>
            model.GetSymbolInfo(receiver).Symbol is ILocalSymbol or IParameterSymbol or IFieldSymbol,
        _ => false,
    };
}
