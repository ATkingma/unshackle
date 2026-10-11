---
kingmadoc: 1
feature: "fa-custom-assertion"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5]
files_expected:
    - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/CustomAssertionPattern.cs
    - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
    - src/Unshackle.DataAccess/Reports/ReportRow.cs
    - src/Unshackle.Interface/Output/SpectreConsoleView.cs
    - src/Unshackle.Tests/Proofs/FluentAssertions/CustomAssertion/
---

# Feature: fa.custom-assertion: eigen FA-assertions als Manual, gegroepeerd per extensie

|               |                         |
| ------------- | ----------------------- |
| **Project**   | Unshackle               |
| **Status**    | Approved                |
| **Generated** | 2026-10-11 by KingmaDoc |

> Opgesteld uit TAAK-091 (ANALYSE-fa-100, categorie 3).

## One-sentence summary

Een aanroep van een niet-FA-methode op een FA-assertions-type (`result.Should().BeFailure()`) wordt `Manual` met één reden per extensie, en rapport en console tonen die aanroepen als één regel met aantal.

## Scope (in / out)

**In scope**

- Herkennen: methode niet uit een `FluentAssertions`-namespace, receiver (extensie) of declarerend type (instance) is of erft van een FA-type waarvan de naam op `Assertions` eindigt.
- Reden: "Custom assertion `Type.Methode` (bestand of assembly): rewrite this FluentAssertions extension to Shouldly once; its calls can stay."
- Rapport (csv/json/md): één rij per extensie (eerste aanroep) met "N call(s), first here." voor de reden. Console: lijst per extensie met aantal, aflopend.

**Out of scope**

- De extensie zelf herschrijven (body gebruikt `Execute.Assertion`), shims genereren.
- Eigen `Should()`-methodes die geen FA-type teruggeven.

## Requirements

- **REQ-1**: WHEN een eigen extensie op een FA-assertions-type wordt aangeroepen THE SYSTEM SHALL Manual geven met de extensienaam (proof `CustomAssertion/extension`).
- **REQ-2**: WHEN een instance-methode van een subklasse van een FA-assertions-type wordt aangeroepen THE SYSTEM SHALL Manual geven (proof `CustomAssertion/subclass`).
- **REQ-3**: WHEN een FA-methode zelf wordt aangeroepen THE SYSTEM SHALL dit patroon niet laten matchen (`IsFluentAssertions` op de methode).
- **REQ-4**: WHEN het rapport wordt geschreven THE SYSTEM SHALL de aanroepen van één extensie samenvoegen tot één rij met aantal (unittest `FromAll_CustomAssertionCalls_OneRowPerAssertionWithCount`).
- **REQ-5**: WHEN de console de samenvatting toont THE SYSTEM SHALL per extensie aantal en reden tonen, met één advieskop.
- **Uitbreiding**: WHEN een eigen extensie binnen een `AssertionScope` staat THE SYSTEM SHALL dezelfde custom-reden geven (niet "Inside an AssertionScope"); `fa.assertion-scope` zet zo'n scope niet om, omdat de aanroep geen FA-methode is (proof `CustomAssertion/in-assertion-scope`).

## Risks

- Een JSON/CSV-consument die Manual-rijen telt, telt bij deze extensies nu één rij per extensie; het aantal staat in de reden.

## Modellen

[fa-custom-assertion-modellen.md](fa-custom-assertion-modellen.md).

## Open questions

- Geen.
