---
id: T-0004
title: Create and list projects
type: feature
status: backlog
priority: high
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0002, T-0003, T-0009]
adrs: [ADR-0004, ADR-0003]
created: 2026-08-30
updated: 2026-08-30
---

# T-0004: Create and list projects

## Problem / Context

Promoted from [IDEA-001](../IDEAS.md). Projects are the top-level container in the product — issues live inside them, and nothing else in the domain can exist first. Until a project can be created and retrieved, there is no product, only infrastructure.

This is also the first *product* capability to go through the contract-first pipeline built in T-0002, so it doubles as the real-world test of that pipeline ([ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)).

## Desired Outcome

An authenticated caller can create a project and retrieve the list of projects, through endpoints declared in `spec/openapi.yaml` and implemented behind generated contracts.

## User / Business Value

Sam gets the structure that a flat list cannot provide — work grouped by project rather than piled together. Priya gets the first addressable resource to build against. For the PoC, this is the first evidence that the contract-first method produces working product capability, not just scaffolding.

## Scope

### In Scope

- Specification of the project resource in `spec/openapi.yaml`: create and list operations, schemas, error responses, required scopes.
- **Removal of T-0002's disposable placeholder resource** from the specification and of its generated output — projects is the first real resource, and the placeholder exists only until it arrives.
- Implementation behind the generated controller contracts.
- Persistence via EF Core, with the migration that introduces the projects table.
- Pagination on the list endpoint — mandatory per [ENGINEERING.md](../../standards/ENGINEERING.md).
- Validation of project input, declared in the specification (not only in code).
- Unit and integration tests per [TESTING.md](../../standards/TESTING.md), including the unauthenticated-caller case.

### Out of Scope

- Issues (T-0005) and anything that lives inside a project.
- Updating, archiving, or deleting projects — deliberately deferred until the questions in *Risks* are answered.
- Project membership or per-project permissions — roles are global, so no such concept exists ([GLOSSARY](../../governance/GLOSSARY.md)).
- Defining the role policies themselves — that is T-0009; this ticket only applies them.
- Any UI.

## Acceptance Criteria

- [ ] AC1: Given a caller holding the `admin` role, when they create a project with valid input, then it is persisted and returned with its identifier.
- [ ] AC2: Given a caller holding only the `member` role, when they attempt to create a project, then the API returns 403 and nothing is persisted — project creation is an admin act (`PROJECT.md` §5).
- [ ] AC2b: Given a caller of either role, when they request the project list, then it is returned — listing is not restricted.
- [ ] AC2c: Given an unauthenticated or invalid-token caller, when they attempt either operation, then the API returns 401 — distinct from the 403 of AC2.
- [ ] AC3: Given invalid input (as declared in the specification), when a project is created, then the API returns 400 with an `application/problem+json` body naming the offending field.
- [ ] AC4: Given more projects exist than one page holds, when the list is requested, then results are paginated and the response carries what a client needs to fetch the next page — no unbounded result set is ever returned.
- [ ] AC5: Given the specification, when `./tools/generate.sh` is run and the drift check follows, then the diff is empty — the endpoints were designed in the spec, not in the controller.
- [ ] AC6: Given the endpoints, when they are exercised by integration tests against real PostgreSQL, then behaviour matches what the specification declares.

## Examples / Scenarios

- An `admin` creates a project, then a `member` lists it: it appears.
- A `member` attempts to create one: 403, nothing written.
- Create with a missing or empty name: 400 with a problem document, not a 500.
- Create two projects with the same name: **behaviour undecided — see Risks.**
- List when no projects exist: empty page, 200, not 404.
- List with more items than the page size: pagination behaves and the caller can reach the rest.

## Technical Notes

The specification comes first: design the resource in `spec/openapi.yaml`, regenerate, then implement the generated interface. A controller declaring its own routes is a review rejection ([ENGINEERING.md](../../standards/ENGINEERING.md)).

## Dependencies

- **T-0002** — the contract-first pipeline must exist before a resource can be specified and generated.
- **T-0003** — the test harness, for AC6.
- **T-0009** — the `admin` policy must exist before creation can be restricted to it.

## Risks / Unknowns

- **Project keys are undecided.** Jira uses short keys (`PROJ-123`) that make issue identifiers human-readable. Whether Got Issues does the same is open ([IDEA-001](../IDEAS.md)) and affects both this schema and issue identity in T-0005 — cheap now, expensive later. **Refinement should settle it before implementation.**
- **Archiving projects is out of scope but is an admin act** when it arrives (maintainer, 2026-08-30) — recorded so the follow-up ticket inherits the rule rather than rediscovering it.
- Name uniqueness is unspecified — duplicates allowed, or rejected?
- This is the first real exercise of the generated `aspnetcore` contracts. If the output proves unworkable, [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) requires superseding rather than a quiet workaround, and this ticket's Work Log is where that evidence gets recorded.

## Testing Notes

Integration tests through `WebApplicationFactory` against PostgreSQL in Testcontainers; the 401 case in AC2 is required, not optional ([SECURITY.md](../../standards/SECURITY.md)). The drift check in AC5 is part of the suite.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) — contract-first pipeline
- [ENGINEERING.md](../../standards/ENGINEERING.md) — the contract-first rule and mandatory pagination
- [TESTING.md](../../standards/TESTING.md), [SECURITY.md](../../standards/SECURITY.md)
- [IDEA-001](../IDEAS.md) — the originating idea

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — checked during refinement; note applied exceptions here.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** Created by promoting IDEA-001 during backlog seeding.
- **Decided:** Sliced vertically (one resource, end to end through the spec) rather than by layer, so each ticket delivers observable behaviour.
- **Remaining:** Refinement to Ready. Project keys and creation authorisation are the two decisions to settle there.
- **Open questions / blockers:** none blocking creation; `PROJECT.md` Q7 shapes AC2's precision.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** Applied the maintainer's Q7 answer. Project creation is now an `admin` act (AC1/AC2), listing stays open to any authenticated caller, and the 403-vs-401 distinction is explicit. Added T-0009 as a dependency.
- **Decided:** Kept policy *definition* in T-0009 and only its *application* here, so authorisation is defined once centrally rather than per endpoint.
- **Remaining:** Refinement to Ready; project keys remain the open decision.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
