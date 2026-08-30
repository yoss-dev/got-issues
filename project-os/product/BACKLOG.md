# Backlog

The ordered index of all work items. **Full ticket content lives in one file per ticket under [`tickets/`](tickets/)** — this file is only the index: identity, order, status, dependencies. This hybrid design keeps Git diffs small (editing a ticket touches one file), avoids merge conflicts between agents working on different tickets, and stays navigable at hundreds of tickets.

**Rules**

- Order within the Active table **is** priority: top row = most important next. Only the Product Owner persona reorders; material reorders get a changelog entry below.
- Every row links to its ticket file: `tickets/T-NNNN-short-slug.md`, created from [`templates/TICKET_TEMPLATE.md`](../templates/TICKET_TEMPLATE.md).
- The ticket file's `status` field is authoritative; this index mirrors it. When they disagree, trust the ticket file and fix the index.
- IDs are `T-NNNN`, assigned sequentially, never reused. Next ID: **T-0001**.
- When a ticket reaches `done` or `dropped`, move its row to the Completed table (append; do not reorder history).

## Active

| # | ID | Title | Type | Status | Depends on |
| --- | --- | --- | --- | --- | --- |

*(empty — no tickets yet)*

## Completed

| ID | Title | Type | Outcome | Finished |
| --- | --- | --- | --- | --- |

*(empty)*

## Changelog

*Material reorders, drops, and bulk changes. One line each: date, what, why.*

- 2026-08-30 — Backlog initialized (empty).
