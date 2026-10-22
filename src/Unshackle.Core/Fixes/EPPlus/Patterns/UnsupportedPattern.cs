using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.EPPlus.Patterns;

/// <summary>
/// TAAK-309 (queue row 11): runs last and reports every member (method, property, field, constructor, top-level statement, type with an
/// EPPlus base type or attribute) that still names EPPlus after the other patterns, as Manual with a typed reason and the APIs it uses,
/// each with the queue row that will migrate it. So nothing of EPPlus is left behind silently (general rule, council round 8).
/// A public or protected signature in a packable project that exposes an EPPlus type is Manual by design (E6, <c>PublicApiExposesOldLibrary</c>).
/// Code another EPPlus pattern already reports (a licence line or package it left Manual) is not reported twice.
/// </summary>
public class UnsupportedPattern : IPattern
{
    public const string UnmigratedCode = "UnmigratedEPPlusApi";

    public const string PublicApiCode = "PublicApiExposesOldLibrary";

    private static readonly LicenseContextPattern s_licenseContext = new();

    private static readonly PackagePattern s_package = new();

    public string Id => "epplus.unsupported";

    public string Description => "any EPPlus API no other pattern migrated → Manual with the APIs and their queue rows (E6: public API exposing EPPlus)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        List<TextSpan> reported = [.. s_licenseContext.Find(root, model).Concat(s_package.Find(root, model)).Select(n => n.Span)];
        return root.DescendantNodes()
            .Where(n => n is SimpleNameSyntax && !n.Ancestors().Any(a => a is UsingDirectiveSyntax) && EPPlusApi.Name(n, model) is not null)
            .Where(n => !reported.Any(span => span.Contains(n.Span)))
            .Select(Anchor)
            .OfType<SyntaxNode>()
            .Distinct();
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        string where = $"line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
        if (ExposedSignature(node, model) is string exposed)
        {
            return PatternResult.Manual($"[{PublicApiCode}] {exposed} ({where}) is public API of a packable project and exposes EPPlus: "
                + "its callers outside the solution would break. Change the signature by hand (by design, council round 8 E6).");
        }

        IEnumerable<string> apis = node.DescendantNodesAndSelf().Where(n => n is SimpleNameSyntax).Select(n => EPPlusApi.Name(n, model))
            .OfType<string>().Distinct(StringComparer.Ordinal);
        return PatternResult.Manual($"[{UnmigratedCode}] EPPlus API not migrated ({where}): "
            + string.Join(", ", apis.Select(a => $"{a} ({EPPlusApi.Row(a)})")) + ".");
    }

    // The member the name belongs to; a name in a type's base list or attribute anchors to the type.
    private static SyntaxNode? Anchor(SyntaxNode name) =>
        name.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault(m => m is not BaseNamespaceDeclarationSyntax);

    // E6: a public/protected member (or type) of an outside-visible type whose signature names an EPPlus type, in a packable project.
    private static string? ExposedSignature(SyntaxNode node, SemanticModel model)
    {
        ISymbol? symbol = node is FieldDeclarationSyntax field
            ? field.Declaration.Variables.Select(v => model.GetDeclaredSymbol(v)).FirstOrDefault()
            : model.GetDeclaredSymbol(node);
        if (symbol is null || !IsVisibleOutside(symbol) || !IsPackable(model.Compilation))
        {
            return null;
        }

        IEnumerable<ITypeSymbol> signature = symbol switch
        {
            IMethodSymbol method => [method.ReturnType, .. method.Parameters.Select(p => p.Type)],
            IPropertySymbol property => [property.Type, .. property.Parameters.Select(p => p.Type)],
            IFieldSymbol f => [f.Type],
            IEventSymbol e => [e.Type],
            INamedTypeSymbol type => [.. type.BaseType is { } b ? [b] : Array.Empty<ITypeSymbol>(), .. type.Interfaces],
            _ => [],
        };
        return signature.Any(EPPlusApi.Mentions) ? symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat) : null;
    }

    private static bool IsVisibleOutside(ISymbol symbol) =>
        symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal
        && (symbol.ContainingType is null || IsVisibleOutside(symbol.ContainingType));

    // Same rule as MediatR's AssemblyResolver: a library is packable unless it references a test framework (IsPackable itself is not visible).
    private static bool IsPackable(Compilation compilation) =>
        compilation.Options.OutputKind == OutputKind.DynamicallyLinkedLibrary
        && !compilation.ReferencedAssemblyNames.Any(a => EPPlusApi.TestFrameworks.Contains(a.Name));
}
