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
`_mapper.Map<B>(a)` becomes `a.ToDto()`. `ProjectTo<>` is rewritten too when the result is provably the same.

## Roadmap
State on 2026-10-28 (see `claude/docs/overdracht.md` for the ongoing work).

| Step | Goal | State |
| --- | --- | --- |
| 1 | Scan + report (CSV/JSON/Markdown), load solution, count calls | Done |
| 2 | FluentAssertions → Shouldly: the most used assertions first, `--dry-run` with diff | 100%: corpus round 5 (2026-10-14): 16/16 solutions build after the fix, 0 regressions apart from Akka DData (bug in Akka, reported as a warning, TAAK-200). See `claude/tasks/CORPUSRUN-fa.md` |
| 3 | MediatR → Mediator: handlers, `ValueTask`, behaviors, processors, DI registration | 100%: 7 of 9 corpus projects fully migrated and green; evo1/evo2 rightly Manual |
| 4 | Launch: README with GIF, NuGet, testing on a real open-source project, post on r/dotnet | Done in substance (TAAK-019); waiting on the user (publishing, post) |
| 5 | AutoMapper → hand-written mapping (`ToB()` extensions) | 100%: all rows of `claude/tasks/automapper-queue.md` done (TAAK-146, -155..159, -244..267); corpus run on the 14 usable survey repos with 0 regressions (TAAK-258) |
| 6 | Blind validation: unseen open-source projects per fix, tool frozen per round, every finding reviewed | Done (TAAK-268..270, 3 rounds, 18 repos): FA 6/6 migrated and green; MediatR 3/6 migrated, 3 justly blocked; AutoMapper 1/6 migrated, 5 justly blocked. 0 regressions. See `claude/tasks/CORPUSRUN-blind.md` |
| 7 | Moq → NSubstitute (TAAK-307) | Patterns built; corpus round 2 (5 repos): B2-B7 fixed, B1 accepted limitation (proxy type name, not Manual-detectable), H items open; TAAK-330 report leftovers done, corpus rerun open. See `claude/tasks/CORPUSRUN-moq.md` |
| 8 | Polly v7 → Microsoft.Extensions.Resilience (TAAK-308) | Patterns built; corpus round 3 (2026-10-27): 12 of 14 repos build and test with 0 regressions; B5/B6 (package downgrade) fixed in TAAK-354 as Manual, rerun open. Polly fee applies from 2026-11-16. See `claude/tasks/CORPUSRUN-polly.md` |
| 9 | EPPlus → ClosedXML (TAAK-309) | Patterns built; per-project migration (TAAK-326): 17 of 30 loadable repos migrate at least one project, after-fix builds green (montr, manga, simoona). TAAK-327 flow types + `epplus.address` done, corpus re-measure open; transitive-dependency loss in TAAK-329. See `claude/tasks/CORPUSRUN-epplus.md` |
| 10 | MassTransit v8 → open-source bus (TAAK-310) | Report-only detector (10 patterns); corpus 0 bugs. Rewrite not yet studied |
| 11 | SpecFlow → Reqnroll (TAAK-340) | First slice built (packages, namespaces, renamed types, v3→v4 breaks), all-or-nothing. Open: `<Using>` items and `Directory.Build.props`, `specflow.json` → `reqnroll.json`, Cucumber-expression risk, corpus round; per-project migration in backlog TAAK-353 |
| 12 | xUnit v2 → v3 (TAAK-341) | Patterns built (packages, `OutputType Exe`, abstractions, async lifetime/void, record-exception-async, extensibility Manual). Open: council 22 items X10-X13, `xunit.runner.visualstudio` bump, corpus round (equal discovered-test count, X2), blind round |
| 13 | Z.EntityFramework bulk → EF Core `ExecuteDelete`/`ExecuteUpdate` (TAAK-342) | `efbulk.delete`, `efbulk.update`, `efbulk.unsupported` built with SQL run-time proof; EF Core < 7 blocked. Open: corpus round (step 9) |

### Conclusion

All three migrations are feature-complete for the patterns in their queues. On 18 open-source projects the tool had never seen (step 6) it caused 0 test regressions:
FluentAssertions → Shouldly migrated 6 of 6 projects fully and green; MediatR 3 of 6; AutoMapper 1 of 6. Where it did not migrate, it stopped
on purpose with a Manual reason (cycles, private setters, `AddMediatR` in a generic library method) instead of guessing. Every defect the blind
rounds found broke the build after `fix`, never a test silently, and each is fixed with a proof. Launch claim: FluentAssertions → Shouldly is
production-ready; MediatR and AutoMapper are safe and migrate what is provably equivalent, and list the rest for the user.

## Risks
- **The hype fades.** Hence keep the engine generic (a separate general search-and-replace was considered and dropped from the roadmap, 2026-10-14).
- **Small differences in behaviour.** Hence always a report and `--dry-run` first, and run the user's tests after the fix.
- **Others come out with separate converters.** Speed matters.
