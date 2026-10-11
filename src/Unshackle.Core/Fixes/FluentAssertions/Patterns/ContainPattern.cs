using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class ContainPattern : FluentAssertionPattern
{
    public override string Id => "fa.contain";

    public override string Description => "x.Should().Contain(item|predicate) → x.ShouldContain(item|predicate), NotContain → ShouldNotContain";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "Contain", "NotContain");

    // Before the argument split: the dictionary overloads take params key/value pairs.
    protected override PatternResult? RewriteBeforeArguments(AssertionContext context) =>
        context.Model.GetTypeInfo(context.Subject).Type is { } subjectType
        && subjectType.AllInterfaces.Prepend(subjectType).Any(t => t.OriginalDefinition.Name is "IDictionary" or "IReadOnlyDictionary")
            ? PatternResult.Manual($"Dictionary: {context.Name}(...) takes key/value pairs; use ShouldContainKey/ShouldContainKeyAndValue by hand.")
            : null;

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        SemanticModel model = context.Model;
        string name = context.Name;
        IReadOnlyList<ArgumentSyntax> values = context.Values;
        if (values.Count != 1 || model.GetSymbolInfo(context.Assertion).Symbol is not IMethodSymbol method)
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

        ITypeSymbol? type = model.GetTypeInfo(context.Subject).Type;
        if (type is null or IErrorTypeSymbol
            || type.SpecialType != SpecialType.System_String && !type.AllInterfaces.Prepend(type).Any(IsGenericEnumerable))
        {
            return PatternResult.Manual("The subject is not a string or a generic IEnumerable<T>.");
        }

        string shouldName = name == "Contain" ? "ShouldContain" : "ShouldNotContain";
        ArgumentListSyntax arguments = context.Assertion.ArgumentList;
        if (type.SpecialType == SpecialType.System_String)
        {
            // FluentAssertions compares substrings case-sensitively, Shouldly case-insensitively by default.
            ArgumentSyntax caseSensitive = SyntaxFactory.Argument(SyntaxFactory.ParseExpression("Shouldly.Case.Sensitive"));
            arguments = FluentAssertionsSyntax.Arguments(values[0], caseSensitive, context.Because);
        }

        return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                FluentAssertionsSyntax.NotNull(context.Subject, model).WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName(shouldName)),
            arguments));
    }

    private static bool IsGenericEnumerable(ITypeSymbol t) =>
        t.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T;
}
