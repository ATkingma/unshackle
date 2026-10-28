using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.MassTransit.Patterns;

// Shared checks of the MassTransit detector. A symbol that resolves must live in a MassTransit namespace; one that does not resolve (a
// transport package the project builds against but the model lacks) is matched by name in a file that imports MassTransit.
internal static class MassTransitSyntax
{
    public const string Namespace = "MassTransit";

    public const string Guidance =
        " For now, pin MassTransit < 9; 9.x is commercial. OpenTransit (the v8 fork) would keep this code as it is once it ships on NuGet; Wolverine and Rebus need it rewritten by hand.";

    public static bool IsMassTransit(ISymbol? symbol) =>
        symbol?.ContainingNamespace?.ToDisplayString() is string ns && (ns == Namespace || ns.StartsWith(Namespace + ".", StringComparison.Ordinal));

    // Invocations of these method names that bind to MassTransit, or bind to nothing in a file that imports MassTransit.
    public static IEnumerable<SyntaxNode> FindCalls(SyntaxNode root, SemanticModel model, IReadOnlySet<string> names) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => MethodName(i) is string name && names.Contains(name) && Binds(i, model, root));

    // Classes whose base list names one of these generic types (by metadata name without arity, e.g. IConsumer).
    public static IEnumerable<SyntaxNode> FindClasses(SyntaxNode root, SemanticModel model, IReadOnlySet<string> typeNames) =>
        root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => c.BaseList is { } list && list.Types.Any(t => NamesType(t.Type, model, root, typeNames)));

    public static bool NamesType(TypeSyntax type, SemanticModel model, SyntaxNode root, IReadOnlySet<string> typeNames)
    {
        string? name = type switch
        {
            GenericNameSyntax generic => generic.Identifier.ValueText,
            QualifiedNameSyntax { Right: GenericNameSyntax generic } => generic.Identifier.ValueText,
            QualifiedNameSyntax { Right: IdentifierNameSyntax identifier } => identifier.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            _ => null,
        };
        if (name is null || !typeNames.Contains(name))
        {
            return false;
        }

        ISymbol? symbol = model.GetSymbolInfo(type).Symbol;
        return symbol is null ? ImportsMassTransit(root) : IsMassTransit(symbol);
    }

    private static bool Binds(InvocationExpressionSyntax invocation, SemanticModel model, SyntaxNode root)
    {
        SymbolInfo info = model.GetSymbolInfo(invocation);
        ISymbol? symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        return symbol is null ? ImportsMassTransit(root) : IsMassTransit(symbol);
    }

    private static string? MethodName(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        MemberBindingExpressionSyntax binding => binding.Name.Identifier.ValueText,
        SimpleNameSyntax simple => simple.Identifier.ValueText,
        _ => null,
    };

    private static bool ImportsMassTransit(SyntaxNode root) =>
        root.DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax).OfType<UsingDirectiveSyntax>()
            .Any(u => u.Name?.ToString() is string name && (name == Namespace || name.StartsWith(Namespace + ".", StringComparison.Ordinal)))
        || root.DescendantTokens().Any(t => t.IsKind(SyntaxKind.IdentifierToken) && t.ValueText == Namespace);
}
