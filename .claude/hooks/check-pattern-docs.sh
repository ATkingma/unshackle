#!/usr/bin/env bash
# Every pattern class needs KingmaDoc docs (plan + models + verify) and a row in docs/projectinfo/README.md.
# Usage: check-pattern-docs.sh stop|subagent-stop|post-edit|check   (hook JSON on stdin; check: exit 1 when docs miss)
# The logic lives in claude/tools/check_pattern_docs.py, so no jq is needed (TAAK-302).
set -u
root="$(cd "$(dirname "$0")/../.." && pwd)"
# python3 can be the Windows Store stub, so probe that the interpreter really runs.
for py in python3 python; do
    if "$py" -I -c '' >/dev/null 2>&1; then
        exec "$py" -I "$root/claude/tools/check_pattern_docs.py" "$@"
    fi
done
echo "check-pattern-docs: no working python3/python found" >&2
exit 2
