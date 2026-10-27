# TAAK-342 — efbulk fix: Z.EntityFramework.Plus/Extensions batch calls → EF Core ExecuteDelete/ExecuteUpdate

**Status**: step 3/7 — first slice done (delete + update patterns), waits for council
**Created**: 2026-10-27
**Branch / worktree**: `taak-342` (agent worktree under `.claude/worktrees/`)
**Goal**: an `efbulk` fix that rewrites batch delete/update calls of the Z libraries on EF Core queries to EF Core 7+ native
`ExecuteDelete`/`ExecuteUpdate`; everything else stays (later: Manual rows).

## Context for the next agent

- Scout report and scope: `docs/projectinfo/architectuur/candidate-efbulk.md` (TAAK-430). Verdict "conditional go, narrow".
- **License rule**: Z.EntityFramework.Plus.EFCore depends on the paid Z.EntityFramework.Extensions.EFCore, so **neither** may enter
  `Directory.Packages.props`. Proof/unit types for Z are stubs compiled in-memory with the real assembly names:
  `src/Unshackle.Tests/Core/EfBulk/EfBulkReferences.cs`. EF Core 10 is real (`Microsoft.EntityFrameworkCore.Sqlite`, already a test package).
- **API note (scout correction)**: `DeleteFromQuery`/`UpdateFromQuery` belong to Z.EntityFramework.**Extensions**; Plus itself has
  `Delete()`/`DeleteAsync()`/`Update(factory)`/`UpdateAsync(factory)` in namespace `Z.EntityFramework.Plus`. The stub signatures follow
  the zzzprojects docs from memory and are **not yet checked against the nupkg** (step 4). The Extensions stub puts its methods in
  namespace `Microsoft.EntityFrameworkCore` (also unverified); the fix does not depend on it, it matches by assembly name.
- **EF version guard**: EF Core 7-9 declare `ExecuteDelete` in `RelationalQueryableExtensions`, EF Core 10 in
  `EntityFrameworkQueryableExtensions` (core). `EfBulkCall.HasNative` checks both; none → Manual `NoNativeExecute`.
- Code: `src/Unshackle.Core/Fixes/EfBulk/` (`EfBulkFix`, `EfBulkCall`, `EfBulkReason`, `Patterns/DeletePattern`, `Patterns/UpdatePattern`),
  registered in `src/Unshackle.Interface/Cli.cs`, `ProofTests.cs`, `PreFilterContractTests.cs`.
- Proofs: `src/Unshackle.Tests/Proofs/EfBulk/Delete/`, `.../Update/`. Unit tests: `src/Unshackle.Tests/Core/EfBulk/EfBulkFixTests.cs`.
- Docs: `docs/projectinfo/patterns/efbulk-{delete,update}-{plan,modellen,verify}.md`, rows in `docs/projectinfo/README.md`.
- Package policy in this slice: **no package or project file is touched** (`Package` is null); the usings `Z.EntityFramework.Plus` /
  `Z.EntityFramework.Extensions` go only where nothing binds to them anymore.

## Steps

- [x] 1. Read scout report, pick narrow scope: Plus `Delete/Update` + Extensions `DeleteFromQuery/UpdateFromQuery` (+Async)
- [x] 2. Fix skeleton, assembly-based receiver match, EF7+ guard, stub references, Cli registration
- [x] 3. Patterns `efbulk.delete` and `efbulk.update` (plain initializer → `SetProperty` chain), proofs, unit tests, KingmaDoc, README rows
- [ ] 4. Verify the Z signatures (and Extensions namespace) against the real nupkgs without adding them to our packages (download to a scratch dir, read metadata only); adjust the stubs
- [ ] 5. After council: EFCore.BulkExtensions `BatchDelete/BatchUpdate`, a `efbulk.unsupported` pattern (Bulk*, Future, FromCache, AuditTrail as Manual rows), package removal rule
- [ ] 6. Run-time proof: SQLite `ToQueryString`/SQL log of the native calls vs. the expected SQL (Plus cannot run without the paid Extensions trial)
- [ ] 7. Corpus round (protagonist, Plutus, morenote, NexusStack, Lavcode, OpenttdDiscord), README/roadmap text, archive

## Council questions

1. **Go / priority** (scout Q1): is the license motive (Plus depends on paid Extensions; trial expiry ERROR_005) enough to ship this fix? Slice 1 is built but not yet listed in the README roadmap text.
2. **Scope** (scout Q2): include EFCore.BulkExtensions `BatchDelete/BatchUpdate` (deprecated by its author, cFOSS license) in v1? Slice 1: no.
3. **EF version** (scout Q3): EF Core 7+ as hard precondition. Slice 1: Manual `NoNativeExecute` per call when the compilation has no `ExecuteDelete`/`ExecuteUpdate`; no project-level blocker row yet.
4. **Non-trivial values** (scout Q4): slice 1 takes the conservative option, Manual for method calls, conditionals, interpolated strings, static properties (`DateTime.Now`: EF may evaluate on the server where Z evaluated on the client, unverified), navigations, owned types. Should a tested set (e.g. `Math.Max`) become Fixed?
5. **Package policy** (scout Q5): slice 1 touches no package. Proposal: remove the Z package per project only at zero remaining Z usages (like Polly's rule), else leave it and report.
6. **Stubs vs real types**: the real Z packages cannot be referenced (license rule), so proofs compile "before" against stubs. Acceptable, or should step 4 also add a CI check that the stub signatures match a downloaded nupkg?
7. **Async overloads with a builder and token** (`DeleteAsync(builder, ct)`): Manual `BatchOptions` in slice 1; fine?
8. **README note** on the MIT fork `EFCore.BulkExtensions.MIT` for Bulk* users (scout Q6).

## See also

- `docs/projectinfo/architectuur/candidate-efbulk.md` — scout report
- `claude/tasks/TAAK-430-scout-efbulk.md` — scout task
- `claude/docs/council.md` — decision council
- `docs/projectinfo/patterns/efbulk-delete-plan.md`, `docs/projectinfo/patterns/efbulk-update-plan.md`
