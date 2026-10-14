# TAAK-242 — Old using removed while a Manual call still needs it (call unbound until `using System;` is added)

**Status**: ✅ completed
**Finished**: 2026-10-14
**Created**: 2026-10-14 (FA corpus round 5 full: EventFlow `BUILD-NA-FIX-FAALT`, CS1061 `'DateTime' does not contain a definition for 'At'`)
**Goal**: a file keeps a removed using as long as a call left in the file still binds to it, also when that call only binds once the namespaces the rewrites need are imported.

## Context for the next agent

- EventFlow `Voyages.cs` has no `using System;`. `1.October(2008).At(12, 00)`: the month call became `new DateTime(2008, 10, 1)`
  (pattern asks for `System`), `At` stayed Manual. `UsingDirectives.UpdateAsync` decided which removed usings are still used on the
  model *before* `using System;` was added: there `DateTime` binds to nothing, so `.At` binds to nothing, and
  `using FluentAssertions.Extensions;` looked unused → removed → build error.
- `UnboundCalls` only covers the fix's `RemovedExtensionMethods` (`Should`), not every extension of a removed namespace.

## Steps

- [x] 1. `UsingDirectives.UpdateAsync`: with missing usings, also compute the used removed namespaces on the document with the missing
  usings added, and take the union (only more usings kept, never fewer).
- [x] 2. Proof `TimeSpanExtensions/and-no-using-system` (`5.Seconds().And(3.Seconds())` without `using System;`): fails without step 1.
- [x] 3. Verify `fa-timespan-extensions` REQ-6.

## Result

- 1148/1148 green. EventFlow itself is also fixed by TAAK-243 (`At` is now rewritten).
