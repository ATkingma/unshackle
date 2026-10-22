# Verification: moq-create

|               |                                                           |
| ------------- | --------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/moq-create-plan.md`](moq-create-plan.md) |
| **Status**    | Deviations found                                          |
| **Generated** | 2026-10-22 by KingmaDoc (agent moq-skeleton)              |

> Verified by an AI agent against the code of TAAK-307 step 3. Review every deviation; the agent reports, it does not decide.

## Requirements

Code: `src/Unshackle.Core/Fixes/Moq/`. Proofs: `src/Unshackle.Tests/Proofs/Moq/Create/`, run by `ProofTests` (entry `Moq`; references in
`src/Unshackle.Tests/Core/Moq/MoqReferences.cs`: before Moq 4.18.4 + Castle.Core, after NSubstitute). Unit tests: `src/Unshackle.Tests/Core/Moq/`.

| REQ   | Verdict | Evidence |
| ----- | ------- | -------- |
| REQ-1 | Met | `local-object.before.cs` / `.after.cs`; `CreatePatternTests.Rewrite_LocalUsedOnlyAsObject_FixesDeclarationAndObject` |
| REQ-2 | Met | `typed-local.*` |
| REQ-3 | Met | `field.*`, `field-assigned.*` (`this._clock.Object`) |
| REQ-4 | Met | `inline-object.*` |
| REQ-5 | Met | `mock-of.*` |
| REQ-6 | Met | `explicit-loose.*`, `strict.manual.txt` |
| REQ-7 | Met | `setup-used.manual.txt` |
| REQ-8 | Met | `string-return.manual.txt`, `setter.manual.txt`; unit tests `Rewrite_InterfaceReturningInterface_ManualForNewMockButFixedForMockOf`, `Rewrite_MockOfInterfaceWithClassReturn_Manual` |
| REQ-9 | Met | unit tests `Rewrite_MockOfWithPredicate_Manual`, `Rewrite_MockGet_Manual`, `Rewrite_MockParameter_ManualAndObjectLeftAlone`, `Rewrite_TwoMocksInOneDeclaration_ManualAndObjectsLeftAlone`, `Find_OwnMockClass_FindsNothing` |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| 1 | Proof | Council rule: a proof pins the behaviour that could change | Proofs compile before (Moq) and after (NSubstitute) but do not run both mocks; the defaults table in `MockDefaults` is from library source | Medium: council question |
| 2 | Scope | `Mock.Get` in draft queue row 1 | Manual `MockGet` until the setup rows exist | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed (counts in the TAAK-307 progress log) |
