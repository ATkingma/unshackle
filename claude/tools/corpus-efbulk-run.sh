#!/bin/bash
# Sequential EF bulk corpus driver (TAAK-342 step 9) (one repo at a time, skips finished results).
#   CORPUS=<root>/corpus-polly TOOL=<unshackle.dll> corpus-efbulk-run.sh <base|scan|after> [repo...]
# Repos and solutions come from corpus-efbulk-list.txt ("name sln"); FIX=efbulk. Wraps corpus-am.sh one.
HERE=$(cd "$(dirname "$0")" && pwd)
M=${1:?mode}; shift
export FIX=efbulk CORPUS=${CORPUS:?}
ONLY=" $* "
grep -vE '^\s*(#|$)' "$HERE/corpus-efbulk-list.txt" | while read -r N SLN; do
  SLN=${SLN//\"/}
  [ -z "$*" ] || [[ "$ONLY" == *" $N "* ]] || continue
  S=$(cat "$CORPUS/_results/$M/$N/status.txt" 2>/dev/null)
  [ -z "$S" ] || { echo "skip $N ($S)"; continue; }
  echo "run $M $N"; bash "$HERE/corpus-efbulk-am.sh" one "$M" "$N" "$SLN" || [ $? -ne 255 ] || { echo "stop: run incomplete (H1, disk below threshold)"; exit 1; }
done
