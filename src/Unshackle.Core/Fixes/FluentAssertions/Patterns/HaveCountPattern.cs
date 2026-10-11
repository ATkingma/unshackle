using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class HaveCountPattern : FluentAssertionPattern
{
    // FluentAssertions name → Shouldly comparison on the count (probed at the boundaries n-1, n, n+1).
    private static readonly Dictionary<string, string> s_methods = new(StringComparer.Ordinal)
    {
        ["HaveCount"] = "ShouldBe",
        ["HaveCountGreaterThan"] = "ShouldBeGreaterThan",
        ["HaveCountGreaterThanOrEqualTo"] = "ShouldBeGreaterThanOrEqualTo",
        ["HaveCountGreaterOrEqualTo"] = "ShouldBeGreaterThanOrEqualTo",
        ["HaveCountLessThan"] = "ShouldBeLessThan",
        ["HaveCountLessThanOrEqualTo"] = "ShouldBeLessThanOrEqualTo",
        ["HaveCountLessOrEqualTo"] = "ShouldBeLessThanOrEqualTo",
    };

    public override string Id => "fa.have-count";

    public override string Description => "x.Should().HaveCount(n) → x.Count.ShouldBe(n), HaveCountGreaterThan(n) → x.Count.ShouldBeGreaterThan(n)";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, [.. s_methods.Keys]);

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        SemanticModel model = context.Model;
        InvocationExpressionSyntax assertion = context.Assertion;
        ExpressionSyntax subject = context.Subject;
        if (subject is ConditionalAccessExpressionSyntax)
        {
            return PatternResult.Manual("The subject uses ?. so the count cannot be read directly.");
        }

        if (model.GetSymbolInfo(assertion).Symbol is not IMethodSymbol { Parameters: [{ Type.SpecialType: SpecialType.System_Int32 }, ..] })
        {
            return PatternResult.Manual("Predicate: rewrite to Count.ShouldBeGreaterThan/ShouldBeLessThan/... by hand.");
        }

        ITypeSymbol? type = model.GetTypeInfo(subject).Type;
        if (type is null or IErrorTypeSymbol)
        {
            return PatternResult.Manual("The type of the subject is unknown.");
        }

        ExpressionSyntax receiver = FluentAssertionsSyntax.NotNullReceiver(subject, model);
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
            SyntaxFactory.InvocationExpression(Member(count, s_methods[context.Name]), assertion.ArgumentList),
            namespaces);
    }

    private static MemberAccessExpressionSyntax Member(ExpressionSyntax expression, string name) =>
        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, SyntaxFactory.IdentifierName(name));

    // The type itself or one of its interfaces is (a constructed form of) the named System.Collections(.Generic) interface.
    private static bool Implements(ITypeSymbol type, params string[] names) =>
        type.AllInterfaces.Prepend(type).Any(t => names.Contains(t.OriginalDefinition.Name)
            && t.OriginalDefinition.ContainingNamespace.ToDisplayString() is "System.Collections.Generic"
            && t.OriginalDefinition is INamedTypeSymbol { Arity: > 0 });

    private static bool HasIntProperty(ITypeSymbol type, string name) =>
        type.AllInterfaces.Prepend(type).Any(t => t.GetMembers(name).OfType<IPropertySymbol>().Any(p =>
            p.Type.SpecialType == SpecialType.System_Int32 && p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic));
}
