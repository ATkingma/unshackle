using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// A Profile whose CreateMap&lt;A, B&gt;() calls carry a resolver form of MapFrom (MapFrom&lt;TResolver&gt;(), MapFrom(new TResolver()),
/// MapFrom&lt;TResolver, TSourceMember&gt;(s => s.Y), MapFrom((s, d) => expr), MapFrom((s, d, m, ctx) => expr)) → the same ToB extension
/// methods as <see cref="CreateMapPattern"/>, with X = new TResolver().Resolve(source, ...) or X = expr; see <see cref="ValueResolvers"/>
/// for what moves. Ignore() and MapFrom(s => expr) in the same profile work too; this pattern claims every profile with a resolver form.
/// </summary>
public class ValueResolverPattern : CreateMapPattern
{
    public override string Id => "automapper.value-resolver";

    public override string Description =>
        "ForMember(d => d.X, o => o.MapFrom<TResolver>()) and MapFrom((s, d) => expr) → X = new TResolver().Resolve(source, default!, default!, default!) or X = expr in B ToB(this A source); resolvers with DI dependencies or that use the destination, member or context stay Manual";

    public override IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => ProfileSyntax.IsProfileWithMaps(c, model) && ValueResolvers.UsesResolver(c)).Select(ProfileSyntax.Anchor).Distinct();
}
