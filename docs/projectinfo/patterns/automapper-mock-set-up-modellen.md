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
use: every use a creation, constructor argument or stub?
stubs: stubs at least one map and constructs a class?
proof: every stub proven equal (Disproof null)?
stable: after the plan: classes not under test, no hand-off, all calls migrated and stubbed?
removed: stubs removed with a warning, classes migrate
kept: stubs stay; Manual names test file:line and reason

start -> use
use -> stubs: yes
use -> kept: no
stubs -> proof: yes
stubs -> kept: no
proof -> stable: yes
proof -> kept: no
stable -> removed: yes
stable -> kept: no (vetoed, plan rebuilt)
```

## See also

- [plan](automapper-mock-set-up-plan.md), [verify](automapper-mock-set-up-verify.md)
