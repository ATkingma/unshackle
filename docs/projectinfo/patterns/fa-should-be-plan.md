---
kingmadoc: 1
feature: "fa-should-be"
status: approved
requirements: [REQ-1, REQ-2, REQ-3, REQ-4, REQ-5, REQ-6, REQ-7, REQ-8, REQ-9, REQ-10]
files_expected:
  - src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ShouldBePattern.cs
  - src/Unshackle.Core/Fixes/FluentAssertions/FluentAssertionsFix.cs
  - src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/
---
# Feature: fa.should-be: x.Should().Be(y) wordt x.ShouldBe(y), met behoud van het because-argument.

| | |
|---|---|
| **Project** | Unshackle |
| **Status** | Approved |
| **Generated** | 2026-10-10T23:33+02:00 by KingmaDoc 0.3.0.dev9 |

> Achteraf opgesteld uit TAAK-001 na de bouw. Dit plan beschrijft wat is gebouwd en waarom; het is geen ontwerp vooraf.
> Alles gemarkeerd als _(inferred)_ komt uit de codebase-analyse.

## One-sentence summary

fa.should-be: x.Should().Be(y) wordt x.ShouldBe(y), met behoud van het because-argument.

## Scope (in / out)

**In scope**

- `x.Should().Be(y[, because])` als losse assertion (statement of body van een void-lambda) naar `x.ShouldBe(y[, because])`.
- Type-afhankelijke aanpassing van het verwachte argument: een cast als een constante alleen impliciet naar het subjecttype converteert (`byte`, enum).
- `Manual(reden)` voor elk geval dat niet veilig te herschrijven is.
- Alleen het patroon zelf; de `using`-wijzigingen (`FluentAssertions` eruit, `Shouldly` erin) doet de `Fixer`.

**Out of scope**

- `NotBe` en andere `Be*`-methodes (eigen patronen).
- `.And`/`.Which`-ketens splitsen.
- Csproj-wijzigingen (package eruit, Shouldly erin).
- `Be(expected, comparer)`: de comparer wordt niet doorgegeven aan `ShouldBe`.

## Requirements

- **REQ-1**: WHEN `x.Should().Be(y)` een losse assertion is THE SYSTEM SHALL het herschrijven naar `x.ShouldBe(y)` (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/simple.*.cs`, met `int` en `string`).
- **REQ-2**: WHEN `Be` een `because`-string zonder `becauseArgs` krijgt THE SYSTEM SHALL die als tweede argument van `ShouldBe` overnemen (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/because.*.cs`).
- **REQ-3**: WHEN `Be` de body is van een void-lambda (bijv. `Array.ForEach(totals, t => t.Should().Be(20m))`) THE SYSTEM SHALL ook die herschrijven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/lambda.*.cs`).
- **REQ-4**: WHEN het verwachte argument een constante is die alleen impliciet naar het subjecttype converteert (`byte`, enum) THE SYSTEM SHALL een cast naar het subjecttype toevoegen, en bij `long` met `1` geen cast (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/constant-cast.*.cs`).
- **REQ-5**: IF het resultaat van `Be` wordt gebruikt (`.And`, `.Which`, waarde) THEN THE SYSTEM SHALL een Manual-finding geven en de code ongewijzigd laten (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/chain.*`).
- **REQ-6**: IF `Be` `becauseArgs` of benoemde argumenten heeft THEN THE SYSTEM SHALL Manual geven omdat Shouldly niet formatteert (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/because-args.*`).
- **REQ-7**: IF `Be` een comparer meekrijgt THEN THE SYSTEM SHALL Manual geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/comparer.*`).
- **REQ-8**: IF het verwachte type niet veilig naar het subjecttype te brengen is (bijv. `int` tegenover `int?`) THEN THE SYSTEM SHALL Manual geven (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/nullable-expected.*`).
- **REQ-9**: IF `Be` uit `FluentAssertions.Xml` of `FluentAssertions.Types` komt THEN THE SYSTEM SHALL Manual geven omdat de gelijkheid anders is (proof `src/Unshackle.Tests/Proofs/FluentAssertions/ShouldBe/xml.*`).
- **REQ-10**: IF het type van het subject onbekend is (error type) of `Be` niet in de vorm `x.Should().Be(...)` staat THEN THE SYSTEM SHALL Manual geven. _Geen proof: alleen via code bewaakt._
- **Uitbreiding TAAK-097**: WHEN `becauseArgs` bij een constante, formatteerbare `because` staan THE SYSTEM SHALL `x.ShouldBe(y, string.Format(CultureInfo.InvariantCulture, because, args))` geven (proof `because-args.*`, nu `.after.cs`); benoemde argumenten, een niet-constante of niet-formatteerbare `because` blijven Manual (proofs `edge-named-because.*`, `because-args-variable.*`, `because-args-bad-format.*`). Vervangt REQ-6.
- **Uitbreiding TAAK-100**: IF de assertie in een `AssertionScope` staat die niet door `fa.assertion-scope` wordt omgezet THEN THE SYSTEM SHALL Manual "Inside an AssertionScope" geven (proofs `assertion-scope-nested.*`, `assertion-scope-using-block.*`, `assertion-scope-using-var.*`).
- **Uitbreiding TAAK-169**: WHEN het verwachte argument alleen via een door de gebruiker gedefinieerde impliciete conversie naar het subjecttype gaat (`BsonValue b; b.Should().Be(42)`) THE SYSTEM SHALL een cast naar het subjecttype toevoegen: `b.ShouldBe((BsonValue)42)`. Typeinferentie van `ShouldBe<T>` gebruikt zulke conversies niet (CS1929/CS0411). Gemeten (FA 7.2.2 vs Shouldly 4.3.0): met de cast vergelijken beide `Equals`, zelfde uitkomst bij gelijk, ongelijk en een klasse zonder `Equals`-override (referentiegelijkheid, faalt in beide) (proof `implicit-conversion.*`).

## Assumptions

- _(inferred)_ Gebouwd op de bestaande stack: C#, Roslyn, Shouldly 4.3.0 als doel, FluentAssertions 7.2.2 als bron.
- De foundation uit TAAK-001 (`IPattern`, `Fixer`, `ProofTests`) bestaat; dit patroon was het eerste en bewijst de hele keten.
- `ShouldBe<T>(actual, expected)` leidt T af uit beide kanten en laat geen numerieke of nullable conversie toe op de receiver.
- De `Fixer` regelt de usings en de trivia; het patroon bouwt kale nodes.
- Gelijkheid van `Be` (`Equals`) komt overeen met `ShouldBe`, behalve in de Manual-gevallen hierboven.

## Risks

- Een FA-conversie van het argument die niet met een cast te vangen is (bijv. `Guid` tegenover `string`): wordt Manual in plaats van een gok. Beperkt tot gevallen waar `ClassifyConversion` geen veilige vorm vindt.
- Foutmelding van Shouldly verschilt van die van FA; geen Manual nodig, wel een gedocumenteerd verschil.
- Alleen de `because`-tekst blijft behouden; format-args gaan niet verloren want die worden Manual.
## Modellen

Klassendiagram, sequencediagram en beslisboom: [fa-should-be-modellen.md](fa-should-be-modellen.md).

## Open questions

- **Wat als `Be` een andere gelijkheid gebruikt dan `ShouldBe`?** Alleen bij XML en `MethodInfoSelector` (Types); die zijn Manual.
- **Hoe komt de using erbij?** Via de `Fixer` (`IFix.AddedNamespaces`), niet via het patroon.

**Answered while planning**

- [x] Wat lost dit op en voor wie? Developers die van FluentAssertions af willen; `Be` is de meest gebruikte assertion (samples: `OrderTests.cs:24,61`, `OrderHandlerTests.cs:26,27,69`).
- [x] Wie of wat triggert het? De `Fixer` draait het patroon per document.
- [x] Externe systemen? Roslyn (SemanticModel); geen datastore.
- [x] Welke modules veranderen? `Unshackle.Core` (patroon en registratie in `FluentAssertionsFix`) en `Unshackle.Tests` (proofs).
- [x] Hoe weten we dat het werkt? `ProofTests` draait alle proofs: before compileert tegen FA, after is exact gelijk en compileert tegen Shouldly; manual.txt-reden komt voor in het rapport.

## Appendix: codebase context _(inferred)_

- **Tech stack:** C#
- **Entry points:** `src/Unshackle.Interface/Program.cs`
- **Config files:** _none found_
- **Test directories:** _none found_

| Language | Files |
|---|---|
| csharp | 76 |
| markdown | 24 |
| json | 2 |
| yaml | 2 |
| shell | 1 |

<details>
<summary>File tree (142 files)</summary>

```text
unshackle/
├── .claude/
│   ├── hooks/
│   │   └── check-pattern-docs.sh
│   ├── skills/
│   │   ├── taak-oppakken/
│   │   │   └── …
│   │   ├── taak-specifiek/
│   │   │   └── …
│   │   └── taak-start/
│   │       └── …
│   └── settings.json
├── .github/
│   └── workflows/
│       └── ci.yml
├── .vibe-wise/
│   ├── profile.md
│   ├── progress.md
│   └── project-map.md
├── claude/
│   ├── docs/
│   │   ├── agent-werkwijze.md
│   │   ├── code-standaarden.md
│   │   ├── design-patterns.md
│   │   ├── INDEX.md
│   │   └── patroon-loop.md
│   └── tasks/
│       ├── archief/
│       │   └── …
│       ├── patroon-queue.md
│       └── TAAK-003-fa-be-true-false.md
├── docs/
│   └── projectinfo/
│       ├── architectuur/
│       │   └── …
│       ├── beslissingen.md
│       ├── project.md
│       └── README.md
├── samples/
│   ├── SampleShop/
│   │   ├── SampleShop.Tests/
│   │   │   └── …
│   │   ├── SampleShop/
│   │   │   └── …
│   │   └── SampleShop.slnx
│   ├── .editorconfig
│   ├── Directory.Build.props
│   └── Directory.Packages.props
├── src/
│   ├── Unshackle.Core/
│   │   ├── Abstractions/
│   │   │   └── …
│   │   ├── Fixes/
│   │   │   └── …
│   │   ├── Models/
│   │   │   └── …
│   │   ├── Fixer.cs
│   │   ├── Unshackle.Core.csproj
│   │   └── UsingDirectives.cs
│   ├── Unshackle.DataAccess/
│   │   ├── Reports/
│   │   │   └── …
│   │   ├── Solutions/
│   │   │   └── …
│   │   └── Unshackle.DataAccess.csproj
│   ├── Unshackle.Interface/
│   │   ├── Commands/
│   │   │   └── …
│   │   ├── Output/
│   │   │   └── …
│   │   ├── Program.cs
│   │   └── Unshackle.Interface.csproj
│   └── Unshackle.Tests/
│       ├── Core/
│       │   └── …
│       ├── DataAccess/
│       │   └── …
│       ├── Helpers/
│       │   └── …
│       ├── Integration/
│       │   └── …
│       ├── Interface/
│       │   └── …
│       ├── Proofs/
│       │   └── …
│       ├── ProofFix.cs
│       ├── ProofTests.cs
│       └── Unshackle.Tests.csproj
├── .editorconfig
├── .featuredoc.yml
├── .gitignore
├── CLAUDE.md
├── Directory.Build.props
├── Directory.Packages.props
├── global.json
├── LICENSE
├── README.md
└── Unshackle.slnx
```

</details>
