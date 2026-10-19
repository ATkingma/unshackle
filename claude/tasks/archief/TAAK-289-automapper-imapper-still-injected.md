# TAAK-289 — AutoMapper: `AddAutoMapper` stays because `IMapper` is still injected

**Status**: ✅ done (2026-10-19, branch `worktree-agent-ac55b759e0d16a786`, not pushed; the main session merges)
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- pi22317 and assistiva migrated most maps but kept `AddAutoMapper` because classes still injected `IMapper` after all their calls were migrated.
- A first attempt (another machine, `worktree-agent-a61f15fbd8fdc5ded` @ `2105675`) was lost; this is a rebuild from scratch.

## Approach

New pattern `automapper.unused-imapper` (`UnusedMapperPattern`) with a solution-wide plan `MapperCleanup`; `AddAutoMapperPattern` ignores uses
inside code the plan removes. Details: [plan](../../docs/projectinfo/patterns/automapper-unused-imapper-plan.md).

## Steps

- [x] 1. Measure on the corpus repo(s) and probe the old library; KingmaDoc plan for touched patterns/features
- [x] 2. Implementation + unit tests + proofs (`_Combined` where patterns cooperate)
- [x] 3. Corpus `after` on pi22317, assistiva, culinary, msb, encryptednotes, estore: build green, 0 regressions; before/after numbers here and in CORPUSRUN-blind.md
- [x] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## Measurement (step 1)

- pi22317: `IMapper` in 3 services (only Map calls), 3 test fixtures (`_mapper = MapperHelper.CreateMapper()` passed to the constructor),
  `SeedDemoData` (`GetService<IMapper>()` + `if (mapper == null) { …; return; }`), and the factory `MapperHelper.CreateMapper()`.
- assistiva: 12 controllers with only Map calls (DI only). Blocker 2: `builder.Services.AddAutoMapper(...)` is a top-level statement, and removing
  it was an internal error ("not the element of a list") once nothing used `IMapper`.

## Choices

- **Greatest fixed point** over all `IMapper` fields, constructor parameters, locals and mapper factories: a symbol goes when every use is a
  migrated (not kept) call or inside code the plan removes. All-or-nothing in `AutoMapperFix` makes "found and not kept" enough.
- **Keys by documentation comment id**: symbols of a service seen from a test project are other compilation's symbols (the first version compared them
  with `SymbolEqualityComparer` and kept every parameter).
- **Droppable values only**: names, member accesses, `?? throw`, `CreateMapper()`, `new MapperConfiguration/Mapper(..)`, a removed factory,
  `GetService/GetRequiredService<IMapper>()` (only while the solution calls `AddAutoMapper`). A `null` argument is not droppable (a test of the null check).
- **The null-guard assumption, reviewed conservatively**: a `GetService<IMapper>()` lookup with `if (x == null) …` (no `else`) is removed only when the
  solution calls `AddAutoMapper` (so the branch never ran), and the finding carries a warning. Without the registration, or with another check, the lookup stays.
- **One finding per outermost type**, so a removed first member or statement leaves no blank line.
- Empty constructors and factory classes stay.

## Results (step 3)

| Repo           | Before (F / M) | After (F / M) | Tests          | Regressions |
| -------------- | -------------- | ------------- | -------------- | ----------- |
| pi22317        | 21 / 4         | 33 / 0 (full) | 49/60 → 49/60  | 0 |
| assistiva      | 55 / 1         | 68 / 0 (full) | 14/16 → 14/16  | 0 |
| culinary       | 2 / 70         | 3 / 70        | 82/149 → 82/149 | 0 |
| msb            | 8 / 29         | 12 / 29       | 156/156 → 156/156 | 0 |
| encryptednotes | 10 / 6         | 10 / 6        | 85/85 → 85/85  | 0 |
| estore         | 10 / 22        | 10 / 22       | 45/46 → 45/46  | 0 |

The other four keep `IMapper` because of mocked `IMapper` classes (TAAK-279), not this blocker.

## Open decisions for the user

- Keep the null-check removal on a DI lookup (with warning), or keep lookup + registration (pi22317 then stays partial)?

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
