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
| REQ-8 | Met | `setter.manual.txt`; unit tests `Rewrite_InlineInterfaceReturningInterface_ManualForBoth`, `Rewrite_MockOfInterfaceWithClassReturn_Manual` |
| REQ-10 | Met | `string-return.after.cs`; run-time `create-pinned-local`, `create-pinned-field`; unit tests `Rewrite_StringMember_PinsMoqDefault`, `Rewrite_ElevenStringMembers_TooManyDefaultPins`, `Rewrite_PinnedFieldInitializer_ManualAndObjectLeftAlone` |
| REQ-11 | Met | run-time `create-unused`; unit test `Rewrite_UnusedMock_FixedWithInfo` |
| REQ-12 | Met | `_Runtime/create-defaults`, `create-mock-of`, `create-pinned-*`, `create-unused`, `create-must-fail` (NullReferenceException under both) |
| REQ-9 | Met | unit tests `Rewrite_MockOfWithPredicate_Manual`, `Rewrite_MockGet_Manual`, `Rewrite_MockParameter_ManualAndObjectLeftAlone`, `Rewrite_TwoMocksInOneDeclaration_ManualAndObjectsLeftAlone`, `Find_OwnMockClass_FindsNothing` |

## Deviations

| # | Area  | Plan says | Code does | Impact |
| - | ----- | --------- | --------- | ------ |
| 1 | Scope | Q1: members the test sets up get no pin | Only a member set up for every argument (`Arg.Any` only, or no parameters) is exempt; one set up for some arguments keeps its pin, because Moq still answers null for the other arguments | Low: stricter reading |
| 3 | Scope | Q3 `TargetFrameworkUnsupported` | Not in the pattern: it does not see the TFM | Medium: needs the Fixer/project step |
| 2 | Scope | `Mock.Get` in draft queue row 1 | Manual `MockGet` until the setup rows exist | Low |

## Validation results

| Check | Command        | Result |
| ----- | -------------- | ------ |
| Build | `dotnet build` | passed: 0 warnings, 0 errors |
| Tests | `dotnet test`  | passed (counts in the TAAK-307 progress log) |
