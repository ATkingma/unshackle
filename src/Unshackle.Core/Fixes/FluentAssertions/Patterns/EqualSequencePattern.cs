using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class EqualSequencePattern : FluentAssertionPattern
{
    public override string Id => "fa.equal-sequence";

    public override string Description => "x.Should().Equal(seq) / Equal(1, 2) → x.ShouldBe(seq) / x.ShouldBe(new[] { 1, 2 }) (order-sensitive); on a dictionary of value items → x.ShouldBe(dict, ignoreOrder: true)";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "Equal");

    // Before the argument split: Equal(params T[] elements) would be reported as becauseArgs.
    protected override PatternResult? RewriteBeforeArguments(AssertionContext context)
    {
        SemanticModel model = context.Model;
        InvocationExpressionSyntax assertion = context.Assertion;
        var method = model.GetSymbolInfo(assertion).Symbol as IMethodSymbol;
        if (method is { ContainingType.Name: "GenericDictionaryAssertions" })
        {
            return DictionaryPairs(context) is { } reason ? PatternResult.Manual(reason) : null;
        }

        if (method is not { ContainingType.Name: "GenericCollectionAssertions" or "StringCollectionAssertions" })
        {
            return PatternResult.Manual("Equal(...) on a dictionary compares without order; check ShouldBe or ShouldBeEquivalentTo by hand.");
        }

        // Equal(params T[] elements): build the array FluentAssertions builds, typed so ShouldBe infers the element type.
        if (method.Parameters is [{ IsParams: true, Type: IArrayTypeSymbol arrayType }]
            && assertion.ArgumentList.Arguments.All(a => a.NameColon is null)
            && !(assertion.ArgumentList.Arguments.Count == 1
                && SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(assertion.ArgumentList.Arguments[0].Expression).ConvertedType, arrayType)
                && model.GetTypeInfo(assertion.ArgumentList.Arguments[0].Expression).Type is IArrayTypeSymbol))
        {
            // Equal(["a", "b"]): the collection expression is the array itself, not one element of it (TAAK-113).
            IEnumerable<ExpressionSyntax> values = assertion.ArgumentList.Arguments.Select(a => a.Expression);
            if (assertion.ArgumentList.Arguments is [{ Expression: CollectionExpressionSyntax collection }])
            {
                if (!collection.Elements.All(e => e is ExpressionElementSyntax))
                {
                    return PatternResult.Manual("Equal([...]) with a spread element: write the expected sequence as an array by hand.");
                }

                values = collection.Elements.Cast<ExpressionElementSyntax>().Select(e => e.Expression);
            }

            ExpressionSyntax array = ParamsArraySyntax.Create(assertion, values, arrayType, model);
            return Fixed(context, SyntaxFactory.Argument(array));
        }

        if (method.Parameters.Any(p => p.Type is INamedTypeSymbol { DelegateInvokeMethod: not null }))
        {
            return PatternResult.Manual("Equal(...) with a predicate: pass an IEqualityComparer to ShouldBe by hand.");
        }

        return null;
    }

    // The only sequence ShouldBe with a message is (expected, ignoreOrder, customMessage). A dictionary (only here when
    // DictionaryPairs accepted it) compares its pairs in any order, like FluentAssertions (TAAK-225).
    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        if (context.Model.GetSymbolInfo(context.Assertion).Symbol is IMethodSymbol { ContainingType.Name: "GenericDictionaryAssertions" })
        {
            ArgumentSyntax ignoreOrder = SyntaxFactory.ParseArgumentList("(ignoreOrder: true)").Arguments[0];
            return context.Because is null
                ? Fixed(context, context.Values[0], ignoreOrder)
                : Fixed(context, context.Values[0], ignoreOrder, context.Because);
        }

        return context.Because is null
            ? Fixed(context, context.Values[0])
            : Fixed(context, context.Values[0], SyntaxFactory.ParseArgumentList("(ignoreOrder: false)").Arguments[0], context.Because);
    }

    // Null when dict.ShouldBe(expected, ignoreOrder: true) means the same, else the Manual reason. FluentAssertions compares the keys
    // and per key the value with Equals, Shouldly the KeyValuePairs with Equals (probed in TAAK-225: order, missing/extra key, other
    // value, null values, empty, immutable and read-only subjects). Only for keys and values whose Equals is the value (IsValueLike),
    // and an expectation that is itself a dictionary with the same pairs (a list could repeat a key, ShouldBe infers one pair type).
    private static string? DictionaryPairs(AssertionContext context)
    {
        const string Hint = "Equal(...) on a dictionary compares without order; check ShouldBe or ShouldBeEquivalentTo by hand.";
        SemanticModel model = context.Model;
        if (context.Assertion.ArgumentList.Arguments.Count == 0
            || Pair(model.GetTypeInfo(context.Subject).Type) is not { } pair
            || model.GetTypeInfo(context.Assertion.ArgumentList.Arguments[0].Expression).Type is not { } expectedType)
        {
            return Hint;
        }

        if (!SymbolEqualityComparer.Default.Equals(pair, Pair(expectedType)) || !IsDictionary(expectedType))
        {
            return $"{Hint} The expectation is no dictionary with the same key and value types.";
        }

        return pair.TypeArguments.All(BeEquivalentToObjectPattern.IsValueLike)
            ? null
            : $"{Hint} The keys or values ({pair.TypeArguments[0].Name}, {pair.TypeArguments[1].Name}) are no leaf, record or struct of leaves, so their Equals may differ from what the test means.";
    }

    // KeyValuePair<TKey, TValue> of the one IEnumerable<KeyValuePair<,>> the type is or implements, else null.
    private static INamedTypeSymbol? Pair(ITypeSymbol? type) =>
        type is null
            ? null
            : type.AllInterfaces.Prepend(type).OfType<INamedTypeSymbol>()
                .Where(i => i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                .Select(i => i.TypeArguments[0])
                .OfType<INamedTypeSymbol>()
                .Where(t => t.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.KeyValuePair<TKey, TValue>")
                .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
                .SingleOrDefault();

    private static bool IsDictionary(ITypeSymbol type) =>
        type.AllInterfaces.Prepend(type).OfType<INamedTypeSymbol>()
            .Any(i => i.OriginalDefinition.ToDisplayString() is "System.Collections.Generic.IDictionary<TKey, TValue>"
                or "System.Collections.Generic.IReadOnlyDictionary<TKey, TValue>");

    private static PatternResult Fixed(AssertionContext context, params IEnumerable<ArgumentSyntax> arguments) =>
        PatternResult.Fixed(FluentAssertionsSyntax.Invoke(context.Subject.WithoutTrailingTrivia(), "ShouldBe", arguments));
}
