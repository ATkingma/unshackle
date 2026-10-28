#!/bin/bash
# AutoMapper corpus harness (Linux). Port of the Windows corpus-am.sh, based on corpus-regression.sh.
#
#   corpus-am.sh clone                          # shallow-clone $REPOS (default claude/tools/corpus-am-repos.txt) into $CORPUS/src (skips existing)
#   corpus-am.sh one  <base|scan|after> <name> <sln relative path> [fix]
#   corpus-am.sh list <base|scan|after> <file>  # lines "name sln [fix]"; '#' = comment; max $JOBS (2) at once
#   corpus-am.sh summary <base|scan|after>
#
# Env: CORPUS (required: the corpus root on a large disk; never /tmp: tmpfs in RAM), TOOL (published unshackle.dll),
# FIX (default fix id when a line has none; default automapper), REPOS, JOBS, TEST_TIMEOUT (s, default 3600), EXTRA (extra msbuild args), NOLOCK=1 (delete packages.lock.json in the copy; needed for bitwarden).
# Sources in $CORPUS/src/<name> are untrusted clones: only restore/build/test and the tool run on them. A fresh copy
# (no .git/bin/obj/node_modules) is made per run in $CORPUS/work/<mode>-<name> and deleted afterwards.
# base  = restore, build, test x2.  scan = restore, build, `scan --fix $FIX --report`.
# after = restore, build, `fix --fix $FIX --force`, clean bin/obj, restore, build (fails: stop), test, compare with _results/base.
set -u
HERE=$(cd "$(dirname "$0")" && pwd)
CORPUS=${CORPUS:?set CORPUS to the corpus root, e.g. <disk>/unshackle-corpus/corpus-automapper (never /tmp)}
JOBS=${JOBS:-2}
TEST_TIMEOUT=${TEST_TIMEOUT:-3600}
EXTRA=${EXTRA:-}
FIX=${FIX:-automapper}
REPOS=${REPOS:-$HERE/corpus-am-repos.txt}
PY="python3 -I $HERE/corpus-regression.py"
R=$CORPUS/_results
P="-p:NuGetAudit=false -p:AllowMissingPrunePackageData=true -p:SkipNSwag=True -p:WarningsNotAsErrors=NU1507 $EXTRA"
mkdir -p "$CORPUS/tmp" "$CORPUS/work" "$R"
export CORPUS JOBS TEST_TIMEOUT EXTRA FIX REPOS TOOL=${TOOL:-}
export TEMP=$CORPUS/tmp TMP=$CORPUS/tmp TMPDIR=$CORPUS/tmp
export DOCKER_HOST=tcp://127.0.0.1:1 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 DOTNET_ROLL_FORWARD=Major
# H1 (council round 15): before each repo the corpus disk needs MIN_FREE_GB (default 20) free. Below it the run stops (it does not skip
# the repo): "run incomplete: disk below threshold" goes to $R/INCOMPLETE.txt, `one` exits 255 (xargs in `list` stops starting repos)
# and every later `one` refuses until that file is removed. Results of a run with that file are not counted.
MIN_FREE_GB=${MIN_FREE_GB:-20}
INCOMPLETE=$R/INCOMPLETE.txt
free_gb() { df -Pk "$CORPUS" | awk 'NR==2 { print int($4 / 1048576) }'; }
disk_ok() {
  [ ! -e "$INCOMPLETE" ] || { echo "run incomplete (see $INCOMPLETE): not starting $1" >&2; return 1; }
  local F; F=$(free_gb)
  [ "${F:-0}" -lt "$MIN_FREE_GB" ] || return 0
  echo "run incomplete: disk below threshold (${F:-?} GB free < ${MIN_FREE_GB} GB on $CORPUS) before $1 at $(date '+%F %T')" | tee -a "$INCOMPLETE" >&2
  return 1
}

clone() {
  grep -vE '^\s*(#|$)' "$REPOS" | while read -r N REPO REF; do
    [ -e "$CORPUS/src/$N" ] && { echo "skip $N"; continue; }
    # abbreviated shas cannot be fetched: resolve to the full sha (tags pass through)
    FULL=$(gh api "repos/$REPO/commits/$REF" --jq .sha 2>/dev/null || echo "$REF")
    mkdir -p "$CORPUS/src/$N" && cd "$CORPUS/src/$N" && git init -q && git remote add origin "https://github.com/$REPO" \
      && git fetch -q --depth 1 origin "$FULL" && git checkout -q FETCH_HEAD && echo "ok $N $FULL" || echo "FAILED $N"
  done
}

one() {
  local M=$1 N=$2 SLN=$3 F=${4:-$FIX}
  local SRC=$CORPUS/src/$N D=$R/$M/$N W=$CORPUS/work/$M-$N T0=$(date +%s)
  local L=$D/logs
  disk_ok "$M $N" || exit 255
  rm -rf "$D" "$W"; mkdir -p "$W" "$L"
  tar -C "$SRC" --exclude=.git --exclude=bin --exclude=obj --exclude=node_modules -cf - . | tar -C "$W" -xf -
  # lock files pin versions/sources of the original machine (bitwarden: NU1101 on nuget.org-only restore)
  [ -z "${NOLOCK:-}" ] || find "$W" -name packages.lock.json -delete
  [ -f "$W/nuget.config" ] || [ -f "$W/NuGet.Config" ] || [ -f "$W/NuGet.config" ] || cat > "$W/nuget.config" <<'XML'
<configuration><packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources></configuration>
XML
  local RUNNER; RUNNER=$($PY globaljson "$W")
  # RUNNER_FORCE=vstest: solutions whose test projects are VSTest although a marker looks like MTP (fluentmigrator); drop the MTP pin.
  if [ -n "${RUNNER_FORCE:-}" ]; then RUNNER=$RUNNER_FORCE
    [ "$RUNNER" != vstest ] || find "$W" -name global.json -exec python3 -I -c 'import json,sys;p=sys.argv[1];j=json.load(open(p,encoding="utf-8-sig"));j.pop("test",None);json.dump(j,open(p,"w"))' {} \;
  fi
  cd "$W" || return
  local TEST=(dotnet test "$SLN" --no-build --logger trx --results-directory)
  [ "$RUNNER" = mtp ] && TEST=(dotnet test --solution "$SLN" --no-build ${TRX_FLAG:---report-trx} --results-directory)  # TRX_FLAG=--report-xunit-trx: xunit v3 without the TrxReport extension (orchardcore)
  local TO=1800
  # H1: the work copy (with its obj/bin) goes as soon as the repo is done; with JOBS > 1 also any obj/bin built in the source clone.
  status() { echo "$1" > "$D/status.txt"; echo "$(( $(date +%s)-T0 ))s" > "$D/duration.txt"; cd "$CORPUS" && rm -rf "$W"
             [ "$JOBS" -le 1 ] || find "$SRC" -name node_modules -prune -o -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +; }
  step() { local s=$1; shift; local t=$(date +%s); timeout "$TO" nice "$@" > "$L/$s.log" 2>&1; local e=$?
           echo "$s exit=$e t=$(( $(date +%s)-t ))s" >> "$D/steps.txt"; return $e; }
  # Moq H1: a restore can fail transiently under load (NU1102 on packages that exist), so it gets one retry
  restore() { step "$1" dotnet restore "$SLN" $P || { sleep 30; step "$1-retry" dotnet restore "$SLN" $P; }; }
  restore restore0 || { status "restore-fails"; return; }
  case $M in
  base)
    step build0 dotnet build "$SLN" $P --no-restore || { status "baseline-build-fails"; return; }
    TO=$TEST_TIMEOUT
    step test0a "${TEST[@]}" "$D/trx-base1"
    step test0b "${TEST[@]}" "$D/trx-base2" ;;
  scan)
    # a partial build (npm/cargo/yarn project fails) still lets the scan run on the rest of the solution
    step build0 dotnet build "$SLN" $P --no-restore || echo "build-partial" > "$D/build.txt"
    TO=3600
    step scan dotnet "$TOOL" scan "$SLN" --fix "$F" --report "$D/report.json" ;;
  after)
    step build0 dotnet build "$SLN" $P --no-restore || true  # source generators must run before types resolve
    TO=3600; step fix dotnet "$TOOL" fix "$SLN" --fix "$F" --force --report "$D/report.json" || { status "fix-fails"; return; }; TO=1800
    find . -name node_modules -prune -o -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +  # keep node_modules/**/bin (npm tools)
    restore restore1 || { status "restore-after-fix-fails"; return; }
    step build1 dotnet build "$SLN" $P --no-restore || {
      grep -oE 'error [A-Z]+[0-9]+' "$L/build1.log" | sort | uniq -c | sort -rn > "$D/build1-errors.txt"
      status "BUILD-AFTER-FIX-FAILS"; return; }
    TO=$TEST_TIMEOUT
    step test1 "${TEST[@]}" "$D/trx-after"
    $PY compare "$N" "$R/base/$N/trx-base1" "$R/base/$N/trx-base2" "$D/trx-after" "$D/compare.json" ;;
  esac
  status "ok"
}

case ${1:-} in
  clone) clone ;;
  one) shift; one "$@" ;;
  list) M=$2; grep -vE '^\s*(#|$)' "$3" | xargs -P "$JOBS" -L 1 bash "$0" one "$M" ;;
  summary) [ ! -e "$INCOMPLETE" ] || cat "$INCOMPLETE"; $PY summary "$R/$2" ;;
  *) sed -n '2,8p' "$0"; exit 2 ;;
esac
