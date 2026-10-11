using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class ContainPattern : IPattern
{
    public string Id => "fa.contain";

    public string Description => "x.Should().Contain(item|predicate) → x.ShouldContain(item|predicate), NotContain → ShouldNotContain";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "Contain", "NotContain");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        var assertion = (InvocationExpressionSyntax)node;
        string name = ((MemberAccessExpressionSyntax)assertion.Expression).Name.Identifier.ValueText;
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual($"{name}(...) is not in the form x.Should().{name}(...), e.g. after .And.");
        }

        if (!FluentAssertionsSyntax.IsResultDiscarded(assertion, model))
        {
            return PatternResult.Manual($"The result of {name}(...) is used (.And/.Which or a value); Shouldly's ShouldContain returns nothing.");
        }

        if (model.GetTypeInfo(subject).Type is { } subjectType
            && subjectType.AllInterfaces.Prepend(subjectType).Any(t => t.OriginalDefinition.Name is "IDictionary" or "IReadOnlyDictionary"))
        {
            return PatternResult.Manual($"Dictionary: {name}(...) takes key/value pairs; use ShouldContainKey/ShouldContainKeyAndValue by hand.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because))
        {
            return PatternResult.Manual($"{name}(...) with becauseArgs or named arguments: Shouldly cannot format the message.");
        }

        if (values.Count != 1 || model.GetSymbolInfo(assertion).Symbol is not IMethodSymbol method)
        {
            return PatternResult.Manual($"{name}(...) with an occurrence constraint: no Shouldly equivalent.");
        }

        // The declared parameter tells the overloads apart: T (an item), an expression (a predicate) or IEnumerable<T> (all items).
        ITypeSymbol declared = method.OriginalDefinition.Parameters[0].Type;
        if (declared.TypeKind != TypeKind.TypeParameter && declared.SpecialType != SpecialType.System_String
            && declared.OriginalDefinition.Name is not ("Expression" or "KeyValuePair"))
        {
            return PatternResult.Manual($"{name}({declared.Name}) checks several items at once: use ShouldContain per item or ShouldBe/ShouldBeSubsetOf by hand.");
        }

        ITypeSymbol? type = model.GetTypeInfo(subject).Type;
        if (type is null or IErrorTypeSymbol
            || type.SpecialType != SpecialType.System_String && !type.AllInterfaces.Prepend(type).Any(IsGenericEnumerable))
        {
            return PatternResult.Manual("The subject is not a string or a generic IEnumerable<T>.");
        }

        string shouldName = name == "Contain" ? "ShouldContain" : "ShouldNotContain";
        ArgumentListSyntax arguments = assertion.ArgumentList;
        if (type.SpecialType == SpecialType.System_String)
        {
            // FluentAssertions compares substrings case-sensitively, Shouldly case-insensitively by default.
            ArgumentSyntax caseSensitive = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(" Shouldly.Case.Sensitive"));
            arguments = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>(
                because is null ? new[] { values[0], caseSensitive } : new[] { values[0], caseSensitive, because.WithLeadingTrivia(SyntaxFactory.Space) }));
        }

        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                FluentAssertionsSyntax.NotNull(subject, model).WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName(shouldName)),
            arguments));
    }

    private static bool IsGenericEnumerable(ITypeSymbol t) =>
        t.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T;
}
