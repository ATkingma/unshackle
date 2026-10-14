# Verification: automapper-add-automapper

|               |                                                                                                    |
| ------------- | -------------------------------------------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/automapper-add-automapper-plan.md`](automapper-add-automapper-plan.md) |
| **Status**    | Verified                                                                                           |
| **Generated** | 2026-10-12 by KingmaDoc skill (by hand, `kingmadoc` not available)                            |

> Verified by an AI agent against the code at uncommitted changes.

## Requirements

Code: `AddAutoMapperPattern.cs`, `AutoMapperFix.cs`. Proofs: `src/Unshackle.Tests/Proofs/AutoMapper/AddAutoMapper/`.

| REQ   | Verdict | Evidence            |
| ----- | ------- | ----------------- |
| REQ-1 | Met | `simple`          |
| REQ-2 | Met | `parameter`       |
| REQ-3 | Met | `mapper-injected` |
| REQ-4 | Met | `result-used`     |

## Build and tests

- `dotnet build`: 0 warnings, 0 errors.
- `dotnet test`: 35 failing, all the known CRLF errors of a clean main (XmlProjectFileStoreTests, FixerTests, CommandTests.FixExecuteAsync_DirtyTree…, SampleShopTests); no new errors, AddAutoMapper proofs green.

## Deviations

- The solution path (`RewriteAsync`, service usage in another project) has no own proof; proofs run on one compilation.
- PNG/SVG of the models not rendered (no `kingmadoc`/`d2`); the D2 sources are there.

## See also

- [plan](automapper-add-automapper-plan.md), [modellen](automapper-add-automapper-modellen.md)
