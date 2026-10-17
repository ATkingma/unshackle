# Reporting issues

Unshackle grows by real-world code. If it misses something in your codebase, open an issue:
**[New issue](https://github.com/ATkingma/unshackle/issues/new/choose)** and pick one of the three forms.

| Form                | Use it when                                                                         |
| ------------------- | ----------------------------------------------------------------------------------- |
| **Missing pattern** | Unshackle leaves code untouched, or reports it as manual, but it could be rewritten |
| **New library**     | You want to migrate away from a library Unshackle does not support yet              |
| **Wrong rewrite**   | The rewritten code does not compile, or behaves differently than before             |

## Missing pattern

Supported patterns are listed in [patterns.md](patterns.md).
Check there first, then include:

1. **The smallest compilable before-snippet**, with the declared types. The right rewrite often depends on the
   type: `HaveCount` on a `List<T>` becomes `.Count`, on an `IEnumerable<T>` it becomes `.Count()`.
2. **The after-snippet you expect.**
3. **Behaviour differences** you know of: case sensitivity, ordering, null handling, exception types, messages.
4. **Versions** of the old and new library.
5. **How often** it occurs. This helps us prioritise.

Good example:

```csharp
// before (FluentAssertions 7.2.2)
IReadOnlyList<Order> orders = repository.All();
orders.Should().ContainSingle();

// after (Shouldly 4.3.0)
orders.ShouldHaveSingleItem();
```

Unsure what the after should be, for example with a predicate overload the new library lacks? Say so. A clear before-snippet is already useful.

## New library

We support libraries whose license changed to commercial or restrictive, and that have a free target to
migrate to. Include:

1. **Library and NuGet package**, with a link.
2. **The license change**: from which version, to what, and a link to the announcement.
3. **The migration target**: the free alternative, or plain code.
4. **The three to five most common usages** and what each should become.
5. Optionally: **the scale** in your codebase.

## Wrong rewrite

Include the pattern ID from the report (column `Pattern`, e.g. `fa.have-count`), the code before, what
Unshackle produced, what you expected, and the compile error or failing test. Remove anything confidential;
a reduced snippet that still shows the problem is best.

## What happens next

Each accepted pattern becomes a task with research, a plan and a verification document in
[docs/projectinfo/patterns/](projectinfo/patterns/), and a set of before/after proofs that run in CI.
