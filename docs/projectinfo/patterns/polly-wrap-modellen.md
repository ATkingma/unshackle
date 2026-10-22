# Models: polly.wrap

Facts from `src/Unshackle.Core/Fixes/Polly/Patterns/WrapPattern.cs` and `src/Unshackle.Core/Fixes/Polly/PollyWrap.cs`.

## Figure 1: classes

**Figure 1.** The wrap reuses the holder machinery and the retry options.

```mermaid
classDiagram
    class WrapPattern { Find() Rewrite() }
    class PollyWrap { Operands() Plan() OperandCreation() OperandLocals() IsOperand() RemoveOperandLocal() }
    class PollyRetry { Targets() PlanPipeline() PlanOptions() PlanWrapUse() }
    WrapPattern --> PollyWrap
    WrapPattern --> PollyRetry
    PollyRetry --> PollyWrap
```

| Element | Role |
| --- | --- |
| `PollyWrap.Operands` | operands outermost first; nested wraps flattened |
| `PollyWrap.Plan` | one `AddRetry(PlanOptions(operand))` per operand |
| `PollyWrap.IsOperand` | tells the retry patterns to leave an operand alone |
| `PollyRetry.PlanWrapUse` | the wrap's own plan, which decides whether operand locals go |

## Figure 2: sequence

**Figure 2.** One outermost wrap, its use and its operand locals.

```mermaid
sequenceDiagram
    Fixer->>WrapPattern: Find
    WrapPattern->>PollyRetry: Targets(wrap)
    WrapPattern->>PollyWrap: OperandLocals(wrap)
    Fixer->>WrapPattern: Rewrite(use or holder)
    WrapPattern->>PollyRetry: PlanExecute / PlanDeclaration
    PollyRetry->>PollyWrap: Plan(wrap)
    Fixer->>WrapPattern: Rewrite(operand local)
    WrapPattern->>PollyWrap: RemoveOperandLocal
    PollyWrap->>PollyRetry: PlanWrapUse(outer wrap)
```

## Figure 3: decision tree

**Figure 3.** Questions asked for a wrap.

```mermaid
flowchart TD
    A[WrapAsync from Polly] --> B{2+ operands, no named or array arguments?}
    B -- no --> M1[Manual WrapOrderNotProvable]
    B -- yes --> C{every operand a retry in place or a local used only here?}
    C -- no --> M1
    C -- yes --> D{same result type?}
    D -- no --> M1
    D -- yes --> E{each operand's options plannable?}
    E -- no --> M2[Manual with the retry code]
    E -- yes --> F[Fixed: builder with AddRetry per operand; operand locals removed]
```

| Leaf | Proof |
| --- | --- |
| Fixed, arity 2 | `two-locals` (+ trace, reversal differs) |
| Fixed, arity 3 | `three-inline` (+ trace, reversal differs) |
| `WrapOrderNotProvable` | `operand-reused` |

## See also

- [polly-wrap-plan.md](polly-wrap-plan.md)
- [polly-wrap-verify.md](polly-wrap-verify.md)
