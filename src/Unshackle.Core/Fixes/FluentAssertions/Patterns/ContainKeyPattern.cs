using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

public class ContainKeyPattern : FluentAssertionPattern
{
    public override string Id => "fa.contain-key";

    public override string Description => "dict.Should().ContainKey(k) → dict.ShouldContainKey(k), NotContainKey → ShouldNotContainKey";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(
            root, model, "ContainKey", "NotContainKey", "ContainKeys", "NotContainKeys", "ContainValue", "NotContainValue", "ContainValues", "NotContainValues");

    // Before the argument split: ContainKeys/ContainValues take params, which it would report as becauseArgs.
    protected override PatternResult? RewriteBeforeArguments(AssertionContext context)
    {
        string name = context.Name;
        if (name.EndsWith("Keys", StringComparison.Ordinal))
        {
            return PatternResult.Manual($"{name}(...) checks several keys at once: use one ShouldContainKey/ShouldNotContainKey per key by hand.");
        }

        if (name.EndsWith("Value", StringComparison.Ordinal) || name.EndsWith("Values", StringComparison.Ordinal))
        {
            return PatternResult.Manual($"{name}(...) has no Shouldly equivalent (only ShouldContainKeyAndValue and ShouldNotContainValueForKey): check .Values by hand.");
        }

        return null;
    }

    protected override PatternResult RewriteAssertion(AssertionContext context)
    {
        if (context.Values.Count != 1)
        {
            return PatternResult.Manual($"{context.Name}(...) with an unexpected argument list.");
        }

        ITypeSymbol? subjectType = context.Model.GetTypeInfo(context.Subject).Type;
        if (subjectType is null or IErrorTypeSymbol || IsGenericDictionary(subjectType))
        {
            return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    FluentAssertionsSyntax.NotNull(context.Subject, context.Model).WithoutTrailingTrivia(),
                    SyntaxFactory.IdentifierName("Should" + context.Name)),
                FluentAssertionsSyntax.Arguments([.. context.Values, context.Because])));
        }

        // Shouldly's ShouldContainKey only exists for IDictionary<,>. IReadOnlyDictionary, HttpHeaders and the like: ask the
        // subject itself (ContainsKey or Contains), which keeps its comparer, and assert the bool.
        ITypeSymbol? keyType = context.Model.GetTypeInfo(context.Values[0].Expression).Type;
        string? lookup = FindLookup(subjectType, keyType, context.Model.Compilation);
        if (lookup is null)
        {
            return PatternResult.Manual($"{context.Name}(...) on {subjectType.ToDisplayString()}: Shouldly's ShouldContainKey needs an IDictionary<,> and the type has no ContainsKey/Contains(key) method; assert by hand.");
        }

        string assertion = context.Name == "ContainKey" ? "ShouldBeTrue" : "ShouldBeFalse";
        InvocationExpressionSyntax call = FluentAssertionsSyntax.Invoke(
            FluentAssertionsSyntax.NotNullReceiver(context.Subject, context.Model), lookup, context.Values[0]);
        return PatternResult.Fixed(FluentAssertionsSyntax.Invoke(call, assertion, context.Because));
    }

    private static bool IsGenericDictionary(ITypeSymbol type) =>
        type.AllInterfaces.Prepend(type).Any(t => t.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IDictionary<TKey, TValue>");

    // The first public instance bool method ContainsKey(key), else Contains(key), on the type, its bases or its interfaces.
    private static string? FindLookup(ITypeSymbol type, ITypeSymbol? keyType, Compilation compilation)
    {
        IEnumerable<ITypeSymbol> candidates = type.AllInterfaces.Cast<ITypeSymbol>().Prepend(type);
        for (INamedTypeSymbol? baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            candidates = candidates.Append(baseType);
        }

        ITypeSymbol[] all = [.. candidates];
        foreach (string name in new[] { "ContainsKey", "Contains" })
        {
            bool found = all.SelectMany(t => t.GetMembers(name)).OfType<IMethodSymbol>().Any(m =>
                m is { IsStatic: false, DeclaredAccessibility: Accessibility.Public, Parameters.Length: 1, ReturnType.SpecialType: SpecialType.System_Boolean, IsGenericMethod: false }
                && (keyType is null || compilation.ClassifyConversion(keyType, m.Parameters[0].Type).IsImplicit));
            if (found)
            {
                return name;
            }
        }

        return null;
    }
}
