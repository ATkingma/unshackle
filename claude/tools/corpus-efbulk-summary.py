"""EF bulk corpus summary (TAAK-342 step 9): per repo Fixed/Manual per pattern and the Manual reason codes.

    python -I corpus-efbulk-summary.py <corpus>/_results/<scan|after>
"""
import collections
import json
import os
import sys

root = sys.argv[1]
for name in sorted(os.listdir(root)):
    d = os.path.join(root, name)
    sp = os.path.join(d, "status.txt")
    status = open(sp).read().strip() if os.path.exists(sp) else "?"
    print(f"== {name}: {status}")
    p = os.path.join(d, "report.json")
    if not os.path.exists(p):
        continue
    rows = json.load(open(p, encoding="utf-8"))
    count = collections.Counter((r["Pattern"], r["Status"]) for r in rows)
    for (pat, st), c in sorted(count.items()):
        print(f"   {pat} {st}: {c}")
    reasons = collections.Counter(
        (r["Pattern"], (r.get("Reason") or "").split(":")[0]) for r in rows if r["Status"] != "Fixed")
    for (pat, rc), c in sorted(reasons.items()):
        print(f"      {pat} {rc}: {c}")
