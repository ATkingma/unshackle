# Models: fa.timespan-extensions

Facts from `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/TimeSpanExtensionsPattern.cs`. The pattern implements `IPattern` directly (no `x.Should()`), and is last in `FluentAssertionsFix.Patterns`.

## Figure 1 — Classes

```d2
direction: right
IPattern: {shape: class; "Find(root, model)"; "Rewrite(node, model)"}
TimeSpanExtensionsPattern: {shape: class; "-s_factories: Dictionary"; "-s_months: string[]"; "-Method(invocation, model)"; "-RewriteAt(invocation, arguments, definition, model)"; "-MonthReason(name, month, dayAndYear, model)"; "-NeedsParentheses(invocation)"; "-TypeName(name, model, position)"}
PatternResult: {shape: class}
FluentTimeSpanExtensions: "FA: FluentTimeSpanExtensions"
FluentDateTimeExtensions: "FA: FluentDateTimeExtensions"
TimeSpanExtensionsPattern -> IPattern: implements
TimeSpanExtensionsPattern -> PatternResult: Fixed / Manual
TimeSpanExtensionsPattern -> FluentTimeSpanExtensions: recognises
TimeSpanExtensionsPattern -> FluentDateTimeExtensions: recognises
```

| Element | Role |
| --- | --- |
| `TimeSpanExtensionsPattern` | Id `fa.timespan-extensions` |
| `s_factories` | `Ticks`→`FromTicks`, `Milliseconds`→`FromMilliseconds`, … `Days`→`FromDays` |
| `s_months` | `January` … `December` → month number |
| `RewriteAt` | `At` on a `DateTime` (TAAK-243): constructor with the date parts, or `d.Date + time`; a month call as the date becomes one constructor |
| `MonthReason` | Manual reason when day and year are both computed (shared by month and `At`) |
| `TypeName` | `TimeSpan`/`DateTime` bare, with `using System`, or `global::System.*` |

## Figure 2 — Order

```d2
shape: sequence_diagram
Fixer; Pattern: TimeSpanExtensionsPattern; Model: SemanticModel
Fixer -> Pattern: Find(root, model)
Pattern -> Model: GetSymbolInfo(invocation)
Fixer -> Pattern: Rewrite(node, model)
Pattern -> Model: GetConstantValue (month) / LookupNamespacesAndTypes (TypeName)
Pattern -> Fixer: Fixed(new expression, "System"?) or Manual(reason)
```

| Step | What |
| --- | --- |
| Find | Every invocation that binds to `FluentAssertions.Extensions.FluentTimeSpanExtensions` or `FluentDateTimeExtensions`, except a month call that is the date of an `At` call |
| Rewrite | Arguments in order (receiver first), then factory, month or Manual |

## Figure 3 — Decision tree of `Rewrite`

```d2
q0: "At on a DateTime?"
a1: "TimeSpan argument?"
a2: "Date a month call?"
a3: "Microseconds/nanoseconds?"
a4: "Date safe to evaluate twice?"
fa1: "d.Date + t / new DateTime(y, m, n) + t"
fa2: "new DateTime(y, m, n, h, min, s)"
fa3: "new DateTime(d.Year, d.Month, d.Day, h, min, s, ms, d.Kind)"
ma: "Manual: microseconds / computed date"
q1: "Name in s_factories and one number parameter?"
q2: "Month with day + year?"
q3: "Day or year constant?"
f1: "TimeSpan.FromX(n)"
f2: "new DateTime(y, m, d)"
m1: "Manual: computed day and year"
m2: "Manual: no direct replacement"
q0 -> a1: yes
q0 -> q1: no
a1 -> fa1: yes
a1 -> a3: no
a3 -> ma: yes
a3 -> a2: no
a2 -> fa2: yes
a2 -> a4: no
a4 -> fa3: yes
a4 -> ma: no
q1 -> f1: yes
q1 -> q2: no
q2 -> q3: yes
q2 -> m2: no
q3 -> f2: yes
q3 -> m1: no
```

| Leaf | Proof / test |
| --- | --- |
| `TimeSpan.FromX(n)` | `simple`, `static-call`, `no-using-system`, `Rewrite_OwnTimeSpanTypeInScope_QualifiesSystemTimeSpan` |
| `new DateTime(y, m, d)` | `month` |
| Manual: computed day and year | `month-computed` |
| Manual: no direct replacement | `and`, `offset`, `and-no-using-system` |
| `d.Date + t` / `new DateTime(y, m, n) + t` | `at-timespan` |
| `new DateTime(y, m, n, h, min, s)` | `month-at` |
| `new DateTime(d.Year, …, d.Kind)` | `at-variable`, `Rewrite_DateTimeAt_ConstructorWithDateParts` |
| Manual: microseconds / computed date | `at-microseconds`, `at-computed`, `Rewrite_DateTimeAtMicroseconds_Manual` |

The figures are not rendered (no `kingmadoc` CLI on this machine).

## See also

- [fa-timespan-extensions-plan.md](fa-timespan-extensions-plan.md) - plan
- [fa-timespan-extensions-verify.md](fa-timespan-extensions-verify.md) - verification
- `src/Unshackle.Core/Fixes/FluentAssertions/Patterns/TimeSpanExtensionsPattern.cs`
- `src/Unshackle.Tests/Proofs/FluentAssertions/TimeSpanExtensions/`
