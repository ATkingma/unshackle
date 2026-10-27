# TAAK-342 — efbulk fix: Z.EntityFramework.Plus/Extensions batch calls → EF Core ExecuteDelete/ExecuteUpdate

**Status**: step 3/9 — first slice done; council round 17 decided (B1-B8), next is step 4
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
  `Delete()`/`DeleteAsync()`/`Update(factory)`/`UpdateAsync(factory)` in namespace `Z.EntityFramework.Plus`. The stub signatures were
  checked against the real nupkgs (step 4, see Findings).
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
- [x] 4. Verify the Z signatures (and Extensions namespace) against the real nupkgs without adding them to our packages (download to a scratch dir, read metadata only); adjust the stubs
  - Findings (Z.EntityFramework.Plus.EFCore and Z.EntityFramework.Extensions.EFCore 10.105.9, lib/net10.0, inspected as Roslyn
    metadata symbols; nothing executed). Correct already: Plus `BatchDeleteExtensions`/`BatchUpdateExtensions` in `Z.EntityFramework.Plus`,
    all `where T : class`, `int`/`Task<int>` returns, `CancellationToken cancellationToken = default` on the factory/delete async overloads.
    Wrong and fixed: (a) the Extensions methods live in **global-namespace** classes `BatchDeleteExtensions`/`BatchUpdateExtensions`,
    not `Microsoft.EntityFrameworkCore.BatchQueryExtensions` (still no using needed; the fix matches by assembly, unaffected);
    (b) `BatchDelete`/`BatchUpdate` are declared in the **Extensions** assembly, namespace `Z.EntityFramework.Extensions` (Plus' builder
    overloads use them), and `BatchUpdate.BatchSize` is `int?`; (c) `Future<T>()` returns `QueryFutureEnumerable<T>`
    (: `BaseQueryFuture`, `IEnumerable<T>`), not `IEnumerable<T>`; (d) missing overloads: Extensions `DeleteFromQuery(Async)` and
    `UpdateFromQuery(Async)` with a batch builder, and in both packages `Expression<Func<T, object>>` (anonymous object),
    `IDictionary<string, object>` and `ExpandoObject` overloads. The stubs now carry the builder and `Func<T, object>` shapes; a new unit
    test pins that an anonymous-object update is Manual `UpdateFactoryShape`. Patterns and proofs needed no change (the patterns match by
    assembly + IQueryable receiver and by argument shape, which already sends builder/dictionary/anonymous forms to Manual).
- [x] 5. B3: one project-level Manual row `EfCoreBelow7` "EF Core < 7: upgrade to EF Core 7+ ... first; N batch call(s) blocked" next to the
  per-call `NoNativeExecute` (new default member `IFix.ProjectSummary(findings)`, called by the Fixer per project file after the document
  pass; blocks nothing). B7: the `BatchOptions` reason names the options the builder assigns. B4: every Manual reason quotes the exact
  expression (whitespace collapsed). B6 docs: version 10.105.9 and the verified signatures in both `efbulk-*-plan.md`. Unit tests in
  `EfBulkFixTests`
- [x] 6. Run-time proof: `src/Unshackle.Tests/Core/EfBulk/EfBulkSqlTests.cs` (EF Core 10 + SQLite in memory, command interceptor):
  ExecuteDelete and ExecuteUpdate (captured local, entity arithmetic, enum, negation) each send exactly one statement, no SELECT first,
  captured values as parameters, entity members as columns; rows checked. Decimal: SQLite evaluates `ef_divide`/`ef_multiply` with .NET
  decimal semantics (10/3 exact to 28 digits, overflow throws), which proves nothing about SQL Server column precision
- [x] 7. B4 allow-list: captured locals/parameters, entity member access, numeric arithmetic → Fixed (proofs captured-local,
  entity-member, numeric-arithmetic, plus the existing plus-initializer); string concatenation and any binary operator on string/decimal
  operands → Manual (proofs string-concatenation, decimal-arithmetic); `DateTime.Now` stays Manual (proof static-now); `Math.*` stays Manual
  (method-call). No `DateTime.Now`/`Math.*` SQL proof was added, so neither is widened
- [ ] 8. `efbulk.unsupported` (Bulk*, Future, FromCache, AuditTrail as Manual rows; B8: the reason/README names
  `EFCore.BulkExtensions.MIT` as an alternative, not migrated, after checking its license/maintenance) and B5 package removal at zero Z
  usages per project (dependents counted, Plus and Extensions), else a row with the remaining usages file:line. EFCore.BulkExtensions
  stays out (B2)
- [ ] 9. Corpus round (protagonist, Plutus, morenote, NexusStack, Lavcode, OpenttdDiscord); B1: README roadmap line only now (steps 4
  and 6 done); archive

## Council questions

1. **Go / priority** (scout Q1): is the license motive (Plus depends on paid Extensions; trial expiry ERROR_005) enough to ship this fix? Slice 1 is built but not yet listed in the README roadmap text.
   **Decision (round 17 B1)**: go, narrow; README roadmap line only after steps 4 and 6.
2. **Scope** (scout Q2): include EFCore.BulkExtensions `BatchDelete/BatchUpdate` (deprecated by its author, cFOSS license) in v1? Slice 1: no.
   **Decision (round 17 B2)**: no; a later slice decided by corpus counts.
3. **EF version** (scout Q3): EF Core 7+ as hard precondition. Slice 1: Manual `NoNativeExecute` per call when the compilation has no `ExecuteDelete`/`ExecuteUpdate`; no project-level blocker row yet.
   **Decision (round 17 B3)**: per-call Manual stays, plus one project-level Warning row.
4. **Non-trivial values** (scout Q4): slice 1 takes the conservative option, Manual for method calls, conditionals, interpolated strings, static properties (`DateTime.Now`: EF may evaluate on the server where Z evaluated on the client, unverified), navigations, owned types. Should a tested set (e.g. `Math.Max`) become Fixed?
   **Decision (round 17 B4)**: Manual by default; allow-list (literals, captured locals/parameters, entity member access, numeric arithmetic, no string concatenation), each with a proof plus a decimal overflow/rounding proof; `DateTime.Now`/`Math.*` only after a step-6 SQL proof.
5. **Package policy** (scout Q5): slice 1 touches no package. Proposal: remove the Z package per project only at zero remaining Z usages (like Polly's rule), else leave it and report.
   **Decision (round 17 B5)**: yes, at zero Z usages per project (Bulk*/Future/FromCache/AuditTrail and dependents counted, Plus and Extensions both); else report remaining usages with file:line.
6. **Stubs vs real types**: the real Z packages cannot be referenced (license rule), so proofs compile "before" against stubs. Acceptable, or should step 4 also add a CI check that the stub signatures match a downloaded nupkg?
   **Decision (round 17 B6)**: stubs accepted; one-time step-4 check, signatures + nupkg version in the docs, version named in `EfBulkReferences.cs`; no CI download.
7. **Async overloads with a builder and token** (`DeleteAsync(builder, ct)`): Manual `BatchOptions` in slice 1; fine?
   **Decision (round 17 B7)**: yes, Manual `BatchOptions` naming the options; token-only overload later if common.
8. **README note** on the MIT fork `EFCore.BulkExtensions.MIT` for Bulk* users (scout Q6).
   **Decision (round 17 B8)**: yes, one neutral line after checking the fork's license and maintenance.

9. **Decimal arithmetic** (step 7): kept Manual. The SQLite run-time proof cannot show SQL Server/PostgreSQL rounding to the column
   precision, and Z cannot run here. Widen only with a SQL Server (or Testcontainers) proof, or accept "same SQL as Z, same provider" as enough?
10. **String comparison/concatenation operands** (step 7): any binary operator with a string operand is Manual, so `Open = o.Status == "x"`
   is Manual too. Allow string equality comparisons?
11. **Captured members beyond the B4 list** (step 7): the pattern still allows captured fields, instance properties of captured values
   (`request.Status`), enum members and constants (all client-evaluated by both Z and EF, as slice 1 did). Keep, or narrow to the literal B4 list?
12. **Project row severity** (step 5): "Warning row" is implemented as a non-blocking project-level Manual row (code `EfCoreBelow7`); the
   report has no separate Warning kind for project rows. Enough?

## See also

- `docs/projectinfo/architectuur/candidate-efbulk.md` — scout report
- `claude/tasks/TAAK-430-scout-efbulk.md` — scout task
- `claude/docs/council.md` — decision council
- `claude/tasks/COUNCIL-round-17.md` — decisions B1-B8
- `docs/projectinfo/patterns/efbulk-delete-plan.md`, `docs/projectinfo/patterns/efbulk-update-plan.md`
