# ANALYSE — Manual reduction round 2 (TAAK-306 steps 1–2)

**Date**: 2026-10-22  
**Source data** (read-only, no builds run):

| Set | Path | Reports | Date | Note |
| --- | --- | --- | --- | --- |
| blind | `<corpus>/corpus-blind/_results/after-298/*/report.json` | 36 (24 with MediatR/AutoMapper rows) | after TAAK-298 | current |
| mediatr | `<corpus>/corpus-mediatr/_results/*/report.json` | 9 | 2026-10-12 | **stale**: before TAAK-271..298 |
| automapper | `<corpus>/corpus-automapper/_results/scan/*/report.json` | 16 | 2026-10-15 | **stale**: before TAAK-271..298 |

Method: FluentAssertions rows ignored. Cascade rows ("Not rewritten: this fix changes nothing while other findings are
Manual", `project-file`) are dropped so only **root** reasons count. Reasons are normalised (type names, numbers,
parentheses stripped) and ranked by the number of repos they occur in. Script: a throw-away Python aggregator (not in
the repo); rerun it on fresh reports before acting on the stale sets.

## Step 1 — Inventory

### Repo outcome per set

| Set | Repos with MR/AM rows | Fully clean | Partial | All Manual |
| --- | --- | --- | --- | --- |
| blind (after-298) | 24 | 12 | 9 (baseapi, corebanking, culinary, encryptednotes, estore, fiapgames, fullcart, hospitalappt, msb) | 3 (miru, starter = MediatR; restapi = AutoMapper) |
| mediatr (stale) | 9 | 1 | 0 | 8 (all-or-nothing) |
| automapper (stale) | 16 | 1 | 0 | 15 (all-or-nothing at that time) |

Since TAAK-271..278, AutoMapper is no longer all-or-nothing per solution (blind shows partial repos with kept maps);
MediatR still is (a single root Manual blocks the whole solution: miru 98 rows, starter 11 rows).

### Root reasons, blind (after-298)

| Repos | Rows | Fix | Pattern | Reason (normalised) | Repos |
| --- | --- | --- | --- | --- | --- |
| 6 | 10 | AM | add-automapper | IMapper still used, so AddAutoMapper stays | follow-up of the rows below |
| 4 | 36 | AM | map-call | class under test gets a mocked IMapper, keeps calling IMapper | culinary, encryptednotes, estore, msb |
| 4 | 14 | AM | kept-map | CreateMap stays in profile because a call site keeps IMapper | culinary, encryptednotes, estore, msb |
| 3 | 43 | AM | map-call | mock set-up of IMapper in a test stays | culinary, estore, msb |
| 3 | 18 | AM | map-call | map stays on AutoMapper (unsupported config); write by hand | baseapi, fiapgames, hospitalappt |
| 2 | 5 | AM | map-call | map found another way at run time, would throw missing-type-map | estore, msb |
| 2 | 3 | AM | kept-map | destination member has non-public setter | baseapi, fiapgames |
| 1 | 8 | AM | kept-map / map-call | ReverseMap unflattening of a MapFrom path | hospitalappt |
| 1 | 15 | MR | kept | tied to a decorator that cannot be migrated | fullcart |
| 1 | 1 | MR | generic-decorator | open-generic decorator registration | fullcart |
| 1 | 2 | MR | add-mediatr | scans assembly of a type parameter (source generator needs it at compile time) | miru |
| 1 | 1 | MR | add-mediatr | scans an assembly passed as a parameter of a public method | starter |
| 1 | 2+2+1 | AM | map-call / kept-map / configuration-test | no built-in conversion between member types | restapi |

### Root reasons, mediatr corpus (stale)

| Repos | Rows | Pattern | Reason | Repos |
| --- | --- | --- | --- | --- |
| 3 | 34 | unsupported | Mediator's source generator does not support generic messages/handlers | eshop, kgr, nadir |
| 2 | 2 | add-mediatr | scanned assembly has a non-public handler | ardalis, evo2 |
| 2 | 2 | add-mediatr | scans an assembly passed as a parameter (internal / public method) | evo1, evo2 |
| 1 | 11 | unsupported | a package builds on MediatR (Ardalis-style) | ardalis |
| 1 | 1 | request-processor | pre/post processor not async / wrong return | nadir |

### Root reasons, automapper corpus (stale, top)

| Repos | Rows | Pattern | Reason |
| --- | --- | --- | --- |
| 9 | 39 | map-call | no CreateMap in a rewritten profile, no `ToX()` exists |
| 6 | 44 (+4 map-into) | map-call | `Map<T>` maps a type parameter |
| 6 | 21 | map-call | source may be null and result goes where null is not allowed |
| 5 | 13 | project-to | ProjectTo has no destination type of its own |
| 4 | 89 | map-call | CreateMap outside a Profile constructor (MapperConfiguration lambda) |
| 3 | 10 | create-map | ProjectTo / Map-with-options still present, so CreateMap rewrites stay undone |
| 3 | 5 | map-call | collection kind not copied |
| 2–1 | many | map-call | profile not rewritten because of one unsupported member (null deref, ctor choice, cycles, `opts`, …) |

## Step 2 — Ranking (repos unblocked) and verdict

"Unblocked" = repos where the reason is the (or a) root of the remaining Manual rows. Blind counts weigh more (current).

| Rank | Blocker | Repos (blind / stale) | Verdict | Task |
| --- | --- | --- | --- | --- |
| 1 | AM: class under test gets a mocked `IMapper` (map-call + kept-map + mock set-up + add-automapper follow-ups) | 4 / — | **fixable with a design choice** (council): e.g. keep `IMapper` mock but generate an adapter, or rewrite `Setup(m => m.Map<X>(…))` to the real generated method | TAAK-311 |
| 2 | MR: `AddMediatR` scans a non-literal assembly (type parameter, parameter of a public/internal method) or one with non-public handlers | 2 (miru, starter) / 3 | **partly fixable**: resolve the assembly at the call sites (all callers known in the solution); non-public handler → justified unless `internal` is visible to the generated code | TAAK-312 |
| 3 | AM: nullable source into non-nullable target | — / 6 | **fixable**: emit `src is null ? null! : src.ToX()` only where AutoMapper semantics (returns null) are kept exactly; otherwise justified | TAAK-313 |
| 4 | AM: CreateMap outside a Profile constructor (`new MapperConfiguration(cfg => …)`) | — / 4 (89 rows) | **fixable**: treat the config lambda as a profile body | TAAK-314 |
| 5 | MR: generic messages/handlers | — / 3 (34 rows) | **justified for now** (Mediator SG limitation); option to close generics per used type argument → council | TAAK-315 (council first) |
| 6 | AM: `Map<T>` on a type parameter / no static destination; ProjectTo without own destination | — / 6 + 5 | **justified** (run-time choice) | — |
| 7 | AM: no CreateMap in a rewritten profile | — / 9 | **mostly derived** (map lives in a profile that is not rewritten); re-measure after ranks 1–4 | — |
| 8 | AM: non-public setter, ReverseMap unflattening, missing member conversions | 2–3 / several | **justified** per row (semantics cannot be kept) | — |
| 9 | MR: decorator / package builds on MediatR (fullcart, ardalis) | 1 / 1 | **justified** | — |

## Caveats

- The mediatr and automapper corpus reports predate TAAK-271..298; ranks 3–5 must be re-measured before a task starts
  (main session: rerun those corpora, CPU-heavy).
- Rows counted per pattern; one root can cascade into many rows (miru: 2 root rows → 98 Manual rows).

## Re-measure 2026-10-22 (WIP, stopped at session end)

Scan only (`unshackle scan --max-degree 4`), tool published from main db9238f, copies under `D:/playground/pool-tmp/remeasure/`.

| Set | Repo | Build | Fixed | Manual | TAAK-313 rows | TAAK-314 rows |
| --- | --- | --- | ---: | ---: | ---: | ---: |
| mediatr | kgr | ok | 217 | 0 | – | – |
| mediatr | eshop | fails (partial; scan ran) | 52 | 0 | – | – |

- kgr was 90 Manual (generic messages, project file) in the stale set; eshop did not load before. Both are now 0 Manual.
- Not done: mediatr jason (stopped mid-scan), eshoponweb; every AutoMapper repo (altinnauth, bitwarden, cats, dasblog, devarch,
  eshoponweb, gnomeshade, jason, jhipster, kavita, lancommander, letterbook, neozhu, nop, ombi, personalasst, skoruba, xipona).
- MediatR sources ardalis, amantin, nadir, evo1 and evo2 are gone from `corpus-mediatr/src` (`peek/` is empty); they need a re-clone.
- So no verdict yet on A3/A4 (TAAK-313/314): those counts come only from the AutoMapper corpus. Resume with
  `run.sh list.txt` minus the done lines, then `agg.py` (both in `D:/playground/pool-tmp/remeasure/`).

## See also

- `claude/tasks/TAAK-306-manual-reduction-round-2.md` — the task this analysis belongs to
- `claude/tasks/TAAK-311-am-mocked-imapper.md` .. `TAAK-315-mr-generic-messages.md` — step-3 tasks
- `claude/docs/INDEX.md` — documentation index
