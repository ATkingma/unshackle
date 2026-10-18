#!/usr/bin/env bash
# Checks HEAD in a clean LF worktree before a push; exits non-zero on the first failing check (push only after exit 0).
# Usage: claude/tools/verify-head.sh <scratch-dir on a large disk, never /tmp>
set -euo pipefail
base="${1:?usage: verify-head.sh <scratch-dir>}"
mkdir -p "$base"
# A unique folder per run: two runs sharing one folder removed each other's worktree mid-test (TAAK-284).
dir="$(mktemp -d "$base/verify-head.XXXXXX")"
results="$dir-results"
repo="$(git rev-parse --show-toplevel)"
rmdir "$dir"; git -C "$repo" worktree prune
git -C "$repo" -c core.autocrlf=false worktree add -q --detach "$dir" HEAD
trap 'git -C "$repo" worktree remove --force "$dir"; rm -rf "$results"' EXIT
cd "$dir"
# Local machine paths must never reach the public repo (write <corpus>/... or repo-relative paths).
if git grep -nE '/(home|mnt)/[A-Za-z]' -- ':!claude/tools/verify-head.sh'; then echo "local paths found"; exit 1; fi
dotnet build -v q
if ! dotnet test -- --max-threads 4 --report-xunit-trx --results-directory "$results"; then
  # Name the failing tests (a flaky one has failed under load before, TAAK-284).
  grep -ho 'testName="[^"]*" [^>]*outcome="Failed"' "$results"/*.trx | sed 's/ .*//' || true
  exit 1
fi
dotnet test --solution samples/SampleShop/SampleShop.slnx
echo '{}' | bash .claude/hooks/check-pattern-docs.sh subagent-stop | grep -q . && { echo "pattern docs missing"; exit 1; }
echo "VERIFY OK: $(git rev-parse --short HEAD)"
