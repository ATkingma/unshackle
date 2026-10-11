using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// The Throw/ThrowAsync/NotThrow assertions whose subject is x.Invoking(s => ...) or x.Awaiting(s => ...); the other
// patterns leave those to this one and turn the subject into `() => ...` with s replaced by x.
public class InvokingPattern : IPattern
{
    public string Id => "fa.invoking";

    public string Description => "x.Invoking(s => s.M()).Should().Throw<T>() / Awaiting / NotThrow → Should.Throw<T>(() => x.M()) etc.";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        ThrowPattern.Find(root, model, t => IsInvoking(t, model))
            .Concat(ThrowAsyncPattern.Find(root, model, t => IsInvoking(t, model)))
            .Concat(NotThrowPattern.Find(root, model, t => IsInvoking(t, model)));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => RewriteAssertion(node, model);

    // Hands the node to the pattern of its assertion method; those patterns handle ThrowExactly and Invoking subjects too.
    internal static PatternResult RewriteAssertion(SyntaxNode node, SemanticModel model)
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
    internal static bool IsInvoking(InvocationExpressionSyntax assertion, SemanticModel model) =>
        FluentAssertionsSyntax.GetSubject(assertion) is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Invoking" or "Awaiting" },
        } call
        && FluentAssertionsSyntax.IsFluentAssertions(model.GetSymbolInfo(call).Symbol);

    // For `x.Invoking(s => body)`: action = `() => body[s := x]` and lambda = the original lambda (to inspect its body).
    // False with a reason when x may have side effects or is a value type (Invoking works on a copy), or the
    // argument is not a one-parameter lambda.
    internal static bool TryGetAction(
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
