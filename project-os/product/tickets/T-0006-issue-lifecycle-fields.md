---
id: T-0006
title: Track an issue's lifecycle — type, status, priority, assignee
type: feature
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0005, T-0009]
adrs: [ADR-0004]
created: 2026-08-30
updated: 2026-08-30
---

# T-0006: Track an issue's lifecycle — type, status, priority, assignee

## Problem / Context

Promoted from [IDEA-002](../IDEAS.md). This is what separates a tracker from a list: knowing what state work is in and who holds it. An issue that cannot change state records only that something was once written down.

Configurable workflows and validated transitions are explicitly a **later goal** ([`PROJECT.md`](../../PROJECT.md) §3), so this ticket delivers the fields and their changes — not a workflow engine.

## Desired Outcome

An issue carries a type, status, priority, and assignee, and an authenticated caller can change them as work progresses.

## User / Business Value

Sam can see what is in flight and who holds it — the question a flat list cannot answer. Priya's automation can move issues in response to external events (a CI failure, a merged change).

## Scope

### In Scope

- Specification of the lifecycle fields on the issue schema, and the operation(s) that change them.
- A fixed set of types, statuses, and priorities, declared as enumerations in the specification so clients get them generated.
- Assignment to a user, and unassignment.
- The EF Core migration adding the fields.
- Unit and integration tests, including rejection of values outside the declared sets.

### Out of Scope

- **Configurable workflows and validated transitions** — a later goal, deliberately not built here. Any transition between declared statuses is permitted.
- Per-project customisation of the sets.
- Assignment history or an audit trail (see *Risks*).
- Notifying anyone of a change — notifications are a non-goal.

## Acceptance Criteria

- [ ] AC1: Given an existing issue, when an authenticated caller changes its status, priority, or type to a value within the declared set, then the change is persisted and reflected on subsequent reads.
- [ ] AC2: Given a value outside the declared set, when it is submitted, then the API returns 400 with an `application/problem+json` body — enum violations are rejected at the contract boundary, not stored.
- [ ] AC3: Given an existing user, when an issue is assigned to them, then the assignment is persisted; and when the issue is unassigned, then it is recorded as having no assignee, distinguishable from never having been assigned only if refinement decides that distinction matters.
- [ ] AC4: Given an assignee identifier that does not correspond to a known user in the projection (T-0009), when assignment is attempted, then the API returns a problem document and the issue is unchanged.
- [ ] AC5: Given any two declared statuses, when an issue moves directly between them, then the API permits it — transition validation is explicitly out of scope and must not be implemented ahead of the workflow goal.
- [ ] AC6: Given the specification, when generation and the drift check run, then the diff is empty.

## Examples / Scenarios

- Move an issue from its initial status to any other: permitted (AC5).
- Submit a status not in the enumeration: 400.
- Assign, then reassign, then unassign: each persists.
- Assign to an unknown user: rejected, issue unchanged.

## Technical Notes

Declaring the sets as OpenAPI enumerations means clients get them as generated types — a genuine benefit of contract-first, and the reason not to model them as free strings.

AC5 is a deliberate constraint against gold-plating: transition rules are a *later* product goal, and building them early would pre-empt a Product Owner decision.

## Dependencies

- **T-0005** — the issue resource must exist.
- **T-0009** — assignment needs users to be addressable; T-0009 provides the user projection built from token claims.

## Risks / Unknowns

- **Which types, statuses, and priorities?** Unanswered ([IDEA-002](../IDEAS.md)). Changing a declared enumeration after clients have generated against it is a breaking contract change, so refinement should not treat this as a detail.
- **Assignment depends on the user projection from T-0009.** Resolved as a dependency rather than an unknown (2026-08-30), but if T-0009 slips, this ticket cannot proceed — assignment to a subject with no local record has nothing to point at.
- Whether status-change history is worth keeping is open. Not building it is cheap now; adding it retroactively cannot recover the history that was never recorded — a genuine one-way door worth a deliberate decision.

## Testing Notes

Integration tests covering each field's happy path plus the enum-rejection case; AC5 needs a test that a "backwards" transition is permitted, which is the kind of behaviour a future workflow feature would deliberately break.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md), [ENGINEERING.md](../../standards/ENGINEERING.md), [TESTING.md](../../standards/TESTING.md)
- [PROJECT.md](../../PROJECT.md) §3 — workflows as a later goal
- [IDEA-002](../IDEAS.md) — the originating idea

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — checked during refinement; note applied exceptions here.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** Created by promoting IDEA-002 during backlog seeding.
- **Decided:** Made "any transition is permitted" an explicit acceptance criterion rather than an omission, so an implementer cannot helpfully add workflow rules that pre-empt a later product decision.
- **Remaining:** Refinement to Ready; the enumerations and the user dependency are the decisions to settle.
- **Open questions / blockers:** none blocking creation; the user-concept dependency may reshape scope.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** The user-concept gap flagged at creation is resolved: T-0009 provides the user projection, and is now a dependency. Assignment validation (AC4) points at it.
- **Decided:** none beyond the dependency.
- **Remaining:** Refinement to Ready; the enumerations are still the open decision.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
