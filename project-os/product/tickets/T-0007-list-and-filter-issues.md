---
id: T-0007
title: List and filter a project's issues, paginated
type: feature
status: ready
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0006]
adrs: [ADR-0004]
created: 2026-08-30
updated: 2026-08-31
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
- [ ] AC4: Given a caller requesting a page size above the declared maximum, when the request is served, then the API returns **400** with an `application/problem+json` body — **not** a silently capped page. *(Rewritten 2026-08-31: the drafted criterion said "caps it at the maximum", which contradicts the contract already shipped by [T-0002](T-0002-contract-first-codegen-pipeline.md) — see Work Log.)*
- [ ] AC5: Given a stable data set, when a caller pages through the whole list, then every issue appears exactly once — no duplicates and no omissions across page boundaries.
- [ ] AC6: Given a list request over a project with many issues, when it is served, then the number of database queries does not grow with the number of issues returned (no N+1).
- [ ] AC7: Given an unauthenticated or invalid-token caller, when the list is requested, then the API returns 401.
- [ ] AC8: Given issues with and without an assignee, when `unassigned=true` is supplied, then only issues with no assignee are returned; and when both `assignee` and `unassigned=true` are supplied, then the API returns 400 — the two are contradictory and must not be silently resolved.
- [ ] AC9: Given a project that does not exist, when its issue list is requested, then the API returns 404 — distinct from an existing project with no issues, which returns 200 and an empty page.

## Examples / Scenarios

- Filter by status: only matching issues.
- Filter by assignee and priority together: intersection, not union.
- Request page size 10000 when the maximum is 100: **400**, not a capped page (AC4).
- Page through 250 issues at 100 per page: 100 + 100 + 50, each issue once (AC5).
- Empty project: an empty page with 200, not 404.
- Filter by a status outside the declared set: 400 with a problem document.
- Filter `unassigned=true`: only issues with no assignee (AC8).
- Supply `assignee=alice` and `unassigned=true` together: 400 (AC8).
- List a project that does not exist: 404. List an existing project with no issues: 200, empty page (AC9).
- **Counter-example, explicitly not expected:** a client-chosen sort order. Ordering is fixed (see Technical Notes); client sorting is Out of Scope and would reopen AC5's stability question.

## Technical Notes

AC5 and AC6 are the two criteria most likely to be quietly skipped, and the two most likely to matter later. Offset paging over an unstable ordering is the classic source of duplicated and skipped rows; a stable tiebreaker in the ordering is the usual fix.

AC6 needs a way to observe query counts — an EF Core interceptor or logging assertion in the test. **Confirmed practical, 2026-08-31:** a `DbCommandInterceptor` registered on the test host's `DbContextOptions` counts `ReaderExecuting` calls, and the integration tier already builds its own host and already captures logger output (`CapturingLoggerProvider`, [T-0009](T-0009-role-authorisation-and-user-projection.md)), so the pattern exists. AC6 asserts a **count that does not grow with the number of issues** — seed 5, seed 50, assert the count is equal — rather than an absolute number, which would break on any harmless refactor.

**Decisions taken in refinement, 2026-08-31.**

- **Pagination is `page`/`pageSize`, 1-based, default 20, maximum 100, oversize rejected with 400.** Not a fresh choice: [T-0002](T-0002-contract-first-codegen-pipeline.md) already shipped exactly this in `spec/openapi.yaml`, and its acceptance specifically settled capped-versus-rejected in favour of rejected ("a client asking for 10 000 and receiving 100 without being told is worse"). Cursor paging is better for AC5 in general, and a second pagination style in a five-endpoint API is worse than the weaker of two consistent ones. If cursors are ever wanted, they should replace offsets everywhere, as their own ticket.
- **Default ordering is newest first, tie-broken by the issue's own key.** "Newest first" alone is not a total order — two issues created in the same transaction can share a timestamp, and that is precisely how offset paging duplicates and skips rows. The tiebreaker is what makes AC5 achievable rather than aspirational.
- **Unassigned is a separate boolean, not a magic `assignee` value** (AC8). `assignee=none` would collide with a user whose subject is `none`, and the collision would be silent and rare, which is the worst combination. Supplying both is a contradiction and is rejected rather than resolved by precedence — a precedence rule is a thing to remember, and remembering is what this project keeps getting wrong.
- **A missing project is 404, an empty one is 200** (AC9). The distinction is invisible to a client that only checks for rows, and it is exactly the distinction a dashboard needs to tell "no work" from "wrong URL".

## Dependencies

- **T-0006** — filtering by lifecycle fields requires them to exist.

## Risks / Unknowns

- ~~How pagination is expressed in the contract~~ — **settled by precedent, not by preference**: the shipped contract already uses `page`/`pageSize`. See Technical Notes.
- ~~Default and maximum page sizes are unset~~ — **20 and 100**, already declared in `spec/openapi.yaml`.
- ~~Whether an unassigned issue is matchable~~ — **closed as AC8.**
- ~~AC6 is untestable without query observability~~ — **confirmed practical**; the interceptor pattern is described in Technical Notes.
- **Remaining risk: AC5 is the criterion most likely to pass vacuously.** A paging test that seeds fewer rows than a page proves nothing, and one that seeds an exact multiple of the page size never exercises a partial final page. The test must cross a boundary *and* end on one.
- **Remaining risk: this ticket inherits T-0006's enumerations.** Filtering by status and type is only as meaningful as the sets they range over; if those change, this ticket's filters change with them.

## Testing Notes

Integration tests against real PostgreSQL with enough seeded issues to cross a page boundary — a paging test with three rows proves nothing. AC6 requires asserting query counts, not timing.

**Mutate first** ([TESTING.md](../../standards/TESTING.md)): AC5 — remove the ordering tiebreaker and confirm the paging test fails. If it still passes, the test is not crossing a boundary where ties occur and is not testing what it claims. Then AC6 — remove the eager-load and confirm the query-count assertion fails; an N+1 guard that survives its own N+1 is the most common vacuous test in this class.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) — contract-first pipeline
- [ENGINEERING.md](../../standards/ENGINEERING.md) — mandatory pagination, no N+1
- [TESTING.md](../../standards/TESTING.md)
- [IDEA-002](../IDEAS.md) — the originating idea

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-31 during `refinement-session`. All nine universal items hold. Item 5: depends on T-0006, which is not yet `ready` — that constrains sequencing, not readiness. Conditional items: no UX; no ADR-bar decision (the pagination style follows the shipped contract rather than setting a precedent); no new personal data beyond what T-0006 introduces; no migration of its own. No exceptions applied.

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

### 2026-08-31 — Refinement (claude-sm-9d4e) — PO · BA · ENG · ARCH · QA

**A drafted criterion contradicted the shipped contract, and the contract wins.** AC4 said the
server "caps it at the maximum rather than returning everything". `spec/openapi.yaml` — shipped
by [T-0002](T-0002-contract-first-codegen-pipeline.md) — says the opposite in as many words:
*"a larger value is rejected with 400 rather than silently reduced — a client asking for 10 000
and receiving 100 without being told is worse."* That wording exists because T-0002's own
acceptance raised capped-versus-rejected as a defect and settled it.

Per [WoW](../../governance/WAY_OF_WORKING.md) §3 the conflict resolves upward: a shipped
contract and a decided precedent outrank a criterion drafted before either existed. AC4 is
rewritten to match, with the change marked in place rather than silently corrected — an
implementer who read the old AC4 and the spec would have had to pick one, and picking the
ticket would have produced an API inconsistent with itself.

**This is the second time refinement has caught a ticket carrying a decision the codebase had
already made.** The first was T-0015's Out of Scope disowning a residual it was meant to take.
Worth watching: tickets written before the code exists accumulate claims the code later
falsifies, and nothing re-reads them.

**Product (PO).** Unchanged and clearly valuable — the endpoint the primary personas actually
use daily.

**Analysis (BA).** Three ambiguities closed as criteria rather than notes: unassigned filtering
(**AC8**, with the contradictory-parameters case), missing-versus-empty project (**AC9**), and
a counter-example forbidding client-chosen sorting, which would quietly reopen AC5.

**Engineering (ENG).** No hidden dependencies. The query-count observability AC6 needs was
confirmed practical rather than assumed — the interceptor pattern and its non-absolute
assertion are written into Technical Notes so the implementer does not have to discover both.

**Architecture (ARCH).** No ADR-bar decision. Pagination style *would* have met the bar had it
been open; it is not, because the contract already answers it.

**QA.** Every criterion is independently verifiable. AC5 and AC6 are named as the two most
likely to pass vacuously, with the specific mutation for each.

**Sizing.** One operation, filter parameters, an index or two, and tests. Within the guideline.

- **Did:** Applied all five perspectives; rewrote AC4 against the shipped contract; added AC8
  and AC9; settled pagination, ordering and the unassigned filter; confirmed AC6 is testable.
- **Decided:** page/pageSize per the existing contract; newest-first with a key tiebreaker;
  `unassigned` as its own boolean; missing project 404, empty project 200.
- **Remaining:** implementation, after T-0006.
- **Open questions / blockers:** none. It inherits T-0006's enumerations, which is a dependency
  rather than an unknown.
- **DoR verdict:** **ready.**
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
