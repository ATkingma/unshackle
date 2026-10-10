# CLAUDE.md

Lees eerst:
- `PROJECT.md`: wat Unshackle is, waarom, wat er al bestaat, roadmap
- `ARCHITECTURE.md`: projecten, afhankelijkheden, Fixer → Fix → Pattern, testen (proofs, mocking, types)

Werkafspraken:
- Communiceer in het Nederlands; code, namen en commitberichten in het Engels.
- Core verwijst naar geen ander project. Nieuwe I/O hoort in DataAccess, achter een interface uit Core.
- Elk nieuw patroon krijgt proofs in `tests/Unshackle.Tests/Proofs/<Fix>/<Pattern>/`.
- De code-structuur en codestijl zijn nog niet vastgelegd (zie "Nog te beslissen" in ARCHITECTURE.md). Stel die vast met de gebruiker voordat je veel code schrijft.
