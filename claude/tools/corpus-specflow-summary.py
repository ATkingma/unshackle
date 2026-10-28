#!/usr/bin/env python3
"""SpecFlow corpus summary: per solution status, test compare, and report inventory (S13 step texts, S15 App.config, S17 kept/closure).

    python3 -I corpus-specflow-summary.py <OUT dir of corpus-regression.sh>
"""
import collections
import json
import os
import sys


def rows(path):
    try:
        with open(path, encoding="utf-8") as f:
            return json.load(f)
    except (OSError, ValueError):
        return []


def main(out):
    for name in sorted(os.listdir(out)):
        d = os.path.join(out, name)
        st = os.path.join(d, "status.txt")
        if not os.path.isfile(st):
            continue
        status = open(st).read().strip()
        cmp = rows(os.path.join(d, "compare.json"))
        report = rows(os.path.join(d, "report.json"))
        by = collections.Counter((r["Pattern"], r["Status"]) for r in report)
        appconfig = sum(1 for r in report if "App.config" in r.get("Reason", ""))
        print(f"## {name}: {status}")
        if cmp:
            print("compare:", json.dumps(cmp) if not isinstance(cmp, dict) else
                  {k: v for k, v in cmp.items() if not isinstance(v, (list, dict))})
        for (p, s), n in sorted(by.items()):
            print(f"  {p:32} {s:8} {n}")
        print(f"  App.config rows: {appconfig}")
        for r in report:
            if r["Status"] == "Manual" or r["Pattern"] in ("specflow.kept", "specflow.closure"):
                print(f"    {r['Pattern']} {r['File']}:{r['Line']} {r['Reason'][:160]}")


if __name__ == "__main__":
    main(sys.argv[1])
