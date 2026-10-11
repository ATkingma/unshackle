using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class HaveCountPattern : IPattern
{
    public string Id => "fa.have-count";

    public string Description => "x.Should().HaveCount(n) → x.Count.ShouldBe(n)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "HaveCount");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var assertion = (InvocationExpressionSyntax)node;
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual("HaveCount(...) is not in the form x.Should().HaveCount(n), e.g. after .And.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual("The result of HaveCount(...) is used (.And/.Which or a value); ShouldBe returns nothing.");
        }

        if (subject is ConditionalAccessExpressionSyntax)
        {
            return PatternResult.Manual("The subject uses ?. so the count cannot be read directly.");
        }

        if (model.GetSymbolInfo(assertion).Symbol is not IMethodSymbol { Parameters: [{ Type.SpecialType: SpecialType.System_Int32 }, ..] })
        {
            return PatternResult.Manual("Predicate: rewrite to Count.ShouldBeGreaterThan/ShouldBeLessThan/... by hand.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out _, out _))
        {
            return PatternResult.Manual("HaveCount(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        ITypeSymbol? type = model.GetTypeInfo(subject).Type;
        if (type is null or IErrorTypeSymbol)
        {
            return PatternResult.Manual("The type of the subject is unknown.");
        }

        ExpressionSyntax receiver = IsSafeReceiver(subject) ? FluentAssertionsSyntax.NotNull(subject, model).WithoutTrailingTrivia() : SyntaxFactory.ParenthesizedExpression(subject.WithoutTrivia());
        string[] namespaces = [];
        ExpressionSyntax count;
        if (Implements(type, "IDictionary", "IReadOnlyDictionary"))
        {
            return PatternResult.Manual("Dictionary: check whether Count or Keys is meant.");
        }
        else if (type is IArrayTypeSymbol || HasIntProperty(type, "Length") && !HasIntProperty(type, "Count"))
        {
            count = Member(receiver, "Length");
        }
        else if (HasIntProperty(type, "Count"))
        {
            count = Member(receiver, "Count");
        }
        else if (Implements(type, "IEnumerable"))
        {
            count = SyntaxFactory.InvocationExpression(Member(receiver, "Count"));
            namespaces = ["System.Linq"];
        }
        else
        {
            return PatternResult.Manual("Non-generic IEnumerable: use Cast<T>().Count().");
        }

        return PatternResult.Fixed(
            SyntaxFactory.InvocationExpression(Member(count, "ShouldBe"), assertion.ArgumentList),
            namespaces);
    }

    private static MemberAccessExpressionSyntax Member(ExpressionSyntax expression, string name) =>
        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, SyntaxFactory.IdentifierName(name));

    private static bool IsSafeReceiver(ExpressionSyntax subject) => subject is
        IdentifierNameSyntax or GenericNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax
        or ElementAccessExpressionSyntax or ParenthesizedExpressionSyntax or ThisExpressionSyntax or ObjectCreationExpressionSyntax
        || subject.IsKind(SyntaxKind.SuppressNullableWarningExpression);

    // The type itself or one of its interfaces is (a constructed form of) the named System.Collections(.Generic) interface.
    private static bool Implements(ITypeSymbol type, params string[] names) =>
        type.AllInterfaces.Prepend(type).Any(t => names.Contains(t.OriginalDefinition.Name)
            && t.OriginalDefinition.ContainingNamespace.ToDisplayString() is "System.Collections.Generic"
            && t.OriginalDefinition is INamedTypeSymbol { Arity: > 0 });

    private static bool HasIntProperty(ITypeSymbol type, string name) =>
        type.AllInterfaces.Prepend(type).Any(t => t.GetMembers(name).OfType<IPropertySymbol>().Any(p =>
            p.Type.SpecialType == SpecialType.System_Int32 && p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic));
}
