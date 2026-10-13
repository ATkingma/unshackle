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
    // One node per Throw/ThrowExactly chain the pattern owns (see ThrowChains.Find).
    public static IEnumerable<SyntaxNode> FindThrow(SyntaxNode root, SemanticModel model, Func<InvocationExpressionSyntax, bool> owns) =>
        ThrowChains.Find(root, model, ThrowChains.SyncNames, owns);

    // One node per ThrowAsync/ThrowExactlyAsync chain the pattern owns (see ThrowChains.Find).
    public static IEnumerable<SyntaxNode> FindThrowAsync(SyntaxNode root, SemanticModel model, Func<InvocationExpressionSyntax, bool> owns) =>
        ThrowChains.Find(root, model, ThrowChains.AsyncNames, owns);

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

    // Hands the node to the NotThrow pattern (its nodes are the NotThrow calls) or to the Throw chain rewrite; those handle
    // ThrowExactly and Invoking subjects too.
    public static PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        node is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } && access.Name.Identifier.ValueText.StartsWith("NotThrow", StringComparison.Ordinal)
            ? new NotThrowPattern().Rewrite(node, model)
            : ThrowChains.Rewrite(node, model);

    // True when the subject of the assertion is a FluentAssertions Invoking(...) or Awaiting(...) call.
    public static bool IsInvoking(InvocationExpressionSyntax assertion, SemanticModel model) =>
        FluentAssertionsSyntax.GetSubject(assertion) is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Invoking" or "Awaiting" },
        } call
        && FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(call).Symbol);

    // For `x.Invoking(s => body)`: action = `() => body[s := x]` and inspected = the original lambda (to inspect its body).
    // For `x.Invoking(d)` with d a method group or a local, parameter or field of a delegate type: action = `() => d(x)`
    // and inspected = d; d is read when the action runs instead of before, which is the same for these stable references.
    // False with a reason when x may have side effects or is a value type (Invoking works on a copy), or the
    // argument is neither of those.
    public static bool TryGetAction(
        ExpressionSyntax subject,
        SemanticModel model,
        out ExpressionSyntax action,
        out ExpressionSyntax inspected,
        out string reason)
    {
        action = subject;
        inspected = subject;
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

        if (call.ArgumentList.Arguments is [{ NameColon: null, Expression: var delegateExpression }]
            && delegateExpression is not AnonymousFunctionExpressionSyntax
            && (model.GetSymbolInfo(delegateExpression).Symbol is IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.LocalFunction, Parameters.Length: 1 }
                || IsStable(delegateExpression, model) && model.GetTypeInfo(delegateExpression).Type is { TypeKind: TypeKind.Delegate }))
        {
            inspected = delegateExpression;
            action = SyntaxFactory.ParseExpression($"() => {delegateExpression.WithoutTrivia()}({receiver.WithoutTrivia()})");
            return true;
        }

        if (call.ArgumentList.Arguments is not [{ NameColon: null, Expression: AnonymousFunctionExpressionSyntax function }]
            || model.GetSymbolInfo(function).Symbol is not IMethodSymbol { Parameters: [var parameter] })
        {
            reason = "Invoking/Awaiting without a one-parameter lambda, method group or delegate variable: pass the delegate by hand.";
            return false;
        }

        inspected = function;
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
