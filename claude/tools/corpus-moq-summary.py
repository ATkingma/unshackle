#!/usr/bin/env python3 -I
"""Markdown tables for CORPUSRUN-moq.md from <corpus root>/_results.

Usage: python3 -I claude/tools/corpus-moq-summary.py <corpus root>
"""
import collections, json, pathlib, sys

root = pathlib.Path(sys.argv[1]) / '_results'


def load(path):
    try:
        return json.loads(path.read_text(encoding='utf-8-sig'))
    except Exception:
        return None


def status(mode, name):
    p = root / mode / name / 'status.txt'
    return p.read_text().strip() if p.exists() else '-'


names = sorted({d.name for m in ('base', 'scan', 'after') if (root / m).exists() for d in (root / m).iterdir() if d.is_dir()})
print('| Repo | base | scan | after | Fixed | Manual | tests before (pass/fail/total) | tests after | regressions |')
print('| ---- | ---- | ---- | ----- | ----- | ------ | ------------------------------ | ----------- | ----------- |')
reasons = collections.Counter()
for n in names:
    rep = load(root / 'after' / n / 'report.json') or load(root / 'scan' / n / 'report.json') or []
    fixed = sum(1 for x in rep if x['Status'] == 'Fixed')
    manual = sum(1 for x in rep if x['Status'] == 'Manual')
    for x in rep:
        if x['Status'] == 'Manual':
            reasons[x['Reason'].split(':')[0]] += 1
    c = load(root / 'after' / n / 'compare.json')
    b = f"{c['before']['passed']}/{c['before']['failed']}/{c['before']['total']}" if c else '-'
    a = f"{c['after']['passed']}/{c['after']['failed']}/{c['after']['total']}" if c else '-'
    r = str(len(c['regressions'])) if c else '-'
    print(f"| {n} | {status('base', n)} | {status('scan', n)} | {status('after', n)} | {fixed} | {manual} | {b} | {a} | {r} |")
print()
print('Manual reasons (all repos, after report where present): ' + ', '.join(f'{k} {v}' for k, v in reasons.most_common(12)))
