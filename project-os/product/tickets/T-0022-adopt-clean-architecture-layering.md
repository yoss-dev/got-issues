---
id: T-0022
title: Adopt Clean Architecture layering, and make the result the paradigm
type: technical
status: backlog
priority: high
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0004, T-0005]
adrs: [ADR-0010]
created: 2026-08-31
updated: 2026-08-31
---

# T-0022: Adopt Clean Architecture layering, and make the result the paradigm

## Problem / Context

Requested by the maintainer on 2026-08-31 and decided in
[ADR-0010](../../architecture/adr/ADR-0010-clean-architecture-layering.md), which supersedes
[ADR-0009](../../architecture/adr/ADR-0009-controllers-talk-to-the-dbcontext-and-invariants-are-extracted.md).

Today the API service is one project. Controllers take `GotIssuesDbContext` and query it inline;
domain rules, persistence and HTTP handling share a method in at least one place — the
issue-number allocator in `IssuesController.CreateIssue`, which is a correctness-critical
invariant written in raw SQL inside a request handler.

[T-0006](T-0006-issue-lifecycle-fields.md), [T-0007](T-0007-list-and-filter-issues.md) and
[T-0008](T-0008-comment-on-an-issue.md) all add behaviour over these same entities. The cost of
introducing a boundary rises with every ticket that ships without one, which is why this is not
deferred until something forces it.

**This ticket's output is a pattern, not just a refactor.** Every product ticket after it copies
what it produces, so an inconsistency here is inherited rather than isolated.

## Desired Outcome

The API service is layered — domain, application with ports, infrastructure adapters, controllers
— the existing behaviour is unchanged, and the next implementer has a documented example to
follow rather than one to infer.

## User / Business Value

Nothing user-visible, deliberately: this is groundwork, and the value is in what it makes cheaper.
Concretely, the three committed product tickets each add operations over projects and issues; a
boundary they can all copy is worth more than each inventing one. The domain rule most worth
protecting — that an issue number is allocated exactly once per project — becomes testable without
HTTP or a database, and unreachable from code that has no business allocating one.

## Scope

### In Scope

- **Projects:** `GotIssues.Domain`, `GotIssues.Application`, `GotIssues.Infrastructure`, with
  `GotIssues.Api` referencing Application and Infrastructure. Dependencies point inward only.
- **Domain:** entities and their invariants, no EF attributes and no ASP.NET types. The
  issue-number allocation rule lives here.
- **Application:** one use case per existing operation — `CreateProject`, `ListProjects`,
  `CreateIssue`, `GetIssue` — depending on ports (`IProjectRepository`, `IIssueRepository`,
  `IUnitOfWork`) that this layer owns. Use cases return results, never `IActionResult`.
- **Infrastructure:** EF Core adapters implementing the ports; `GotIssuesDbContext`, the
  migrations and `IEntityTypeConfiguration` mappings move here. The allocator's raw SQL moves here
  with them.
- **Controllers:** implement the generated contract, call one use case, map its result to a
  status code. No `DbContext`, no queries.
- **`UserProjectionMiddleware`** — decide and record whether it goes through a port or keeps
  direct access as infrastructure-adjacent code. It is not a use case and forcing it into one
  would be worse than an argued exception.
- **Documentation of the pattern** in [ARCHITECTURE.md](../../architecture/ARCHITECTURE.md) and
  [ENGINEERING.md](../../standards/ENGINEERING.md), via `evolve-governance`. ARCHITECTURE.md
  currently says the API service does "request handling, domain logic, persistence" — this ticket
  makes that false and must not leave it standing.

### Out of Scope

- **Any behaviour change.** This is a refactor; if an endpoint's response changes in any way, the
  refactor is wrong.
- **CQRS, MediatR, domain events, a separate read model** — explicitly not adopted by ADR-0010.
- **Separating domain entities from persistence entities** — ADR-0010 keeps one set for now and
  says why.
- New endpoints, new fields, new migrations. The schema does not change.
- The identity host (`apps/GotIssues.IdentityHost`), which has no domain logic to layer.

## Acceptance Criteria

- [ ] AC1: Given the solution, when it is built, then `GotIssues.Domain` references no EF Core, no ASP.NET Core and no `GotIssues.Infrastructure` — enforced by project references, so a violation fails the build rather than review.
- [ ] AC2: Given `GotIssues.Application`, when its dependencies are inspected, then it depends on `GotIssues.Domain` and its own port interfaces only — no EF Core types, no `DbContext`, no `IActionResult`.
- [ ] AC3: Given each controller, when it is read, then it calls exactly one use case and maps the result; it contains no LINQ query, no `DbContext` reference and no SQL.
- [ ] AC4: Given the **existing test suite**, when it runs after the refactor, then **every test passes unmodified**. A test that must change to accommodate the new structure is evidence that behaviour changed, and is a defect in this ticket — not a test to update.
- [ ] AC5: Given `tools/smoke.sh`, when it runs against the refactored service, then all its checks pass — the stack behaves identically from outside.
- [ ] AC6: Given the issue-number allocation rule, when it is tested, then it is tested **without HTTP and without a database** for its domain logic, and its persistence adapter keeps the concurrency test that already exists (T-0005 AC1d) unchanged.
- [ ] AC7: Given `spec/openapi.yaml` and `./tools/check-drift.sh`, when they run, then the diff is empty — the contract is untouched by this work.
- [ ] AC8: Given [ARCHITECTURE.md](../../architecture/ARCHITECTURE.md) and [ENGINEERING.md](../../standards/ENGINEERING.md), when they are read after this ticket, then they describe the layering as built, with an example an implementer can copy, and no sentence still describes the previous arrangement.
- [ ] AC9: Given a reviewer asking "where would a new operation go?", when they read the documentation from AC8, then the answer is unambiguous without reading the existing code.

## Examples / Scenarios

- `POST /projects` → `ProjectsController` → `CreateProject` use case → `IProjectRepository` → EF adapter. The controller maps a "key already used" result to 409; it does not catch `DbUpdateException`.
- Allocating an issue number: the rule lives in Domain, the `UPDATE … RETURNING` lives in the Infrastructure adapter, and T-0005's ten-way concurrency test passes unchanged.
- **Counter-example:** a use case returning `NotFoundResult`. Use cases do not know about HTTP.
- **Counter-example:** a domain type carrying `[Column]` or `[Required]`. Mapping is Infrastructure's job via `IEntityTypeConfiguration`.

## Technical Notes

**The order that keeps this reviewable:** create the projects and move types with their behaviour
intact first, then introduce ports, then thin the controllers. Each step should leave the suite
green, so a failure names the step that caused it. A single commit that moves everything at once
is unreviewable and, on a change this size, unbisectable.

**The allocator is the one piece with a real invariant** and the reason ADR-0009 existed at all.
Its raw SQL names a column in a string nothing checks against the entity; moving it into an
adapter does not fix that, but it does give it one home where a test can pin it.

## Dependencies

**T-0004** and **T-0005** — the code being refactored. Both must be `done`; T-0005 is in
acceptance at the time of writing.

## Risks / Unknowns

- **This refactors working, accepted code, which is where behaviour quietly changes.** AC4 is the
  guard and it is deliberately strict: tests may not be modified. If one must change, stop and
  record why — that is the finding, not an obstacle.
- **Sequencing against [T-0006](T-0006-issue-lifecycle-fields.md), which is committed in
  SPRINT-003.** Doing T-0006 first means writing it in the old shape and migrating it twice; doing
  this first delays the MVP by the size of this ticket. **A maintainer decision, not an
  implementer's** — see the Work Log.
- **Sizing is genuinely uncertain** and may exceed the DoR guideline. Four projects, four use
  cases, three ports and a documentation obligation is not a two-day task on a first pass. If
  refinement judges it too large, the seam is *projects and ports first, controllers thinned
  second* — but note that stopping between them leaves the codebase in two shapes at once, which
  is worse than either.
- **Pass-through is expected, not a smell here.** `ListProjects` will forward through three layers
  to a single query. ADR-0010 accepts that as the price of uniformity; a reviewer treating each
  instance as a finding would be relitigating the ADR.

## Testing Notes

The existing suite **is** the specification for this ticket: 102 integration and unit tests plus
13 smoke checks, all of which must pass without modification (AC4, AC5). That is a stronger
safety net than anything that could be written for the refactor itself.

New tests are wanted in one place only: the domain rule extracted per AC6, which becomes testable
without infrastructure for the first time.

Per the amended [TESTING.md](../../standards/TESTING.md), mutation is **not** required here — this
ticket adds no coverage claim that a test is the sole evidence for. The unchanged suite is the
evidence.

## Relevant ADRs & Documentation

- [ADR-0010](../../architecture/adr/ADR-0010-clean-architecture-layering.md) — **the decision this implements**, including the two sub-decisions marked open to reversal
- [ADR-0009](../../architecture/adr/ADR-0009-controllers-talk-to-the-dbcontext-and-invariants-are-extracted.md) — superseded; read for the cost this decision accepts
- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) — unchanged; the generated contract stays the delivery boundary
- [ADR-0008](../../architecture/adr/ADR-0008-role-restrictions-declared-in-the-contract-enforced-by-policy.md) — policy attributes stay on controllers

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined. Sizing (item 7) is the item most likely to fail; see Risks.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-31 — Product Owner (claude-sm-9d4e)

- **Did:** Created from the maintainer's instruction to adopt repositories, ports and Clean Architecture, and to make the first implementation the paradigm for later work. Recorded as [ADR-0010](../../architecture/adr/ADR-0010-clean-architecture-layering.md), superseding ADR-0009.
- **Decided:** scope covers the existing four operations only — this is a refactor with a documentation obligation, not a redesign. AC4 forbids modifying tests, because on a refactor of accepted code the unchanged suite is the only real guard.
- **Remaining:** refinement, where sizing is the open question.
- **Open questions / blockers:** **one, for the maintainer** — whether this runs before [T-0006](T-0006-issue-lifecycle-fields.md), which is committed in SPRINT-003 and is the MVP's last ticket. Doing T-0006 first means building it in the old shape and migrating it twice; doing this first delays the MVP by however long this takes, and its size is not yet known. Recorded rather than assumed.
- **Test state:** n/a — not started.


### 2026-08-31 — Sequencing decided by the maintainer: T-0006 first (claude-sm-9d4e)

The open question recorded at creation is answered: **[T-0006](T-0006-issue-lifecycle-fields.md)
runs first, then this ticket.** The MVP finishes before the refactor starts.

Two consequences this ticket now carries rather than discovers:

1. **Scope grows by whatever T-0006 leaves behind.** This ticket was written against four
   operations — `CreateProject`, `ListProjects`, `CreateIssue`, `GetIssue`. T-0006 adds lifecycle
   mutation and assignment, so there will be a fifth, and its assignee validation reaches the
   `users` projection — a third repository this ticket did not plan for. Refinement should size
   against T-0006 **as built**, not against this description.
2. **AC4 gets stronger, not weaker.** T-0006 will arrive with its own tests, and they are covered
   by the same rule: **no test may be modified to accommodate the refactor.** A larger unchanged
   suite is a better safety net, which is the one way this ordering helps rather than costs.

The cost accepted, stated plainly: lifecycle code is written once in the current shape and moved
once. That is the price of an MVP that ships sooner, and it was the maintainer's call to pay it.

- **Did:** Recorded the sequencing decision and what it changes about this ticket's scope.
- **Decided:** by the maintainer, as above.
- **Remaining:** refinement — now explicitly after T-0006 is done, so it can size against real code.
- **Open questions / blockers:** none. The question recorded at creation is answered.
- **Test state:** n/a — not started.
