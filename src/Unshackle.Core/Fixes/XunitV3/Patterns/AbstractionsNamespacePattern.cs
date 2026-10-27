using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.XunitV3.Patterns;

/// <summary>
/// <c>using Xunit.Abstractions;</c> → <c>using Xunit;</c> (dropped when the scope already has it) and <c>Xunit.Abstractions.ITestOutputHelper</c>
/// → <c>Xunit.ITestOutputHelper</c>: v3 moved ITestOutputHelper to Xunit. Every other Xunit.Abstractions type is extensibility API that v3
/// redesigned, and an own ITestOutputHelper implementation misses the new members: both Manual.
/// </summary>
public class AbstractionsNamespacePattern : IPattern
{
    private const string AbstractionsNamespace = "Xunit.Abstractions";

    private const string OutputHelper = "ITestOutputHelper";

    public string Id => "xunit3.abstractions-namespace";

    public string Description => "using Xunit.Abstractions → using Xunit (ITestOutputHelper)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        if (!model.Compilation.ReferencedAssemblyNames.Any(a => string.Equals(a.Name, XunitV3Assemblies.Abstractions, StringComparison.OrdinalIgnoreCase)))
        {
            return [];
        }

        return root.DescendantNodes().Where(n => n switch
        {
            UsingDirectiveSyntax { Alias: null, StaticKeyword.RawKind: 0 } directive => IsAbstractions(directive.Name),
            QualifiedNameSyntax qualified => IsAbstractions(qualified.Left),
            _ => false,
        });
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => node switch
    {
        UsingDirectiveSyntax directive => RewriteUsing(directive, model),
        QualifiedNameSyntax qualified => RewriteQualified(qualified, model),
        _ => PatternResult.Manual(XunitV3Reason.UnsupportedShape, "unexpected node."),
    };

    private static PatternResult RewriteUsing(UsingDirectiveSyntax directive, SemanticModel model)
    {
        // A global using serves every file of the project; a plain one only its own file.
        IEnumerable<SyntaxTree> trees = directive.GlobalKeyword.IsKind(SyntaxKind.GlobalKeyword) ? model.Compilation.SyntaxTrees : [directive.SyntaxTree];
        if (Blocker(trees, model.Compilation) is PatternResult blocker)
        {
            return blocker;
        }

        // The same scope (file or namespace block) already imports Xunit: the swapped directive would be a duplicate (CS0105).
        SyntaxList<UsingDirectiveSyntax> siblings = directive.Parent switch
        {
            CompilationUnitSyntax unit => unit.Usings,
            BaseNamespaceDeclarationSyntax ns => ns.Usings,
            _ => [],
        };
        if (siblings.Any(u => u != directive && u.Alias is null && u.StaticKeyword.RawKind == 0 && u.Name?.ToString() == "Xunit"
            && u.GlobalKeyword.RawKind == directive.GlobalKeyword.RawKind))
        {
            return PatternResult.Fixed([]);
        }

        NameSyntax xunit = directive.Name is QualifiedNameSyntax { Left: AliasQualifiedNameSyntax alias } ? alias : SyntaxFactory.IdentifierName("Xunit");
        return PatternResult.Fixed(directive.WithName(xunit.WithTriviaFrom(directive.Name!)));
    }

    private static PatternResult RewriteQualified(QualifiedNameSyntax qualified, SemanticModel model)
    {
        if (qualified.Right.Identifier.ValueText != OutputHelper || qualified.Right is GenericNameSyntax)
        {
            return PatternResult.Manual(XunitV3Reason.ExtensibilityType,
                $"{AbstractionsNamespace}.{qualified.Right.Identifier.ValueText} is extensibility API that xUnit v3 redesigned (see the v3 extensibility migration guide); migrate it by hand.");
        }

        if (Blocker([qualified.SyntaxTree], model.Compilation) is PatternResult blocker)
        {
            return blocker;
        }

        NameSyntax xunit = ((QualifiedNameSyntax)qualified.Left).Left;
        return PatternResult.Fixed(qualified.WithLeft(xunit.WithTriviaFrom(qualified.Left)));
    }

    // Manual when the files the swap serves use another Xunit.Abstractions type or implement ITestOutputHelper themselves.
    private static PatternResult? Blocker(IEnumerable<SyntaxTree> trees, Compilation compilation)
    {
        var others = new SortedSet<string>(StringComparer.Ordinal);
        foreach (SyntaxTree tree in trees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            SyntaxNode root = tree.GetRoot();
            foreach (SimpleNameSyntax name in root.DescendantNodes().OfType<SimpleNameSyntax>().Where(n => n.Parent is not UsingDirectiveSyntax))
            {
                if (model.GetSymbolInfo(name).Symbol is INamedTypeSymbol type && XunitV3Assemblies.IsFrom(type, XunitV3Assemblies.Abstractions))
                {
                    if (type.Name != OutputHelper)
                    {
                        others.Add(type.Name);
                    }
                    else if (name.Parent is SimpleBaseTypeSyntax || name.Parent is QualifiedNameSyntax { Parent: SimpleBaseTypeSyntax })
                    {
                        return PatternResult.Manual(XunitV3Reason.OutputHelperImplemented,
                            $"{Location(name)} implements ITestOutputHelper, which has more members in v3 (Output, Write); implement them by hand.");
                    }
                }
            }
        }

        return others.Count == 0
            ? null
            : PatternResult.Manual(XunitV3Reason.ExtensibilityType,
                $"{string.Join(", ", others)} from {AbstractionsNamespace} is extensibility API that xUnit v3 redesigned (see the v3 extensibility migration guide); migrate it by hand.");
    }

    private static string Location(SyntaxNode node) =>
        node.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>() is { } type ? type.Identifier.ValueText : Path.GetFileName(node.SyntaxTree.FilePath);

    private static bool IsAbstractions(NameSyntax? name) => name switch
    {
        QualifiedNameSyntax { Left: IdentifierNameSyntax { Identifier.ValueText: "Xunit" }, Right.Identifier.ValueText: "Abstractions" } => true,
        QualifiedNameSyntax { Left: AliasQualifiedNameSyntax { Alias.Identifier.ValueText: "global", Name.Identifier.ValueText: "Xunit" }, Right.Identifier.ValueText: "Abstractions" } => true,
        _ => false,
    };
}
