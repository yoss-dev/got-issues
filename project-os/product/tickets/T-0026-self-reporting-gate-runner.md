---
id: T-0026
title: One command that runs every merge gate and reports its own exit codes
type: technical
status: backlog
priority: high
owner: none
implemented_by: none
accepted_by: none
depends_on: []
adrs: []
created: 2026-08-31
updated: 2026-08-31
---

# T-0026: One command that runs every merge gate and reports its own exit codes

## Problem / Context

From [RETRO-SPRINT-003](../../delivery/retrospectives/RETRO-SPRINT-003.md), actions 2 — findings D3
and D4. Two failures during SPRINT-003 share a cause that no amount of further guidance addresses.

**The exit-code rule exists, is well written, and was broken three times in one session by the agent
enforcing it.** [TESTING.md](../../standards/TESTING.md) says: *"read the exit status of the tool you
are checking, not of a pipeline it feeds. `dotnet format … | grep …` reports grep's status."* During
[T-0006](T-0006-issue-lifecycle-fields.md)'s completion, gate results were reported from `| grep`,
from `| tail`, and from `${PIPESTATUS[0]}` after an intervening `echo` — twice printing an **empty**
exit code that was nearly recorded as evidence. The rule was added by a previous retrospective and
read by the agent that broke it. Guidance has been tried.

**The gates are not read-only, and nothing says so.** `tools/check-drift.sh` deletes and regenerates
`libs/` by design. During the same session, `git add -A` ran while a backgrounded gate run was
mid-regeneration; the resulting `os:` commit deleted all 62 files of `libs/GotIssues.Client` — 9,496
lines — while its message described 32 lines of documentation (`4e261d9`, repaired by `b3242a4`). It
never reached `origin` because the drift gate caught it. The same collision makes the *gate's own
verdict* untrustworthy: that run reported `drift exit=1`, which may have been an artefact of the
tree moving underneath it rather than a finding. **A gate result taken while its subject was moving
is not evidence in either direction.**

Both are mechanical problems wearing the costume of discipline problems.

## Desired Outcome

One command runs every merge gate, reports each gate's own exit code unambiguously, and cannot
produce a green result from a repository that was changing while it ran.

## User / Business Value

No user-visible change. The value is that the merge gates become evidence instead of a ritual: a
result you can paste into a Work Log without a reader having to trust that the person running it
avoided four separate shell traps. It also removes a class of review finding — SPRINT-003 produced
one where an implementer cited a reviewer's measurements as their own, because re-running six gates
by hand is tedious enough to discourage it.

## Scope

### In Scope

- **`tools/gates.sh`** — runs the [GIT.md](../../standards/GIT.md) merge-gate list in order: build
  (0 warnings), `dotnet format --verify-no-changes`, `tools/check-drift.sh`,
  `tools/validate-project-os/validate.py`, `dotnet test`, `tools/smoke.sh`.
- **Each gate's exit code captured directly** from the process, never through a pipe, and printed
  in a table alongside the headline numbers (test counts, warning counts) so a Work Log entry can be
  copied from the output.
- **Quiescence enforcement**: refuse to start against a dirty working tree, record the `git status`
  and `HEAD` before and after, and **fail the run if either changed during it** — with a message
  saying the verdict is void rather than red.
- **A non-zero overall exit** if any gate failed, so the script itself obeys the rule it exists to
  enforce.
- A `--skip-smoke` (or equivalent) escape hatch for the fast inner loop, which must make its own
  output obviously partial — a partial run that looks complete is worse than no script.
- Wiring: referenced from [GIT.md](../../standards/GIT.md)'s gate list and from
  [`implement-ticket`](../../skills/implement-ticket/SKILL.md), [`review-code`](../../skills/review-code/SKILL.md)
  and [`acceptance-test`](../../skills/acceptance-test/SKILL.md) as the way to run the gates, via
  `evolve-governance`.

### Out of Scope

- Changing what any individual gate checks, or adding a gate. This ticket runs the existing six.
- CI. There is none ([GIT.md](../../standards/GIT.md): the trunk is protected by discipline), and
  adding one is a separate decision.
- Parallelising the gates. Sequential and trustworthy beats fast; the smoke tier alone is ~10
  minutes and dominates.
- Enforcing that agents use it. That is what the skill wiring is for; a script cannot compel.

## Acceptance Criteria

- [ ] AC1: Given all six gates pass, when `tools/gates.sh` runs, then it exits 0 and prints a table with one row per gate showing the gate's own exit code and its headline numbers.
- [ ] AC2: Given any one gate fails, when the script runs, then it exits non-zero, the table shows which gate failed with its real exit code, and the failure is not masked by a later passing gate.
- [ ] AC3: Given a gate is invoked in a way that would lose its status through a pipe, when the script's source is reviewed, then no gate's exit code is read from a pipeline. Demonstrated by mutation: introduce a piped invocation for one gate, make that gate fail, and confirm the script reports it green — then revert. **This is the defect the ticket exists for, so a run that has not been shown to catch it has not been shown to work.**
- [ ] AC4: Given a dirty working tree, when the script starts, then it refuses to run and says so, rather than producing a result.
- [ ] AC5: Given the tree changes while the script is running (simulate: touch a tracked file mid-run), then the run ends with a **void** verdict distinguishable from both pass and fail, naming what changed.
- [ ] AC6: Given the script's output, when it is pasted into a ticket Work Log, then it contains every figure a reader needs — exit codes, test counts, warning counts, the commit it ran against — without the runner adding anything by hand.
- [ ] AC7: Given [GIT.md](../../standards/GIT.md) and the three implementation skills, when they are read after this ticket, then they name this script as the way the gates are run, and no instruction still tells an agent to assemble the gate list by hand.

## Examples / Scenarios

- **The D3 case:** a gate is run as `dotnet test | grep Passed`. Today the reported status is grep's. Under AC3 the script cannot express that.
- **The D4 case:** a commit lands mid-run. Today the run reports `drift exit=1` and a reader cannot tell an artefact from a finding. Under AC5 the run is void and says why.
- **Counter-example — what must NOT happen:** the script prints a green table while one gate was skipped because a tool was missing. A missing tool is a failed gate, not an absent one.

## Technical Notes

The shell traps this ticket exists to eliminate are the ones it is most likely to contain. Notably:
`$?` after any intervening command is not the status you wanted; `set -e` interacts badly with
capturing failures deliberately; and in `zsh` the array is `pipestatus`, not `PIPESTATUS` — one of
the three SPRINT-003 incidents was exactly this. Prefer running each gate as a plain command with its
status captured immediately, redirecting output to a per-gate log file that the table references.

Quiescence can be checked cheaply with `git status --porcelain` plus `git rev-parse HEAD` before and
after, compared as strings. That is sufficient for the failure that actually occurred and does not
require watching the filesystem.

## Dependencies

None. Every gate it wraps already exists.

## Risks / Unknowns

- **A gate runner that is wrong is worse than no gate runner**, because its output is designed to be
  trusted and pasted. AC3 and AC5 are the criteria that matter; the rest is presentation.
- **The escape hatch is the likely failure mode.** `--skip-smoke` will be used, and if its output
  resembles a full run, the ticket has made things worse. Its output must be unmistakable.
- **`smoke.sh` takes ~10 minutes**, so a full run is not something an agent will do casually. This
  ticket does not fix that, and should not try to.

## Testing Notes

AC3 and AC5 are both mutations in the sense [TESTING.md](../../standards/TESTING.md) means: introduce
the defect the script exists to prevent, watch the script fail to be fooled, revert. Verifying the
script only against a passing repository demonstrates that it can print a table.

## Relevant ADRs & Documentation

- [TESTING.md](../../standards/TESTING.md) — the exit-code and attribution rules this makes mechanical
- [GIT.md](../../standards/GIT.md) — the merge-gate list being wrapped, and where AC7 lands
- [RETRO-SPRINT-003](../../delivery/retrospectives/RETRO-SPRINT-003.md) — findings D3 and D4, the evidence
- [T-0006](T-0006-issue-lifecycle-fields.md) — the Work Log containing all four incidents

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`.

---

## Work Log

### 2026-08-31 — Created from RETRO-SPRINT-003 action 2 (claude-sm-9d4e)

- **Did:** Created to hold the retro's second action. Scoped as a script plus its wiring, not as a rule.
- **Decided:** no further written guidance about exit codes. The rule already exists at [TESTING.md](../../standards/TESTING.md) line 72, names the exact failure mode, and was violated three times in one session by the agent enforcing it — which is the strongest available evidence that a third sentence would not work.
- **Remaining:** refinement. Sizing is probably small; AC3 and AC5 are where the real work is.
- **Open questions / blockers:** none.
- **Test state:** n/a — not started.
