---
id: T-0016
title: Make the drift check see everything under libs/, including untracked files
type: technical
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0002]
adrs: [ADR-0004]
created: 2026-08-31
updated: 2026-08-31
---

# T-0016: Make the drift check see everything under `libs/`, including untracked files

## Problem / Context

Raised across T-0002's three review rounds by `claude-rev-8b4f` and deliberately deferred there as more than a review response should carry.

Three symptoms share one root cause. `tools/generate.sh` owns `libs/` by `rm -rf`, which means:

1. **Nothing hand-maintained can live inside it.** `.openapi-generator-ignore` — the generator's own mechanism for excluding files — is deleted on every run, so the proper lever is unusable and unwanted files are removed by `rm` and `find` in the script instead.
2. **The `.openapi-generator/FILES` manifest lists files the script then deletes**, so the generator's own record of its output disagrees with what is on disk.
3. **`tools/check-drift.sh` is blind to untracked files.** It runs `git diff` after regenerating, which reports modifications to tracked files and says nothing about new ones.

**Symptom 3 is the one with a merge gate downstream, and it is why this is a ticket rather than a tidy-up.** `check-drift.sh` is named as a merge gate in [GIT.md](../../standards/GIT.md) and is what makes the contract-first rule enforceable rather than aspirational ([ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)). Today that gate holds only because the generator happens to maintain the tracked `FILES` manifest, which changes when output changes — a property of a file **we do not control and did not design for**. During T-0002's review an additive mutation (a new path and schema) was caught only for that reason. If a generator upgrade stopped writing that manifest, the gate would keep reporting green while the specification and the committed code diverged.

A gate that passes for a reason nobody chose is the same defect this project has found repeatedly: a check that reads as proof of more than it proves.

## Desired Outcome

Regenerating and comparing detects **any** difference under `libs/` — modified, added, or removed — without depending on a manifest the generator maintains for its own purposes.

## User / Business Value

The contract-first guarantee is only as strong as the check that enforces it. This makes the enforcement independent of an upstream implementation detail.

## Scope

### In Scope

- Make the drift check detect untracked and deleted files as well as modified ones (for example `git status --porcelain` over `libs/`, or comparing against a freshly generated tree).
- Decide how generated output is owned, so that either `.openapi-generator-ignore` becomes usable or the script's `rm`/`find` steps become the deliberate, documented mechanism rather than a workaround.
- Reconcile `.openapi-generator/FILES` with what is actually kept, or stop committing it.
- Prove the improved check with mutations of **each** shape: modified, added, deleted.

### Out of Scope

- Changing what is generated, or the generator options ([T-0002](T-0002-contract-first-codegen-pipeline.md) settled those and they are load-bearing).
- CI enforcement — there is no CI (`PROJECT.md` Q6).
- The spec itself.

## Acceptance Criteria

- [ ] AC1: Given generated output modified without regenerating, when the drift check runs, then it fails — the behaviour that exists today, preserved.
- [ ] AC2: Given a **new** file added under `libs/` that generation would not produce, when the drift check runs, then it fails. It does not today.
- [ ] AC3: Given a generated file **deleted** from `libs/`, when the drift check runs, then it fails.
- [ ] AC4: Given the generator no longer wrote `.openapi-generator/FILES`, when the drift check runs against drifted output, then it still fails — the gate does not depend on that manifest.
- [ ] AC5: Given the repository, when generated-output ownership is inspected, then either `.openapi-generator-ignore` survives generation and is used, or the script's removals are documented as the deliberate mechanism with the reason.
- [ ] AC6: Each of AC1–AC4 is demonstrated by mutation — the check seen failing, then restored ([TESTING.md](../../standards/TESTING.md)).

## Examples / Scenarios

- Add `libs/GotIssues.Contracts/src/GotIssues.Contracts/Stowaway.cs`: the check fails. Today it passes.
- Delete a generated model: the check fails.
- Hand-edit a generated file: the check fails (already true).
- Simulate the manifest disappearing: the check still fails on drifted output.

## Dependencies

**T-0002** — the pipeline, the scripts, and the generated tree all originate there.

## Risks / Unknowns

- `git status --porcelain` reports files ignored by `.gitignore` differently from untracked ones; the check must not become noisy about build artefacts under `libs/*/bin` and `obj`, or it will be switched off — the exact failure this ticket exists to prevent.
- The generator may reintroduce files the script deletes; the check must distinguish "generation produced this" from "someone left this here".
- Deciding ownership may mean generating into a scratch directory and syncing, which is a larger change than the check itself.

## Testing Notes

AC6 is what keeps this honest: a drift check that has only ever been seen green proves nothing, which is precisely the finding that produced this ticket.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) — contract-first, and the drift check as its enforcement
- [GIT.md](../../standards/GIT.md) — the drift check as a merge gate
- [T-0002](T-0002-contract-first-codegen-pipeline.md) — where the scripts and the deferral came from

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`.

---

## Work Log

### 2026-08-31 — Software Engineer (claude-sm-9d4e)

- **Did:** Created to give T-0002's deferred generation-ownership residual an actual destination. The reviewer's point was that the Work Log said "whoever writes that ticket inherits the sharp version" and nobody had written it — a residual recorded only in a closed ticket's Work Log is not tracked work.
- **Decided:** framed around symptom 3 (the drift check's blindness) rather than the tidier-looking symptoms 1 and 2, because that is the one with a merge gate downstream. The reviewer's correction — that the gate currently holds only by accident of a manifest we do not control — is the reasoning, and is recorded in Problem / Context rather than left in a review transcript.
- **Remaining:** Refinement.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
