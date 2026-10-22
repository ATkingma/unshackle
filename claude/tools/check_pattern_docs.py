#!/usr/bin/env python3
"""Every pattern class needs KingmaDoc docs (plan + models + verify) and a row in docs/projectinfo/README.md.

Python port of the former jq-based hook (TAAK-302), so it runs on machines without jq.

  check_pattern_docs.py stop|subagent-stop|post-edit   hook mode: hook JSON on stdin, hook JSON on stdout
  check_pattern_docs.py check                          verify mode: lists missing docs, exit 1 when any
"""
import glob
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
PATTERN_GLOB = os.path.join(ROOT, "src", "Unshackle.Core", "Fixes", "*", "Patterns", "*Pattern.cs")
POST_EDIT_RE = re.compile(r".*/src/Unshackle\.Core/Fixes/[^/]*/Patterns/[^/]*Pattern\.cs$")
ID_RE = re.compile(r'Id => "([^"]*)"')
POST_EDIT_TEXT = (
    "Pattern class changed. Document it with KingmaDoc (see claude/docs/patroon-loop.md): "
    "docs/projectinfo/patterns/<id>-plan.md before building, <id>-modellen.md (class, sequence, decision tree) "
    "and <id>-verify.md afterwards, and a row in the pattern table of docs/projectinfo/README.md "
    "(<id> = Pattern.Id with . replaced by -). All documentation in English."
)


def missing_docs():
    try:
        with open(os.path.join(ROOT, "docs", "projectinfo", "README.md"), encoding="utf-8") as f:
            readme = f.read()
    except OSError:
        readme = ""
    missing = []
    for path in sorted(glob.glob(PATTERN_GLOB)):
        with open(path, encoding="utf-8-sig") as f:
            match = ID_RE.search(f.read())
        if not match or not match.group(1):
            continue
        pid = match.group(1)
        slug = pid.replace(".", "-")
        for suffix, label in (("plan", "plan"), ("modellen", "models"), ("verify", "verify")):
            rel = f"docs/projectinfo/patterns/{slug}-{suffix}.md"
            if not os.path.isfile(os.path.join(ROOT, rel)):
                missing.append(f"{pid}: {label} missing ({rel})")
        if f"patterns/{slug}-plan.md" not in readme:
            missing.append(f"{pid}: no row in docs/projectinfo/README.md")
    return missing


def read_input():
    text = sys.stdin.read()
    try:
        data = json.loads(text) if text.strip() else {}
    except json.JSONDecodeError:
        data = {}
    return data if isinstance(data, dict) else {}


def main(argv):
    mode = argv[1] if len(argv) > 1 else "stop"
    if mode == "check":
        missing = missing_docs()
        for line in missing:
            print(line)
        return 1 if missing else 0

    data = read_input()
    if mode == "post-edit":
        tool_input = data.get("tool_input") or {}
        file_path = str(tool_input.get("file_path") or "").replace("\\", "/")
        if POST_EDIT_RE.match(file_path):
            print(json.dumps({"hookSpecificOutput": {"hookEventName": "PostToolUse", "additionalContext": POST_EDIT_TEXT}}))
        return 0

    # Never block twice in a row (prevents an endless stop loop).
    if data.get("stop_hook_active") is True:
        return 0
    missing = missing_docs()
    if not missing:
        return 0
    msg = "Pattern documentation missing: " + "".join(f"{m}; " for m in missing)
    if mode == "subagent-stop":
        # Loop agents must finish their own docs before they stop.
        reason = msg + " Create them with KingmaDoc (claude/docs/patroon-loop.md) for the patterns you built."
        print(json.dumps({"decision": "block", "reason": reason}))
    else:
        # Main session: only warn; a running loop agent may still be writing these docs.
        print(json.dumps({"systemMessage": msg}))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
