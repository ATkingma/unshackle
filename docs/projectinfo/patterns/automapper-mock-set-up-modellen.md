# Models: automapper-mock-set-up

|               |                                                              |
| ------------- | ------------------------------------------------------------ |
| **Plan**      | [`automapper-mock-set-up-plan.md`](automapper-mock-set-up-plan.md) |
| **Generated** | 2026-10-22 by KingmaDoc (by hand, `kingmadoc` not available) |

## Figure 1: classes

```d2
MockSetUpPattern -> MapOwners: ForAsync / IsDischarged
MapCallPattern -> MapOwners: FindAsync leaves discharged stubs out
MapOwners -> MockedMappers: Find + Disproof per test file
MockedMappers -> MockedMapper: one per mock variable
MockedMapper -> MapStub: stubs
```

## Figure 2: sequence (plan)

```d2
shape: sequence_diagram
BuildAsync -> BuildOnceAsync: vetoed = {}
BuildOnceAsync -> MockedMappers: Find(test file)
BuildOnceAsync -> BuildOnceAsync: candidate if no problem and every stub proven
BuildOnceAsync -> BuildOnceAsync: fixpoint of kept / shared pairs (candidates' classes not under test)
BuildOnceAsync -> BuildOnceAsync: Undischarged(candidate)
BuildOnceAsync -> BuildAsync: failures
BuildAsync -> BuildOnceAsync: again with vetoed += failures (until none)
```

## Figure 3: decision tree per mock

```d2
start: mock of IMapper in a test file
check: a check of a Map call (Verify, Received, DidNotReceive)? (A6)
use: every use a creation, constructor argument or stub?
stubs: constructs a class?
matcher: stub source an argument matcher? (A7)
one: member builds exactly one A in a local, no other A?
proof: every stub proven equal (Disproof null)?
bare: no stubs? (A8)
stable: after the plan: classes not under test, no hand-off, all calls migrated and stubbed?
unread: every Map call of its classes plain, value never read?
removed: stubs removed with a warning, classes migrate
info: classes migrate; Info "mock had no stubs; Map now returns a real object"
verify: Manual VerifyOnMapper, "assert the mapped result instead"
notprovable: Manual MockedReturnNotProvable
nostubs: Manual MockWithoutStubsNotProvable
kept: stubs stay; Manual names test file:line and reason

start -> check
check -> verify: yes
check -> use: no
use -> stubs: yes
use -> kept: no
stubs -> matcher: yes
stubs -> kept: no
matcher -> one: yes
matcher -> proof: no
one -> proof: yes
one -> notprovable: no
proof -> bare: yes
proof -> notprovable: no
bare -> unread: yes
bare -> stable: no
unread -> info: yes
unread -> nostubs: no
stable -> removed: yes
stable -> kept: no (vetoed, plan rebuilt)
```

| Leaf | Proof / test |
| ---- | ------------ |
| verify (A6) | `MapCall/mocked-verify`; `FixAsync_VerifyOnMapper_KeepsMockWithTypedCode` |
| removed, matcher (A7) | `_Combined/mocked-matcher-stub-removed`; `FixAsync_MatcherStubProvablyEqual_RemovesStub` |
| notprovable (A7) | `FixAsync_MatcherStubNotProvable_KeepsStubWithTypedCode` |
| info (A8) | `_Combined/mocked-no-stubs`; `FixAsync_MockWithoutStubsValueNeverRead_MigratesWithInfo` |
| nostubs (A8) | `FixAsync_MockWithoutStubsValueUsed_KeepsWithTypedCode` |

## See also

- [plan](automapper-mock-set-up-plan.md), [verify](automapper-mock-set-up-verify.md)
