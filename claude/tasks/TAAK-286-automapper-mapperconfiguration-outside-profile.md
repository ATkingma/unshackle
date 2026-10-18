# TAAK-286 — AutoMapper: maps in `new MapperConfiguration(cfg => …)` outside a Profile

**Status**: step 1/4 — in progress
**Branch**: `worktree-agent-a6808d7e70cc048cd` (worktree `.claude/worktrees/agent-a6808d7e70cc048cd`)
**Created**: 2026-10-18
**Goal**: remove this blocker with proof, or state precisely why it is principally manual.

## Context for the next agent

- hospitalappt (0 Fixed / 32 Manual) keeps its maps in a `MapperConfig` / `new MapperConfiguration(cfg => cfg.CreateMap<…>())`; today only Profile constructors are rewritten (gnomeshade had the same shape in an earlier round).
- Source: `claude/tasks/CORPUSRUN-blind.md` (round 5). Proof rules: `claude/tasks/manual-reduction-queue.md`.
- Record progress per step and commit it as WIP in your branch (`claude/docs/agent-werkwijze.md`).

- Design (step 1): the class that holds `new MapperConfiguration(cfg => ...)` with `cfg.CreateMap` statements is a "map class" like a Profile (`CreateMapPattern.IsProfileWithMaps`); its lambda body takes the place of the profile constructor; the extension class is `<Class>Extensions` next to the outermost type. Every solution-wide consumer (MapCallPattern, NestedMaps, MapOwners, ProjectToPattern, MapBlockers, KeptMapPattern, ConfigurationTestPattern) goes through `CreateMapPattern.MapStatements(class, model)`. Settings (`cfg.AllowNullCollections = ...`, `cfg.AddProfile`) in the lambda stay Manual.
- Measured on hospitalappt (map class rewritten as a Profile by hand, tool 58ded1e): only 6 Fixed / 35 Manual even then; the rest is reverse maps (`ReverseMap` with unflattening `src.User.Name`, `User.PasswordHash` next to `RegisterDto.Password`), not this task's shape. Probes under `<corpus>/probe286/`.

## Approach

Treat `cfg.CreateMap` inside a `MapperConfiguration` lambda like a Profile map when the configuration is built once and only used through its mapper; probe AutoMapper 14.0.0 for configuration-level settings (`AllowNullCollections`, naming conventions) that change map behaviour; anything not provably equal stays Manual.

## Steps

- [→] 1. Measure on the corpus repo(s) and probe the old library; KingmaDoc plan for touched patterns/features
- [ ] 2. Implementation + unit tests + proofs (`_Combined` where patterns cooperate)
- [ ] 3. Corpus `after` on hospitalappt (+ the 14 AutoMapper corpus repos, no regressions): build green, 0 regressions; before/after numbers here and in CORPUSRUN-blind.md
- [ ] 4. Verify, models, docs; `claude/tools/verify-head.sh` → `VERIFY OK`

## See also

- `claude/tasks/manual-reduction-queue.md`, `claude/tasks/CORPUSRUN-blind.md`
