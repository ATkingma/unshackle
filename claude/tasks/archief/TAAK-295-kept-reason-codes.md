# TAAK-295 — Reason codes for every kept finding (after 0.2.0)

**Status**: ✅ done
**Created**: 2026-10-19
**Goal**: council round 4 Q2: kept maps, kept calls, kept `AddAutoMapper` and configuration tests use a typed reason code (like `KeptReason` on the `GetService<IMapper>()` lookup) instead of the `Stays on AutoMapper:` string prefix.

## Context for the next agent

- Do this after release 0.2.0; the output must not change (tests pin the rendered text).
- `AutoMapperFix.AllowsPartial` accepts both the code and the old prefix today; after this task, only the code.

- Also (council round 5 Q2): `IsContainer` in `MapperRegistration.cs` matches on the fully qualified name, like the builder types.

## Steps

- [x] 1. Inventory all prefix uses; plan
- [x] 2. Implementation + tests (rendered text unchanged)
- [x] 3. Corpus `after` on the six AutoMapper repos: identical reports
- [x] 4. Verify, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## Inventory (step 1)

Prefix producers (now `PatternResult.KeptManual(code, reason)`, reason without the prefix):

| Site | Code |
| --- | --- |
| `AddAutoMapperPattern`, result used | `RegistrationResultUsed` |
| `AddAutoMapperPattern`, IMapper still used | `MapperStillUsed` |
| `ConfigurationTestPattern`, maps stay | `ConfigurationCheckWithKeptMaps` |
| `ConfigurationTestPattern`, unmigrated map | `ConfigurationCheckWithUnmigratedMap` |
| `CreateMapPattern`, all maps kept | `ProfileKept` |
| `KeptMapPattern` | `MapKept` |
| `MapOwners.KeptCall`, mock set-up | `MockSetUpKept` |
| `MapOwners.KeptCall`, call | `CallKept` |
| `UnusedMapperPattern` lookup (already typed) | rules 1-3 / `NotInSolution` |

Prefix consumers: `AutoMapperFix.AllowsPartial`, `CreateMapPattern.PlanAsync` and the `MapOwners` plan ("call not migrated") now check
`Kept`. `MapBlockers.ReasonAsync` quotes a profile's Manual reason inside its own text: it uses `MapOwners.Rendered` (prefix + reason
for a kept result), so that text is unchanged. `MapOwners.KeptPrefix` stays only for that quote.

Tests that read the raw `Reason` of a kept finding (`ProofTests` manual.txt, `CombinedProofTests` partial-*, `MapOwnersTests.Reasons`,
`ConfigurationTestPatternTests`) now read `ReportRow.ReasonOf` (the rendered text); the expected strings are unchanged.
`UnusedMapperPatternTests.FixAsync_AddAutoMapperInALambdaPassedToARegistrationCall_RemovesTheLookup` asserted "no kept finding at
all"; other kept findings now carry a code too, so it asserts no registration-rule code (its intent: the lookup is not kept).

`IsContainer` (`MapperRegistration.cs`) now matches `Microsoft.Extensions.DependencyInjection.ServiceCollection` / `IServiceCollection`
on the qualified name (council round 5 Q2).

## Proposals for the council

- Q1: the prefix text lives twice: `ReportRow.KeptPrefix` (DataAccess) and `MapOwners.KeptPrefix` (Core, for the quote in
  `MapBlockers`). Options: (a) keep both (chosen, conservative; DataAccess was out of scope for this task); (b) one constant in Core
  that `ReportRow` uses. Proposal: (b) in a follow-up that may touch DataAccess.
- Q2: MediatR's "Stays on MediatR next to Mediator:" reason is still a plain string (MediatR does not use `AllowsPartial` for it).
  Leave as is (chosen) or give it a code for symmetry?
- Q3: `MapBlockers` quotes a kept reason (with prefix) inside the Manual reason of another finding. Keep the quote (chosen, output
  unchanged) or drop the inner prefix?
- Q4 (found in step 3): the "IMapper is still used (file:line)" example in the kept `add-automapper` row differs between runs.
  Proposal: pick the first user in a stable order (path, then line) so corpus reports compare byte for byte. Not done here (would
  change rendered text, out of this task's scope).

## Progress log

- 2026-10-21: steps 1-2. Inventory above; 8 new `KeptReason` codes, `PatternResult.KeptManual`, `IsContainer` on the qualified name,
  new `Core/AutoMapperFixTests` (4 cases). Build green, `dotnet test` 1652/1652, SampleShop 38/38. Commit "TAAK-295: typed kept reason codes".
- 2026-10-21: step 3. Tool `<corpus>/corpus-blind/_results/tool-295`, harness `corpus-am.sh after` (JOBS=1, own WORKROOT), results
  copied to `<corpus>/corpus-blind/_results/after-t295`. Fixed/Manual identical to TAAK-294 on all six (33/0, 68/0, 3/70, 12/29, 10/6,
  10/22), 0 regressions. Reports byte-identical for pi22317 and assistiva; in the four partial repos exactly one row differs: the
  example file in "IMapper is still used (<file>:<line>)" of the kept `add-automapper` row. That example already changed between
  earlier rounds (estore: t289 OrderController, t292 MemberService, t293/t294 ProductService, now MemberServiceTest), so it is
  existing nondeterminism in `AddAutoMapperPattern` (first IMapper user over `Compilation.SyntaxTrees`), not this change. See council Q4.
- 2026-10-21: step 4. Docs (`klassen.md`, partial-migration models, kept-map plan) updated; `verify-head.sh` → `VERIFY OK: a3485ef`
  (note: `jq` is missing on this machine, so the pattern-docs hook inside verify-head ran without effect). Task archived.

## See also

- `claude/docs/council.md`, `claude/tasks/archief/TAAK-293-council-round-3.md`, `claude/tasks/CORPUSRUN-blind.md`
