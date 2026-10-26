using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Generics;

/// <summary>
/// Where a generic type is written in the solution, and the helpers to compare generic types across the compilations of a solution
/// (a symbol of one project is not equal to the same type seen from another one, so types are compared by metadata name).
/// Extracted from MediatR's GenericRequestPattern (TAAK-193) so AutoMapper's generic Map closure (TAAK-324) uses the same collection.
/// </summary>
public static class GenericUses
{
    // Every simple name per compilation, by its text: built once, so a closure over many calls does not walk the solution again for each.
    private static readonly ConditionalWeakTable<Compilation, Dictionary<string, List<SimpleNameSyntax>>> s_names = new();

    public static async Task<IReadOnlyList<Compilation>> CompilationsAsync(Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(solution);
        var compilations = new List<Compilation>();
        foreach (Project project in solution.Projects.Where(p => p.Language == LanguageNames.CSharp))
        {
            if (await project.GetCompilationAsync(cancellationToken) is Compilation compilation)
            {
                compilations.Add(compilation);
            }
        }

        return compilations;
    }

    /// <summary>Every simple name (identifier or generic name) with this text in the compilation's source.</summary>
    public static IReadOnlyList<SimpleNameSyntax> Names(Compilation compilation, string name) =>
        s_names.GetValue(compilation, Index).GetValueOrDefault(name) ?? [];

    private static Dictionary<string, List<SimpleNameSyntax>> Index(Compilation compilation)
    {
        var index = new Dictionary<string, List<SimpleNameSyntax>>(StringComparer.Ordinal);
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            foreach (SimpleNameSyntax name in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>())
            {
                string text = name.Identifier.ValueText;
                if (!index.TryGetValue(text, out List<SimpleNameSyntax>? names))
                {
                    names = [];
                    index[text] = names;
                }

                names.Add(name);
            }
        }

        return index;
    }

    public static bool ContainsTypeParameter(ITypeSymbol type) => type switch
    {
        ITypeParameterSymbol => true,
        INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter),
        IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
        _ => false,
    };

    public static string Prefix(INamedTypeSymbol type) =>
        type.ContainingNamespace.IsGlobalNamespace ? "" : type.ContainingNamespace.ToDisplayString() + ".";

    /// <summary>The name GetTypeByMetadataName finds the type by (nested types with +).</summary>
    public static string MetadataKey(INamedTypeSymbol type) =>
        type.ContainingType is INamedTypeSymbol outer ? MetadataKey(outer) + "+" + type.MetadataName : Prefix(type) + type.MetadataName;

    public static string ArgumentsKey(INamedTypeSymbol closed) =>
        string.Join(", ", closed.TypeArguments.Select(a => a is INamedTypeSymbol n ? MetadataKey(n) : a.ToDisplayString()));

    public static bool InDeclarationOf(SyntaxNode node, INamedTypeSymbol type, SemanticModel model) =>
        node.Ancestors().OfType<TypeDeclarationSyntax>().Any(t => SymbolEqualityComparer.Default.Equals(model.GetDeclaredSymbol(t), type));

    /// <summary>class S : G&lt;X&gt;, with G&lt;X&gt; as the base class of S.</summary>
    public static bool IsSubclassBase(GenericNameSyntax generic, INamedTypeSymbol type, SemanticModel model)
    {
        SyntaxNode written = generic.Parent is QualifiedNameSyntax { Right: var right } qualified && right == generic ? qualified : generic;
        return written.Parent is SimpleBaseTypeSyntax { Parent: BaseListSyntax { Parent: TypeDeclarationSyntax declaration } }
            && model.GetDeclaredSymbol(declaration) is INamedTypeSymbol { TypeKind: TypeKind.Class } subclass
            && SymbolEqualityComparer.Default.Equals(subclass.BaseType, type);
    }

    /// <summary>Every written use of the generic type in the compilations, except the ones inside its own declaration.</summary>
    public static IReadOnlyList<GenericUse> Find(INamedTypeSymbol type, IReadOnlyList<Compilation> compilations) => Find(type, compilations, false);

    /// <summary>
    /// Every written use of the generic type in the compilations; insideDeclaration also gives the uses inside its own declaration
    /// (G&lt;int&gt; in a member of G closes G too), except G&lt;T&gt; written with its own type parameters.
    /// </summary>
    public static IReadOnlyList<GenericUse> Find(INamedTypeSymbol type, IReadOnlyList<Compilation> compilations, bool insideDeclaration)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(compilations);
        string key = MetadataKey(type);
        var uses = new List<GenericUse>();
        foreach (Compilation compilation in compilations)
        {
            foreach (GenericNameSyntax generic in Names(compilation, type.Name).OfType<GenericNameSyntax>().Where(g => g.TypeArgumentList.Arguments.Count == type.Arity))
            {
                SemanticModel model = compilation.GetSemanticModel(generic.SyntaxTree);
                if (model.GetSymbolInfo(generic).Symbol is not INamedTypeSymbol constructed || MetadataKey(constructed.OriginalDefinition) != key)
                {
                    continue;
                }

                GenericUseKind kind;
                if (generic.IsUnboundGenericName)
                {
                    kind = GenericUseKind.Unbound;
                }
                else if (InDeclarationOf(generic, constructed.OriginalDefinition, model)
                    && (!insideDeclaration || constructed.TypeArguments.SequenceEqual<ITypeSymbol>(constructed.OriginalDefinition.TypeParameters, SymbolEqualityComparer.Default)))
                {
                    continue;
                }
                else if (IsSubclassBase(generic, constructed, model))
                {
                    kind = GenericUseKind.SubclassBase;
                }
                else if (constructed.TypeArguments.Any(ContainsTypeParameter))
                {
                    kind = generic.Ancestors().TakeWhile(a => a is TypeSyntax).Prepend(generic).Last().Parent is ObjectCreationExpressionSyntax
                        ? GenericUseKind.OpenCreation
                        : GenericUseKind.Open;
                }
                else
                {
                    kind = GenericUseKind.Closed;
                }

                uses.Add(new GenericUse
                {
                    Constructed = constructed,
                    Node = generic,
                    Model = model,
                    Kind = kind,
                    Where = Where(generic),
                    AssemblyName = compilation.AssemblyName ?? "",
                });
            }
        }

        return uses;
    }

    public static string Where(SyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        return $"{Path.GetFileName(node.SyntaxTree.FilePath)}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";
    }
}
