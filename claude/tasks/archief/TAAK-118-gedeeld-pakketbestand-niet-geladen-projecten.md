# TAAK-118 — Gedeeld pakketbestand verliest pakket dat een niet-gemigreerd project nog nodig heeft

**Status**: klaar (2026-10-11)
**Aangemaakt**: 2026-10-11

## Probleem

Een project dat bij het laden wordt overgeslagen (TAAK-090/112/135) staat niet in de migraties. Krijgt het
FluentAssertions/MediatR via een gedeeld `Directory.Packages.props` (CPM) of `Directory.Build.props`, dan haalde
`XmlProjectFileStore` daar de `PackageVersion`/`PackageReference` weg → dat project bouwt niet meer.

## Regel

Een gedeeld bestand verliest het oude pakket alleen als geen enkel `.csproj` op schijf eronder het nog gebruikt.
Anders blijft de regel staan, komt het nieuwe pakket ernaast en meldt de Fixer Manual op dat bestand.

## Wat, waar, hoe

- `XmlProjectFileStore.UnloadedUsers` (DataAccess, `IFileSystem`): per gedeeld bestand alle `*.csproj` onder zijn map
  die niet gemigreerd worden en dit bestand als dichtstbijzijnde `Directory.*.props` hebben; gebruik = `PackageReference`
  van een verwijderd pakket in de eigen `.csproj` of in zijn `Directory.Build.props`. Onleesbaar = gebruiker.
- `ProjectFileChange.KeptRemovedPackageFor`: het project waarvoor het gedeelde bestand het pakket houdt.
- `Fixer.MigrateProjectFilesAsync`: zo'n change → Manual i.p.v. Fixed.
- Projecten waarvan de code Manual blijft: al gedekt door `KeepRemovedPackage` (TAAK-017).

## Tests

- `XmlProjectFileStoreTests`: CPM met niet-geladen FA-project, zonder FA, via gedeelde Build.props, nabijere Build.props.
- `FixerProjectFileTests.FixAsync_SharedFileKeptForUnloadedProject_ReportsManual` (NSubstitute).
- `Integration/SharedPackageFileTests`: echte tijdelijke map, CPM, één overgeslagen project dat FA gebruikt.

## Zie ook

- `docs/projectinfo/architectuur/core.md` § Projectbestanden
- `claude/tasks/archief/TAAK-135-loader-razor-en-vreemde-projecten.md`
