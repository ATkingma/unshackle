using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Shouldly's ShouldBeInOrder accepts equal neighbours and nulls like FluentAssertions, and compares with Comparer<T>.Default.
// FluentAssertions compares strings ordinally (probed: "a","B" fails, "Z","Å" passes), Shouldly's default is culture-sensitive,
// so a string key gets StringComparer.Ordinal as comparer.
public class OrderPattern : FluentAssertionPattern
{
    public override string Id => "fa.order";

    public override string Description =>
        "x.Should().BeInAscendingOrder(p => p.K) → x.Select(p => p.K).ShouldBeInOrder(SortDirection.Ascending); BeInDescendingOrder likewise";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "BeInAscendingOrder", "BeInDescendingOrder");

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        SemanticModel model = context.Model;
        if (model.GetSymbolInfo(context.Assertion).Symbol is not IMethodSymbol method)
        {
            return PatternResult.Manual($"{context.Name}(...) has an unknown overload.");
        }

        ArgumentSyntax? selector = null;
        ITypeSymbol keyType;
        if (context.Values.Count == 0)
        {
            keyType = method.ContainingType.TypeArguments.Length > 1 ? method.ContainingType.TypeArguments[1] : method.ContainingType;
        }
        else if (context.Values.Count == 1 && method.Parameters[0].Type is INamedTypeSymbol { Name: "Expression" } && method.TypeArguments.Length == 1)
        {
            selector = context.Values[0];
            if (selector.Expression is not AnonymousFunctionExpressionSyntax)
            {
                return PatternResult.Manual($"{context.Name}(selector) with an Expression variable: Select needs a Func; rewrite it by hand.");
            }

            keyType = method.TypeArguments[0];
        }
        else
        {
            return PatternResult.Manual($"{context.Name}(...) with a comparer or comparison: ShouldBeInOrder takes an IComparer<T> of the key; rewrite it by hand.");
        }

        if (keyType is IErrorTypeSymbol)
        {
            return PatternResult.Manual($"{context.Name}(...) on a collection with an unknown element type.");
        }

        ExpressionSyntax receiver = FluentAssertionsSyntax.NotNullReceiver(context.Subject, model);
        if (selector is not null)
        {
            receiver = FluentAssertionsSyntax.Invoke(receiver, "Select", selector);
        }

        string direction = context.Name == "BeInAscendingOrder" ? "Ascending" : "Descending";
        ArgumentSyntax directionArgument = SyntaxFactory.Argument(SyntaxFactory.ParseExpression($"SortDirection.{direction}"));
        bool isString = keyType.SpecialType == SpecialType.System_String;
        // The comparer slot sits before customMessage: a null placeholder keeps `because` in place.
        ArgumentSyntax? comparer = isString
            ? SyntaxFactory.Argument(SyntaxFactory.ParseExpression("System.StringComparer.Ordinal"))
            : context.Because is null ? null : SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));

        return PatternResult.Fixed(
            FluentAssertionsSyntax.Invoke(receiver, "ShouldBeInOrder", directionArgument, comparer, context.Because),
            selector is null ? [] : ["System.Linq"]);
    }
}
