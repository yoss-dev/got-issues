#!/usr/bin/env bash
#
# The Compose stack smoke check (T-0015).
#
# This is a separate tier on purpose. It builds images and starts real containers, so it
# is minutes rather than seconds, and TESTING.md requires the habitual suite to stay
# fast. `dotnet test` at the repository root does not run it — apps/GotIssues.SmokeTests
# is deliberately absent from GotIssues.slnx — and this script is the only supported way
# to run it.
#
# Usage:
#   tools/smoke.sh              # run the whole check
#   tools/smoke.sh --build-only # compile it without starting anything
#
# Requires Docker. Every stack it starts uses its own project name and ephemeral host
# ports, so it cannot collide with a stack you are already running.

set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$root/apps/GotIssues.SmokeTests/GotIssues.SmokeTests.csproj"

if ! docker info >/dev/null 2>&1; then
  echo "smoke: Docker is not available. This check drives the real Compose stack and cannot run without it." >&2
  exit 2
fi

if [[ "${1:-}" == "--build-only" ]]; then
  # Nothing else compiles this project — it is outside the solution — so this mode
  # exists to catch it rotting without paying for a full stack run.
  exec dotnet build "$project"
fi

echo "smoke: this builds images and starts containers; first run takes several minutes."
exec dotnet test "$project" ${@+"$@"}
