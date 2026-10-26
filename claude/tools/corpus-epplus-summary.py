"""L4 helper: summarise <corpus root>/scan/*/report.json as markdown. Usage: python -I corpus-epplus-summary.py <corpus root>"""
import collections, glob, json, os, re, sys

root = sys.argv[1]
results = {}
for line in open(os.path.join(root, "scan", "_results.txt"), encoding="utf-8"):
    parts = line.split()
    results[parts[0]] = " ".join(parts[1:])
codes = collections.Counter()
patterns = collections.Counter()
print("| Repo | Build / scan | Fixed | Manual | Crash in log |")
print("| --- | --- | ---: | ---: | --- |")
for name, res in results.items():
    report = os.path.join(root, "scan", name, "report.json")
    rows = json.load(open(report, encoding="utf-8-sig")) if os.path.exists(report) else []
    fixed = sum(1 for r in rows if r["Status"] != "Manual")
    manual = len(rows) - fixed
    log = open(os.path.join(root, "scan", name, "scan.log"), encoding="utf-8", errors="ignore").read()
    crash = "yes" if re.search(r"Unhandled exception|   at Unshackle\.", log) else ""
    for r in rows:
        patterns[(r["Pattern"], r["Status"])] += 1
        if r["Status"] == "Manual":
            codes[(r["Pattern"], (r.get("Reason") or "").split(":")[0])] += 1
    print(f"| {name} | {res} | {fixed} | {manual} | {crash} |")
print()
print("| Pattern | Status | Count |")
print("| --- | --- | ---: |")
for (p, s), n in sorted(patterns.items(), key=lambda kv: -kv[1]):
    print(f"| {p} | {s} | {n} |")
print()
print("| Pattern | Manual code | Count |")
print("| --- | --- | ---: |")
for (p, c), n in codes.most_common(40):
    print(f"| {p} | {c} | {n} |")
