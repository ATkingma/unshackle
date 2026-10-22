# Council round 11 — questions (2026-10-22)

Answer by id. Code on main (`ddada54`).

## EPPlus worksheet index (revisits E3)

Harness finding (`src/Unshackle.Tests/Core/EPPlus/WorksheetIndexTests.cs`): EPPlus 4.5.3.3's **netstandard** build (loaded on .NET Core / .NET 5+) is **0-based** by default; its .NET Framework builds are 1-based; both builds, and 5+, read an override from configuration (`EPPlus:ExcelPackage.Compatibility.IsWorksheets1Based` in appsettings.json or app.config). Current code: 4.x integer index → Manual `WorksheetIndexBaseNotProvable`; 5+ → `Worksheet(i + 1)`.

- **E10.** May a 4.x integer index be Fixed by the project's target framework (netstandard/netcore → 0-based, net4x → 1-based)?
- **E11.** Should the presence of the configuration key (in any config file of the project, or set in code) make every integer index Manual, also on 5+?

## AutoMapper mocked IMapper (TAAK-311, `docs/projectinfo/patterns/automapper-mock-set-up-plan.md`)

TAAK-311 removed 0 Manual rows on the 4 blind repos; all blockers are below.

- **A6.** May `Verify(m => m.Map<B>(x), Times.Once)` be dropped with a warning (cf. M4)? (culinary 7, msb 2)
- **A7.** A stub with an argument matcher (`It.IsAny<A>()`), when the test only reads members the generated map copies from a source the test controls: allowed, or always Manual? (culinary 17, msb 9)
- **A8.** A mock without stubs when the test asserts nothing about the mapped result: allowed? (culinary 3, encryptednotes 1)

## MassTransit detector (TAAK-310, `src/Unshackle.Core/Fixes/MassTransit/`)

- **T6.** Report `Publish`/`Send` calls too? (Left out: noisy, other buses use `Send`, map nearly 1:1.)
- **T7.** Some features (plain consumers, close to Rebus) as Info instead of Manual, so the inventory separates easy from hard?

## See also

- `claude/docs/council.md` — rounds 8–10
