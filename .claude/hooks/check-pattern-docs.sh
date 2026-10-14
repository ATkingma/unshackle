#!/usr/bin/env bash
# Every pattern class needs KingmaDoc docs (plan + verify) and a row in docs/projectinfo/README.md.
# Usage: check-pattern-docs.sh stop|subagent-stop|post-edit   (hook JSON on stdin)
set -u
mode="${1:-stop}"
root="$(cd "$(dirname "$0")/../.." && pwd)"
input="$(cat)"

if [ "$mode" = "post-edit" ]; then
    file="$(jq -r '.tool_input.file_path // ""' <<<"$input")"
    case "$file" in
        */src/Unshackle.Core/Fixes/*/Patterns/*Pattern.cs)
            jq -n '{hookSpecificOutput: {hookEventName: "PostToolUse", additionalContext:
                "Pattern class changed. Document it with KingmaDoc (see claude/docs/patroon-loop.md): docs/projectinfo/patterns/<id>-plan.md before building, <id>-modellen.md (class, sequence, decision tree) and <id>-verify.md afterwards, and a row in the pattern table of docs/projectinfo/README.md (<id> = Pattern.Id with . replaced by -). All documentation in English."}}'
            ;;
    esac
    exit 0
fi

# Never block twice in a row (prevents an endless stop loop).
[ "$(jq -r '.stop_hook_active // false' <<<"$input")" = "true" ] && exit 0

missing=()
for f in "$root"/src/Unshackle.Core/Fixes/*/Patterns/*Pattern.cs; do
    [ -e "$f" ] || continue
    id="$(sed -n 's/.*Id => "\([^"]*\)".*/\1/p' "$f" | head -n1)"
    [ -n "$id" ] || continue
    slug="${id//./-}"
    [ -f "$root/docs/projectinfo/patterns/$slug-plan.md" ] || missing+=("$id: plan missing (docs/projectinfo/patterns/$slug-plan.md)")
    [ -f "$root/docs/projectinfo/patterns/$slug-modellen.md" ] || missing+=("$id: models missing (docs/projectinfo/patterns/$slug-modellen.md)")
    [ -f "$root/docs/projectinfo/patterns/$slug-verify.md" ] || missing+=("$id: verify missing (docs/projectinfo/patterns/$slug-verify.md)")
    grep -q "patterns/$slug-plan.md" "$root/docs/projectinfo/README.md" 2>/dev/null || missing+=("$id: no row in docs/projectinfo/README.md")
done

[ ${#missing[@]} -eq 0 ] && exit 0
msg="Pattern documentation missing: $(printf '%s; ' "${missing[@]}")"

if [ "$mode" = "subagent-stop" ]; then
    # Loop agents must finish their own docs before they stop.
    jq -n --arg r "$msg Create them with KingmaDoc (claude/docs/patroon-loop.md) for the patterns you built." '{decision: "block", reason: $r}'
else
    # Main session: only warn; a running loop agent may still be writing these docs.
    jq -n --arg m "$msg" '{systemMessage: $m}'
fi
