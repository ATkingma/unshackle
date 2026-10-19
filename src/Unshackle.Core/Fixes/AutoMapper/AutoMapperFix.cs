using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using Unshackle.Core.Fixes.AutoMapper.Patterns;

namespace Unshackle.Core.Fixes.AutoMapper;

/// <summary>AutoMapper 14 and older → hand-written mapping extension methods.</summary>
public class AutoMapperFix : IFix
{
    public string Id => "automapper";

    // Only removed where nothing of AutoMapper is left; a Profile base class keeps it.
    public IReadOnlyList<string> RemovedNamespaces { get; } = ["AutoMapper"];

    public IReadOnlyList<string> AddedNamespaces { get; } = [];

    // A CreateMap rewritten while mapper.Map still runs fails at run time (missing type map): a Manual finding leaves the solution as it is,
    // unless it is a map, call or registration the plan keeps on AutoMapper (TAAK-272, see MapOwners).
    public bool AllOrNothing => true;

    public bool AllowsPartial(Finding finding) =>
        finding.Result is ManualResult { Reason: var reason } && reason.StartsWith(MapOwners.KeptPrefix, StringComparison.Ordinal);

    // No Package yet: every migrated Profile stays (empty) and keeps AutoMapper referenced; the package goes once the
    // Profile, Map and AddAutoMapper rows of claude/tasks/automapper-queue.md can remove all of it (TAAK-159: AddAutoMapper
    // goes only without IMapper users; the emptied Profiles still keep the package).
    public IReadOnlyList<IPattern> Patterns { get; } = [new CreateMapPattern(), new ForMemberIgnorePattern(), new ForMemberMapFromPattern(), new ValueResolverPattern(), new KeptMapPattern(), new MapCallPattern(), new MapIntoPattern(), new ProjectToPattern(), new UnusedMapperPattern(), new AddAutoMapperPattern(),
        new ConfigurationTestPattern()];
}
