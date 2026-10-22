#!/bin/bash
# Sequential Polly corpus driver (one repo at a time, skips finished results).
#   CORPUS=<root>/corpus-polly TOOL=<unshackle.dll> corpus-polly-run.sh <base|scan|after> [repo...]
# Repos and solutions come from corpus-polly-list.txt ("name sln"); FIX=polly. Wraps corpus-am.sh one.
HERE=$(cd "$(dirname "$0")" && pwd)
M=${1:?mode}; shift
export FIX=polly CORPUS=${CORPUS:?}
ONLY=" $* "
grep -vE '^\s*(#|$)' "$HERE/corpus-polly-list.txt" | while read -r N SLN; do
  SLN=${SLN//\"/}
  [ -z "$*" ] || [[ "$ONLY" == *" $N "* ]] || continue
  S=$(cat "$CORPUS/_results/$M/$N/status.txt" 2>/dev/null)
  [ -z "$S" ] || { echo "skip $N ($S)"; continue; }
  echo "run $M $N"; bash "$HERE/corpus-am.sh" one "$M" "$N" "$SLN"
done
