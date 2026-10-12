using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Counterpart of EqualSequencePattern: Shouldly has no sequence overload of ShouldNotBe, but the generic ShouldNotBe<T>(T, T)
// compares sequences element by element, in order, like FluentAssertions' NotEqual.
public class NotEqualPattern : FluentAssertionPattern
{
    public override string Id => "fa.not-equal";

    public override string Description => "x.Should().NotEqual(seq) → x.ShouldNotBe(seq) (order-sensitive; explicit IEnumerable<T> when the types differ)";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "NotEqual");

    protected override PatternResult? RewriteBeforeArguments(AssertionContext context)
    {
        if (context.Model.GetSymbolInfo(context.Assertion).Symbol is not IMethodSymbol { ContainingType.Name: "GenericCollectionAssertions" or "StringCollectionAssertions" })
        {
            return PatternResult.Manual("NotEqual(...) on a dictionary compares without order; check ShouldNotBe by hand.");
        }

        return null;
    }

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        SemanticModel model = context.Model;
        var method = (IMethodSymbol)model.GetSymbolInfo(context.Assertion).Symbol!;
        ExpressionSyntax expected = context.Values[0].Expression;

        // FluentAssertions' NotEqual fails on a null subject, ShouldNotBe passes it: guard unless the flow state proves non-null
        // (also in a nullable-oblivious context), so the migrated test is not weaker.
        ExpressionSyntax receiver = FluentAssertionsSyntax.IsSafeReceiver(context.Subject)
            ? context.Subject.WithoutTrailingTrivia()
            : SyntaxFactory.ParenthesizedExpression(context.Subject.WithoutTrivia());
        if (model.GetTypeInfo(context.Subject).Nullability.FlowState != NullableFlowState.NotNull)
        {
            receiver = FluentAssertionsSyntax.Invoke(receiver, "ShouldNotBeNull");
        }

        InvocationExpressionSyntax call = FluentAssertionsSyntax.Invoke(receiver, "ShouldNotBe", context.Values[0], context.Because);

        // T is inferred only when both sides have the same type; List<int> against int[] needs ShouldNotBe<IEnumerable<int>>.
        ITypeSymbol? subjectType = model.GetTypeInfo(context.Subject).Type;
        ITypeSymbol? expectedType = model.GetTypeInfo(expected).Type;
        if (subjectType is null || expectedType is null || !SymbolEqualityComparer.Default.Equals(subjectType, expectedType))
        {
            string element = ((INamedTypeSymbol)method.Parameters[0].Type).TypeArguments[0].ToMinimalDisplayString(model, context.Assertion.SpanStart);
            var member = (MemberAccessExpressionSyntax)call.Expression;
            call = call.WithExpression(member.WithName(
                SyntaxFactory.GenericName("ShouldNotBe").WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ParseTypeName($"System.Collections.Generic.IEnumerable<{element}>"))))));
        }

        return PatternResult.Fixed(call);
    }
}
