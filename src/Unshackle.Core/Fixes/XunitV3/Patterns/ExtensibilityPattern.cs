using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.XunitV3.Patterns;

/// <summary>
/// Manual inventory of xUnit extensibility code: a class that derives from or implements a v2 <c>Xunit.Sdk</c> type (a custom
/// <c>DataAttribute</c>, <c>BeforeAfterTestAttribute</c>, orderer, discoverer, test framework or runner), <c>ITestCollectionOrderer</c> or
/// <c>MemberDataAttributeBase</c>, or that carries an <c>Xunit.Sdk</c> discoverer attribute. v3 redesigned that API (other signatures,
/// ValueTask, the Xunit.v3 namespace), so every such class is rewritten by hand. Never Fixed.
/// </summary>
public class ExtensibilityPattern : IPattern
{
    private const string SdkNamespace = "Xunit.Sdk";

    private static readonly HashSet<string> s_extensibilityTypes = new(["Xunit.ITestCollectionOrderer", "Xunit.MemberDataAttributeBase"], StringComparer.Ordinal);

    private static readonly HashSet<string> s_v2Assemblies = new([XunitV3Assemblies.Core, "xunit.execution.dotnet", "xunit.execution.desktop"], StringComparer.OrdinalIgnoreCase);

    public string Id => "xunit3.extensibility";

    public string Description => "Custom xUnit extensibility (Xunit.Sdk data attributes, orderers, discoverers, runners) → Manual";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => model.GetDeclaredSymbol(c) is INamedTypeSymbol type && ExtensibilityTypes(type).Count > 0);
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is not ClassDeclarationSyntax declaration || model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol type)
        {
            return PatternResult.Manual(XunitV3Reason.UnsupportedShape, "unexpected node.");
        }

        return PatternResult.Manual(XunitV3Reason.ExtensibilityType,
            $"{type.Name} builds on the v2 extensibility API ({string.Join(", ", ExtensibilityTypes(type))}); v3 redesigned it, rewrite the class by hand.");
    }

    private static List<string> ExtensibilityTypes(INamedTypeSymbol type)
    {
        // An interface that a plain v2 base type brings (TheoryData implements Xunit.Sdk.ITheoryData) is not the user's extensibility code.
        HashSet<INamedTypeSymbol> inherited = new(BaseTypes(type).Where(b => IsV2(b) && !IsExtensibility(b)).SelectMany(b => b.AllInterfaces), SymbolEqualityComparer.Default);
        IEnumerable<INamedTypeSymbol> bases = BaseTypes(type).Concat(type.AllInterfaces.Where(i => !inherited.Contains(i)));
        IEnumerable<INamedTypeSymbol> attributes = type.GetAttributes().Select(a => a.AttributeClass).OfType<INamedTypeSymbol>();
        return [.. bases.Concat(attributes).Where(IsExtensibility).Select(t => t.OriginalDefinition.ToDisplayString()).Distinct(StringComparer.Ordinal)];
    }

    private static IEnumerable<INamedTypeSymbol> BaseTypes(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
        {
            yield return current;
        }
    }

    private static bool IsV2(INamedTypeSymbol type) => type.ContainingAssembly is { } assembly && s_v2Assemblies.Contains(assembly.Name);

    private static bool IsExtensibility(INamedTypeSymbol type) =>
        IsV2(type)
        &&(type.ContainingNamespace.ToDisplayString() == SdkNamespace || s_extensibilityTypes.Contains(type.OriginalDefinition.ToDisplayString()));
}
