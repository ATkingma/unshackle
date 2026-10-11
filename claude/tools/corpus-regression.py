#!/usr/bin/env python3
"""Helper for corpus-regression.sh.

  globaljson <dir>                     pin every global.json to SDK 10 (VSTest), add one at the root
  compare <name> <base1> <base2> <after> <out.json>
                                       compare per-test outcomes of three TRX directories
  summary <out-dir>                    markdown table of all <name>/compare.json files
  selftest
"""
import json, pathlib, sys, xml.etree.ElementTree as ET
from collections import Counter

NS = '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
SDK = {'version': '10.0.100', 'rollForward': 'latestFeature'}


def globaljson(root):
    root = pathlib.Path(root)
    for p in root.rglob('global.json'):
        try:
            j = json.loads(p.read_text(encoding='utf-8-sig'))
        except Exception:
            j = {}
        j['sdk'] = SDK
        j.pop('test', None)  # no "runner": MTP -> keep VSTest so --logger trx works
        p.write_text(json.dumps(j), encoding='utf-8')
    if not (root / 'global.json').exists():
        (root / 'global.json').write_text(json.dumps({'sdk': SDK}))


def load(trx_dir):
    """{key: (outcome, message)}; key = assembly/tfm :: class.testName [#n for duplicates]."""
    res = {}
    for f in sorted(pathlib.Path(trx_dir).rglob('*.trx')):
        try:
            t = ET.parse(f).getroot()
        except ET.ParseError:
            continue
        defs = {}
        for ut in t.iter(NS + 'UnitTest'):
            m = ut.find(NS + 'TestMethod')
            storage = pathlib.PurePath(ut.get('storage') or '')
            asm = f'{storage.parent.name}/{storage.name}'
            defs[ut.get('id')] = (asm, m.get('className') if m is not None else '')
        seen = Counter()
        for r in t.iter(NS + 'UnitTestResult'):
            asm, cls = defs.get(r.get('testId'), ('?', ''))
            name = r.get('testName') or ''
            if cls and not name.startswith(cls):
                name = f'{cls}.{name}'
            key = f'{asm}::{name}'
            seen[key] += 1
            if seen[key] > 1:
                key += f' #{seen[key]}'
            msg = r.find(f'{NS}Output/{NS}ErrorInfo/{NS}Message')
            res[key] = (r.get('outcome'), (msg.text or '').strip()[:400] if msg is not None else '')
    return res


def compare(name, b1, b2, after, out):
    a, b, c = load(b1), load(b2), load(after)
    flaky = sorted(k for k in a.keys() | b.keys() if a.get(k, (None,))[0] != b.get(k, (None,))[0])
    fl = set(flaky)
    reg, fixed = [], []
    for k in sorted(a.keys() & c.keys()):
        if k in fl:
            continue
        if a[k][0] == 'Passed' and c[k][0] not in ('Passed', 'NotExecuted'):  # Failed/Error/Timeout/Aborted
            reg.append({'test': k, 'message': c[k][1]})
        elif a[k][0] == 'Failed' and c[k][0] == 'Passed':
            fixed.append(k)
    cnt = lambda d, o: sum(1 for v in d.values() if v[0] == o)
    r = {'name': name,
         'before': {'total': len(a), 'passed': cnt(a, 'Passed'), 'failed': cnt(a, 'Failed')},
         'after': {'total': len(c), 'passed': cnt(c, 'Passed'), 'failed': cnt(c, 'Failed')},
         'regressions': reg, 'fixed': fixed, 'flaky': flaky,
         'missing': sorted(a.keys() - c.keys()), 'new': sorted(c.keys() - a.keys())}
    pathlib.Path(out).write_text(json.dumps(r, indent=1))
    return r


def summary(outdir):
    print('| Solution | status | tests vóór (pass/fail) | tests na (pass/fail) | regressies | gefaald→geslaagd | verdwenen | nieuw | flaky | duur |')
    print('| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |')
    for d in sorted(pathlib.Path(outdir).iterdir()):
        if not d.is_dir():
            continue
        st = (d / 'status.txt').read_text().strip() if (d / 'status.txt').exists() else '?'
        dur = (d / 'duration.txt').read_text().strip() if (d / 'duration.txt').exists() else ''
        c = d / 'compare.json'
        if not c.exists():
            print(f'| {d.name} | {st} | | | | | | | | {dur} |')
            continue
        r = json.loads(c.read_text())
        b, a = r['before'], r['after']
        print(f"| {d.name} | {st} | {b['total']} ({b['passed']}/{b['failed']}) | {a['total']} ({a['passed']}/{a['failed']}) "
              f"| {len(r['regressions'])} | {len(r['fixed'])} | {len(r['missing'])} | {len(r['new'])} | {len(r['flaky'])} | {dur} |")


def selftest():
    import tempfile
    def trx(d, rows):
        d.mkdir(parents=True)
        defs = ''.join(f'<UnitTest id="{i}" storage="/x/net8.0/T.dll"><TestMethod className="C"/></UnitTest>' for i, _ in enumerate(rows))
        res = ''.join(f'<UnitTestResult testId="{i}" testName="C.{n}" outcome="{o}"/>' for i, (n, o) in enumerate(rows))
        (d / 'a.trx').write_text(f'<TestRun xmlns="{NS[1:-1]}"><Results>{res}</Results><TestDefinitions>{defs}</TestDefinitions></TestRun>')
    with tempfile.TemporaryDirectory() as t:
        t = pathlib.Path(t)
        trx(t / '1', [('a', 'Passed'), ('b', 'Failed'), ('f', 'Passed'), ('m', 'Passed'), ('d(1)', 'Passed'), ('d(1)', 'Passed')])
        trx(t / '2', [('a', 'Passed'), ('b', 'Failed'), ('f', 'Failed'), ('m', 'Passed'), ('d(1)', 'Passed'), ('d(1)', 'Passed')])
        trx(t / '3', [('a', 'Failed'), ('b', 'Passed'), ('f', 'Failed'), ('n', 'Passed'), ('d(1)', 'Passed'), ('d(1)', 'Passed')])
        r = compare('x', t / '1', t / '2', t / '3', t / 'o.json')
        k = 'net8.0/T.dll::C.'
        assert [x['test'] for x in r['regressions']] == [k + 'a'], r
        assert r['fixed'] == [k + 'b'] and r['flaky'] == [k + 'f'], r
        assert r['missing'] == [k + 'm'] and r['new'] == [k + 'n'], r
        assert r['before']['total'] == 6, r
    print('selftest ok')


if __name__ == '__main__':
    cmd, *args = sys.argv[1:]
    {'globaljson': globaljson, 'compare': compare, 'summary': summary, 'selftest': selftest}[cmd](*args)
