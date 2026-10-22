#!/bin/bash
# L4 helper: library corpus round on Linux, a thin wrapper over corpus-am.sh.
# Usage: corpus-epplus-run.sh <base|scan|after> [list]   (default list: corpus-epplus-list.txt)
# Env: CORPUS (corpus root, never /tmp), TOOL (published unshackle.dll), FIX (default epplus; masstransit uses scan).
HERE=$(cd "$(dirname "$0")" && pwd)
export CORPUS=${CORPUS:?set CORPUS} TOOL=${TOOL:?set TOOL} FIX=${FIX:-epplus}
export NUGET_PACKAGES=${NUGET_PACKAGES:-$CORPUS/../nuget-packages}
exec bash "$HERE/corpus-am.sh" list "$1" "${2:-$HERE/corpus-epplus-list.txt}"
