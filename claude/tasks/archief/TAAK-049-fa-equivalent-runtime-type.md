# TAAK-049 — fa.be-equivalent-to-object: Shouldly eist gelijk runtime-type

**Status**: voltooid (2026-10-11)
**Aangemaakt**: 2026-10-11
**Gevonden in**: TAAK-046 (semantische audit)

## Probleem

`BeEquivalentToObjectPattern` controleert dat subject en verwachting **statisch** hetzelfde type hebben. Shouldly's `ShouldBeEquivalentTo` vergelijkt echter het **runtime**-type; FA vergelijkt alleen de members van de verwachting.

Gemeten (`scratchpad/semprobe`): `Pt a = new Pt2 { X = 1, Y = 2 }; Pt b = new Pt { X = 1 };`
- `a.Should().BeEquivalentTo(b)` PASS
- `a.ShouldBeEquivalentTo(b)` FAIL

Gevolg: groene test wordt rood als het subject een afgeleide instantie is (bv. factory/deserializer die een subtype teruggeeft). In de casus niet geraakt (`SimpleObject` is niet-afgeleid).

## Wat, waar, hoe

- **Waar**: `Unsupported(...)` in `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/BeEquivalentToObjectPattern.cs`.
- **Hoe**: alleen Fixed als het statische type `sealed` is of een struct (dan kan runtime-type niet afwijken), anders Manual met reden "The subject's runtime type may derive from {type}: Shouldly requires equal runtime types, FluentAssertions compares only the expectation's members." Geldt ook voor geneste members met class-type (Shouldly vergelijkt recursief ook types; eerst meten).
- **Afweging**: dit maakt veel huidige Fixed-gevallen Manual (gewone niet-sealed classes). Alternatief: alleen waarschuwen in docs. Meten hoeveel van de 3 casus-Fixed dan Manual worden, en beslissen.

## Stappen

- [x] Probe: geneste member met afgeleide runtime-instantie (FA vs Shouldly)
- [x] Keuze: Manual voor niet-sealed classes, of documenteren
- [x] Proofs + `docs/projectinfo/patterns/` bijwerken
- [x] `dotnet build` + `dotnet test` groen

## Besluit gebruiker

Niet-sealed types worden **niet** Manual: de herschrijving blijft Fixed, met een waarschuwing in het rapport ("ShouldBeEquivalentTo fails when the runtime type differs; FluentAssertions accepted derived types").

## Resultaat

Probe: geneste afgeleide member en beide-afgeleid met verschillend extra member: FA PASS, Shouldly FAIL. `FixedResult` kreeg een optioneel `Warning` (init); `ReportRow` zet het in de kolom Reason (Markdown/Json/Csv), de console-samenvatting telt "with a warning". Geen waarschuwing bij sealed/struct (ook genest) of als beide kanten een `new`-expressie zijn. Unittests `Core/BeEquivalentToObjectPatternTests.cs` en `ReportWriterTests.From_FixedWithWarning_PutsWarningInReason`; proofs `sealed.*` en `both-new.*`. SampleShop ongewijzigd.

## Zie ook

- `claude/tasks/TAAK-046-semantische-audit.md`
