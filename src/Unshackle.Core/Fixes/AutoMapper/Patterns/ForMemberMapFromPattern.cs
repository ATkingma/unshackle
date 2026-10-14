using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// A Profile whose CreateMap&lt;A, B&gt;() calls carry ForMember(d => d.X, o => o.MapFrom(s => expr)) → the same ToB extension
/// methods as <see cref="CreateMapPattern"/>, with X = expr (the lambda parameter becomes source). Ignore() in the same profile
/// works too. The patterns share one rewrite (a profile becomes one static class); this one claims every profile that uses MapFrom.
/// </summary>
public class ForMemberMapFromPattern : CreateMapPattern
{
    public override string Id => "automapper.for-member-map-from";

    public override string Description =>
        "CreateMap<A, B>().ForMember(d => d.X, o => o.MapFrom(s => expr)) → B ToB(this A source) with X = expr; expressions that may throw on null or need a conversion stay Manual";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => IsProfileWithMaps(c, model) && UsesForMember(c, ignore: false)).Select(Anchor).Distinct();
}
