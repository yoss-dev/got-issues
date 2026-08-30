---
id: T-0005
title: Create and read issues within a project
type: feature
status: backlog
priority: high
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0004]
adrs: [ADR-0004]
created: 2026-08-30
updated: 2026-08-30
---

# T-0005: Create and read issues within a project

## Problem / Context

Promoted from [IDEA-001](../IDEAS.md). The issue is the product's central entity — the thing the whole tracker exists to hold. With projects in place (T-0004), issues are what gives them content.

## Desired Outcome

An authenticated caller can create an issue inside a project and retrieve it by its identifier, through spec-declared endpoints.

## User / Business Value

This is the point at which Got Issues becomes usable rather than demonstrable: Sam can file work, and Priya's automation has something to create. For the PoC it is the smallest end-to-end proof that the company could track real work here.

## Scope

### In Scope

- Specification of the issue resource: create (within a project) and read-by-id operations, schemas, errors, scopes.
- Implementation behind generated contracts, plus the EF Core migration introducing issues and their relationship to projects.
- Issue identity — how an issue is addressed by a client (see *Risks*: this depends on the project-key decision in T-0004).
- Title and description handling, with validation declared in the spec.
- Unit and integration tests, including the unauthenticated case and the unknown-project case.

### Out of Scope

- Lifecycle fields — type, status, priority, assignee (T-0006). An issue created here carries whatever minimal defaults refinement agrees.
- Listing and filtering issues (T-0007).
- Comments (T-0008).
- Editing or deleting issues.

## Acceptance Criteria

- [ ] AC1: Given an existing project and an authenticated caller, when they create an issue with valid input, then it is persisted against that project and returned with its identifier.
- [ ] AC2: Given an issue that exists, when it is requested by its identifier, then the API returns it as the specification declares.
- [ ] AC3: Given a project identifier that does not exist, when an issue is created against it, then the API returns 404 with an `application/problem+json` body — not a 500, and not an orphaned issue.
- [ ] AC4: Given an identifier that does not correspond to any issue, when it is requested, then the API returns 404 with a problem document.
- [ ] AC5: Given an unauthenticated or invalid-token caller, when they attempt either operation, then the API returns 401 and nothing is persisted.
- [ ] AC6: Given the specification, when generation and the drift check run, then the diff is empty.

## Examples / Scenarios

- Create an issue in a project, read it back: fields round-trip intact.
- Create against a deleted or nonexistent project: 404, nothing written.
- Create with an empty title: 400 with a problem document.
- Free-text description containing personal data: stored, but never written to logs ([SECURITY.md](../../standards/SECURITY.md)).

## Technical Notes

Spec first, then regenerate, then implement — as with every product ticket.

Issues are the entity everything else in the domain hangs off, so the shape chosen here is expensive to change later. Refinement should treat the schema as a decision, not an implementation detail.

## Dependencies

- **T-0004** — issues live inside projects; the project resource and its identity scheme must exist first.

## Risks / Unknowns

- **Issue identity depends on T-0004's project-key decision.** If projects get Jira-style keys, issues likely get `PROJ-123` identifiers and a per-project sequence; if not, a global identifier. This is the single most consequential unknown here — retrofitting a numbering scheme after issues exist means migrating live data.
- Free-text fields (title, description) may contain personal data typed by users. `SECURITY.md` requires treating them as such: never logged, minimised. Refinement must name this concern per the DoR's security conditional.
- Whether an issue can exist without lifecycle fields at all, or needs defaults from the outset, is a boundary between this ticket and T-0006 that refinement should draw explicitly.

## Testing Notes

Integration tests against real PostgreSQL, covering the 404 paths in AC3/AC4 as first-class cases rather than afterthoughts — those are where an ORM-backed implementation most often returns a 500 instead.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md), [ENGINEERING.md](../../standards/ENGINEERING.md), [TESTING.md](../../standards/TESTING.md), [SECURITY.md](../../standards/SECURITY.md)
- [IDEA-001](../IDEAS.md) — the originating idea

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — checked during refinement; note applied exceptions here.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** Created by promoting IDEA-001 during backlog seeding.
- **Decided:** Split creation/read from lifecycle fields (T-0006) so each ticket stays within the DoR sizing guideline and delivers observable behaviour on its own.
- **Remaining:** Refinement to Ready; issue identity is the decision to settle, and it depends on T-0004's.
- **Open questions / blockers:** none blocking creation.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
