# Verification: efbulk-update

|               |                                                                   |
| ------------- | ----------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/efbulk-update-plan.md`](efbulk-update-plan.md) |
| **Status**    | Verified                                                          |
| **Generated** | 2026-10-27 by KingmaDoc                                           |

> Verified by an AI agent (TAAK-342, first slice) against the code on branch `taak-342`.

## Requirements

Code: `src/Unshackle.Core/Fixes/EfBulk/Patterns/UpdatePattern.cs`, `EfBulkCall.cs`, registration in `EfBulkFix.cs` and `src/Unshackle.Interface/Cli.cs`.
Proofs: `src/Unshackle.Tests/Proofs/EfBulk/Update/` (`ProofTests`, before against the Z stubs + EF Core 10, after against EF Core 10 only). Unit tests: `src/Unshackle.Tests/Core/EfBulk/EfBulkFixTests.cs`.

| REQ | Verdict | Evidence |
| --- | --- | --- |
| REQ-1 | Met | plus-initializer, extensions-async |
| REQ-2 | Met | method-call; unit tests `FixAsync_StaticServerMember_IsManual`, `FixAsync_ConditionalValue_IsManual` |
| REQ-3 | Met | navigation |
| REQ-4 | Met | factory-variable |
| REQ-5 | Met | unit test `FixAsync_LocalNamedSetters_PicksAFreeName` |

## Deviations

- The Z assemblies are stubs (`src/Unshackle.Tests/Core/EfBulk/EfBulkReferences.cs`), so "before compiles against the old library" is proven against the documented signatures, not the real package. No run-time (SQL) proof yet.

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed |
| Tests | `dotnet test` | all green (see TAAK-342) |

## See also

- [efbulk-update-plan.md](efbulk-update-plan.md)
- [efbulk-update-modellen.md](efbulk-update-modellen.md)
