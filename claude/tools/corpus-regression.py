#!/usr/bin/env python3
"""Helper for corpus-regression.sh.

  globaljson <dir>                     pin every global.json to SDK 10, add one at the root; prints the test
                                       runner: "mtp" (Microsoft.Testing.Platform) or "vstest"
  compare <name> <base1> <base2> <after> <out.json>
                                       compare per-test outcomes of three TRX directories
  summary <out-dir>                    markdown table of all <name>/compare.json files
  selftest
"""
import json, pathlib, sys, xml.etree.ElementTree as ET
from collections import Counter

NS = '{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}'
SDK = {'version': '10.0.100', 'rollForward': 'latestFeature'}
MTP = {'runner': 'Microsoft.Testing.Platform'}
MTP_MARKERS = ('Sdk="MSTest.Sdk', '<UseMicrosoftTestingPlatformRunner>true', '<EnableMSTestRunner>true',
               '<EnableNUnitRunner>true', '<TestingPlatformDotnetTestSupport>true')


def is_mtp(root, jsons):
    if any(j.get('test', {}).get('runner') == MTP['runner'] for j in jsons):
        return True
    files = (f for pat in ('*.csproj', '*.props') for f in root.rglob(pat))
    return any(m in f.read_text(errors='ignore') for f in files for m in MTP_MARKERS)


def globaljson(root):
    root = pathlib.Path(root)
    found = {}
    for p in root.rglob('global.json'):
        try:
            found[p] = json.loads(p.read_text(encoding='utf-8-sig'))
        except Exception:
            found[p] = {}
    mtp = is_mtp(root, found.values())
    found.setdefault(root / 'global.json', {})
    for p, j in found.items():
        j['sdk'] = SDK
        if mtp:
            j['test'] = MTP  # SDK 10 refuses MTP projects under VSTest
        else:
            j.pop('test', None)
        p.write_text(json.dumps(j), encoding='utf-8')
    print('mtp' if mtp else 'vstest')


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
    reg, fixed, not_run = [], [], []
    for k in sorted(a.keys() & c.keys()):
        if k in fl:
            continue
        if a[k][0] == 'Passed' and c[k][0] not in ('Passed', 'NotExecuted'):  # Failed/Error/Timeout/Aborted
            reg.append({'test': k, 'message': c[k][1]})
        elif a[k][0] == 'Passed' and c[k][0] == 'NotExecuted':  # TAAK-381: e.g. an undefined step (inconclusive), still a regression
            not_run.append({'test': k, 'message': c[k][1]})
        elif a[k][0] == 'Failed' and c[k][0] == 'Passed':
            fixed.append(k)
    cnt = lambda d, o: sum(1 for v in d.values() if v[0] == o)
    r = {'name': name,
         'before': {'total': len(a), 'passed': cnt(a, 'Passed'), 'failed': cnt(a, 'Failed')},
         'after': {'total': len(c), 'passed': cnt(c, 'Passed'), 'failed': cnt(c, 'Failed')},
         'regressions': reg, 'not_run': not_run, 'fixed': fixed, 'flaky': flaky,
         'missing': sorted(a.keys() - c.keys()), 'new': sorted(c.keys() - a.keys())}
    pathlib.Path(out).write_text(json.dumps(r, indent=1))
    return r


def summary(outdir):
    print('| Solution | status | tests vóór (pass/fail) | tests na (pass/fail) | regressies | geslaagd→niet uitgevoerd | gefaald→geslaagd | verdwenen | nieuw | flaky | duur |')
    print('| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |')
    for d in sorted(pathlib.Path(outdir).iterdir()):
        if not d.is_dir():
            continue
        st = (d / 'status.txt').read_text().strip() if (d / 'status.txt').exists() else '?'
        dur = (d / 'duration.txt').read_text().strip() if (d / 'duration.txt').exists() else ''
        c = d / 'compare.json'
        if not c.exists():
            print(f'| {d.name} | {st} | | | | | | | | | {dur} |')
            continue
        r = json.loads(c.read_text())
        b, a = r['before'], r['after']
        print(f"| {d.name} | {st} | {b['total']} ({b['passed']}/{b['failed']}) | {a['total']} ({a['passed']}/{a['failed']}) "
              f"| {len(r['regressions'])} | {len(r.get('not_run', []))} | {len(r['fixed'])} | {len(r['missing'])} | {len(r['new'])} | {len(r['flaky'])} | {dur} |")


def selftest():
    import tempfile
    def trx(d, rows):
        d.mkdir(parents=True)
        defs = ''.join(f'<UnitTest id="{i}" storage="/x/net8.0/T.dll"><TestMethod className="C"/></UnitTest>' for i, _ in enumerate(rows))
        res = ''.join(f'<UnitTestResult testId="{i}" testName="C.{n}" outcome="{o}"/>' for i, (n, o) in enumerate(rows))
        (d / 'a.trx').write_text(f'<TestRun xmlns="{NS[1:-1]}"><Results>{res}</Results><TestDefinitions>{defs}</TestDefinitions></TestRun>')
    with tempfile.TemporaryDirectory() as t:
        t = pathlib.Path(t)
        trx(t / '1', [('a', 'Passed'), ('b', 'Failed'), ('f', 'Passed'), ('m', 'Passed'), ('d(1)', 'Passed'), ('d(1)', 'Passed'), ('u', 'Passed')])
        trx(t / '2', [('a', 'Passed'), ('b', 'Failed'), ('f', 'Failed'), ('m', 'Passed'), ('d(1)', 'Passed'), ('d(1)', 'Passed'), ('u', 'Passed')])
        trx(t / '3', [('a', 'Failed'), ('b', 'Passed'), ('f', 'Failed'), ('n', 'Passed'), ('d(1)', 'Passed'), ('d(1)', 'Passed'), ('u', 'NotExecuted')])
        r = compare('x', t / '1', t / '2', t / '3', t / 'o.json')
        k = 'net8.0/T.dll::C.'
        assert [x['test'] for x in r['regressions']] == [k + 'a'], r
        assert [x['test'] for x in r['not_run']] == [k + 'u'], r
        assert r['fixed'] == [k + 'b'] and r['flaky'] == [k + 'f'], r
        assert r['missing'] == [k + 'm'] and r['new'] == [k + 'n'], r
        assert r['before']['total'] == 7, r
        (t / 'g1').mkdir(); (t / 'g1' / 'a.csproj').write_text('<Project Sdk="MSTest.Sdk/4.0.2"/>')
        (t / 'g2').mkdir(); (t / 'g2' / 'a.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"/>')
        assert is_mtp(t / 'g1', []) and not is_mtp(t / 'g2', []) and is_mtp(t / 'g2', [{'test': MTP}])
    print('selftest ok')


if __name__ == '__main__':
    cmd, *args = sys.argv[1:]
    {'globaljson': globaljson, 'compare': compare, 'summary': summary, 'selftest': selftest}[cmd](*args)
