---
name: run-sprint
description: Orchestrate the delivery loop - process the current sprint's tickets end to end, delegating review and acceptance to independent sessions, until the sprint drains or every remaining item awaits a human decision.
---

# Skill: run-sprint

## Purpose

Drive committed work to Done without a human hand-cranking each step, while stopping — precisely and only — where the framework requires a human. The loop invokes the per-activity skills; it never replaces them or relaxes their gates.

## When to Use

An active sprint exists with eligible work and a human has asked for autonomous execution ("drain the sprint", "work through the sprint"). Not for planning (run `plan-sprint` first) and not a license to add work — the loop only processes what is committed.

## Active Persona(s)

Scrum Master (orchestration, blocker routing). Each delegated activity runs under its own skill's persona in its own session.

## Inputs

- Optional: a stop-after condition ("stop after T-0007", "one ticket only", a time budget).

## Preconditions

- `delivery/CURRENT_SPRINT.md` holds an active sprint; the validator passes.
- The harness can provide **independent sessions** (subagents / parallel sessions with distinct identities per `standards/GIT.md`) for review and acceptance. If it cannot, the loop still runs but MUST stop at each handover and report which role is needed — degraded mode, stated up front in the run report.

## Context to Load

1. `delivery/CURRENT_SPRINT.md`
2. `governance/WAY_OF_WORKING.md` (§7 execution, §13 escalation)
3. `standards/GIT.md` (lanes, identity, solo mode)
4. Each invoked skill loads its own context — the orchestrator does not pre-load ticket internals.

## Procedure

1. **Select** the next eligible ticket exactly as `pick-up-ticket` defines (committed, unowned, dependencies done, preferring tickets that unblock others). Then per ticket, invoke the chain:
   `pick-up-ticket` → `implement-ticket` → `review-code` *(independent session)* → `acceptance-test` *(independent session, never the implementer's)* → `complete-ticket`.
2. **On a human-shaped obstacle** (WoW §13 condition, unresolvable ambiguity, missing credentials, contested ADR): record it properly (ticket Work Log + sprint Blockers, escalation format), set the ticket `blocked` — **and keep going with the next eligible ticket**. The loop parks work; it never invents answers, weakens criteria, or reorders scope to stay busy.
3. **Failure-loop guard:** a ticket that fails acceptance twice, or bounces review three times, goes `blocked` with a "needs human attention — repeated failure" note and the evidence. Grinding the same ticket indefinitely is worse than stopping.
4. **Hygiene between tickets:** run the validator; push per `GIT.md` (solo mode: commit only). If the validator goes red, fix the state before selecting the next ticket — a red validator is itself a stop-the-line event if the fix isn't obvious.
5. **Stop** when any of these holds:
   - every committed ticket is `done` or `dropped` → the sprint is drained;
   - no ticket is eligible and at least one is `blocked` awaiting a human;
   - the human's stop-after condition is met;
   - a governance conflict, destructive operation, or anything else in WoW §13 applies to the loop itself.
6. **Exit report** — written into the sprint's Notes (the repo record) and surfaced to the human:
   - completed tickets with one-line evidence pointers;
   - every pending decision in the WoW §13 escalation format (issue, why undecidable, options, tradeoffs, recommended default) — batched so the human answers once, not per interruption;
   - discovered work created, defects deferred, validator status;
   - if drained: the suggestion to run `retrospective` (never auto-run — closing a sprint is a human-visible act).

## Validation

- No ticket skipped a gate: every `done` ticket shows review verdict, independent acceptance (`accepted_by ≠ implemented_by`), and a DoD walk.
- Every stop is accounted for: `blocked` tickets carry recorded escalations; nothing is silently abandoned mid-state.
- Validator green at exit (or its findings are themselves in the report).

## Outputs

Progressed sprint state; a batch of resolved tickets; a single consolidated decision digest for the human.

## State Changes

Only through the invoked skills — this skill adds nothing beyond the sprint Notes exit report. It MUST NOT: add work to the sprint, modify governance, run `plan-sprint` or `retrospective`, or touch acceptance criteria.

## Failure / Escalation

- Harness cannot spawn an independent session mid-loop → park the ticket at its handover state with a note, continue others, report the role gap.
- Two orchestrators colliding is safe (claims race per `GIT.md`) but wasteful in solo mode — solo mode runs exactly one.

## Example

"Drain SPRINT-001." The orchestrator claims T-0002 (CI wiring), drives it through review (session `claude-eng-9c1b`) and acceptance (`claude-qa-b81d`) to done; claims T-0003 (scaffolding), hits the undecided app-server port-binding question → records the escalation, parks it `blocked`, moves on; T-0005 fails acceptance twice on a flaky generated-client test → `blocked`, "repeated failure" note. No eligible work remains → exit report: 1 done, 2 blocked with batched decisions (port binding: options + recommended default; flaky test: quarantine-with-ticket vs. regenerate, recommendation attached), validator green. The human answers both in the ticket Work Logs; the next `run-sprint` resumes from repository state alone.
