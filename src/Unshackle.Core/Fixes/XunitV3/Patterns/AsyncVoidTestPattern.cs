using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.XunitV3.Patterns;

/// <summary>
/// <c>async void</c> test methods (<c>[Fact]</c>, <c>[Theory]</c> or an attribute deriving from them) → <c>async Task</c>: v3 fails such a
/// test at run time (xUnit1049). Builds on v2 too.
/// </summary>
public class AsyncVoidTestPattern : IPattern
{
    private const string FactAttribute = "Xunit.FactAttribute";

    private static readonly string[] s_taskNamespaces = ["System.Threading.Tasks"];

    public string Id => "xunit3.async-void-test";

    public string Description => "[Fact]/[Theory] async void → async Task";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(SyntaxKind.AsyncKeyword) && m.ReturnType is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword })
            .Where(m => m.AttributeLists.Count > 0 && model.GetDeclaredSymbol(m) is IMethodSymbol method && method.GetAttributes().Any(a => IsFact(a.AttributeClass)));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is not MethodDeclarationSyntax declaration || model.GetDeclaredSymbol(declaration) is not IMethodSymbol method)
        {
            return PatternResult.Manual(XunitV3Reason.UnsupportedShape, "unexpected node.");
        }

        // A call or method group would change meaning (an awaitable discarded, a delegate type that no longer fits).
        bool referenced = declaration.SyntaxTree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
            .Any(n => n.Identifier.ValueText == method.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, method));
        if (referenced)
        {
            return PatternResult.Manual(XunitV3Reason.AsyncVoidCalled, $"{method.Name} is an async void test that code also calls; make it async Task and update those calls by hand.");
        }

        TypeSyntax task = SyntaxFactory.IdentifierName("Task").WithTriviaFrom(declaration.ReturnType);
        return PatternResult.Fixed(declaration.WithReturnType(task), s_taskNamespaces);
    }

    private static bool IsFact(INamedTypeSymbol? attribute)
    {
        for (INamedTypeSymbol? current = attribute; current is not null; current = current.BaseType)
        {
            if (current.ToDisplayString() == FactAttribute && XunitV3Assemblies.IsFrom(current, XunitV3Assemblies.Core))
            {
                return true;
            }
        }

        return false;
    }
}
