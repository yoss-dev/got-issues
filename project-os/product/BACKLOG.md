# Backlog

The ordered index of all work items. **Full ticket content lives in one file per ticket under [`tickets/`](tickets/)** — this file is only the index: identity, order, status, dependencies. This hybrid design keeps Git diffs small (editing a ticket touches one file), avoids merge conflicts between agents working on different tickets, and stays navigable at hundreds of tickets.

**Rules**

- Order within the Active table **is** priority: top row = most important next. Only the Product Owner persona reorders; material reorders get a changelog entry below.
- Every row links to its ticket file: `tickets/T-NNNN-short-slug.md`, created from [`templates/TICKET_TEMPLATE.md`](../templates/TICKET_TEMPLATE.md).
- The ticket file's `status` field is authoritative; this index mirrors it. When they disagree, trust the ticket file and fix the index.
- IDs are `T-NNNN`, assigned sequentially, never reused. Next ID: **T-0009**.
- When a ticket reaches `done` or `dropped`, move its row to the Completed table (append; do not reorder history).

## Active

| # | ID | Title | Type | Status | Depends on |
| --- | --- | --- | --- | --- | --- |
| 1 | [T-0001](tickets/T-0001-runnable-compose-stack.md) | Runnable Docker Compose stack with API skeleton, PostgreSQL, and identity host | technical | backlog | — |
| 2 | [T-0002](tickets/T-0002-contract-first-codegen-pipeline.md) | Contract-first pipeline — OpenAPI spec, code generation, and drift check | technical | backlog | T-0001 |
| 3 | [T-0003](tickets/T-0003-automated-test-harness.md) | Automated test harness — xUnit, WebApplicationFactory, and PostgreSQL via Testcontainers | technical | backlog | T-0001 |
| 4 | [T-0004](tickets/T-0004-create-and-list-projects.md) | Create and list projects | feature | backlog | T-0002, T-0003 |
| 5 | [T-0005](tickets/T-0005-create-and-read-issues.md) | Create and read issues within a project | feature | backlog | T-0004 |
| 6 | [T-0006](tickets/T-0006-issue-lifecycle-fields.md) | Track an issue's lifecycle — type, status, priority, assignee | feature | backlog | T-0005 |
| 7 | [T-0007](tickets/T-0007-list-and-filter-issues.md) | List and filter a project's issues, paginated | feature | backlog | T-0006 |
| 8 | [T-0008](tickets/T-0008-comment-on-an-issue.md) | Comment on an issue | feature | backlog | T-0005 |

## Completed

| ID | Title | Type | Outcome | Finished |
| --- | --- | --- | --- | --- |

*(empty)*

## Changelog

*Material reorders, drops, and bulk changes. One line each: date, what, why.*

- 2026-08-30 — Backlog initialized (empty).
- 2026-08-30 — Seeded by `bootstrap-project` step 8: T-0001..T-0003, the setup work bootstrap prescribed but did not perform. Ordered by dependency, not by product value — nothing product-facing can be built or verified until the stack, the pipeline, and the harness exist. The four primary use cases were captured as IDEA-001..004 and deliberately **not** promoted (maintainer's call, 2026-08-30).
- 2026-08-30 — IDEA-001/002/003 promoted to T-0004..T-0008, sliced vertically (one capability end to end through the spec) rather than one ticket per entity, so each item stays within the DoR sizing guideline. Ordering follows the dependency chain: projects → issues → lifecycle → filtering, with comments branching off issues. IDEA-004 (auth) deliberately **not** promoted — it cannot be refined to Ready while `PROJECT.md` Q7 (the global role set) is unanswered.
