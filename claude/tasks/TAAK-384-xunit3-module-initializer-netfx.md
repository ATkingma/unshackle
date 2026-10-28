# TAAK-384 — xunit3: netfx build break, ModuleInitializerAttribute polyfill from Microsoft.Testing.Platform

**Status**: step 1/2 — not started
**Created**: 2026-10-28
**Goal**: Decide how the fix reports (or avoids) a build break caused by a source generator and the MTP polyfill on .NET Framework.

## Context for the next agent

- Corpus (`claude/tasks/CORPUSRUN-xunit.md`), CsvHelper `dffe5e9`: after the fix, `net48` test projects fail with 4x CS0122
  `'ModuleInitializerAttribute' is inaccessible due to its protection level` in the generated `CsvRecordRegistrations.g.cs`.
- Cause (likely): xunit.v3 3.2.2 pulls Microsoft.Testing.Platform 1.9.1; its netstandard2.0 assembly (used on net48) declares an internal
  `System.Runtime.CompilerServices.ModuleInitializerAttribute`. CsvHelper's generator (`src/CsvHelper.Generators/RecordEmitter.cs`) only emits its
  own polyfill when the compilation "lacks" the type, so it now emits none and the internal one is inaccessible. Upstream generator bug made
  visible by v3; net8+ targets are fine.
- No own proof: the break needs a third-party source generator and a netfx build (cannot be shown in a `before`/`after` proof).

## Steps

- [ ] 1. Confirm the cause on a minimal netfx project with a generator that checks `GetTypeByMetadataName`; council: Info row for netfx
  test projects with source generators, or accept as a known upstream issue in the docs
- [ ] 2. Build the decision; re-run CsvHelper

## See also

- `claude/tasks/CORPUSRUN-xunit.md` — corpus round
- `claude/tasks/TAAK-341-xunit-v3.md` — the xunit3 fix
