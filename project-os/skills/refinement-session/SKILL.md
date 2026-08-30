---
name: refinement-session
description: Facilitate an interactive backlog refinement session - rank candidate tickets by impact and sequential relevance (or criteria the human requests), let the human select, then refine the selection one ticket at a time with the human available for live answers.
---

# Skill: refinement-session

## Purpose

Turn refinement from a per-ticket chore into a working session: propose what most deserves refinement and in what order, let the human decide, then drive each selected ticket through a full `refine-ticket` — exploiting the one advantage a session has over solo refinement: the Product Owner is *present*, so business questions get answered and recorded now instead of parking tickets.

## When to Use

- The backlog has accumulated unrefined tickets and a sprint is approaching.
- A human says "let's refine", "prep the backlog", "refinement session".

Not for refining a single named ticket (`refine-ticket` directly) and not autonomous — this skill is interactive by design; without a human present it degrades to running `refine-ticket` on the top candidates and parking every question, which should be said out loud before proceeding.

## Active Persona(s)

Product Owner (ranking, selection guidance, live answers — when the human is the PO, the agent presents and the human decides) with Scrum Master facilitation. Each individual refinement runs under `refine-ticket`'s personas.

## Inputs

- Optional: ranking criteria other than the default ("rank by risk", "oldest first", "quick wins", "only bugs"); a candidate cap; a time budget.

## Preconditions

- At least one ticket with `status: backlog` (or `ready` tickets flagged stale by new information — include them, marked as re-refinements).

## Context to Load

1. `product/BACKLOG.md` (order, statuses, dependencies)
2. Candidate ticket files — headers and frontmatter first; full read only for tickets that get selected
3. `product/PRODUCT_VISION.md` (impact judgment)
4. `delivery/CURRENT_SPRINT.md` and the latest retro (what's coming, what hurt)
5. `governance/DEFINITION_OF_READY.md`

## Procedure

1. **Build the ranked candidate list.** Default ranking blends:
   - **Impact** — backlog priority/order, value statement strength, service to the vision and the likely next sprint goal;
   - **Sequential relevance** — tickets that block others rank above the tickets they block; dependency chains surface in workable order; tickets whose refinement would answer questions other tickets share rank higher.
   If the human named other criteria, rank by those instead and say so. Present a compact table: rank, ID, title, type, and a one-line *why this rank* (value signal + dependency note + staleness). Do not pad the list — 5–10 candidates beats the whole backlog.
2. **Let the human select.** Offer the ranking as a recommendation, never a fait accompli: they may reorder, drop, add ("also T-0019"), cap ("top 3"), or redirect the criteria. Their ranking wins — prioritization is PO authority, and in this session the human *is* the PO. If their selection materially changes backlog order, update `BACKLOG.md` with a changelog line.
3. **Refine one at a time.** For each selected ticket, in the agreed order, run the full [`refine-ticket`](../refine-ticket/SKILL.md) procedure — no abbreviated passes because it's a batch. After each ticket, report the verdict in one breath (`ready`, or `not ready — because X`) plus anything spawned (splits, spikes, ADR proposals).
4. **Use the human while you have them.** Questions that `refine-ticket` would park as escalations get asked *now*, batched per ticket, answered live, and transcribed verbatim (attributed, dated) into the ticket before moving on — the WoW §13 answer rule applied in real time. Questions the human can't answer in-session are recorded as normal open questions; no fishing for on-the-spot guesses on decisions that deserve thought.
5. **Respect the session's shape.** Check in at natural breaks ("two refined, three to go — continue?"). If the human leaves mid-session, finish the current ticket, then stop and report rather than continuing without them (see When to Use).
6. **Exit summary** — to the human and, in one line each, to the sprint Notes (or backlog changelog when no sprint is active): tickets now `ready`; tickets still `backlog` with their blocking reasons; questions answered (and where recorded); artifacts spawned; and whether enough is `ready` to suggest `plan-sprint`.

## Validation

- Every refined ticket got the full `refine-ticket` treatment — verdicts trace to DoR items, not session momentum; no ticket marked `ready` because the session was going well.
- Live answers exist in ticket files, not just in the conversation.
- Backlog order changes carry a changelog entry.

## Outputs

A batch of honestly-refined tickets; recorded answers; a ranked view of what remains; a session summary.

## State Changes

Through `refine-ticket`, `create-ticket`, and `create-adr` only — plus `product/BACKLOG.md` reordering (with changelog) and the session summary line. MUST NOT: change sprint state, mark tickets `ready` outside `refine-ticket`, or promote ideas (that is `create-ticket`, a separate PO decision).

## Failure / Escalation

- No candidates → say so; suggest `capture-idea`/`create-ticket` instead of inventing work.
- The human's requested ranking conflicts with recorded dependencies ("refine T-0012 first" when it depends on unrefined T-0008) → surface the conflict, proceed with their choice, note the risk in the ticket.

## Example

"Let's refine — sprint planning is tomorrow." The agent presents five candidates: T-0008 first (blocks two others; value: unblocks the export epic), T-0012, T-0015 (stale-ready — ADR-0006 changed its assumptions), then two bugs. Human: "skip the bugs, they're triaged; do the rest." T-0008 refines to `ready` after the human answers the retention-period question live (transcribed into the ticket); T-0012 splits into T-0021/T-0022 (both refined, T-0021 `ready`); T-0015's re-refinement demotes it to `backlog` — its criteria contradict ADR-0006 and the fix needs thought the human won't rush. Summary: 3 ready, 1 back to backlog with reasons, 1 split, one answered question recorded in T-0008; enough is ready to plan.
