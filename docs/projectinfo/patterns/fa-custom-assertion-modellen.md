# Modellen: fa.custom-assertion

Eigen assertions op een FluentAssertions-type (`result.Should().BeFailure()`) worden Manual, gegroepeerd per extensie. Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CustomAssertionPattern.cs`, `ReportRow.FromAll` en `SpectreConsoleView.ShowSummary`.

**Figure 1.** Klassen rond `CustomAssertionPattern`: hij implementeert `IPattern` direct (niet `FluentAssertionPattern`), levert alleen `ManualResult`, en rapport en console groeperen via `Group`. Weggelaten: `Fixer`, `IFix` en de rapportschrijvers (zie [../modellen/klassen.md](../modellen/klassen.md)).

<!-- kingmadoc:diagram img/fa-custom-assertion-modellen-1.d2 -->
![Modellen: fa.custom-assertion](img/fa-custom-assertion-modellen-1.png)

| Element                  | Pad                                                                            | Rol                                                                                 |
| ------------------------ | ------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------- |
| `IPattern`               | `src/Unshackle.Core/Abstractions/IPattern.cs`                                  | Contract: `Find` en `Rewrite`                                                       |
| `CustomAssertionPattern` | `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CustomAssertionPattern.cs` | Dit patroon, Id `fa.custom-assertion`; `Group` bundelt de Manual-findings per reden |
| `FluentAssertionsSyntax` | `src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsSyntax.cs`          | `IsFluentAssertions(symbol)`                                                        |
| `ReportRow`              | `src/Unshackle.DataAccess/Reports/ReportRow.cs`                                | `FromAll`: één rij per extensie met aantal                                          |
| `SpectreConsoleView`     | `src/Unshackle.Interface/Output/SpectreConsoleView.cs`                         | `ShowSummary`: aantal en reden per extensie                                         |

**Figure 2.** `Fixer` roept `Find` en per node `Rewrite` aan, dat altijd `Manual` levert; daarna groeperen console en rapport de findings via `Group`. Weggelaten: het verzamelen van de findings door de `Fixer`.

<!-- kingmadoc:diagram img/fa-custom-assertion-modellen-2.d2 -->
![Modellen: fa.custom-assertion](img/fa-custom-assertion-modellen-2.png)

| Stap    | Plek                             | Wat gebeurt er                                                                                             |
| ------- | -------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Find    | `CustomAssertionPattern.Find`    | Invocations met member access waarvoor `IsCustomAssertion` geldt                                           |
| Rewrite | `CustomAssertionPattern.Rewrite` | Manual "Custom assertion `Type.M` (bestand): ..."; de reden is gelijk voor elke aanroep van één methode |
| Console | `SpectreConsoleView.ShowSummary` | `Group(findings)`, aflopend op aantal, één advieskop                                                       |
| Rapport | `ReportRow.FromAll`              | `Group(all)`: één rij (bij de eerste aanroep) met aantal; overige aanroepen vallen weg                     |

**Figure 3.** Beslisboom van `Find` (`IsCustomAssertion`) en `Rewrite`; proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/CustomAssertion/` (`<naam>.before.cs` met `.manual.txt`).

<!-- kingmadoc:diagram img/fa-custom-assertion-modellen-3.d2 -->
![Modellen: fa.custom-assertion](img/fa-custom-assertion-modellen-3.png)

| Vraag                                                                 | Bij nee                                          | Proof                 |
| --------------------------------------------------------------------- | ------------------------------------------------ | --------------------- |
| Invocation met member access?                                         | geen match                                       | -                     |
| M niet zelf uit FluentAssertions?                                     | geen match (FA-methoden doen de andere patronen) | alle andere FA-proofs |
| Receiver of declarerend type (of basistype) is FA-type `*Assertions`? | geen match                                       | -                     |

| Voorwaarde                                                                        | Uitkomst                                              | Proofs               |
| --------------------------------------------------------------------------------- | ----------------------------------------------------- | -------------------- |
| Extensiemethode op een FA-assertions-type                                         | Manual: Custom assertion `ResultAssertions.BeFailure` | `extension`          |
| Instance-methode van een subklasse                                                | Manual: Custom assertion `OrderAssertions.BeShipped`  | `subclass`           |
| In een `AssertionScope` (scope wordt niet omgezet: de aanroep is geen FA-methode) | Manual met dezelfde custom-reden                      | `in-assertion-scope` |

## Zie ook

- [fa-custom-assertion-plan.md](fa-custom-assertion-plan.md) - plan
- [fa-custom-assertion-verify.md](fa-custom-assertion-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CustomAssertionPattern.cs` - het patroon
- `src/Unshackle.Tests/Proofs/FluentAssertions/CustomAssertion/` - de proofs
- [../README.md](../README.md)
