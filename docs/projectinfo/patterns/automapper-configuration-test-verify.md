# Verification: automapper-configuration-test

|               |                                                                                                        |
| ------------- | ------------------------------------------------------------------------------------------------------ |
| **Plan**      | [`docs/projectinfo/patterns/automapper-configuration-test-plan.md`](automapper-configuration-test-plan.md) |
| **Status**    | Verified                                                                                               |
| **Generated** | 2026-10-14 by KingmaDoc skill (by hand, `kingmadoc` not available)                                     |

> Verified by an AI agent against the code of the TAAK-250 commit.

## Requirements

Code: `src/Unshackle.Core/Fixes/AutoMapper/Patterns/ConfigurationTestPattern.cs`, `ConfigurationSyntax.cs`, `AutoMapperFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/ConfigurationTest/`, `_Combined/configuration-test-fixture`. Unit tests: `src/Unshackle.Tests/Core/ConfigurationTestPatternTests.cs`.

| REQ    | Verdict | Evidence                                                                                                 |
| ------ | ------- | -------------------------------------------------------------------------------------------------------- |
| REQ-1  | Met     | `ConfigurationTest/validity-test`                                                                        |
| REQ-2  | Met     | `ConfigurationTest/local-configuration` (local config, `CreateMapper()` + `ConfigurationProvider`, expression body, static field) |
| REQ-3  | Met     | `ConfigurationTest/setup-mapper`                                                                         |
| REQ-4  | Met     | `ConfigurationTest/assert-statement`                                                                     |
| REQ-5  | Met     | `ConfigurationTest/class-fixture`                                                                        |
| REQ-6  | Met     | `_Combined/configuration-test-fixture` (real `AutoMapperFix`, all findings Fixed)                        |
| REQ-7  | Met     | `ConfigurationTest/inline-create-map`, `settings`, `method-group`, `Rewrite_ProfileFromReferencedAssembly_ReportsManual` |
| REQ-8  | Met     | `ConfigurationTest/unmigrated-map`, `FixAsync_MapInReferencedProject_ChecksBeforeRemoving` (both cases)   |
| REQ-9  | Met     | `ConfigurationTest/side-effect-receiver`, `Rewrite_AssertAsExpressionBody_ReportsManual`                  |
| REQ-10 | Met     | `Find_MapperPassedOn_FindsNothing` (three forms)                                                         |
| REQ-11 | Met (narrowed) | unit test `FixAsync_ActionCheckWhileMapsStay_ReportsKept`; removal of the `Action`/`NotThrow` pair not built (TAAK-273, see plan) |

## Deviations

- The INDEX row, README row, queue row and `proofs.md` were not written by the agent (instruction: handed to the main session); `proofs-doc.py` not run (no Python on this machine).
- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there.

## Validation results

| Check   | Command                                                     | Result                                   |
| ------- | ----------------------------------------------------------- | ---------------------------------------- |
| Build   | `dotnet build`                                              | passed (0 warnings, warnings = errors)   |
| Tests   | `dotnet test`                                               | 1198/1198 passed                         |
| Samples | `dotnet test --solution samples/SampleShop/SampleShop.slnx` | 38/38 passed                             |
| Lint    | _none_                                                      | _not run_: no lint command configured    |

## See also

- [automapper-configuration-test-plan.md](automapper-configuration-test-plan.md)
- [automapper-configuration-test-modellen.md](automapper-configuration-test-modellen.md)
- `claude/tasks/archief/TAAK-250-automapper-configuration-test.md`
