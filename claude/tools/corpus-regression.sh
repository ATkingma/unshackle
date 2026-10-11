#!/bin/bash
# Corpus regression harness: per solution compare test outcomes before and after `unshackle fix`.
#
#   corpus-regression.sh one  <name> <source dir|.tgz> <solution relative path> <fix id>
#   corpus-regression.sh list <file>      # lines "name source sln fix"; '#' = comment; max $JOBS (3) at once
#   corpus-regression.sh summary
#
# Env: OUT (results, default $PWD/corpus-regression-out), TOOL (published unshackle.dll; default: publish this repo
# once into $OUT/tool), JOBS, TEST_TIMEOUT (s per test run, default 3600).
# Source must be an untouched original (corpus dir or tarball); a fresh copy is made on every run.
# Steps: copy -> global.json SDK 10 (VSTest) -> restore -> build -> test x2 (flaky) -> fix -> clean bin/obj ->
# restore -> build (fails: STOP, no tests on old binaries) -> test -> compare.
set -u
HERE=$(cd "$(dirname "$0")" && pwd)
OUT=${OUT:-$PWD/corpus-regression-out}
JOBS=${JOBS:-3}
TEST_TIMEOUT=${TEST_TIMEOUT:-3600}
PY="python3 -I $HERE/corpus-regression.py"
P="-p:NuGetAudit=false -p:AllowMissingPrunePackageData=true -p:SkipNSwag=True"
export OUT JOBS TEST_TIMEOUT
export DOCKER_HOST=tcp://127.0.0.1:1 DOTNET_CLI_TELEMETRY_OPTOUT=1 MSBUILDDISABLENODEREUSE=1 DOTNET_ROLL_FORWARD=Major

tool() {
  if [ -z "${TOOL:-}" ]; then
    TOOL=$OUT/tool/unshackle.dll
    [ -f "$TOOL" ] || dotnet publish "$HERE/../../src/Unshackle.Interface" -c Release -o "$OUT/tool" > "$OUT/publish.log" 2>&1 \
      || { echo "publish failed, see $OUT/publish.log"; exit 1; }
  fi
  export TOOL
}

one() {
  local N=$1 SRC=$2 SLN=$3 FIX=$4
  local D=$OUT/$N W=$OUT/$N/work L=$OUT/$N/logs T0=$(date +%s)
  rm -rf "$D"; mkdir -p "$W" "$L"
  if [[ $SRC == *.tgz ]]; then tar -xzf "$SRC" -C "$W" --strip-components=1; else cp -r "$SRC/." "$W"; fi
  $PY globaljson "$W"
  cd "$W" || return
  status() { echo "$1" > "$D/status.txt"; echo "$(( $(date +%s)-T0 ))s" > "$D/duration.txt"; }
  step() { local s=$1; shift; local t=$(date +%s); timeout "$TO" nice "$@" > "$L/$s.log" 2>&1; local e=$?
           echo "$s exit=$e t=$(( $(date +%s)-t ))s" >> "$D/steps.txt"; return $e; }
  local TO=1800
  step restore0 dotnet restore "$SLN" $P || { status "restore-faalt"; return; }
  step build0 dotnet build "$SLN" $P --no-restore || { status "baseline-build-faalt"; return; }
  TO=$TEST_TIMEOUT
  step test0a dotnet test "$SLN" --no-build --logger trx --results-directory "$D/trx-base1"
  step test0b dotnet test "$SLN" --no-build --logger trx --results-directory "$D/trx-base2"
  TO=1800
  step fix dotnet "$TOOL" fix "$SLN" --fix "$FIX" --force --report "$D/report.json" || { status "fix-faalt"; return; }
  find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
  step restore1 dotnet restore "$SLN" $P || { status "restore-na-fix-faalt"; return; }
  step build1 dotnet build "$SLN" $P --no-restore || {
    grep -oE 'error [A-Z]+[0-9]+' "$L/build1.log" | sort | uniq -c | sort -rn > "$D/build1-errors.txt"
    status "BUILD-NA-FIX-FAALT"; return; }
  TO=$TEST_TIMEOUT
  step test1 dotnet test "$SLN" --no-build --logger trx --results-directory "$D/trx-after"
  $PY compare "$N" "$D/trx-base1" "$D/trx-base2" "$D/trx-after" "$D/compare.json"
  status "ok"
}

case ${1:-} in
  one) shift; mkdir -p "$OUT"; tool; one "$@" ;;
  list) mkdir -p "$OUT"; tool
        grep -vE '^\s*(#|$)' "$2" | xargs -P "$JOBS" -L 1 bash "$0" one ;;
  summary) $PY summary "$OUT" ;;
  *) sed -n '2,12p' "$0"; exit 2 ;;
esac
