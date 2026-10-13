# Verification: fa-be-equivalent-to-collection

|               |                                                                |
| ------------- | -------------------------------------------------------------- |
| **Plan**      | [`docs/projectinfo/patterns/fa-be-equivalent-to-collection-plan.md`](fa-be-equivalent-to-collection-plan.md) |
| **Status**    | Verified                                                       |
| **Generated** | 2026-10-12 met de hand                                         |

## Requirements

Code: `BeEquivalentToObjectPattern.cs`, `ParamsArraySyntax.cs` (nieuw, ook gebruikt door `EqualSequencePattern.cs`). Proofs: `src/Unshackle.Tests/Proofs/FluentAssertions/BeEquivalentToObject/`, gedraaid door `ProofTests`.

| REQ | Oordeel | Bewijs |
| --- | --- | --- |
| REQ-1 | Gehaald | `collection-ints`, `collection-strings-because`, `collection-nullable-enum`, `edge-array-literal` |
| REQ-2 | Gehaald | `collection-record`, `collection-struct` |
| REQ-3 | Gehaald | `params-strings`, `edge-collection-params`, `params-multi-line` |
| REQ-4 | Gehaald | `params-array` |
| REQ-5 | Gehaald | `collection`, `collection-record-unsealed`, `collection-record-nested`, `collection-element-types` |

## Deviations

| # | Area | Plan says | Code does | Impact |
| --- | --- | --- | --- | --- |
| 1 | Array | taak: `new[] { ... }` | `new string[] { ... }` | Gelijk aan `fa.equal-sequence`; type expliciet, ook met `null`-items |
| 2 | Corpusharnas | taakstap 3: FluentResults/CSFE-harnas | niet gedraaid | De shallow clones zijn verwijderd; semantiek vastgesteld met een probe (FA 7.2.2 vs Shouldly 4.3.0, 13 gevallen gelijk) |

## Validation results

| Check | Command | Result |
| --- | --- | --- |
| Build | `dotnet build` | passed (0 warnings) |
| Proofs | `dotnet test --project src/Unshackle.Tests -- --filter-class "*ProofTests"` | alle BeEquivalentToObject/EqualSequence proofs groen; falers elders zijn van andere taken (AndChain, MediatR) |
| Volledige suite | `dotnet test --solution Unshackle.slnx` (kopie van de werkmap) | 33 bekende CRLF-failures plus andermans halfwerk; geen van mijn proofs |

## Zie ook

- [fa-be-equivalent-to-collection-modellen.md](fa-be-equivalent-to-collection-modellen.md)
