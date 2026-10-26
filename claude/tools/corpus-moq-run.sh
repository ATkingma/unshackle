#!/bin/bash
# Moq corpus round (TAAK-307 row 14): base, scan, after for every repo of corpus-moq-list.txt, one repo at a time (CPU is shared).
#   TOOL=<published unshackle.dll> claude/tools/corpus-moq-run.sh [repo ...]   # no args: all repos in the list
HERE=$(cd "$(dirname "$0")" && pwd)
export RUNNER_FORCE=${RUNNER_FORCE:-} CORPUS=${CORPUS:?set CORPUS to the corpus-moq root on the corpus disk} FIX=moq JOBS=1 TOOL=${TOOL:?set TOOL}
LIST=$HERE/corpus-moq-list.txt
for N in ${@:-$(grep -vE '^\s*(#|$)' "$LIST" | cut -d' ' -f1)}; do
  SLN=$(grep -E "^$N " "$LIST" | cut -d' ' -f2-)
  for M in base scan after; do
    "$HERE/corpus-moq-am.sh" one $M "$N" "$SLN"
    S=$(cat "$CORPUS/_results/$M/$N/status.txt" 2>/dev/null)
    echo "$(date +%T) $N $M $S"
    [ "$M" = base ] && [ "$S" != ok ] && break   # no baseline, no comparison
  done
done
