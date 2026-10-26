"""L4 helper (Windows): restore + `scan --report` per corpus repo. Usage:
python -I corpus-epplus-scan.py <corpus root> <tool dir> <list file> <fix id> [names...]
The corpus root holds src/<name> (untrusted clones), and gets nuget/, tmp/, clihome/ and scan/<name>/ (logs + report.json).
"""
import os, subprocess, sys, time

root, tool, listfile, fix = sys.argv[1:5]
only = set(sys.argv[5:])
env = dict(os.environ)
for k, sub in {"TEMP": "tmp", "TMP": "tmp", "NUGET_PACKAGES": "nuget", "NUGET_HTTP_CACHE_PATH": "nuget-http",
               "DOTNET_CLI_HOME": "clihome"}.items():
    path = os.path.join(root, sub)
    os.makedirs(path, exist_ok=True)
    env[k] = path
env.update({"DOTNET_CLI_TELEMETRY_OPTOUT": "1", "MSBUILDDISABLENODEREUSE": "1", "DOTNET_NOLOGO": "1", "DOTNET_ROLL_FORWARD": "Major"})
props = ["-p:NuGetAudit=false", "-p:TreatWarningsAsErrors=false", "-p:WarningsNotAsErrors=NU1507"]


def run(cmd, cwd, log, timeout):
    t = time.time()
    with open(log, "w", encoding="utf-8") as f:
        try:
            rc = subprocess.run(cmd, cwd=cwd, env=env, stdout=f, stderr=subprocess.STDOUT, timeout=timeout).returncode
        except subprocess.TimeoutExpired:
            rc = "timeout"
    return rc, int(time.time() - t)


for line in open(listfile, encoding="utf-8"):
    parts = line.split()
    if not parts or parts[0].startswith("#") or (only and parts[0] not in only):
        continue
    name, sln = parts[0], parts[1]
    out = os.path.join(root, "scan", name)
    os.makedirs(out, exist_ok=True)
    src = os.path.join(root, os.environ.get("CORPUS_SRC", "src"), name)
    r1 = run(["dotnet", "build", sln, "-m:2", "-nologo", "-clp:ErrorsOnly;Summary"] + props, src, os.path.join(out, "build.log"), 1200)
    r2 = run(["dotnet", os.path.join(tool, "unshackle.dll"), "scan", sln, "--fix", fix, "--max-degree", "4",
              "--report", os.path.join(out, "report.json")], src, os.path.join(out, "scan.log"), 1800)
    with open(os.path.join(root, "scan", "_results.txt"), "a", encoding="utf-8") as r:
        r.write(f"{name} build={r1[0]} {r1[1]}s scan={r2[0]} {r2[1]}s\n")
    print(name, r1, r2, flush=True)
