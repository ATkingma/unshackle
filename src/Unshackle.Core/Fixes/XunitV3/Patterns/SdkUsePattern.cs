using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.XunitV3.Patterns;

/// <summary>
/// Council round 22, X12: an Info row per file that uses v2 <c>Xunit.Sdk</c> types (outside the base lists and attributes of type
/// declarations, which <see cref="ExtensibilityPattern"/> reports). The row names every type with every line, and says when xunit.v3 3.2.2
/// has no type with the same full name (<see cref="XunitV3SdkTypes.MissingInV3"/>). Info, so it never blocks the AllOrNothing fix; step 7
/// (corpus) decides whether any become Manual.
/// </summary>
public class SdkUsePattern : IPattern
{
    private const string SdkNamespace = "Xunit.Sdk";

    private static readonly HashSet<string> s_v2Assemblies = new(XunitV3Assemblies.V2, StringComparer.OrdinalIgnoreCase);

    public string Id => "xunit3.sdk-use";

    public string Description => "Uses of v2 Xunit.Sdk types → Info row per file (type, lines, missing in xunit.v3)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(root);
        return Uses(root, model).Any() ? [root] : [];
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(node);
        IEnumerable<string> types = Uses(node, model)
            .GroupBy(u => u.Type, SymbolEqualityComparer.Default)
            .Select(g => (Type: (INamedTypeSymbol)g.Key!, Lines: g.Select(u => u.Line).Distinct().Order().ToList()))
            .OrderBy(t => t.Lines[0])
            .Select(t => $"{t.Type.Name} ({(t.Lines.Count == 1 ? "line" : "lines")} {string.Join(", ", t.Lines)}"
                + (IsMissingInV3(t.Type) ? "; no same-name type in xunit.v3 3.2.2)" : ")"));
        return PatternResult.Note($"v2 Xunit.Sdk types used in this file: {string.Join(", ", types)}. "
            + "v3 moved or redesigned much of Xunit.Sdk; check each use against xunit.v3 3.2.2 (council round 22, X12).");
    }

    private static IEnumerable<(INamedTypeSymbol Type, int Line)> Uses(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<SimpleNameSyntax>()
            .Where(n => !IsDeclarationOrUsing(n))
            .Select(n => (Type: SdkType(model.GetSymbolInfo(n).Symbol), Node: n))
            .Where(u => u.Type is not null)
            .Select(u => (u.Type!, u.Node.GetLocation().GetLineSpan().StartLinePosition.Line + 1));

    // A using is not a use; a base list or attribute of a type declaration is ExtensibilityPattern's Manual row.
    private static bool IsDeclarationOrUsing(SimpleNameSyntax name) =>
        name.Ancestors().Any(a => a is UsingDirectiveSyntax
            || (a is BaseListSyntax or AttributeListSyntax && a.Parent is BaseTypeDeclarationSyntax));

    private static INamedTypeSymbol? SdkType(ISymbol? symbol)
    {
        INamedTypeSymbol? type = symbol switch
        {
            INamedTypeSymbol named => named.OriginalDefinition,
            IMethodSymbol { MethodKind: MethodKind.Constructor } constructor => constructor.ContainingType.OriginalDefinition,
            _ => null,
        };
        return type is { ContainingAssembly: { } assembly } && s_v2Assemblies.Contains(assembly.Name)
            && type.ContainingNamespace.ToDisplayString() == SdkNamespace
            ? type
            : null;
    }

    private static bool IsMissingInV3(INamedTypeSymbol type) =>
        type.ContainingType is null && XunitV3SdkTypes.MissingInV3.Contains($"{SdkNamespace}.{type.MetadataName}");
}
