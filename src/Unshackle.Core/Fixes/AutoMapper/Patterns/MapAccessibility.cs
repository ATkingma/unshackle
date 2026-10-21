using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// Accessibility checks for <see cref="CreateMapPattern"/>: whether a name is read, whether a symbol is reachable outside the profile, and the effective accessibility of a type.
/// </summary>
internal static class MapAccessibility
{
    // A name that is read (not the right side of a.b, an initializer member, a named argument or a property name of an anonymous type).
    internal static bool IsReference(SimpleNameSyntax name) => name.Parent switch
    {
        MemberAccessExpressionSyntax access => access.Expression == name,
        MemberBindingExpressionSyntax => false,
        NameColonSyntax or NameEqualsSyntax => false,
        AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } assignment => assignment.Left != name,
        _ => true,
    };

    internal static bool IsAvailableOutsideProfile(ISymbol? symbol, ExpressionSyntax body, INamedTypeSymbol? profileType) => symbol switch
    {
        null => false,
        INamedTypeSymbol or INamespaceSymbol or ITypeParameterSymbol => true,
        ILocalSymbol or IParameterSymbol or IRangeVariableSymbol =>
            symbol.Locations.Any(l => l.SourceTree == body.SyntaxTree && body.Span.Contains(l.SourceSpan)),
        _ => symbol.IsStatic && !SymbolEqualityComparer.Default.Equals(symbol.ContainingType, profileType),
    };

    // The accessibility a type really has, through its containing types and type arguments.
    internal static Accessibility Effective(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => Effective(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Aggregate(
            named.ContainingType is { } outer ? Weakest(named.DeclaredAccessibility, Effective(outer)) : named.DeclaredAccessibility,
            (access, argument) => Weakest(access, Effective(argument))),
        _ => Accessibility.Public,
    };

    internal static Accessibility Weakest(Accessibility a, Accessibility b) => (a, b) switch
    {
        (Accessibility.Public, _) => b,
        (_, Accessibility.Public) => a,
        (Accessibility.Internal, Accessibility.Internal) => Accessibility.Internal,
        _ => Accessibility.Private,
    };
}
