using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;
using Unshackle.Core.Fixes.AutoMapper.Patterns;

namespace Unshackle.Core.Fixes.AutoMapper;

/// <summary>AutoMapper 14 and older → hand-written mapping extension methods.</summary>
public class AutoMapperFix : IFix
{
    // Without project properties (tests, a host without MSBuild) packability is guessed from the compilation (TypeArgumentClosure).
    public AutoMapperFix()
    {
        Patterns = CreatePatterns(null);
    }

    // TAAK-324 (council round 10, R1): IsPackable as MSBuild sees it decides whether a public generic Map<T> in a library closes its type arguments.
    public AutoMapperFix(IProjectProperties projectProperties)
    {
        ArgumentNullException.ThrowIfNull(projectProperties);
        Patterns = CreatePatterns(projectProperties);
    }

    public string Id => "automapper";

    // Only removed where nothing of AutoMapper is left; a Profile base class keeps it.
    public IReadOnlyList<string> RemovedNamespaces { get; } = ["AutoMapper"];

    public IReadOnlyList<string> AddedNamespaces { get; } = [];

    // A CreateMap rewritten while mapper.Map still runs fails at run time (missing type map): a Manual finding leaves the solution as it is,
    // unless it is a map, call or registration the plan keeps on AutoMapper (TAAK-272, see MapOwners).
    public bool AllOrNothing => true;

    public bool AllowsPartial(Finding finding) =>
        finding.Result is ManualResult { Kept: not KeptReason.None };

    // No Package yet: every migrated Profile stays (empty) and keeps AutoMapper referenced; the package goes once the
    // Profile, Map and AddAutoMapper rows of claude/tasks/automapper-queue.md can remove all of it (TAAK-159: AddAutoMapper
    // goes only without IMapper users; the emptied Profiles still keep the package).
    public IReadOnlyList<IPattern> Patterns { get; }

    // Every pattern that reaches the solution-wide plan (MapOwners) gets the same properties, so each one sees the same plan.
    private static IReadOnlyList<IPattern> CreatePatterns(IProjectProperties? projectProperties) =>
        [new CreateMapPattern(projectProperties), new ForMemberIgnorePattern(projectProperties), new ForMemberMapFromPattern(projectProperties), new ValueResolverPattern(projectProperties),
        new KeptMapPattern(projectProperties), new MapCallPattern(projectProperties), new GenericMapPattern(projectProperties), new GenericProfilePattern(projectProperties), new MapIntoPattern(projectProperties),
        new ProjectToPattern(projectProperties), new MockSetUpPattern(projectProperties), new UnusedMapperPattern(projectProperties), new AddAutoMapperPattern(projectProperties),
        new ConfigurationTestPattern(projectProperties)];
}
