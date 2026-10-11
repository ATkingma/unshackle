#!/usr/bin/env python3
"""Generates docs/projectinfo/architectuur/proofs.md from src/Unshackle.Tests/Proofs.

Run from the repository root: python3 claude/tools/proofs-doc.py
"""
import difflib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PROOFS = ROOT / "src/Unshackle.Tests/Proofs"
OUT = ROOT / "docs/projectinfo/architectuur/proofs.md"


def code(text):
    text = " ".join(text.split()).replace("|", "\\|")
    return f"`{text[:110]}{'…' if len(text) > 110 else ''}`" if text else ""


def changed(before, after):
    """First changed line pair, so the table shows what the pattern rewrites."""
    a = [l.strip() for l in before.splitlines() if l.strip() and not l.strip().startswith("using ")]
    b = [l.strip() for l in after.splitlines() if l.strip() and not l.strip().startswith("using ")]
    removed, added = [], []
    for line in difflib.ndiff(a, b):
        if line.startswith("- "):
            removed.append(line[2:])
        elif line.startswith("+ "):
            added.append(line[2:])
    if not removed and not added:
        return "alleen usings"
    return f"{code(' '.join(removed[:2]))} → {code(' '.join(added[:2]))}"


def assertion(before):
    lines = [l.strip() for l in before.splitlines()]
    hit = next((l for l in lines if ".Should" in l or "Handle(" in l or "AddMediatR" in l or "CreateMap" in l), "")
    return code(hit)


def rows(folder):
    suffixes = (".before.cs", ".after.cs", ".manual.txt")
    names = sorted({p.name.removesuffix(x) for p in folder.iterdir() for x in suffixes if p.name.endswith(x)})
    for name in names:
        before = folder / f"{name}.before.cs"
        after = folder / f"{name}.after.cs"
        manual = folder / f"{name}.manual.txt"
        src = before.read_text(encoding="utf-8") if before.exists() else ""
        if manual.exists():
            reason = " ".join(manual.read_text(encoding="utf-8").split())
            yield name, "Manual", f"{assertion(src)} — reden: {reason}"
        elif after.exists():
            yield name, "Fixed", changed(src, after.read_text(encoding="utf-8"))


def section(lines, title, base, level="##"):
    libs = sorted(p for p in base.iterdir() if p.is_dir() and not p.name.startswith("_Pending"))
    for lib in libs:
        patterns = sorted(p for p in lib.iterdir() if p.is_dir())
        if not patterns:
            continue
        lines += [f"{level} {title}{lib.name}", ""]
        for pattern in patterns:
            found = list(rows(pattern))
            if not found:
                continue
            lines += [f"{level}# {pattern.name} ({len(found)})", "", "| Proof | Uitkomst | Wat het bewijst |", "| --- | --- | --- |"]
            lines += [f"| `{n}` | {kind} | {text} |" for n, kind, text in found]
            lines.append("")


def main():
    total = sum(1 for p in PROOFS.rglob("*.before.cs") if "_Pending" not in p.parts)
    lines = [
        "# Proofs",
        "",
        "Overzicht van alle proof-tests in `src/Unshackle.Tests/Proofs/`. Gegenereerd door `claude/tools/proofs-doc.py`; niet met de hand bewerken.",
        "",
        "Een proof is een `*.before.cs` (compileert tegen de oude bibliotheek) plus `*.after.cs` (verwachte uitvoer, compileert tegen de nieuwe)",
        "of `*.manual.txt` (verwachte Manual-reden). `ProofTests` en `CombinedProofTests` draaien ze; `_Pending/` bevat bekende fouten met een taak.",
        "Kolom *Wat het bewijst*: bij Fixed de eerste regel die verandert (voor → na), bij Manual de assertion en de reden.",
        "",
        f"Totaal: {total} proofs (zonder `_Pending`).",
        "",
    ]
    section(lines, "", PROOFS)
    pending = PROOFS / "_Pending"
    if pending.exists():
        lines += ["## _Pending (bekende fouten)", ""]
        section(lines, "_Pending / ", pending, "###")
    lines += ["## Zie ook", "", "- `docs/projectinfo/architectuur/testen.md` — hoe proofs draaien", "- `docs/projectinfo/architectuur/edge-cases.md` — herkomst van de edge-proofs", ""]
    OUT.write_text("\n".join(lines), encoding="utf-8")
    print(f"{OUT.relative_to(ROOT)}: {total} proofs")


if __name__ == "__main__":
    main()
