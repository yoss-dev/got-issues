---
name: implement-ticket
description: Implement a claimed ticket within its scope and the project's standards, keeping the Work Log resumable, and hand over to acceptance only when engineering verification fully passes.
---

# Skill: implement-ticket

## Purpose

Build exactly what the ticket asks — to standard, with tests, without scope drift or invented requirements — and leave a trail any successor agent can pick up mid-stream.

## When to Use

After `pick-up-ticket` has claimed the ticket and recorded an implementation plan. Also when *resuming* another agent's `in-progress` ticket whose owner has been explicitly released (see Preconditions).

## Active Persona(s)

Software Engineer; consult the Architect persona (usually the same agent, hat switched, or an escalation) when an architectural question appears.

## Inputs

- The claimed ticket (`in-progress`, `owner:` set to you — or handed to you by a human/owner-release recorded in the Work Log; update `owner` to yourself first, in a pushed commit).

## Preconditions

- Ticket is `in-progress` and owned by you; implementation plan exists in the Work Log.
- Resuming someone else's work without a recorded release is forbidden (duplicate-work guard).

## Context to Load

1. The ticket (including full Work Log — mandatory when resuming)
2. Everything `pick-up-ticket` lists (already loaded if you claimed; reload what you lack)
3. The code you are changing

## Procedure

1. Create the ticket branch (`t-NNNN-slug`) **in its own worktree** (`git worktree add`, per `standards/GIT.md` Working copies — the primary checkout stays on the trunk for process commits; never branch-switch a single checkout). Work the plan there in small, coherent, ticket-referencing commits (`T-NNNN: …`), tests alongside code per `standards/TESTING.md`. Work Log updates may ride the branch; `status`/`owner` changes are trunk-only and never do.
2. **Scope discipline:** build only In Scope; satisfy every acceptance criterion; touch nothing Out of Scope. Necessary out-of-scope discoveries → `create-ticket` + record per WoW §7; fold in only what is inseparable from the criteria, saying so in the Work Log.
3. **Ambiguity or architecture encountered:**
   - Requirement genuinely ambiguous after re-reading ticket + ADRs + vision → STOP on that thread; record the question and options in the Work Log; escalate per WoW §13 if it blocks the ticket. **Never invent the requirement.**
   - Decision meeting the [ADR bar](../../architecture/adr/README.md) → run `create-adr` before building on the decision.
   - Blocked externally → `status: blocked`, blocker recorded in ticket + sprint Blockers section; pick other work via `pick-up-ticket`.
4. **Keep the Work Log resumable:** update at every significant decision, before any pause, and at least at each session end (template's session-entry format). The bar: a stranger continues without asking you anything.
5. **Engineering verification (before handover):** all acceptance criteria self-checked against the *running* code/tests, not intentions; full relevant suite green (record the exact command and result); lint/static analysis clean; self-review of the diff for scope creep, debug leftovers, standards violations, undocumented decisions; docs updated per `standards/DOCUMENTATION.md`.
   - **Mutate the claims that carry weight, not every claim** (`standards/TESTING.md`): one where a test is the only evidence for something another ticket's DoD depends on, and any claim a reviewer challenges. Not required where a compiler, constraint or framework already enforces the property — record the enforcement instead. One mutant per claim, cheapest tier that can host it, and no re-mutating a claim whose code has not changed shape.
   - **Verification against a running service must be attributable** to the process under test, and tool exit codes read from the tool rather than a pipeline (`standards/TESTING.md`).
   - **A mutant only counts if it reaches the assertion** — the build accepting it is necessary and not sufficient, and a red suite is not proof the mutant caused it. Confirm the thing you changed is the thing that caused the result (`standards/TESTING.md`).
   - **Run every gate in the working copy under test.** The ticket's worktree is not the trunk checkout; a gate run in the wrong one describes the wrong tree.
   - **Hold the test infrastructure to the same standards as the code it checks** — including reading the result of its own teardown (`standards/TESTING.md`).
6. **Hand over,** in the fixed sequence from `standards/GIT.md`: final Work Log entry on the branch (what was built, decisions, test evidence, the branch/PR reference, anything QA should probe) → open the PR → independent review via `review-code` (note the deviation in the Work Log if no independent reviewer is available) → merge → then, directly on the trunk, set `status: in-acceptance`, `implemented_by: <your id>`, clear `owner` to `none` (the QA persona must be a fresh session — WoW §2), update the sprint table (`os: T-NNNN in-acceptance`); push, then remove the ticket's worktree (`git worktree remove`) and delete the branch.

## Validation

- Every acceptance criterion has a passing self-check recorded with evidence (test name / command output), not just claimed.
- No unrecorded scope change; no criterion changed (that would be a PO decision, WoW §7).
- The diff is reviewable: coherent commits, no unrelated changes.

## Outputs

Implemented, self-verified change-set; resumable Work Log; ticket in `in-acceptance`.

## State Changes

May modify: product code and tests, the ticket file, `delivery/CURRENT_SPRINT.md` (status/blockers/discovered-work), new tickets via `create-ticket`, ADRs via `create-adr`, docs per standards. MUST NOT modify: acceptance criteria, DoR/DoD, other agents' in-progress tickets.

## Failure / Escalation

- Cannot satisfy a criterion as written (contradicts an ADR, a constraint, or reality) → precedence conflict: record it, escalate; do not ship a "close enough" variant.
- Time/complexity explodes beyond the DoR sizing → record honestly; propose a split to the PO persona rather than grinding a giant ticket to a half-done state.

## Example

Implementing T-0031, the agent discovers invoices have no index on `issued_at`, making date filtering slow at scale. Adding an index is inseparable from AC3 ("results within 2 s for 10k invoices") — done within the ticket, noted in the Work Log. A tempting bonus ("also filter by amount") is NOT built; it's already out of scope and lives in T-0035. Suite green (`pytest` output recorded), status → `in-acceptance`, owner cleared.
