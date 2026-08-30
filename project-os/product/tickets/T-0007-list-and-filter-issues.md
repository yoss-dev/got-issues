---
id: T-0007
title: List and filter a project's issues, paginated
type: feature
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0006]
adrs: [ADR-0004]
created: 2026-08-30
updated: 2026-08-30
---

# T-0007: List and filter a project's issues, paginated

## Problem / Context

Promoted from [IDEA-002](../IDEAS.md). Creating and reading issues one at a time (T-0005) does not answer the question people actually ask: *what is open, and who has it?* Once lifecycle fields exist (T-0006), filtering by them is what makes the tracker usable.

Full query/search is a **later goal** ([`PROJECT.md`](../../PROJECT.md) §3) — this is filtering on declared fields, not a query language.

## Desired Outcome

An authenticated caller can list a project's issues, filtered by status, assignee, type, or priority, with pagination.

## User / Business Value

This is the endpoint Sam uses daily and the one Priya's dashboards call. It is also where the mandatory-pagination rule earns its place: an unbounded issue list is the most likely place for this API to fall over under real data.

## Scope

### In Scope

- Specification of the list operation with filter parameters on the lifecycle fields, and pagination declared in the contract.
- Implementation behind generated contracts, with efficient queries — no N+1, no unbounded result set ([ENGINEERING.md](../../standards/ENGINEERING.md)).
- A defined default and maximum page size, enforced server-side.
- A defined default ordering, so paging is stable and results are reproducible.
- Integration tests covering filters, paging boundaries, and query efficiency.

### Out of Scope

- Free-text or full-text search, and any query language (JQL-equivalent) — a later goal, and "JQL / search" is on the glossary's deliberately-absent list.
- Cross-project listing.
- Sorting chosen by the client (unless refinement decides it is cheap and settles the stability question).
- Saved filters or views.

## Acceptance Criteria

- [ ] AC1: Given a project with issues, when the list is requested without filters, then all its issues are returned in the defined default order, paginated.
- [ ] AC2: Given issues in several statuses, when the list is filtered by status, then only matching issues are returned; the same holds for assignee, type, and priority.
- [ ] AC3: Given two filters at once, when both are supplied, then they combine — results match all supplied filters, not any of them.
- [ ] AC4: Given more issues than the maximum page size, when a caller requests a larger page, then the server caps it at the maximum rather than returning everything.
- [ ] AC5: Given a stable data set, when a caller pages through the whole list, then every issue appears exactly once — no duplicates and no omissions across page boundaries.
- [ ] AC6: Given a list request over a project with many issues, when it is served, then the number of database queries does not grow with the number of issues returned (no N+1).
- [ ] AC7: Given an unauthenticated or invalid-token caller, when the list is requested, then the API returns 401.

## Examples / Scenarios

- Filter by status: only matching issues.
- Filter by assignee and priority together: intersection, not union.
- Request page size 10000 when the maximum is 100: capped at 100.
- Page through 250 issues at 100 per page: 100 + 100 + 50, each issue once (AC5).
- Empty project: an empty page with 200, not 404.
- Filter by a status outside the declared set: 400 with a problem document.

## Technical Notes

AC5 and AC6 are the two criteria most likely to be quietly skipped, and the two most likely to matter later. Offset paging over an unstable ordering is the classic source of duplicated and skipped rows; a stable tiebreaker in the ordering is the usual fix.

AC6 needs a way to observe query counts — an EF Core interceptor or logging assertion in the test. Refinement should confirm this is practical rather than leaving the implementer to discover it.

## Dependencies

- **T-0006** — filtering by lifecycle fields requires them to exist.

## Risks / Unknowns

- **How pagination is expressed in the contract** (offset/limit vs. cursor) is undecided and is a breaking change once clients generate against it. Cursor paging solves AC5 more robustly; offset is simpler and more familiar. Refinement should choose deliberately — this is exactly the kind of decision contract-first exists to force early.
- Default and maximum page sizes are unset; picking them arbitrarily is fine, picking them silently is not.
- Whether an unassigned issue is matchable by an "unassigned" filter is a real gap in AC2 that refinement should close.
- AC6 is untestable without query observability; if that proves impractical, the criterion needs rewriting rather than dropping.

## Testing Notes

Integration tests against real PostgreSQL with enough seeded issues to cross a page boundary — a paging test with three rows proves nothing. AC6 requires asserting query counts, not timing.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) — contract-first pipeline
- [ENGINEERING.md](../../standards/ENGINEERING.md) — mandatory pagination, no N+1
- [TESTING.md](../../standards/TESTING.md)
- [IDEA-002](../IDEAS.md) — the originating idea

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — checked during refinement; note applied exceptions here.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** Created by promoting IDEA-002 during backlog seeding.
- **Decided:** Made paging correctness (AC5) and query efficiency (AC6) explicit criteria rather than leaving them to the engineering standards, because both are easy to omit and expensive to discover in production.
- **Remaining:** Refinement to Ready; the pagination style is the decision to settle, and it is contract-breaking once shipped.
- **Open questions / blockers:** none blocking creation.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
