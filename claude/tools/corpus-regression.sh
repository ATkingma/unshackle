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
# Steps: copy -> global.json SDK 10 (VSTest; MTP + --report-trx if the source uses MTP) -> restore -> build ->
# test x2 (flaky) -> fix -> clean bin/obj ->
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
# H1 (council round 15): before each solution $OUT's disk needs MIN_FREE_GB (default 20) free; below it the run stops (exit 255, xargs
# stops starting solutions) and "run incomplete: disk below threshold" goes to $OUT/INCOMPLETE.txt until that file is removed.
MIN_FREE_GB=${MIN_FREE_GB:-20}
disk_ok() {
  [ ! -e "$OUT/INCOMPLETE.txt" ] || { echo "run incomplete (see $OUT/INCOMPLETE.txt): not starting $1" >&2; return 1; }
  local F; F=$(df -Pk "$OUT" | awk 'NR==2 { print int($4 / 1048576) }')
  [ "${F:-0}" -lt "$MIN_FREE_GB" ] || return 0
  echo "run incomplete: disk below threshold (${F:-?} GB free < ${MIN_FREE_GB} GB on $OUT) before $1 at $(date '+%F %T')" | tee -a "$OUT/INCOMPLETE.txt" >&2
  return 1
}

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
  disk_ok "$N" || exit 255
  rm -rf "$D"; mkdir -p "$W" "$L"
  if [[ $SRC == *.tgz ]]; then tar -xzf "$SRC" -C "$W" --strip-components=1; else cp -r "$SRC/." "$W"; fi
  local RUNNER; RUNNER=$($PY globaljson "$W")
  cd "$W" || return
  local TEST=(dotnet test "$SLN" --no-build --logger trx --results-directory)
  # shortcut: MTP mode takes --solution only, a .csproj target needs --project
  [ "$RUNNER" = mtp ] && TEST=(dotnet test --solution "$SLN" --no-build --report-trx --results-directory)
  # H1: the work copy (with its obj/bin) goes as soon as the solution is done; the results stay in $D.
  status() { echo "$1" > "$D/status.txt"; echo "$(( $(date +%s)-T0 ))s" > "$D/duration.txt"; cd "$OUT" && rm -rf "$W"; }
  step() { local s=$1; shift; local t=$(date +%s); timeout "$TO" nice "$@" > "$L/$s.log" 2>&1; local e=$?
           echo "$s exit=$e t=$(( $(date +%s)-t ))s" >> "$D/steps.txt"; return $e; }
  local TO=1800
  step restore0 dotnet restore "$SLN" $P || { status "restore-faalt"; return; }
  step build0 dotnet build "$SLN" $P --no-restore || { status "baseline-build-faalt"; return; }
  TO=$TEST_TIMEOUT
  step test0a "${TEST[@]}" "$D/trx-base1"
  step test0b "${TEST[@]}" "$D/trx-base2"
  TO=1800
  step fix dotnet "$TOOL" fix "$SLN" --fix "$FIX" --force --report "$D/report.json" || { status "fix-faalt"; return; }
  find . -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
  step restore1 dotnet restore "$SLN" $P || { status "restore-na-fix-faalt"; return; }
  step build1 dotnet build "$SLN" $P --no-restore || {
    grep -oE 'error [A-Z]+[0-9]+' "$L/build1.log" | sort | uniq -c | sort -rn > "$D/build1-errors.txt"
    status "BUILD-NA-FIX-FAALT"; return; }
  TO=$TEST_TIMEOUT
  step test1 "${TEST[@]}" "$D/trx-after"
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
