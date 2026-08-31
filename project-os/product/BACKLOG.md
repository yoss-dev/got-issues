# Backlog

The ordered index of all work items. **Full ticket content lives in one file per ticket under [`tickets/`](tickets/)** — this file is only the index: identity, order, status, dependencies. This hybrid design keeps Git diffs small (editing a ticket touches one file), avoids merge conflicts between agents working on different tickets, and stays navigable at hundreds of tickets.

**Rules**

- Order within the Active table **is** priority: top row = most important next. Only the Product Owner persona reorders; material reorders get a changelog entry below.
- Every row links to its ticket file: `tickets/T-NNNN-short-slug.md`, created from [`templates/TICKET_TEMPLATE.md`](../templates/TICKET_TEMPLATE.md).
- The ticket file's `status` field is authoritative; this index mirrors it. When they disagree, trust the ticket file and fix the index.
- IDs are `T-NNNN`, assigned sequentially, never reused. Next ID: **T-0016**.
- When a ticket reaches `done` or `dropped`, move its row to the Completed table (append; do not reorder history).

## Active

| # | ID | Title | Type | Status | Depends on |
| --- | --- | --- | --- | --- | --- |
| 1 | [T-0010](tickets/T-0010-duende-identity-host.md) | Duende IdentityServer host in the stack, with the API as resource server | technical | in-progress | T-0001 |
| 2 | [T-0002](tickets/T-0002-contract-first-codegen-pipeline.md) | Contract-first pipeline — OpenAPI spec, code generation, and drift check | technical | ready | T-0001, T-0011 |
| 3 | [T-0009](tickets/T-0009-role-authorisation-and-user-projection.md) | Role-based authorisation and the user projection from token claims | feature | ready | T-0003, T-0010 |
| 4 | [T-0004](tickets/T-0004-create-and-list-projects.md) | Create and list projects | feature | backlog | T-0002, T-0003, T-0009 |
| 5 | [T-0005](tickets/T-0005-create-and-read-issues.md) | Create and read issues within a project | feature | backlog | T-0004 |
| 6 | [T-0006](tickets/T-0006-issue-lifecycle-fields.md) | Track an issue's lifecycle — type, status, priority, assignee | feature | backlog | T-0005, T-0009 |
| 7 | [T-0007](tickets/T-0007-list-and-filter-issues.md) | List and filter a project's issues, paginated | feature | backlog | T-0006 |
| 8 | [T-0008](tickets/T-0008-comment-on-an-issue.md) | Comment on an issue | feature | backlog | T-0005, T-0009 |
| 9 | [T-0015](tickets/T-0015-compose-stack-smoke-test.md) | Automated coverage for behaviour that needs the real Compose stack | technical | backlog | T-0003, T-0010 |
| 10 | [T-0012](tickets/T-0012-pin-container-base-images.md) | Pin container base images to immutable digests | technical | backlog | T-0001 |
| 11 | [T-0014](tickets/T-0014-correct-testing-standard-commands.md) | Correct TESTING.md's suite commands to match reality | technical | backlog | — |
| 12 | [T-0013](tickets/T-0013-enforce-migration-boundary-with-db-privileges.md) | Enforce the migration boundary with database privileges, not convention | technical | backlog | T-0001 |

## Completed

| ID | Title | Type | Outcome | Finished |
| --- | --- | --- | --- | --- |
| [T-0011](tickets/T-0011-spike-aspnetcore-generator-viability.md) | SPIKE: is OpenAPI Generator's aspnetcore output workable on ASP.NET Core 10? | spike | done | 2026-08-30 |
| [T-0001](tickets/T-0001-runnable-compose-stack.md) | Runnable Docker Compose stack with API skeleton and PostgreSQL | technical | done | 2026-08-30 |
| [T-0003](tickets/T-0003-automated-test-harness.md) | Automated test harness — xUnit, WebApplicationFactory, and PostgreSQL via Testcontainers | technical | done | 2026-08-30 |

## Changelog

*Material reorders, drops, and bulk changes. One line each: date, what, why.*

- 2026-08-30 — Backlog initialized (empty).
- 2026-08-30 — Seeded by `bootstrap-project` step 8: T-0001..T-0003, the setup work bootstrap prescribed but did not perform. Ordered by dependency, not by product value — nothing product-facing can be built or verified until the stack, the pipeline, and the harness exist. The four primary use cases were captured as IDEA-001..004 and deliberately **not** promoted (maintainer's call, 2026-08-30).
- 2026-08-30 — T-0015 widened during T-0010's review: it was being handed token-validation coverage while its own Out of Scope disowned API behaviour, so T-0010's AC3, three AC4 refusals, and the identity host's no-migrate guard had no owner. Now scoped by the real constraint — anything needing the running stack — with AC6/AC7 added.
- 2026-08-30 — T-0015 created from T-0003's acceptance: the harness structurally cannot reach T-0001's Compose-level criteria (cold start, non-destructive restart, slow-database tolerance), which remain verified only by hand. Ticketed rather than left as Work Log prose.
- 2026-08-30 — T-0012, T-0013, T-0014 created from T-0001's review deferrals, so DoD item 4 is met by linked tickets rather than Work Log prose. Placed below the product work: none is urgent, and T-0013 is explicitly low priority since the boundary it hardens currently holds. T-0014 is a governance change requiring human approval.
- 2026-08-30 — **SPRINT-001 planned.** Committed T-0001, T-0011, T-0003, T-0010 (goal: the stack runs from a clean clone with a working token round-trip, proved by tests). Continuous flow, no end date. T-0002 held back pending T-0011's verdict; T-0009 held back because both its dependencies are in this sprint. Order unchanged.
- 2026-08-30 — `refinement-session` complete: **T-0001, T-0003, T-0002, T-0009, T-0010 all refined → `ready`** (5 of 5 attempted). Spawned **T-0011**, a 4-hour spike gating T-0002, after the maintainer chose to answer the `aspnetcore` generator question before building the pipeline rather than during it; T-0011 is placed at position 2 because it has no dependencies and its verdict could supersede ADR-0004. T-0010 moved above T-0002 (it only needs T-0001, while T-0002 waits on the spike). Positions 6–11 shifted; relative product-ticket priority unchanged.
- 2026-08-30 — `refinement-session` (order chosen by the maintainer: T-0001, T-0003, T-0002, T-0009). **T-0001 refined → `ready`**, and **split**: the Duende identity host became **T-0010**, inserted at position 4 ahead of T-0009, which now depends on it rather than on T-0001. T-0001's refinement also produced **ADR-0005** (operational endpoints are outside the API contract), which resolved a circular conflict between the contract-first rule and T-0001's health endpoint. Positions 4–10 shifted down by one; relative priority is otherwise unchanged.
- 2026-08-30 — Q7 answered (global roles `admin`/`member`, carried as a Duende token claim). IDEA-004 promoted to **T-0009**, placed above the product tickets because T-0004 (admin-only project creation), T-0006 (assignment) and T-0008 (comment authorship) all depend on it. It also closes the "missing user concept" gap those tickets carried.
- 2026-08-30 — IDEA-001/002/003 promoted to T-0004..T-0008, sliced vertically (one capability end to end through the spec) rather than one ticket per entity, so each item stays within the DoR sizing guideline. Ordering follows the dependency chain: projects → issues → lifecycle → filtering, with comments branching off issues. IDEA-004 (auth) deliberately **not** promoted — it cannot be refined to Ready while `PROJECT.md` Q7 (the global role set) is unanswered.
