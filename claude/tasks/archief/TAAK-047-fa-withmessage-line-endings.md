# TAAK-047 — fa.throw/fa.throw-async: WithMessage negeert regeleinden, Shouldly-vorm niet

**Status**: voltooid (2026-10-11)
**Aangemaakt**: 2026-10-11
**Gevonden in**: TAAK-046 (semantische audit)

## Probleem

`ThrowMessage.Check` (`src/Unshackle.Core/Fixes/FluentAssertions/Patterns/ThrowMessage.cs`) vertaalt `WithMessage("[*]x[*]")` naar `ShouldBe(x, StringCompareShould.IgnoreCase)` / `ShouldContain|StartWith|EndWith(x, Case.Insensitive)`. FA 7 negeert bij `WithMessage` ook het verschil `\r\n` vs `\n`; Shouldly met alleen IgnoreCase niet.

Gemeten (FA 7.2.2, Shouldly 4.3.0, `scratchpad/semprobe`):
- `WithMessage("a\nb")` op "a\r\nb": FA PASS, `ShouldBe("a\nb", IgnoreCase)` FAIL.
- `WithMessage("*line1\nline2*")` op "Line1\r\nLine2 end": FA PASS, `ShouldContain(.., Case.Insensitive)` FAIL.
- `ShouldBe(.., IgnoreCase | IgnoreLineEndings)` PASS.

Gevolg: een FA-test die groen was (bv. multiline-melding op Windows) wordt rood na de fix. Alleen als het letterlijke patroon `\r` of `\n` bevat.

## Wat, waar, hoe

- **Waar**: `ThrowMessage.Check`.
- **Hoe**:
  - `ShouldBe`-tak: optie `Shouldly.StringCompareShould.IgnoreCase | Shouldly.StringCompareShould.IgnoreLineEndings` wanneer `literal` `\r` of `\n` bevat (of altijd: zonder regeleinde in het patroon is de vlag gedragsneutraal; kies "alleen indien nodig" voor leesbare output).
  - `ShouldContain/StartWith/EndWith`-tak: Shouldly heeft daar geen regeleinde-optie → `PatternResult.Manual("WithMessage(...) pattern contains a line break: FluentAssertions ignores line-ending differences, Shouldly's ShouldContain does not.")`.
- **Waarom**: gedrag gelijk houden; Manual waar Shouldly het niet kan uitdrukken (projectregel: pattern gooit nooit, liever Manual).

## Stappen

- [x] Proofs in `src/Unshackle.Tests/Proofs/FluentAssertions/Throw/` en `ThrowAsync/`: exacte melding met `\n` (after met IgnoreLineEndings) en `*..\n..*` (manual.txt)
- [x] Wijziging in `ThrowMessage.cs`
- [x] KingmaDoc plan/verify van fa.throw en fa.throw-async bijwerken (`docs/projectinfo/patterns/`)
- [x] `dotnet build` + `dotnet test` groen

## Resultaat

`ThrowMessage.Check` geeft nu `PatternResult`: exacte melding met regeleinde → `IgnoreCase | IgnoreLineEndings` (probe PASS, ook omgekeerd \n vs \r\n; IgnoreLineEndings alleen faalt op hoofdletters), `*`-vorm met regeleinde → Manual. Proofs `message-line-break.*` en `message-contains-line-break.*` in Throw/ en ThrowAsync/.

## Zie ook

- `claude/tasks/TAAK-046-semantische-audit.md`
