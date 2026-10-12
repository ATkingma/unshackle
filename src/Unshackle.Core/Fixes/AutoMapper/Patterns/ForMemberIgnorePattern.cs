using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// A Profile whose CreateMap&lt;A, B&gt;() calls carry ForMember(d => d.X, o => o.Ignore()) → the same ToB extension
/// methods as <see cref="CreateMapPattern"/>, with the ignored members left out. The two patterns share one rewrite
/// (a profile becomes one static class); this one claims the profiles that use Ignore, the other the rest.
/// </summary>
public class ForMemberIgnorePattern : CreateMapPattern
{
    public override string Id => "automapper.for-member-ignore";

    public override string Description =>
        "CreateMap<A, B>().ForMember(d => d.X, o => o.Ignore()) → B ToB(this A source) without X; other options and unmatched members stay Manual";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => IsProfileWithMaps(c, model) && UsesForMemberIgnore(c));
}
