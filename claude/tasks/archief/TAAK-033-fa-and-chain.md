# TAAK-033 — Patroon fa.and-chain

**Status**: ✅ voltooid
**Aangemaakt**: 2026-10-11
**Doel**: `x.Should().A().And.B();` wordt `x.ShouldA(); x.ShouldB();`, waarbij elke schakel door zijn eigen patroon wordt herschreven.

## Context voor de volgende agent

- Rij 16 in `claude/tasks/patroon-queue.md` (ronde 2). Voorbeeld van splitsen: `BeGreaterLessPattern` (TAAK-010).

## Analyse

FA 7.2.2: bijna elke assertion geeft `AndConstraint<TAssertions>` of `AndWhichConstraint<TAssertions, T>`; `.And` is dezelfde assertions-instantie op hetzelfde subject. Uitzondering: `Throw<T>().And` is de exception zelf (geen FA-methodes). Shouldly heeft geen ketens: elke `ShouldX` is een losse call. Splitsen evalueert het subject per schakel opnieuw.

## Keuzes

| #   | Keuze                                                                                                                     | Reden                                                                                                                                                    |
| --- | ------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | `AndChainPattern` is een Composite over de assertion-patronen (`FluentAssertionsFix.s_assertions`)                        | Geen gedupliceerde schakel-logica; nieuwe patronen werken vanzelf in ketens                                                                              |
| 2   | Splitsen naar `x.Should().M(args);` en speculatief compileren (`Compilation.ReplaceSyntaxTree`)                           | De schakel-patronen hebben een semantisch model nodig; dit levert het echte model voor de gesplitste code                                                |
| 3   | Eén niet-gevonden of Manual-schakel → hele keten Manual                                                                   | Half herschreven ketens zijn verwarrend; reden noemt de schakel                                                                                          |
| 4   | Bijwerkingen-vrij = `this`, local, parameter, field, property zonder argumenten (en member access daarop, of op een type) | Conservatief, zoals gevraagd; aanroepen en indexers → Manual                                                                                             |
| 5   | `.And`-ontvanger moet hetzelfde type hebben als `x.Should()`                                                              | Een gelijknamige methode op een ander subject zou na splitsen stil iets anders controleren. Geen compileerbaar FA 7-voorbeeld gevonden, dus zonder proof |
| 6   | Patroon staat direct na `AllSatisfyPattern`, vóór de assertion-patronen                                                   | Die melden `.And`-schakels anders als Manual; AllSatisfy-lussen eerst openvouwen                                                                         |
| 7   | `BeGreaterLessPattern` behoudt eigen ketenlogica                                                                          | Raakt zijn proofs niet; in de volledige fix pakt `fa.and-chain` de keten eerst                                                                           |

## Stappen

- [x] 1. Taak, onderzoek, keuzes
- [x] 2. KingmaDoc-plan (7 REQ)
- [x] 3. `AndChainPattern` + registratie
- [x] 4. Proofs (15 cases in `AndChain/`)
- [x] 5. Build/test groen (412 + 38 sample), modellen, verify, archiveren, rij `klaar`

## Gewijzigde bestanden

- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/AndChainPattern.cs` (nieuw), `FluentAssertionsFix.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/AndChain/`
- `docs/projectinfo/patterns/fa-and-chain-{plan,modellen,verify}.md`, `docs/projectinfo/README.md` (rij), `edge-cases.md`

## Zie ook

- `claude/tasks/archief/TAAK-010-fa-be-greater-less.md`
- `docs/projectinfo/patterns/fa-and-chain-plan.md`
