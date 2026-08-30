---
name: plan-sprint
description: Close out the previous sprint file if needed, set a sprint goal, and commit ready backlog work into delivery/CURRENT_SPRINT.md based on priority, dependencies, risk, and capacity.
---

# Skill: plan-sprint

## Purpose

Start a sprint with one clear goal and a realistic, dependency-clean commitment of Ready work, recorded in the authoritative sprint file.

## When to Use

- No active sprint exists and there is Ready work.
- The previous sprint ended (retro done, or explicitly skipped — note that for the retro).

## Active Persona(s)

Product Owner (goal, selection) with Scrum Master (process, realism) and Software Engineer (feasibility, dependency sanity) perspectives.

## Inputs

- Optional: human-provided sprint goal or capacity notes. If a human PO is reachable, the proposed goal SHOULD be confirmed with them; if not, proceed and flag the goal as agent-proposed in Notes.

## Preconditions

- `CURRENT_SPRINT.md` shows no active sprint (previous one archived). If an unarchived finished sprint is found, archive it first (see Procedure step 1).
- At least one ticket with `status: ready`.

## Context to Load

1. `delivery/CURRENT_SPRINT.md` and, if closing out, `templates/SPRINT_TEMPLATE.md`
2. `product/BACKLOG.md` + the ticket files of the top Ready candidates
3. `PROJECT.md` (constraints, open questions that gate work)
4. Latest retro in `delivery/retrospectives/` (capacity signals, committed process changes)
5. `governance/DEFINITION_OF_READY.md`

## Procedure

1. **Close out if needed:** if `CURRENT_SPRINT.md` still holds a finished sprint, copy it verbatim to `delivery/sprints/SPRINT-NNN.md`; return unfinished `committed` tickets to `ready` (Work Logs preserved) and note this for the retrospective.
2. **Set the sprint goal:** one outcome, derived from backlog priority and vision. Not a list of tickets — the sentence that arbitrates mid-sprint decisions.
3. **Select work,** walking the backlog top-down, admitting a ticket only if: it is `ready` (spot-check DoR — don't re-refine, but reject stale readiness); it serves or at least doesn't crowd out the goal; its `depends_on` are `done` or also selected *and sequenced earlier*; external dependencies (credentials, humans, third parties) are actually available.
4. **Respect capacity:** use the last 1–3 archived sprints' throughput as the default capacity; without history, commit conservatively (the retro corrects). Prefer finishing fewer tickets over starting many. Note high-risk selections in the sprint Notes.
5. **Write `CURRENT_SPRINT.md`** from the template: number (increment), goal, dates (or continuous-flow note), Committed Work table, empty Blockers / Discovered Work / Notes sections seeded per template.
6. **Update each selected ticket:** `status: committed`, `updated` date; mirror in `BACKLOG.md`.
7. If a human requested specific work that is not Ready: do NOT commit it; record it in Notes with what's missing, and (optionally) run `refine-ticket` on it first.

## Validation

- Exactly one goal; every committed ticket is `ready`-verified; no committed ticket depends on an uncommitted, un-done ticket; sprint file, ticket files, and backlog index agree.

## Outputs

Active `CURRENT_SPRINT.md`; archived previous sprint (if applicable); updated tickets and backlog index.

## State Changes

May modify: `delivery/CURRENT_SPRINT.md`, `delivery/sprints/` (archive), committed ticket files, `product/BACKLOG.md`.

## Failure / Escalation

- Nothing is Ready → do not plan a sprint of un-ready work; run/request refinement and report.
- Goal requires materially changing prior commitments or conflicts with stakeholder input → escalate to human PO (WoW §13).

## Example

SPRINT-004, goal: "A user can find any past invoice without contacting support." Committed: T-0031 (date filtering, ready), T-0033 (invoice list pagination fix, ready, sequenced first because T-0031 depends on it), T-0028 (unrelated logging chore, small, doesn't crowd the goal). T-0035 (full-text search) stays back: `backlog`, open UX question recorded in refinement.
