using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.XunitV3.Patterns;

/// <summary>
/// <c>[CollectionBehavior("Type", "Assembly")]</c>, <c>[TestCaseOrderer(..)]</c>, <c>[TestCollectionOrderer(..)]</c> and
/// <c>[TestFramework(..)]</c> with type and assembly names → <c>typeof(Type)</c>: v3 only has the <c>Type</c> constructor. Fixed when
/// both names are constant strings and the type is declared in this project (assembly name equal); a built-in or another assembly's
/// type stays Manual, as its v3 counterpart has another name or shape.
/// </summary>
public class TypeNameAttributePattern : IPattern
{
    private static readonly HashSet<string> s_attributes = new(
        ["Xunit.CollectionBehaviorAttribute", "Xunit.TestCaseOrdererAttribute", "Xunit.TestCollectionOrdererAttribute", "Xunit.TestFrameworkAttribute"],
        StringComparer.Ordinal);

    public string Id => "xunit3.type-name-attribute";

    public string Description => "[TestCaseOrderer(\"Type\", \"Assembly\")] and friends → typeof(Type)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root.DescendantNodes().OfType<AttributeSyntax>()
            .Where(a => a.ArgumentList is { Arguments.Count: 2 } && model.GetSymbolInfo(a).Symbol is IMethodSymbol constructor
                && s_attributes.Contains(constructor.ContainingType.ToDisplayString()) && XunitV3Assemblies.IsFrom(constructor, XunitV3Assemblies.Core)
                && constructor.Parameters.All(p => p.Type.SpecialType == SpecialType.System_String));
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is not AttributeSyntax { ArgumentList: { Arguments.Count: 2 } arguments } attribute)
        {
            return PatternResult.Manual(XunitV3Reason.UnsupportedShape, "unexpected node.");
        }

        if (model.GetConstantValue(arguments.Arguments[0].Expression).Value is not string typeName
            || model.GetConstantValue(arguments.Arguments[1].Expression).Value is not string assemblyName)
        {
            return PatternResult.Manual(XunitV3Reason.TypeNameArgument, $"{attribute.Name} takes names that are not constant strings; v3 takes typeof(..), rewrite it by hand.");
        }

        INamedTypeSymbol? type = model.Compilation.GetTypeByMetadataName(typeName.Trim());
        if (type is null || !SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, model.Compilation.Assembly)
            || !string.Equals(model.Compilation.AssemblyName, assemblyName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            // Council 22 X11: no v2 → v3 name map; the documented v3 name is only a hint in the text.
            string hint = XunitV3SdkTypes.V3Hints.TryGetValue(typeName.Trim(), out string? v3Name) ? $" Hint: the documented v3 type is {v3Name}." : "";
            return PatternResult.Manual(XunitV3Reason.TypeNameArgument,
                $"{attribute.Name} names {typeName} in {assemblyName}, which is not a type of this project; no v2 → v3 name map, and v3 takes typeof(..) of a v3 type, so pick its v3 counterpart by hand.{hint}");
        }

        TypeSyntax typeSyntax = SyntaxFactory.ParseTypeName(type.ToMinimalDisplayString(model, attribute.SpanStart));
        AttributeArgumentListSyntax typed = arguments.WithArguments(
            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(typeSyntax))));
        return PatternResult.Fixed(attribute.WithArgumentList(typed));
    }
}
