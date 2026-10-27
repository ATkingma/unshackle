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
/// Code another EPPlus pattern already reports (a licence line or package it left Manual) is not reported twice; nor (council round 16, E24)
/// a member or address <c>epplus.flow-types</c> or <c>epplus.address</c> left Manual, so each node has one row with the most specific reason.
/// A flow-types member whose signature is E6 public API is skipped by flow-types and gets its one <c>PublicApiExposesOldLibrary</c> row here,
/// with the flow-types reason folded into the text (council round 18, E31). Likewise an <c>epplus.address</c> read left Manual in a member
/// this pattern reports anyway is folded into this row (council round 19, R3).
/// </summary>
public class UnsupportedPattern : IPattern
{
    private static readonly LicenseContextPattern s_licenseContext = new();

    private static readonly PackagePattern s_package = new();

    private static readonly FlowTypesPattern s_flowTypes = new();

    private static readonly AddressPattern s_address = new();

    public string Id => "epplus.unsupported";

    public string Description => "any EPPlus API no other pattern migrated → Manual with the APIs and their queue rows (E6: public API exposing EPPlus)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        // These patterns ran before this one: what they still find is what they left Manual.
        List<TextSpan> reported = [.. Reported(root, model).Concat(s_address.Find(root, model).Select(n => n.Span))];
        return Unreported(root, reported, model).Select(Anchor).OfType<SyntaxNode>().Distinct();
    }

    // R3 (council round 19): whether this pattern reports the member of an address read for another EPPlus name than the address reads
    // themselves; then AddressPattern leaves the read to this row.
    internal static bool ReportsBesides(SyntaxNode addressRead, IReadOnlyList<SyntaxNode> addressReads, SyntaxNode root, SemanticModel model)
    {
        if (Anchor(addressRead) is not { } member)
        {
            return false;
        }

        List<TextSpan> reported = [.. Reported(root, model).Concat(addressReads.Select(n => n.Span))];
        return ExposedSignature(member, model) is not null || Unreported(member, reported, model).Any();
    }

    // What the patterns before this one still find: what they left Manual (the address reads are added by the caller).
    private static IEnumerable<TextSpan> Reported(SyntaxNode root, SemanticModel model) =>
        s_licenseContext.Find(root, model).Concat(s_package.Find(root, model)).Concat(s_flowTypes.Find(root, model)).Select(n => n.Span);

    private static IEnumerable<SyntaxNode> Unreported(SyntaxNode scope, List<TextSpan> reported, SemanticModel model) =>
        scope.DescendantNodes()
            .Where(n => n is SimpleNameSyntax && !n.Ancestors().Any(a => a is UsingDirectiveSyntax) && EPPlusApi.Name(n, model) is not null)
            .Where(n => !reported.Any(span => span.Contains(n.Span)) && !InHandledTail(n, model));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        string where = $"line {node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";

        // R3 (council round 19): address reads AddressPattern left to this row.
        string address = string.Concat(AddressPattern.Candidates(node, model).Select(n => s_address.Rewrite(n, model))
            .OfType<ManualResult>().Select(m => $" Also epplus.address {m.Code}: {m.Reason}"));
        if (ExposedSignature(node, model) is string exposed)
        {
            // E31 (council round 18): flow-types skips this node, so its reason is folded into this one row.
            string flow = FlowTypesPattern.IsCandidate(node, model) && s_flowTypes.Rewrite(node, model) is ManualResult { Code: { } code, Reason: var reason }
                ? $" Also epplus.flow-types {code}: {reason}"
                : string.Empty;
            return PatternResult.Manual(EPPlusReason.PublicApiExposesOldLibrary, $"{exposed} ({where}) is public API of a packable project and exposes EPPlus: "
                + "its callers outside the solution would break. Change the signature by hand (by design, council round 8 E6)." + flow + address);
        }

        IEnumerable<string> apis = node.DescendantNodesAndSelf().Where(n => n is SimpleNameSyntax && !InHandledTail(n, model)).Select(n => EPPlusApi.Name(n, model))
            .OfType<string>().Distinct(StringComparer.Ordinal);
        return PatternResult.Manual(EPPlusReason.UnmigratedEPPlusApi, $"EPPlus API not migrated ({where}): "
            + string.Join(", ", apis.Select(a => $"{a} ({EPPlusApi.Row(a)})")) + "." + address);
    }

    // Row 8: a style statement (one that starts from an ExcelStyle) was rewritten or reported by StylesPattern, which runs before this
    // pattern; only the receiver in front of .Style is left for this one. (A syntax annotation does not survive the workspace between patterns.)
    // Row 9 likewise: a layout statement (before or after LayoutPattern) leaves only its receiver (range, row, column, sheet).
    // Rows 7 and 10: the ClosedXML member a rewrite put on a receiver that is still EPPlus.
    private static bool InHandledTail(SyntaxNode name, SemanticModel model) =>
        DimensionMapping.IsRewrittenName(name, model) || name.Ancestors().OfType<ExpressionStatementSyntax>().FirstOrDefault() is { } statement
        && (StylesMapping.StyleOf(statement, model) is { } style
                ? style is MemberAccessExpressionSyntax access ? access.Expression : style
                : LayoutMapping.Receiver(statement, model)) is { } receiver
        && !receiver.Span.Contains(name.Span);

    // The member the name belongs to; a name in a type's base list or attribute anchors to the type.
    private static SyntaxNode? Anchor(SyntaxNode name) =>
        name.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault(m => m is not BaseNamespaceDeclarationSyntax);

    // E6: a public/protected member (or type) of an outside-visible type whose signature names an EPPlus type, in a packable project.
    internal static string? ExposedSignature(SyntaxNode node, SemanticModel model)
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
