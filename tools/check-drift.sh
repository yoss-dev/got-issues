#!/usr/bin/env bash
#
# Fails if the committed generated code does not match spec/openapi.yaml.
#
# This is a merge gate (standards/GIT.md). It is what makes the contract-first
# rule real rather than aspirational: without it, "regenerate after editing the
# spec" is a discipline nothing enforces.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "${REPO_ROOT}"

if ! git diff --quiet -- libs/ || ! git diff --cached --quiet -- libs/; then
  echo "check-drift: refusing to run — libs/ has uncommitted changes." >&2
  echo "Commit or stash them first; otherwise this check cannot tell your edits" >&2
  echo "from generator output." >&2
  exit 2
fi

./tools/generate.sh > /dev/null

if git diff --quiet -- libs/; then
  echo "check-drift: OK — generated code matches spec/openapi.yaml."
  exit 0
fi

echo "check-drift: DRIFT — spec/openapi.yaml and the committed generated code disagree." >&2
echo "" >&2
git --no-pager diff --stat -- libs/ >&2
echo "" >&2
echo "The specification changed without regenerating, or generated code was" >&2
echo "hand-edited. Run ./tools/generate.sh and commit the result alongside the" >&2
echo "spec change (ADR-0004)." >&2
exit 1
