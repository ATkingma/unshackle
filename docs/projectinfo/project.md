# Unshackle: project idea

## In one sentence
A `dotnet tool` that uses Roslyn to automatically migrate .NET code away from libraries that
have gone commercial. First a report, then a preview, only then changes.

## The problem
In 2025 three widely used .NET libraries switched to a commercial licence:

| Library | Last free version | After that | Common alternative |
|---|---|---|---|
| FluentAssertions | 7.2.2 (Apache-2.0) | from 8: commercial | Shouldly, AwesomeAssertions |
| MediatR | 12.5.0 (Apache-2.0) | from 13: RPL-1.5 or commercial | Mediator (source-generated), plain DI |
| AutoMapper | 14.0.0 (MIT) | from 15: RPL-1.5 or commercial | Mapperly, hand-written mapping |

Teams can pin the old version, but then get no more updates. Whoever migrates
has to change hundreds to thousands of places. Find-and-replace does not work, because the
right rewrite depends on types and overloads.

Proof that the need exists:
- [RoseMCP #334](https://github.com/AtomicBlom/RoseMCP/issues/334): 3,724 assertions in ~150 files
  to Shouldly. "No tool rewrites a code pattern across a solution", so the plan was a
  throwaway Roslyn script.
- [Altinn/dialogporten #3024](https://github.com/Altinn/dialogporten/issues/3024): replacing MassTransit,
  FluentAssertions, AutoMapper and MediatR.

## What already exists (research 10 October 2026)
| Tool | Why it does not close the gap |
|---|---|
| [FluentAssertionsToShouldlyConverter](https://github.com/kmtchl/FluentAssertionsToShouldlyConverter) | One library, list and dictionary checks often manual work |
| [Shouldly.FromFluentAssertions](https://github.com/galadril/Shouldly.FromFluentAssertions) | PowerShell, replaces text and knows nothing about types |
| AutoMapperToMapperly (mark-new) | Only .NET Framework (according to search results) |
| [AutoMap.Generator](https://github.com/Swevo/AutoMap.Generator) | Leaves the AutoMapper configuration in place, no `.ForMember` yet |
| [NuGet License Gate](https://marketplace.visualstudio.com/items?itemName=ReadyStack.nuget-commercial-license-gate) | Only warns |
| [RoseMCP](https://github.com/AtomicBlom/RoseMCP) `rose_replace_pattern` | General semantic search-and-replace in an MCP server, without recipes |
| OpenRewrite / Moderne | C# recipes are paid |
| MediatR → Mediator | Nothing automated found |

**The gap:** there are separate scripts per library, but no tool that handles several libraries,
looks at types and reports up front what can be done automatically and what cannot.

## What it should look like
```
dotnet tool install -g unshackle

unshackle scan MijnApp.sln --fix fluentassertions --report rapport.csv
unshackle fix  MijnApp.sln --fix fluentassertions --dry-run
unshackle fix  MijnApp.sln --fix fluentassertions
unshackle list
```

## Examples of rewrites

**FluentAssertions → Shouldly**
```csharp
result.Should().Be(42);                     →  result.ShouldBe(42);
users.Should().HaveCount(3);                →  users.Count.ShouldBe(3);        // List
                                            →  users.Count().ShouldBe(3);      // IEnumerable
name.Should().BeEquivalentTo("timme");      →  name.ShouldBe("timme", StringCompareShould.IgnoreCase);
act.Should().Throw<ArgumentException>();    →  Should.Throw<ArgumentException>(act);
```
Roslyn is needed here for two reasons. Whether `.Count` or `.Count()` is right depends on the type.
And `BeEquivalentTo` on strings ignores case. With text replacement tests silently become stricter.

**MediatR → Mediator**: `using MediatR` becomes `using Mediator`, `Task<T> Handle` becomes
`ValueTask<T> Handle` (only for methods that implement the MediatR interface), and
`AddMediatR(...)` becomes `AddMediator()`.

**AutoMapper → hand-written mapping**: every `CreateMap<A, B>()` with `.ForMember` becomes a
`ToDto()` extension method with all properties written out (also the implicitly mapped ones), and
`_mapper.Map<B>(a)` becomes `a.ToDto()`. `ProjectTo<>` goes into the report as manual work.

## Roadmap
State on 2026-10-15 (see `claude/docs/overdracht.md` for the ongoing work).

| Step | Goal | State |
| --- | --- | --- |
| 1 | Scan + report (CSV/JSON/Markdown), load solution, count calls | Done |
| 2 | FluentAssertions → Shouldly: the 15 most used assertions, `--dry-run` with diff | 100%: corpus round 5 (2026-10-14): 16/16 solutions build after the fix, 0 regressions apart from Akka DData (bug in Akka, reported as a warning, TAAK-200). See `claude/tasks/CORPUSRUN-fa.md` |
| 3 | MediatR → Mediator: handlers, `ValueTask`, DI registration (behaviors → report) | 100%: 7 of 9 corpus projects fully migrated and green; evo1/evo2 rightly Manual |
| 4 | Launch: README with GIF, NuGet, testing on a real open-source project, post on r/dotnet | Done in substance (TAAK-019); waiting on the user (publishing, post) |
| 5 | AutoMapper → hand-written mapping (`ToB()` extensions) | 100%: all rows of `claude/tasks/automapper-queue.md` done (TAAK-146, -155..159, -244..267); corpus run on the 14 usable survey repos with 0 regressions (TAAK-258) |
| 6 | Blind validation: 3 unseen open-source projects per fix, tool frozen, every finding reviewed by hand | In progress (TAAK-268): baseline 7/9 ok; ctwapi and omnikeeper do not build upstream and are being replaced. See `claude/tasks/CORPUSRUN-blind.md` |

### Conclusion

All three migrations are feature-complete for the patterns in their queues, and every corpus round ended with 0 regressions.
Those corpora were also used to develop the patterns, so the numbers overstate quality on code the tool has never seen.
The launch (step 4) should therefore quote the blind round (step 6), not the development corpora: only when that round
also shows 0 regressions and a hand review finds no wrong rewrites is the tool ready to publish. Defects found there
become their own tasks and a second blind round.

## Risks
- **The hype fades.** Hence keep the engine generic (a separate general search-and-replace was considered and dropped from the roadmap, 2026-10-14).
- **Small differences in behaviour.** Hence always a report and `--dry-run` first, and run the user's tests after the fix.
- **Others come out with separate converters.** Speed matters.
