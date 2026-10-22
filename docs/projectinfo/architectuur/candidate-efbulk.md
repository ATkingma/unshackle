# Candidate: Z.EntityFramework.Plus / Z.EntityFramework.Extensions / EFCore.BulkExtensions → EF Core ExecuteUpdate/ExecuteDelete

Scout report for TAAK-430 (2026-10-22). Light survey: web sources, shallow clones, regex counts, no builds. Numbers are indicative.

## Verdict

**Conditional go, narrow scope, after the four planned fixes (Moq, Polly, EPPlus, MassTransit). Effort M. Demand low-to-medium.**
The mapping `X.DeleteFromQuery()/BatchDelete()` → `ExecuteDelete()` and `X.UpdateFromQuery(x => new T { P = e })/BatchUpdate()` →
`ExecuteUpdate(s => s.SetProperty(...))` is mechanical and provable. But the license motive is weaker than for Moq or EPPlus
(see below), and most `Bulk*` calls (the actually paid part) have no native equivalent and stay `Manual`, so the package often stays.

## License facts (primary sources, checked 2026-10-22)

| Library | Status | Source |
| ------- | ------ | ------ |
| Z.EntityFramework.Extensions(.EFCore) (`BulkInsert/Update/Delete/Merge/SaveChanges`) | Paid, per developer seat; SQL Server list price from 999 USD (1 seat), perpetual. Latest 10.105.9 (2026-10). Trial is time-limited and stops at the end of the month. | https://entityframework-extensions.net/pricing , https://www.nuget.org/packages/Z.EntityFramework.Extensions.EFCore ("operates under a paid licensing model") |
| Z.EntityFramework.Plus.EFCore (`BatchDelete/BatchUpdate`, `DeleteFromQuery/UpdateFromQuery`, `Future`, `FromCache`, `AuditTrail`) | Repo is **MIT**. NuGet 10.105.9 (2026-10-13) nevertheless **depends on Z.EntityFramework.Extensions.EFCore (= same version)** and Z.Expressions.Eval. The Extensions page states the EF Plus features are free. Whether the batch calls trip the Extensions trial check at runtime was **not verified** (needs a runtime test or nupkg inspection). | https://github.com/zzzprojects/EntityFramework-Plus/blob/master/LICENSE , https://www.nuget.org/packages/Z.EntityFramework.Plus.EFCore |
| EFCore.BulkExtensions (borisdj) | Dual license "cFOSS": free below 1 M USD annual gross revenue, otherwise commercial. LICENSE says "Version 1.0, January 2024"; the README ties it to v8 onward. The README marks Batch Update/Delete "Deprecated from EF8 (v7+ has native Execute-Up/Del)". Latest line targets EF Core 10. The exact v8.0.0 release date was not found in release notes (only 8.1.2 listed). A MIT fork exists: `videokojot/EFCore.BulkExtensions.MIT` (seen in GitHub code search, not evaluated). | https://github.com/borisdj/EFCore.BulkExtensions , https://raw.githubusercontent.com/borisdj/EFCore.BulkExtensions/master/LICENSE.txt |
| Evidence of user pain | ASP.NET Zero users hit trial-expired `ERROR_005` on 2023-11-01 after upgrading (Extensions trial). | https://support.aspnetzero.com/QA/Questions/11795 |

Consequence: the honest README motive is "remove a paid/dual-licensed dependency (and its trial nag) where EF Core now does it
natively". For Plus alone it is mostly redundancy + the transitive paid package, not a license violation.

## Native target (EF Core primary sources)

Docs: https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete ; EF10 notes
https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew
- `ExecuteDelete(Async)` and `ExecuteUpdate(Async)` exist from EF Core 7. Relational providers only.
- EF10 (LTS, Nov 2025, supported to 2028-11-10): `ExecuteUpdateAsync` takes a plain (non-expression) lambda, so conditional `SetProperty`
  is easy; complex types / JSON columns supported. Owned types are not.
- Limits: no insert, one table per statement, no batching of several calls, no navigations inside `SetProperty` (use `Select` first).

## Mapping sketch

| Source shape | Target | Class |
| ------------ | ------ | ----- |
| `q.DeleteFromQuery()` / `Async(ct)` (Plus) | `q.ExecuteDelete()` / `ExecuteDeleteAsync(ct)` | Fixed (EF7+; net6 EF6 projects excluded) |
| `q.BatchDelete()` / `BatchDeleteAsync()` (Plus, BulkExtensions) | same | Fixed |
| `q.UpdateFromQuery(x => new T { A = e1, B = e2 })` (+Async) (Plus) | `q.ExecuteUpdate(s => s.SetProperty(x => x.A, x => e1).SetProperty(...))` | Fixed when the initializer has only plain property assignments; unassigned members are untouched in both. Constant values use the short form. |
| `q.BatchUpdate(x => new T {...})` (Plus / BulkExtensions) | same as above | Fixed |
| `q.BatchUpdate(new T{...}, new List<string>{"A"})` (BulkExtensions entity+column list) | SetProperty from the listed columns | Manual (v1) |
| `q.BatchDelete/Update` on a helper wrapper (Damselfly `BaseModel.BatchDelete<T>`) | the wrapper body is rewritten, call sites untouched | Fixed in the wrapper |
| Initializer with ternary / `Math.Max` / method calls | SetProperty, but SQL translation depends on provider/EF version | Manual unless in a tested set (protagonist uses `Math.Max(0, ...)`) |
| `BulkInsert`, `BulkInsertOptimized`, `BulkMerge`, `BulkInsertOrUpdate`, `BulkSynchronize`, `BulkSaveChanges`, all options/`BulkConfig`, `BulkUpdate(list)` | none native (insert is `AddRange`+`SaveChanges`, which is not equivalent in speed) | Manual |
| `Future`, `FromCache`, `AuditTrail`, `IncludeFilter`, `DeferredX` | none | Manual |
| `BulkDelete(list)` of entities | `Where(ids.Contains).ExecuteDelete()` | Manual (key shape, composite keys) |

Fixed must also verify the receiver is an `IQueryable<T>` from an EF Core `DbSet` (semantic model), because the same names exist in
non-EF libraries (see false positives below).

## Behaviour differences to document (README / `Manual` reasons)

- **Change tracker**: neither tool updates tracked entities; both bypass `SaveChanges`. Same as native. Docs warn about mixing with tracked edits.
- **Interceptors / SaveChanges overrides / audit**: native `ExecuteX` does not call `SaveChanges`, so `SaveChangesInterceptor`, timestamps and audit hooks do not run. Plus batch ops also bypass them (to be confirmed), so no regression expected; `Z.EF.Plus.AuditTrail` users are Manual anyway.
- **Transactions**: native does not open an implicit transaction; uses the ambient one. Plus equivalent behaviour needs a runtime test (open question).
- **Concurrency / row count**: both return affected rows (`int`); no automatic concurrency tokens in either.
- **Query filters**: apply to the source query in both (to verify in the proof run; soft-delete filters are the risk).
- **Provider limits**: relational only; the provider must support `UPDATE ... FROM` translation (Pomelo/Npgsql OK, Oracle/Cosmos not).
- **Dual TFM**: `SetProperty` expression form works on EF7-9; EF10 also accepts the statement lambda. Emit the expression form for compatibility.
- **Dynamic `Expression<Func<T,T>>` variables** passed to `UpdateFromQuery`/`BatchUpdate`: not an initializer, so Manual.

## Survey (13 EF repos plus 6 false positives)

Clones: `D:/playground/corpus-candidates/efbulk/<dir>/`, shallow, short sha = HEAD at clone time (2026-10-22). Found by GitHub code search for
`BatchDeleteAsync`, `DeleteFromQuery(Async)`, `UpdateFromQuery(Async)`, `BulkInsertAsync`, `BulkInsertOrUpdateAsync`, `BatchUpdateAsync` and csproj
package references (biased towards C# repos GitHub indexes well; many are Chinese-community admin templates). Counts are call sites in `*.cs`
(comments stripped, regex on `.Name(`), per API shape. "Plus" = Z.EntityFramework.Plus, "Ext" = Z.EntityFramework.Extensions, "BE" = EFCore.BulkExtensions.

| Dir (repo) | sha, last commit | Package (version) | DeleteFromQuery | UpdateFromQuery | BatchDelete | BatchUpdate | BulkInsert | BulkUpdate | BulkDelete | BulkSaveChanges | Future/Cache | native Execute* |
| ---------- | ---------------- | ----------------- | --: | --: | --: | --: | --: | --: | --: | --: | --: | --: |
| protagonist (`dlcs/protagonist`) | 7d9dda4, 2026-10-20 | Plus 10.105.8 | 1 | 6 | - | - | - | - | - | - | - | 1 |
| NexusStack (`yongpengW/NexusStack`) | b61d616, 2026-07-20 | Ext 10.105.6 | 3 | 4 | 8 | - | - | - | - | - | - | - |
| OpenttdDiscord (`shoter/OpenttdDiscord`) | 31e7354, 2026-03-22 | Ext 10.105.2.1 | 7 (`DeleteFromQueryExt`) | - | - | - | - | - | - | - | - | - |
| Plutus (`StefanFanaru/Plutus`) | 34b90b6, 2025-03-14 | Plus 9.103.7.1 | 1 | 9 | - | - | - | - | - | - | - | - |
| morenote (`morenote/Server`) | 954da1f, 2025-09-06 | Plus 5.1.8 / 8.102.2.4 / 8.103.6 | 3 | 8 | - | - | - | - | - | - | 12 | - |
| Damselfly (`Webreaper/Damselfly`) | e110926, 2026-01-14 | BE 10.0.0 | - | - | 6 | 7 | 9 | 4 | 2 | - | - | 11 |
| yofi (`jcoliz/yofi`) | 0522c8a, 2025-12-23 | BE 8.0.3 | - | - | 4 | 2 | 12 | 1 | 8 | - | - | - |
| Fineform (`Takt365/Lean.Fineform`) | 9ab07a1, 2026-01-08 | Ext 4.0.89 (EF6 line) | 33 | 1 | - | - | 41 | 58 | - | 85 | - | - |
| sharding-core (`dotnetcore/sharding-core`) | cbdd84a, 2026-09-10 | BE 6.5.6 + Plus 6.14.4 | - | - | - | 1 | 6 | - | - | - | - | 11 |
| Lavcode (`hal-wang/Lavcode`) | a540fa7, 2024-07-27 | Ext 6.14.4 | 9 | 4 | - | - | - | - | - | - | - | - |
| LionFrame (`levy-w-wang/LionFrame`) | 12c0634, 2022-05-22 | Plus 3.1.5 (EF Core 3) | 9 | 7 | - | - | - | - | - | - | - | - |
| PcrBattleChannel (`acaly/PcrBattleChannel`) | 3737cc6, 2021-04-10 | Plus 5.1.13 | 6 | - | - | - | - | - | - | - | - | - |
| Packt8 (`PacktPublishing/ASP.NET-Core-8-and-Angular`) | 2f31647, 2025-10-28 | BE 8.0.0 | - | - | - | - | 0 | - | - | - | - | - |

False positives, rejected (same method names, not EF): `zhontai/Admin.Core` (FreeSql), `IeuanWalker/Email.io` (Azure Table helper `UpdateFromQuery`),
`Riniba/TelegramMonitor`, `OfficeDev/microsoft-teams-apps-company-communicator`, `leooneone/aibpm.plus`, `holomodular/ServiceBricks` (no hits). Lesson:
the fix must resolve the receiver type semantically, never match by name. (Admin.Core etc. are also still cloned for reference.)

Caveats: packages are often declared in props/`Directory.Packages.props` not scanned, so "-" in the package column is not proof of absence;
Fineform 4.0.89 looks like the EF6 package (no `ExecuteUpdate` exists there; out of scope); LionFrame (EF Core 3) and PcrBattleChannel (2021) are too old
for EF7+; Packt8 declares BE 8.0.0 but has no call in `*.cs` I could match. Not verified by build.

### Reading the numbers

- Mappable shapes (DeleteFromQuery, UpdateFromQuery, BatchDelete, BatchUpdate) appear in 12 of 13 repos; 139 call sites in total, 83 in repos on
  EF Core 5+ with a current package (excluding Fineform, LionFrame, PcrBattleChannel). Of the Fixed shape, `UpdateFromQuery` with an initializer dominates (protagonist, Plutus, morenote, NexusStack).
- Manual shapes (BulkInsert/Update/Delete/SaveChanges) concentrate in 4 repos (Fineform, yofi, Damselfly, sharding-core). In those the package cannot be removed.
- Package removal would be complete only in repos that use batch calls alone (protagonist, Plutus, morenote, NexusStack, Lavcode, OpenttdDiscord); elsewhere the fix reduces usage without dropping the dependency.
- Wrapper pattern (Damselfly) already mixes both: a wrapper `BatchDelete` over BulkExtensions next to 11 native `ExecuteUpdate` calls, so the maintainers have migrated by hand in places.

## Demand

Low-to-medium. EF Core is ubiquitous, but the batch calls are a small slice of data-access code, and the paid-license pressure sits on `Bulk*`, which we
cannot migrate. Plus users are mostly hobby/enterprise-template repos on EF 5-8; BulkExtensions has a free tier below 1 M USD revenue, so most OSS
repos are unaffected. Real pressure: companies above the threshold, and anyone bitten by the Extensions trial expiry (ERROR_005).

## Effort: M

- Two small `IPattern`s (Delete, Update) plus a shared receiver check (EF Core `DbSet`/`IQueryable<T>` semantic filter), property-initializer to `SetProperty` chain, async/`CancellationToken` argument handling, using and package swap logic that keeps the package when `Bulk*` remains.
- Proofs need real EF Core assemblies (Microsoft.EntityFrameworkCore.Relational 8/10) for `after`, and the old Z packages for `before` (license check for pinning the last free/usable version of Plus: MIT, fine; Extensions is paid, so `before` proofs must only compile against Plus types, or use stub sources).
- Behavioural proof idea: SQLite in-memory runs both old and new in a sample (Plus needs a trial-capable Extensions at runtime, so possibly not runnable); otherwise compile proof plus EF Core's SQL-string check via `ToQueryString`-style logging.
- Risks (below) push it from S to M.

## Top risks

1. Plus runtime license/trial behaviour is unverified, so the stated motive may be wrong for Plus-only repos.
2. Expression translation differences (`Math.Max`, ternaries, string ops) between Plus's own expression compiler and EF's SQL translator; mitigate by Manual for non-trivial initializers.
3. Fixed-but-partial migration leaves the project with the paid package still referenced, for Bulk*-heavy repos (value is mostly cosmetic).
4. Name collisions (FreeSql, Azure Table, LanguageExt `DeleteFromQueryExt`) require strict semantic matching.
5. EF6/old EF Core TFMs (Fineform, LionFrame) must be skipped by a TFM/package-version guard.
6. Verification: our corpus harness proves compile only; behaviour (filters, transactions, interceptors) needs sample tests.

## Draft task (taak-start format)

```
# TAAK-431 — efbulk fix: Z.EntityFramework.Plus / EFCore.BulkExtensions batch calls -> ExecuteUpdate/ExecuteDelete
Status: step 0/6 — not started (blocked on council go)
Goal: An `efbulk` fix rewriting DeleteFromQuery/BatchDelete/UpdateFromQuery/BatchUpdate on EF Core DbSet queries to ExecuteDelete/ExecuteUpdate, everything else Manual.
Steps:
- [ ] 1. Verify: does Z.EntityFramework.Plus batch run without a license after the Extensions trial? nupkg inspection + sample run
- [ ] 2. Council: Fixed/Manual line, package removal rule, EF version guard (EF7+ only), wrapper methods
- [ ] 3. EfBulkFix skeleton + semantic receiver check + using/package swap; proofs under Proofs/EfBulk/
- [ ] 4. Patterns: Delete (3 spellings), Update (initializer -> SetProperty chain), BulkExtensions BatchDelete/Update
- [ ] 5. Corpus round: protagonist, Plutus, morenote, NexusStack, Damselfly, yofi, sharding-core + 8 more; builds after fix
- [ ] 6. Blind round, README text, roadmap row
```

## Council questions

1. Go at all, given the Plus license evidence is weaker than for the four planned libraries? Order: after MassTransit, or earlier because of the "free" scope (S-sized subset)?
2. Scope: Plus batch only, or also BulkExtensions `Batch*` (deprecated by its own author) in v1? And is `Bulk*` entirely Manual?
3. Accept EF7+ as a hard precondition (skip projects on older EF Core/EF6 with a report note)?
4. Initializers with non-trivial expressions (ternary, `Math.*`, method calls): Fixed with a warning, or Manual?
5. Package policy: remove the Z package only if zero usages remain; otherwise leave and report?
6. Should the README mention the MIT fork `EFCore.BulkExtensions.MIT` as an alternative for Bulk* users?

## See also

- `docs/projectinfo/project.md` — roadmap
- `docs/projectinfo/architectuur/corpus-automapper.md` — model of a survey
- `claude/tasks/TAAK-430-scout-efbulk.md` — this scout's task
- `claude/tasks/CLAIMS.md` — file claims and reserved numbers
