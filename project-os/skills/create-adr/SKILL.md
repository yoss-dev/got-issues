---
name: create-adr
description: Decide whether a decision meets the ADR bar; if so, create a numbered ADR from the template, update the index, link related work, and route acceptance to the right authority.
---

# Skill: create-adr

## Purpose

Capture a significant architectural decision — context, options, consequences — as a permanent, linked record, with the right authority accepting it.

## When to Use

- Refinement or implementation surfaces a decision meeting the [ADR bar](../../architecture/adr/README.md).
- Bootstrap records a deliberate initial stack choice.
- An existing Accepted ADR must change (→ a *superseding* ADR; never edit history).

## Active Persona(s)

Software Architect (authoring, bar judgment, acceptance within authority); Software Engineer often drafts context from the trenches.

## Inputs

- The decision or question, and the ticket(s) that surfaced it.

## Preconditions

- None beyond a real decision to record. (When in doubt whether the bar is met, step 1 settles it.)

## Context to Load

1. `architecture/adr/README.md` (bar, conventions, index, next ID)
2. `templates/ADR_TEMPLATE.md`
3. `architecture/ARCHITECTURE.md`, `PROJECT.md` (§4–5)
4. Existing ADRs in the same area (search the index — the decision may exist, conflict, or supersede)
5. The originating ticket(s)

## Procedure

1. **Apply the bar** (adr/README.md). Below it → no ADR; record the decision in the ticket Work Log instead, and say so to whoever asked. ADR inflation is an anti-pattern.
2. **Check the index** for prior art: an existing Accepted ADR already covering this → follow it or supersede it; never write a silently contradicting ADR.
3. **Author** `ADR-NNNN-short-slug.md` from the template: assertive decision title; context readable by a stranger; **at least two genuinely considered options** with honest rejection reasons; consequences including real negatives; risks; follow-up actions.
4. **Route acceptance:**
   - Options don't differ materially in business consequence, cost, or risk → the Architect persona may set `Accepted` directly.
   - They do differ materially — or security/privacy implications are unclear → leave `Proposed` and escalate per WoW §13 with the ADR itself as the options/tradeoffs write-up (that's what the escalation format asks for anyway).
5. **Wire the links:** add the ADR to the index table and bump *Next ID*; add `ADR-NNNN` to each related ticket's frontmatter `adrs:` and its *Relevant ADRs* section; list those tickets in the ADR's Related Tickets; create tickets for follow-up actions via `create-ticket` and link them; if superseding, set the old ADR's status to `Superseded by ADR-NNNN` (the only edit allowed on it) and cross-link both.
6. **Update `architecture/ARCHITECTURE.md`** where the decision changes the current-state map.

## Validation

- ID unique and sequential; index row matches the file; status is one of the legal values; ≥2 options with reasons; Negative consequences non-empty; every related ticket links back.

## Outputs

A Proposed or Accepted ADR, wired into the index, tickets, and architecture overview; follow-up tickets.

## State Changes

May modify: new ADR file, `architecture/adr/README.md` (index), superseded ADR's status line, related ticket files, `architecture/ARCHITECTURE.md`, tickets via `create-ticket`. MUST NOT modify: the body of any existing Accepted ADR.

## Failure / Escalation

- Decision needed mid-implementation but acceptance requires a human → the implementing ticket goes `blocked` on the Proposed ADR (recorded both places); don't build ahead on an unaccepted material decision.

## Example

Implementing report exports surfaces a queue-vs-cron choice affecting infra and future features → bar met. ADR-0009 "Use a job queue for asynchronous work" is drafted with 3 options (in-process cron — rejected: no retry semantics; managed queue — rejected: new vendor + cost, escalation-worthy if chosen; library-based DB-backed queue — chosen: no new infra). Options differ materially in cost → left `Proposed`, escalated with a recommended default; T-0044 goes `blocked` pending the call.
