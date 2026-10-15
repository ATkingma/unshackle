# Corpus run — blind validation round (TAAK-268)

Tool: published from main `23c0f28` into `<corpus>/corpus-blind/_results/tool-23c0f28/`. The tool is not changed during this round.
Harness: `claude/tools/corpus-am.sh` with `REPOS=claude/tools/corpus-blind-repos.txt`, list `claude/tools/corpus-blind-list.txt` (fix per line).
Corpus root: `/mnt/dikkeschijf/unshackle-corpus/corpus-blind/`.

## Selection (step 2)

Picked from GitHub code search (`gh search code 'Include="<Library>" Version="<major>.'`, csproj) on 2026-10-15, before any run. None is on the exclusion list. Shas pinned in `corpus-blind-repos.txt`.

| Name | Repo | Fix | Size | Why |
| --- | --- | --- | --- | --- |
| skunkdeco | bfriesen/RandomSkunk.DependencyInjection.Decorator | fluentassertions | small (38 kB) | Library with one test project; FA 6; multi-target incl. net461/netstandard |
| ogooreck | oskardudycz/Ogooreck | fluentassertions | medium | API-testing library (128 stars); FA 6 across multi-targeted test projects |
| ~~omnikeeper~~ | max-bytes/omnikeeper | fluentassertions | large | Replaced: restore fails upstream (NU1201, plugins on net6 reference a newer `Omnikeeper.Base`) |
| xrepo | andyalm/xrepo | fluentassertions | medium (3.5 MB) | Replacement for omnikeeper: CLI tool with an FA 6 test project |
| realworld | minhlong149/realworld-aspnetcore | mediatr | small | RealWorld (Conduit) API with MediatR 12 handlers and a test project |
| fullcart | AmrElshaer/FullCart | mediatr | medium (35 files use MediatR) | Clean-architecture shop; MediatR 12 |
| miru | MiruFx/Miru | mediatr | large (8 test projects) | Web framework built on MediatR; net5–net9 |
| culinary | culinary-code/backend | automapper | small | API with AutoMapper 13 profiles and a test project |
| ~~ctwapi~~ | Coflnet/CtwApi | automapper | small | Replaced: build fails upstream (test project misses the `Coflnet` reference, CS0246) |
| fiapgames | thaynnaraluciano/fiap-cloud-games | automapper | small | Replacement for ctwapi: CQRS API with AutoMapper 13 and 2 test projects |
| msb | Robnoir/MSB | automapper | large (106 files) | Application using AutoMapper 13 (and MediatR); one test project |

## Results

Tool `23c0f28`, run 2026-10-15.

| Repo | Fix | Fixed | Manual | Tests before → after | Regressions | Outcome |
| --- | --- | ---: | ---: | --- | ---: | --- |
| skunkdeco | fluentassertions | 110 | 14 | 26/26 → 26/26 | 0 | migrated, green |
| ogooreck | fluentassertions | 23 | 9 | 20/20 → — | — | **build fails after fix**: 12× CS0452 (TAAK-269) |
| xrepo | fluentassertions | 122 | 9 | 71/71 → 71/71 | 0 | migrated, green |
| realworld | mediatr | 10 | 0 | 10/10 → 10/10 | 0 | fully migrated, green |
| fullcart | mediatr | 0 | 33 | 9/9 → 9/9 | 0 | blocked: 2 justified Manual block 31 findings (TAAK-270) |
| miru | mediatr | 0 | 99 | 415/474 → 414/474 ¹ | 0 | blocked: 4 Manual, could be migrated (TAAK-270) |
| culinary | automapper | 0 | 46 | 149/149 → 149/149 | 0 | blocked: cycle, null sources |
| msb | automapper | 0 | 33 | 156/156 → 156/156 | 0 | blocked: cycle, unflattening |
| fiapgames | automapper | 0 | 32 | 91/91 → 91/91 | 0 | blocked: one profile (TAAK-270) |

¹ Miru: 59 tests need a database and fail in both baseline runs; one more is flaky (compare marks it flaky, not a regression). The tool changed no file.

## Review (step 6)

- **Fixed rewrites (FA, 265; MediatR, 10)**: covered by green test suites in skunkdeco, xrepo and realworld. One defect: `NotBeNull()` on an unconstrained type parameter (ogooreck) → TAAK-269. FA Manual reasons sampled (`Be` on `FluentAssertions.Types`, `.And`/`.Which` used, `.And` on another subject, `BeEquivalentTo` on non-plain types): all justified.
- **Manual (MediatR/AutoMapper)**: the all-or-nothing rule is right (a partial migration breaks at runtime), but on unseen code it means 5 of 6 repos get no change at all. Blockers per repo and verdict in TAAK-270; cycles, null sources and unflattening are justified.

## Conclusion

- **FluentAssertions**: ready on unseen code, apart from one build break (TAAK-269). 2 of 3 repos fully green, 0 regressions.
- **MediatR**: correct where it runs (realworld, 0 regressions), but coverage is limited: 2 of 3 repos blocked by a handful of Manual findings.
- **AutoMapper**: safe (0 regressions, nothing wrongly rewritten) but on unseen code it migrated nothing; cycles and `ReverseMap`/unflattening are common in real profiles.
- Nothing was rewritten wrongly in a way that tests passed but behaviour changed, as far as the tests and review show. Launch claim: "FA → Shouldly: production-ready; MediatR and AutoMapper: safe, report the spots to fix by hand." Round 2 after TAAK-269/270.

## Round 2 (after TAAK-269/270)

Tool `12f86c0` (fixes merged), then `d1f33bf` (two fixes after round 2). Only the three affected repos rerun.

| Repo | Tool | Fixed / Manual | Tests before → after | Regressions | Outcome |
| --- | --- | --- | --- | ---: | --- |
| ogooreck | `12f86c0` | 23 / 9 | — | — | build fails: 12× CS8600, `((object)result)` on a `T` that may be null → fixed in `d1f33bf` with `(object?)` |
| miru | `12f86c0` | 97 / 0 | — | — | build fails: MSG0007, `AddMediatR` in a generic method scans `TAssemblyOfType`; Mediator's generator needs compile-time assemblies → Manual in `d1f33bf` |
| fiapgames | `12f86c0` | 0 / 32 | 91/91 → 91/91 | 0 | still blocked: `UsuarioModel.Nome { get; private set; }` (justified Manual) |
| ogooreck | `d1f33bf` | 23 / 9 | 20/20 → 20/20 | 0 | migrated, green |
| miru | `d1f33bf` | 0 / 98 | 415/474 → 429/474 ¹ | 0 | blocked (justified): generic `AddMediatR` must move to the host by hand |

¹ Database-dependent tests; their count of failures varies between runs, none is a regression.

### Conclusion after round 2

- **FluentAssertions**: 3/3 unseen repos migrated and green, 0 regressions.
- **MediatR**: 1/3 fully migrated; the other two are blocked by Manual findings that are justified (direct `MediatR.Mediator` use, `AddMediatR` in a generic library method). Round 2 also showed that a Fixed rewrite (the expression-to-block lambda of TAAK-270) can break the generator; caught by the build, now Manual.
- **AutoMapper**: 0/3 migrated, all blocked by justified Manual findings (cycles, unflattening, private setters); 0 regressions.

## See also

- `claude/tasks/TAAK-268-blind-validation-round.md`
- `claude/tasks/CORPUSRUN-automapper.md`
