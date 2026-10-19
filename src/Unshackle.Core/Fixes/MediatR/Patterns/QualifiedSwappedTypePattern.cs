using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

/// <summary>
/// A swapped type written with its namespace (afripay: <c>record Q : MediatR.IRequest&lt;R&gt;</c> in a file without <c>using MediatR;</c>):
/// the using swap does not reach it, so the namespace part becomes <c>Mediator</c> (<c>global::MediatR.X</c> → <c>global::Mediator.X</c>).
/// </summary>
public class QualifiedSwappedTypePattern : IPattern
{
    private const string OldNamespace = "MediatR";

    private const string NewNamespace = "Mediator";

    private static readonly HashSet<string> s_swapped = [.. MediatRFix.SwappedTypeNames];

    public string Id => "mediatr.qualified-swapped-type";

    public string Description => "MediatR.IRequest<T> (a swapped type written with its namespace) → Mediator.IRequest<T>";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<QualifiedNameSyntax>().Where(q => IsOldNamespace(q.Left) && IsSwapped(q, model));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var name = (QualifiedNameSyntax)node;
        NameSyntax left = name.Left is AliasQualifiedNameSyntax alias
            ? alias.WithName(SyntaxFactory.IdentifierName(NewNamespace))
            : SyntaxFactory.IdentifierName(NewNamespace);
        return PatternResult.Fixed(name.WithLeft(left.WithTriviaFrom(name.Left)));
    }

    private static bool IsOldNamespace(NameSyntax left) => left switch
    {
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText == OldNamespace,
        AliasQualifiedNameSyntax { Alias.Identifier.ValueText: "global", Name: IdentifierNameSyntax identifier } => identifier.Identifier.ValueText == OldNamespace,
        _ => false,
    };

    private static bool IsSwapped(QualifiedNameSyntax name, SemanticModel model) =>
        model.GetSymbolInfo(name).Symbol is INamedTypeSymbol type
        && type.ContainingNamespace.ToDisplayString() == OldNamespace
        && s_swapped.Contains($"{OldNamespace}.{type.MetadataName}");
}
