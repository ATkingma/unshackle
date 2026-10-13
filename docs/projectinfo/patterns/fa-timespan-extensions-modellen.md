# Modellen: fa.timespan-extensions

Feiten uit `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/TimeSpanExtensionsPattern.cs`. Het patroon implementeert `IPattern` direct (geen `x.Should()`), staat als laatste in `FluentAssertionsFix.Patterns`.

## Figuur 1 — Klassen

```d2
direction: right
IPattern: {shape: class; "Find(root, model)"; "Rewrite(node, model)"}
TimeSpanExtensionsPattern: {shape: class; "-s_factories: Dictionary"; "-s_months: string[]"; "-Method(invocation, model)"; "-TypeName(name, model, position)"}
PatternResult: {shape: class}
FluentTimeSpanExtensions: "FA: FluentTimeSpanExtensions"
FluentDateTimeExtensions: "FA: FluentDateTimeExtensions"
TimeSpanExtensionsPattern -> IPattern: implementeert
TimeSpanExtensionsPattern -> PatternResult: Fixed / Manual
TimeSpanExtensionsPattern -> FluentTimeSpanExtensions: herkent
TimeSpanExtensionsPattern -> FluentDateTimeExtensions: herkent
```

| Element | Rol |
| --- | --- |
| `TimeSpanExtensionsPattern` | Id `fa.timespan-extensions` |
| `s_factories` | `Ticks`→`FromTicks`, `Milliseconds`→`FromMilliseconds`, … `Days`→`FromDays` |
| `s_months` | `January` … `December` → maandnummer |
| `TypeName` | `TimeSpan`/`DateTime` kaal, met `using System`, of `global::System.*` |

## Figuur 2 — Volgorde

```d2
shape: sequence_diagram
Fixer; Pattern: TimeSpanExtensionsPattern; Model: SemanticModel
Fixer -> Pattern: Find(root, model)
Pattern -> Model: GetSymbolInfo(invocation)
Fixer -> Pattern: Rewrite(node, model)
Pattern -> Model: GetConstantValue (maand) / LookupNamespacesAndTypes (TypeName)
Pattern -> Fixer: Fixed(nieuwe expressie, "System"?) of Manual(reden)
```

| Stap | Wat |
| --- | --- |
| Find | Elke invocation die bindt aan `FluentAssertions.Extensions.FluentTimeSpanExtensions` of `FluentDateTimeExtensions` |
| Rewrite | Argumenten op volgorde (ontvanger eerst), dan factory, maand of Manual |

## Figuur 3 — Beslisboom van `Rewrite`

```d2
q1: "Naam in s_factories en één getal-parameter?"
q2: "Maand met dag + jaar?"
q3: "Dag of jaar constant?"
f1: "TimeSpan.FromX(n)"
f2: "new DateTime(y, m, d)"
m1: "Manual: berekende dag en jaar"
m2: "Manual: geen directe vervanging"
q1 -> f1: ja
q1 -> q2: nee
q2 -> q3: ja
q2 -> m2: nee
q3 -> f2: ja
q3 -> m1: nee
```

| Blad | Proof / test |
| --- | --- |
| `TimeSpan.FromX(n)` | `simple`, `static-call`, `no-using-system`, `Rewrite_OwnTimeSpanTypeInScope_QualifiesSystemTimeSpan` |
| `new DateTime(y, m, d)` | `month` |
| Manual: berekende dag en jaar | `month-computed` |
| Manual: geen directe vervanging | `and`, `offset`, `Rewrite_DateTimeAt_Manual` |

De figuren zijn niet gerenderd (geen `kingmadoc`-CLI op deze machine).

## Zie ook

- [fa-timespan-extensions-plan.md](fa-timespan-extensions-plan.md) - plan
- [fa-timespan-extensions-verify.md](fa-timespan-extensions-verify.md) - verificatie
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/TimeSpanExtensionsPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/TimeSpanExtensions/`
