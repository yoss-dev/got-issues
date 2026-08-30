---
name: create-ticket
description: Convert a sufficiently understood need (usually a promoted idea) into a structured ticket file and register it in the backlog index.
---

# Skill: create-ticket

## Purpose

Turn a need the Product Owner persona judges worth pursuing into a structured, outcome-oriented backlog ticket that refinement can then drive to Ready.

## When to Use

- Promoting an idea from `IDEAS.md`.
- Recording a confirmed defect (type `bug`).
- Capturing agreed technical work, a spike, or a chore.

Not for raw ideas (use `capture-idea`) and not for silently absorbing mid-sprint discoveries (WoW §7 governs those — they come here first, then get recorded in the sprint).

## Active Persona(s)

Product Owner (value, outcome, initial priority) with Business Analyst support (structure, first-pass criteria).

## Inputs

- The source: an `IDEA-NNN`, a defect report, a retro action, an ADR follow-up, or a human request.

## Preconditions

- The need is understood well enough to state a problem, a desired outcome, and identifiable value. If it isn't, it belongs in `IDEAS.md`.

## Context to Load

1. `product/BACKLOG.md` (next ID, duplicate check, ordering context)
2. `templates/TICKET_TEMPLATE.md`
3. The source material (idea entry, defect report, ADR)
4. `product/PRODUCT_VISION.md` (value framing)
5. `governance/DEFINITION_OF_READY.md` (to know what refinement will need — not to satisfy it now)

## Procedure

1. Check the backlog for an existing ticket covering the need; enrich instead of duplicating.
2. Take the next `T-NNNN` from `BACKLOG.md` and create `product/tickets/T-NNNN-short-slug.md` from the template.
3. Fill what is genuinely known: problem/context, desired outcome, value, first-cut scope, and draft acceptance criteria. **Describe outcomes and constraints, not implementation.** Record honest gaps in *Risks / Unknowns* rather than papering over them — an imperfect ticket that admits its gaps is better than a polished fiction.
4. For bugs: reproduction steps, expected vs. observed, severity, affected versions/environments.
5. For spikes: the question, why it matters now, time box, output form (per DoR exceptions).
6. Set frontmatter: `status: backlog`, type, coarse priority, `created`/`updated`, known `depends_on`.
7. Register the ticket in `BACKLOG.md`'s Active table at a position reflecting PO-persona priority judgment; update *Next ID*.
8. Update the source: mark the idea `promoted → T-NNNN` in `IDEAS.md`, or link the ticket from the originating ADR/retro.

## Validation

- Ticket file exists, follows the template, ID matches the index, ID sequence unbroken.
- The ticket describes an outcome a QA persona could eventually verify — even if criteria are still rough.
- Backlog row and ticket frontmatter agree.

## Outputs

One ticket file; updated backlog index; updated source artifact.

## State Changes

May modify: new file in `product/tickets/`, `product/BACKLOG.md`, `product/IDEAS.md` (status line of the promoted idea).

## Failure / Escalation

- Cannot articulate value → do not create the ticket; return the item to `IDEAS.md` with the value question recorded.
- Conflicts with vision non-goals or `PROJECT.md` constraints → escalate to human PO before creating.

## Example

`IDEA-007` (invoice search) is promoted: `tickets/T-0031-invoice-date-filtering.md` — outcome: "users can filter their invoice list by date range"; out of scope: full-text search, amount filters (noted as possible follow-ups); unknowns: "max invoices per user unverified — check with ops during refinement". Status `backlog`, placed 4th in the Active table; IDEA-007 marked `promoted → T-0031`.
