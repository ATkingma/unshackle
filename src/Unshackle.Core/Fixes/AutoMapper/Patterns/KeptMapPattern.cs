using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// Reports each CreateMap statement that a partial migration (TAAK-272, <see cref="MapOwners"/>) leaves in a profile next to its generated
/// extension class: Manual, with why the map stays on AutoMapper. Runs after the profile patterns, so it sees the trimmed constructor; a
/// profile that keeps all its maps is reported once by the profile pattern instead.
/// </summary>
public class KeptMapPattern : ISolutionPattern
{
    public string Id => "automapper.kept-map";

    public string Description =>
        "CreateMap<A, B>() left in a trimmed profile by the partial migration → Manual (stays on AutoMapper), with the reason; changes nothing";

    // Only after a rewrite: a profile whose <Profile>Extensions class sits in the same file.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        HashSet<string> extensions = [.. root.DescendantNodes().OfType<ClassDeclarationSyntax>().Select(c => c.Identifier.ValueText)];
        return root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Where(c => CreateMapPattern.IsProfileWithMaps(c, model) && extensions.Contains(CreateMapPattern.ExtensionsName(c)))
            .SelectMany(c => CreateMapPattern.MapStatements(c, model))
            .Where(s => MapOwners.StatementPair(s, model) is not null);
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => Kept(node, model, MapOwners.None);

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        return Kept(node, model, await MapOwners.ForAsync(solution, model, cancellationToken));
    }

    private static PatternResult Kept(SyntaxNode node, SemanticModel model, MapOwners owners)
    {
        (ITypeSymbol from, ITypeSymbol to, _) = MapOwners.StatementPair(node, model)!.Value;
        return PatternResult.Manual($"{MapOwners.KeptPrefix}CreateMap<{from.Name}, {to.Name}> stays in the profile, the rest of it is migrated: "
            + (owners.Reason(from, to) ?? owners.Shared(from, to) ?? "the profile pattern left it in place"));
    }
}
