using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// Also the base of NotBePattern: the same checks, only the Shouldly name differs.
public class ShouldBePattern : FluentAssertionPattern
{
    public override string Id => "fa.should-be";

    public override string Description => "x.Should().Be(y) → x.ShouldBe(y)";

    protected virtual string ShouldName => "ShouldBe";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "Be");

    // No NotNull(subject): ShouldBe<T> accepts a nullable T, and a null subject passes FluentAssertions' NotBe
    // while ShouldNotBeNull would make it fail.
    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        SemanticModel model = context.Model;
        InvocationExpressionSyntax assertion = context.Assertion;

        // XElement/XDocument and MethodInfoSelector Be(...) do not compare with Equals like ShouldBe does. TypeAssertions does:
        // Be<T>()/Be(typeof(T)) and NotBe match ShouldBe(typeof(T)) exactly (probed: same, base, interface, open generic, null).
        ISymbol? method = model.GetSymbolInfo(assertion).Symbol;
        string? assertionsNamespace = method?.ContainingNamespace.ToDisplayString();
        bool typeAssertion = method?.ContainingType.Name == "TypeAssertions";
        if (assertionsNamespace is "FluentAssertions.Xml" or "FluentAssertions.Types" && !typeAssertion)
        {
            return PatternResult.Manual($"{context.Name}(...) from {assertionsNamespace} has different equality semantics than {ShouldName}.");
        }

        if (typeAssertion && method is IMethodSymbol { TypeArguments: [ITypeSymbol typeArgument] })
        {
            ExpressionSyntax typeOf = SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(typeArgument.ToMinimalDisplayString(model, assertion.SpanStart)));
            return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(
                context.Subject.WithoutTrailingTrivia(), ShouldName, SyntaxFactory.Argument(typeOf), context.Because));
        }

        if (context.Values.Count != 1)
        {
            return PatternResult.Manual($"{context.Name}(...) with a comparer: pass it to {ShouldName} by hand.");
        }

        ExpressionSyntax expected = context.Values[0].Expression;
        ITypeSymbol? subjectType = model.GetTypeInfo(context.Subject).Type;
        if (subjectType is null or IErrorTypeSymbol)
        {
            return PatternResult.Manual("The type of the subject is unknown.");
        }

        ExpressionSyntax? newExpected = FluentAssertionsSyntax.ConvertExpected(expected, subjectType, model, assertion.SpanStart);
        if (newExpected is null)
        {
            return PatternResult.Manual(
                $"Expected type {model.GetTypeInfo(expected).Type} differs from subject type {subjectType}; {ShouldName} cannot infer one type.");
        }

        ArgumentListSyntax arguments = FluentAssertionsSyntax.Arguments(context.Values[0].WithExpression(newExpected), context.Because);
        InvocationExpressionSyntax rewritten = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                context.Subject.WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName(ShouldName)),
            arguments);
        return new FixedResult([rewritten], []) { Warning = EqualsOrderWarning(subjectType, model.GetTypeInfo(expected).Type) };
    }

    // FluentAssertions calls actual.Equals(expected), Shouldly expected.Equals(actual): only an asymmetric Equals tells them apart
    // (Akka's ORMultiValueDictionaryDelta, TAAK-200). Warned for an own Equals (a record's generated one is symmetric, so not), and for
    // two sides whose runtime type is unknown (object, a type parameter, an interface).
    private static string? EqualsOrderWarning(ITypeSymbol subjectType, ITypeSymbol? expectedType)
    {
        if (expectedType is null)
        {
            return null;
        }

        const string Order = "Shouldly compares expected.Equals(actual), FluentAssertions actual.Equals(expected)";
        if ((OwnEquals(subjectType) ?? OwnEquals(expectedType)) is INamedTypeSymbol own)
        {
            return $"{Order}; check symmetry of {own.ToDisplayString()}.Equals.";
        }

        return IsOpen(subjectType) && IsOpen(expectedType)
            ? $"{Order}; the runtime types are unknown here ({subjectType.ToDisplayString()}, {expectedType.ToDisplayString()}), check symmetry of their Equals."
            : null;

        static bool IsOpen(ITypeSymbol type) =>
            type.SpecialType == SpecialType.System_Object || type.TypeKind is TypeKind.TypeParameter or TypeKind.Interface;
    }

    // The type, or a base type, outside the BCL that declares an Equals(x) of its own; null when there is none.
    private static INamedTypeSymbol? OwnEquals(ITypeSymbol type)
    {
        for (var current = type as INamedTypeSymbol; current is not null && !IsBcl(current.ContainingAssembly); current = current.BaseType)
        {
            if (current.TypeKind is TypeKind.Class or TypeKind.Struct
                && current.GetMembers("Equals").OfType<IMethodSymbol>().Any(m => !m.IsStatic && !m.IsImplicitlyDeclared && m.Parameters.Length == 1))
            {
                return current;
            }
        }

        return null;
    }

    private static bool IsBcl(IAssemblySymbol? assembly) =>
        assembly?.Name is { } name
        && (name is "mscorlib" or "netstandard" || name.StartsWith("System", StringComparison.Ordinal) || name.StartsWith("Microsoft", StringComparison.Ordinal));
}
