---
name: retrospective
description: Close a sprint with an evidence-based review - archive the sprint, analyze what the repository shows actually happened, and produce owned improvement actions that land as tickets, governance proposals, or skill changes.
---

# Skill: retrospective

## Purpose

Turn delivery history into system improvement: find what worked, what caused friction, and what recurs — grounded in repository evidence — and convert findings into owned, landing-place-specific actions.

## When to Use

At sprint end (goal met, or dates elapsed), before the next `plan-sprint`. Also mid-sprint after a severe process failure (incident-style mini-retro, same format).

## Active Persona(s)

Scrum Master (facilitation and honesty about process), drawing on all persona perspectives.

## Inputs

- The finished sprint in `delivery/CURRENT_SPRINT.md`.

## Preconditions

- Sprint work has stopped: every committed ticket is `done`, `dropped`, or explicitly returning to backlog. In-flight tickets are either driven to a handover state first or returned to `ready` with resumable Work Logs.

## Context to Load

1. `delivery/CURRENT_SPRINT.md` (final state)
2. Every ticket touched this sprint — **especially Work Logs** (decisions, escalations, rework) and acceptance verdicts
3. The previous retro (`delivery/retrospectives/`) — its actions are reviewed first
4. `templates/RETROSPECTIVE_TEMPLATE.md`
5. `product/BACKLOG.md` (flow: what became ready/done/dropped)

## Procedure

1. **Archive the sprint:** copy `CURRENT_SPRINT.md` verbatim to `delivery/sprints/SPRINT-NNN.md`; return unfinished tickets to `ready` (status + backlog mirror), owners cleared, Work Logs already resumable per WoW §8.
2. **Review the previous retro's actions** honestly: done / in progress / dropped-with-reason. Repeatedly dropped actions are themselves a finding.
3. **Mine the evidence** (not memory, not vibes): goal achieved? why/why not; per-ticket friction signals — acceptance failures and their causes, `blocked` episodes and durations, discovered work volume, escalations and their latency, criteria that turned out ambiguous, DoR/DoD items that caught real problems vs. pure ceremony this sprint; recurring defect patterns across bug tickets; skills/templates that agents visibly fought or worked around (Work Log traces).
4. **Distinguish** implementation issues (belong in tickets) from *system* issues (process, governance, skills, tooling, missing automation) — the retro exists for the latter.
5. **Write the retro** from the template into `delivery/retrospectives/RETRO-SPRINT-NNN.md`, citing tickets for every claim.
6. **Convert findings to actions**, each with an owner and a landing place: tooling/automation/code → ticket via `create-ticket`; rule/template/skill change → `evolve-governance` proposal (the retro entry doubles as its record — and note whether it is project-agnostic and worth upstreaming per `FOUNDATION.md`); knowledge gap → glossary/docs ticket. Fewer, finished actions beat a long aspirational list — 3 owned actions is a good retro.
7. **Reset `CURRENT_SPRINT.md`** to its "no active sprint" state (pointing at `plan-sprint`), bumping the next-sprint number.

## Validation

- Sprint archived byte-for-byte before reset; every observation cites evidence; every action has owner + landing place; previous actions accounted for; no in-flight ticket left ownerless in a non-resumable state.

## Outputs

Archived sprint; retro document; created tickets and governance proposals; reset sprint file.

## State Changes

May modify: `delivery/sprints/`, `delivery/retrospectives/`, `delivery/CURRENT_SPRINT.md`, ticket statuses (unfinished → `ready`), `product/BACKLOG.md`, tickets via `create-ticket`; governance only via `evolve-governance`.

## Failure / Escalation

- The same systemic issue appears in a third consecutive retro despite actions → escalate to a human with the pattern and evidence; the improvement loop itself is failing (WoW §15).

## Example

RETRO-SPRINT-004: goal partially met (T-0031 done, T-0035 returned — blocked 4 days on a UX question). Evidence: 2 acceptance failures, both invalid-input handling missed in refinement. Actions: (1) add an invalid-input prompt to `refine-ticket` step 2 — `evolve-governance`, owner SM, human-approved; (2) T-0038 "seed anonymized invoice fixtures" — owner ENG; (3) escalation latency on UX questions raised to the human PO with options (dedicated design column vs. weekly design review).
