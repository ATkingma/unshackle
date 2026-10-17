# TAAK-273 — AutoMapper specific blockers: cycles, unflattening, null sources, private setters

**Status**: step 3/5 — in progress
**Created**: 2026-10-17
**Branch**: `worktree-agent-a3adf49382715b2e3` (worktree `.claude/worktrees/agent-a3adf49382715b2e3`)
**Goal**: the AutoMapper causes found in the blind corpus become Fixed wherever AutoMapper's behaviour can be reproduced exactly, each with probe + proofs; the rest gets a precise "principally manual" reason.

## Context for the next agent

- Causes from `claude/tasks/CORPUSRUN-blind.md`: culinary (cycle, null sources), msb (cycle, unflattening), fiapgames (`UsuarioModel.Nome { get; private set; }`), restapi/baseapi (see TAAK-271 table).
- Proof rules: `claude/tasks/manual-reduction-queue.md`.
- 2026-10-17: picked up by an agent in a worktree. Corpus dir `<corpus>/t273` (tool-before = main `41db520`, scan-before running via `run-before.sh`; base results linked from t272/corpus-blind).
- Probe (step 2): `<corpus>/t273/probe` (AutoMapper 14.0.0), output in `output.txt`:
  destination-only cycle (msb shape) maps, `Box.Order` null, shared source box → **two** destination copies (no reference preservation);
  real cycle `Node.Children` keeps the self reference and maps a shared leaf of a tree to **one** instance; msb `ShelfDto.WarehouseName` reverse → `Warehouse` null,
  forward `WarehouseName` null; control `Order2Dto.CustomerName` reverse creates `Customer{Name}`; `Map<B>(null)` / `Map<A,B>(null)` → null; private setter written.
- Decisions: private setters → principally manual reason (no corpus case meets the constructor rule: fiapgames' constructor also sets `Ativo`, the
  `Atualizar` map writes the private `Id`); restapi/msb broken mappings → reason text says principally manual. Plans: REQs added to the plans of
  nested-maps (11), reverse-map (11), map-call (13, 14), create-map (9, 10), convert-using (9), configuration-test (11).
- SampleShop: Customer→CustomerDto stays on AutoMapper anyway (`AfterMap`, out of scope), so the configuration test stays (maps remain).

## Approach (per cause, measure first)

- **Cycles**: what does AutoMapper 14 do on a self-referencing map without `PreserveReferences`/`MaxDepth` (stack overflow, depth limit, reference detection)? If the data cannot be cyclic (tree) a recursive `ToB()` is equivalent; otherwise reproduce the measured behaviour or Manual.
- **Unflattening** on `ReverseMap`: reproduce AutoMapper's member creation for `CustomerName` → `Customer.Name` (creates intermediate objects?) exactly, else Manual.
- **Null sources**: extend the TAAK-263 null-safe chains where AutoMapper's null substitution is provable.
- **Private setters**: AutoMapper sets them via reflection. Options: matching constructor (TAAK-266 rules), or generated code is impossible → principally manual (state so). Measure.

## Scope from TAAK-271 (measured with AutoMapper 14.0.0; details in `claude/tasks/archief/TAAK-271-analyse-manual-blockers.md`)

| Cause                                                                                               | Finding                                                                                                                                                                                          | Decision                                                                                                                                                                                                                                                                                                      |
| --------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Cycle (culinary 1, msb 1 roots)                                                                     | AutoMapper recurses only through **mapped** members. The cycle is a destination-only type cycle (`Preference.Accounts`, `BoxModel.Order` have no source member); the executed mapping is a tree. | **Solvable**: the cycle check must follow only members that have a source counterpart and are not `Ignore`d. A real source-side cycle keeps the reference in AutoMapper (`ReferenceEquals` true) and stays Manual. Proof: `_Combined` case with a destination-only cycle (Fixed) and one real cycle (Manual). |
| "Unflattening" on `ReverseMap` (msb 1)                                                              | **False positive**: `ShelfDto.WarehouseName` is not unflattened into `Warehouse` (`Warehouse` stays `null`; forward `WarehouseName` stays empty).                                                | **Solvable**: apply AutoMapper's real rule (`Warehouse` + `Name` must exist on the nested type); otherwise no Manual.                                                                                                                                                                                         |
| Null source (culinary 3)                                                                            | `Map<T>(null)` returns `null`.                                                                                                                                                                   | **Solvable**: `x is null ? default! : x.ToDto()` (or `x?.ToDto()` where the target is nullable).                                                                                                                                                                                                              |
| Private setters (fiapgames 1, baseapi 1)                                                            | AutoMapper writes them through reflection and does **not** use a constructor whose parameters have no source (fiapgames: `perfil`).                                                              | fiapgames: **solvable only if** a public constructor consists of assignments `member = parameter` and every parameter has a source member or default equal to what AutoMapper leaves. baseapi (no constructor, no public setter): **principally manual**, say so in the reason.                               |
| `CreateMap<Money,string>` (SampleShop)                                                              | generates `Money.ToString()`, which already exists                                                                                                                                               | **Solvable**: pick another extension name for destination `string` (e.g. `ToMappedString`).                                                                                                                                                                                                                   |
| `AssertConfigurationIsValid` inside `Action act = () => ...; act.Should().NotThrow();` (SampleShop) | not a statement of its own                                                                                                                                                                       | **Solvable**: remove the whole test body in that form once no AutoMapper configuration remains (not while partial migration keeps maps; then keep the test).                                                                                                                                                  |
| restapi `List<DayEntity>` → `List<int>`; msb `Map<OrderModel>(AddOrderDto)` without a map           | AutoMapper throws itself (non-empty list / missing type map)                                                                                                                                     | **Principally manual**: nothing equivalent to generate. Reason text must say the source mapping is broken.                                                                                                                                                                                                    |

## Steps

- [x] 1. KingmaDoc plan per touched pattern; read TAAK-271 for counts
- [x] 2. Probe programs per cause (AutoMapper 14.0.0)
- [→] 3. Implementation + proofs + unit tests per cause
- [ ] 4. Corpus scan/after on culinary, msb, fiapgames, restapi, baseapi: 0 regressions
- [ ] 5. Verify, models, commit

## Changed files

_(filled in during execution)_

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/archief/TAAK-272-automapper-partial-migration.md`
