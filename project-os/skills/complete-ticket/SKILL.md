---
name: complete-ticket
description: Final gate to Done - verify the full Definition of Done atop a passed acceptance, then update ticket, sprint, backlog, and follow-up state consistently.
---

# Skill: complete-ticket

## Purpose

Decide whether an accepted ticket genuinely meets the Definition of Done, and make the repository state reflect completion everywhere it must — or send the work back with reasons.

## When to Use

Immediately after `acceptance-test` records a Pass verdict. Usually the same QA session continues into this skill.

## Active Persona(s)

QA / Test Engineer (verification) with Scrum Master perspective (state hygiene).

## Inputs

- Ticket ID with a recorded acceptance Pass.

## Preconditions

- `status: in-acceptance` and the latest Work Log entry is a QA Pass verdict with evidence. No Pass verdict → run `acceptance-test` first; this skill never substitutes for it.

## Context to Load

1. The ticket (full)
2. `governance/DEFINITION_OF_DONE.md`
3. `delivery/CURRENT_SPRINT.md`, `product/BACKLOG.md`
4. `architecture/adr/README.md` (if the Work Log mentions decisions)

## Procedure

1. **Walk the DoD literally** — universal items 1–8, then each conditional item, first deciding applicability ("touches persistent data? then Migrations applies"). Acceptance already covered behavior; focus on what remains: Work Log completeness, deferred-defect tickets existing and PO-acknowledged, ADR Accepted (not just Proposed) where required, docs, state consistency.
2. **Spikes:** verify the spike DoD instead (question answered/time box honored, findings written, follow-ups created and linked).
3. **Any DoD item fails** → the ticket is not Done: record precisely which item and why in the Work Log; `status: in-progress`, owner cleared, sprint table updated. Do not negotiate the DoD down — a deviation is a recorded human/PO decision, linked in the Work Log.
4. **All items pass** → finalize, in one commit:
   - ticket: `status: done`, `accepted_by: <your id>` (MUST differ from `implemented_by` — if they match, stop: independence was violated, and a genuinely independent acceptance must run first), tick the DoD checkbox, `updated`, closing Work Log entry (DoD confirmed, date, verifier id);
   - sprint table: `done`;
   - backlog: move the row to Completed (outcome `done`, finish date);
   - follow-ups: every deferred item / discovered-work note points at a real ticket ID;
   - unblocking: scan sprint + backlog for tickets whose `depends_on` includes this one; note in the sprint (and remove satisfied "Blocked by" cells) that they are now eligible.

## Validation

- Ticket file, sprint table, and backlog index all agree on `done`; no dangling references ("will file a ticket" without an ID); DoD walk recorded item-by-item where non-obvious, not as a bare "DoD ok".

## Outputs

A ticket verifiably Done, with consistent state everywhere, and newly unblocked work made visible.

## State Changes

May modify: the ticket, `delivery/CURRENT_SPRINT.md`, `product/BACKLOG.md`, "Blocked by" cells of dependent tickets' sprint rows. MUST NOT modify: code, criteria, DoD.

## Failure / Escalation

- DoD item cannot be verified from repository state (e.g., "deployed cleanly" with no pipeline evidence) → treat as failing; request the evidence or escalate. Unverifiable ≠ passed.

## Example

T-0031 passed acceptance after the 422 fix. DoD walk: universal 1–8 pass (Work Log shows decisions + test evidence); conditionals — regression test applies? no (feature, not bug-fix); ADR? none needed (Work Log confirms); migrations? yes, index migration present and tested; docs? API reference updated. Finalize commit: ticket `done`, sprint row `done`, backlog row moved to Completed, sprint note "T-0035 no longer blocked by T-0031".
