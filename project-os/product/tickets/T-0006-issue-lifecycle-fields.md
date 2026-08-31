---
id: T-0006
title: Track an issue's lifecycle — type, status, priority, assignee
type: feature
status: in-progress
priority: normal
owner: claude-sm-9d4e
implemented_by: none
accepted_by: none
depends_on: [T-0005, T-0009]
adrs: [ADR-0004, ADR-0010]
created: 2026-08-30
updated: 2026-08-31
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
- A fixed set of types, statuses, and priorities, declared as enumerations in the specification so clients get them generated. **Decided by the maintainer 2026-08-31** — `type`: `bug`, `task` · `status`: `open`, `in_progress`, `done` · `priority`: `low`, `normal`, `high`. Defaults: `task`, `open`, `normal`.
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
- [ ] AC2: Given a value outside the declared set — `type` ∈ {`bug`, `task`}, `status` ∈ {`open`, `in_progress`, `done`}, `priority` ∈ {`low`, `normal`, `high`} — when it is submitted, then the API returns 400 with an `application/problem+json` body; enum violations are rejected at the contract boundary, not stored.
- [ ] AC3: Given an existing user, when an issue is assigned to them by their `subject`, then the assignment is persisted; and when the issue is unassigned, then `assignee` reads as null. **An issue never assigned and an issue since unassigned are indistinguishable** — refinement decided this on 2026-08-31 (see Technical Notes).
- [ ] AC4: Given an assignee `subject` with no row in the `users` projection ([T-0009](T-0009-role-authorisation-and-user-projection.md)), when assignment is attempted, then the API returns **400** with an `application/problem+json` body naming the offending field, and the issue is unchanged on a subsequent read.
- [ ] AC5: Given any two declared statuses, when an issue moves directly between them, then the API permits it — transition validation is explicitly out of scope and must not be implemented ahead of the workflow goal.
- [ ] AC6: Given the specification, when generation and the drift check run, then the diff is empty.
- [ ] AC7: Given an issue whose lifecycle fields have never been set, when it is read, then `type` is `task`, `status` is `open` and `priority` is `normal` — declared as defaults in the specification, so no client has to infer them; `assignee` is the only nullable lifecycle field.
- [ ] AC8: Given a caller with the `member` role, when they change any lifecycle field or assign any issue, then the request is permitted — lifecycle changes are not an admin act ([PROJECT.md](../../PROJECT.md) §5 names the three admin acts, and none of them is this).

## Examples / Scenarios

- Move an issue from its initial status to any other, including "backwards" from the last declared status to the first: permitted (AC5).
- Submit a status not in the enumeration: 400, and nothing is written.
- Assign, then reassign, then unassign: each persists; the final read shows `assignee: null`.
- Assign to a `subject` with no projection row: 400, issue unchanged.
- Assign to a user, then read the issue: `assignee` carries enough to display the person without a second call — decided as `subject` plus `displayName`, see Technical Notes.
- A newly created issue, read immediately: type, status and priority hold their declared defaults; `assignee` is null (AC7).
- **Counter-example, explicitly not expected:** rejecting a transition because it is "not allowed" from the current status. Any declared status may follow any other (AC5).

## Technical Notes

Declaring the sets as OpenAPI enumerations means clients get them as generated types — a genuine benefit of contract-first, and the reason not to model them as free strings.

AC5 is a deliberate constraint against gold-plating: transition rules are a *later* product goal, and building them early would pre-empt a Product Owner decision.

**Decisions taken in refinement, 2026-08-31.** Each was a gap that would otherwise have been filled differently by two reasonable implementers:

- **The assignee is identified by `subject`**, the key of the `users` projection ([T-0009](T-0009-role-authorisation-and-user-projection.md)). There is no other stable identifier: the projection stores no email, and `displayName` is a convenience field that is not unique and can be trimmed.
- **The read model carries `subject` and `displayName`.** Returning a bare subject would force every client into a second call to render "assigned to whom", and the projection exists precisely so it need not.
- **Unassigned and never-assigned are not distinguished.** Assignment history is out of scope, and inventing a tri-state now (`null` vs. absent) would leak an audit concept into a schema that cannot support it. If history is later wanted, it arrives as its own resource, not as a subtle null.
- **An unknown assignee is 400, not 404.** The subject arrives inside a request body as a field value; 404 belongs to the addressed resource, and the issue in the path does exist.
- **Lifecycle changes are a `member` act** (AC8). `PROJECT.md` §5 names three admin acts — creating and archiving projects, assigning roles, deleting issues and comments — and moving an issue is none of them.

**Testable only against seeded projection rows for now.** No token this system issues carries a `sub` ([T-0018](T-0018-user-subject-tokens.md)), so no request can populate `users` by itself. AC3 and AC4 must seed the projection directly, exactly as [T-0009](T-0009-role-authorisation-and-user-projection.md)'s tests do. This is a testing constraint, not a dependency: the feature works the moment real user tokens exist.

## Dependencies

- **T-0005** — the issue resource must exist.
- **T-0009** — assignment needs users to be addressable; T-0009 provides the user projection built from token claims.

## Risks / Unknowns

- ~~Which types, statuses, and priorities?~~ — **answered by the maintainer, 2026-08-31** (see Work Log). The asymmetry remains worth remembering for anyone tempted to extend the sets: adding a value is additive, renaming or removing one is a breaking contract change for every generated client.
- **`in_review` was considered and deliberately left out.** It is the state a software team most obviously distinguishes, and it is also the first one that invites the transition rules AC5 forbids. If it is wanted, adding it later is the additive direction.
- **Assignment depends on the user projection from T-0009.** Resolved as a dependency rather than an unknown (2026-08-30), but if T-0009 slips, this ticket cannot proceed — assignment to a subject with no local record has nothing to point at.
- Whether status-change history is worth keeping is open. Not building it is cheap now; adding it retroactively cannot recover the history that was never recorded — a genuine one-way door worth a deliberate decision.

## Testing Notes

Integration tests covering each field's happy path plus the enum-rejection case; AC5 needs a test that a "backwards" transition is permitted, which is the kind of behaviour a future workflow feature would deliberately break.

**Per [TESTING.md](../../standards/TESTING.md), the claims to mutate first** are AC2 (delete the enum from the specification and confirm the rejection test fails — an enum enforced only by hand-written code is exactly the drift ADR-0004 exists to prevent) and AC4 (remove the projection lookup and confirm an unknown assignee is no longer rejected). Both are the kind of guard that passes vacuously if the request never reaches the validation.

Assignment tests seed `users` directly (see Technical Notes). A test that assigns to a subject it also invented, without seeding, would pass for the wrong reason — it would be asserting 400 against a genuinely unknown user while believing it tested the happy path.

## Relevant ADRs & Documentation

- [ADR-0010](../../architecture/adr/ADR-0010-clean-architecture-layering.md) — **supersedes ADR-0009 and takes the extraction with it.** This ticket no longer moves the allocator; [T-0022](T-0022-adopt-clean-architecture-layering.md) does, as part of the layering. What this ticket inherits instead is the *shape* — if T-0022 lands first, lifecycle changes are written as a use case behind a port, not as controller code
- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md), [ENGINEERING.md](../../standards/ENGINEERING.md), [TESTING.md](../../standards/TESTING.md)
- [PROJECT.md](../../PROJECT.md) §3 — workflows as a later goal
- [IDEA-002](../IDEAS.md) — the originating idea

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-31 during `refinement-session`; the one failing item (item 3, verifiable criteria — the enumerations were undeclared) was closed the same day by the maintainer's answer. All nine universal items now hold. Item 5: depends on T-0005 and T-0009; T-0009 is `done`. Conditional items: security/privacy — assignment stores a subject already held by T-0009 and the read model returns a display name governed by `PROJECT.md` Q8; data-shape impact identified (four columns and a non-cascading foreign key); no UX; no ADR-bar decision. No exceptions applied.

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

### 2026-08-31 — Refinement (claude-sm-9d4e) — PO · BA · ENG · ARCH · QA · SEC

**Product (PO).** Outcome unchanged and vision-aligned: `PROJECT.md` §3 names issues among the
product goals, and workflows are explicitly *later*. A reader would recognise "done" from the
criteria.

**Analysis (BA).** Five places where two reasonable implementers would have built different
things, all now closed in Technical Notes: what identifies an assignee, what the read model
returns, whether unassigned differs from never-assigned, whether an unknown assignee is 400 or
404, and whether unset fields default or read null (now **AC7**). Added **AC8** because the
role question was unstated and would otherwise be answered by whoever wrote the endpoint —
`PROJECT.md` §5 names the three admin acts and moving an issue is not among them. Added a
counter-example so an implementer cannot helpfully add transition validation.

**Engineering (ENG).** Implementable on the current stack: enumerations in
`spec/openapi.yaml`, generated into `libs/`, an EF migration adding four columns and a foreign
key to `users(Subject)`. No hidden dependency beyond the ones recorded. One constraint worth
naming: the assignee column must be nullable and the foreign key must not cascade — deleting a
user must not delete their issues, and that is a decision an implementer would otherwise make
silently in a migration.

**Architecture (ARCH).** No decision at the ADR bar. The enumeration *values* are a product
decision, not an architectural one; how they are declared is already settled by
[ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md).

**QA.** Every criterion is independently verifiable **except AC2 and AC7, which name a
declared set that does not yet exist.** That is the blocker.

**Security.** Assignment writes a `subject` into a new table column — an identifier already
stored by T-0009, so no new class of personal data. `displayName` in the read model is
personal data already returned by nothing else; `PROJECT.md` Q8 governs it and is unchanged by
this ticket.

**Sizing.** Comfortably within the guideline once the sets are fixed: four fields, one
migration, one operation, and the tests. No split needed.

#### The blocking question, with a proposal

**Which types, statuses and priorities?** I am not deciding this: it is a product decision
whose cost is asymmetric — adding a value later is additive, removing or renaming one is a
breaking contract change for every generated client. My proposal, deliberately small:

| Set | Proposed values | Why |
| --- | --- | --- |
| `type` | `bug`, `task` | The two that carry different meaning for a team. `story` and `epic` imply a planning model this project does not have; adding either later is additive. |
| `status` | `open`, `in_progress`, `done` | The minimum that answers "what is in flight" — the ticket's stated value. `closed`/`resolved` as distinct from `done` is a workflow distinction and workflows are a later goal. |
| `priority` | `low`, `normal`, `high` | Three levels people actually distinguish. `critical` sounds useful and in practice becomes a second `high`. |

**Defaults** (AC7): `type: task`, `status: open`, `priority: normal`.

**The one I would push back on if asked to expand:** more statuses. Every added status invites
the transition rules that AC5 forbids, and this project's own history says premature structure
is expensive to remove.

- **Did:** Applied all six perspectives; closed five ambiguities; added AC7 and AC8; recorded
  the assignment-testing constraint that follows from no token carrying a `sub`.
- **Decided:** assignee identified by `subject`; read model carries `subject` + `displayName`;
  unassigned indistinguishable from never-assigned; unknown assignee is 400; lifecycle changes
  are a member act; the assignee foreign key does not cascade.
- **Remaining:** the maintainer picks the three enumerations (proposal above), then this is
  `ready`.
- **Open questions / blockers:** **one, blocking** — the enumeration values. AC2 and AC7 cannot
  be verified without them.
- **DoR verdict:** **not ready** — DoR item 3 (verifiable criteria) fails while the declared
  sets are undeclared. Everything else holds.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — Product Owner decision, recorded verbatim (maintainer, via `refinement-session`)

Asked in-session with the asymmetry stated — adding a value later is additive, renaming or
removing one is a breaking contract change for every generated client — and answered:

> **Minimal.** `type`: `bug`, `task` · `status`: `open`, `in_progress`, `done` ·
> `priority`: `low`, `normal`, `high`. Defaults: `task`, `open`, `normal`.

The alternatives offered were a Jira-shaped set (adding `story`, `epic`, `in_review`, `closed`,
`critical`) and minimal-plus-`in_review`. The minimal set was chosen over both.

This closes the only DoR failure on this ticket. AC2 and AC7 now name concrete values, Scope
carries the sets, and `in_review`'s deliberate absence is recorded in Risks so the next reader
knows it was considered rather than overlooked.

- **Did:** Put the decision to the maintainer with its cost asymmetry; transcribed the answer
  into Scope, AC2, AC7 and Risks.
- **Decided:** by the maintainer, as above.
- **Remaining:** implementation.
- **Open questions / blockers:** none.
- **DoR verdict:** **ready** — the item-3 failure recorded earlier today is closed.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — ADR-0009 accepted; this ticket performs its rule 3 (claude-sm-9d4e)

[ADR-0009](../../architecture/adr/ADR-0009-controllers-talk-to-the-dbcontext-and-invariants-are-extracted.md)
was accepted by the maintainer on 2026-08-31: controllers keep taking `GotIssuesDbContext`
directly, and **domain invariants are extracted into named types**. Its rule 3 names one concrete
instance, and this is the ticket that performs it — because it is the next one to touch issue
creation.

**What moves:** the issue-number allocator currently in `IssuesController.CreateIssue` — the
`UPDATE projects SET "NextIssueNumber" = "NextIssueNumber" + 1 … RETURNING` statement and the
transaction around it — into a named type under `apps/GotIssues.Api/Domain/`.

**Why it qualifies under rule 2** ("would this still have to be true if a second caller did
it?"): yes, unambiguously. Any future path that creates an issue must allocate the same way, and
[T-0005](T-0005-create-and-read-issues.md) records what goes wrong when it does not —
`MAX(number)+1` passes twelve of thirteen tests and duplicates under concurrency.

**What the move is actually worth**, beyond tidiness: the allocator names its column in a raw SQL
string that nothing checks against the entity. Rename `NextIssueNumber` on the model today and the
code compiles, ships, and fails at runtime. In one named type that fragility has one home and can
be pinned by a test.

**Not a licence to add layers.** ADR-0009 rejects a pass-through service explicitly. This ticket
extracts one invariant; it does not introduce `IssueService`, and a reviewer should reject that if
it appears.

- **Did:** Recorded ADR-0009's acceptance and the extraction it assigns here.
- **Decided:** nothing new — this transcribes an accepted decision.
- **Remaining:** unchanged; this ticket is `committed` and not started.
- **Open questions / blockers:** none.
- **Test state:** n/a — not started.

### 2026-08-31 — ADR-0009 superseded; the extraction moves to T-0022 (claude-sm-9d4e)

The entry above assigned this ticket ADR-0009's rule 3 — moving the issue-number allocator out of
`IssuesController`. **That obligation has moved.**
[ADR-0010](../../architecture/adr/ADR-0010-clean-architecture-layering.md) supersedes ADR-0009 the
same day, on the maintainer's judgement that the current implementation is an anti-pattern, and
[T-0022](T-0022-adopt-clean-architecture-layering.md) now performs the extraction as part of
adopting layering across the service.

Correcting it here rather than leaving it: a ticket carrying an obligation that has moved is a
false pointer of exactly the kind [DoD](../../governance/DEFINITION_OF_DONE.md) item 4 exists to
prevent, and this one would have been read by whoever picks this ticket up next.

**What this ticket inherits instead is a sequencing question, and it is the maintainer's.** This
is the MVP's last committed ticket in SPRINT-003; T-0022 refactors the code it builds on.

- **Build T-0006 first:** the MVP finishes sooner, and its lifecycle code is then migrated by
  T-0022 — written once in the old shape, moved once. The migration is mechanical, and T-0022's
  AC4 (no test may be modified) applies to whatever T-0006 leaves behind.
- **Build T-0022 first:** T-0006 is written directly in the target shape and never migrated, but
  the MVP waits for a refactor whose size is not yet estimated, and whose own ticket says sizing
  is the DoR item most likely to fail.

I have not assumed either. Recorded in T-0022's Work Log as its one open question.

- **Did:** Corrected the superseded ADR reference and the obligation that moved with it.
- **Decided:** nothing — this transcribes a decision and removes a claim that is no longer true.
- **Remaining:** unchanged; `committed`, not started, pending the sequencing answer.
- **Open questions / blockers:** the sequencing question above.
- **Test state:** n/a — not started.


### 2026-08-31 — Sequencing decided: this ticket runs first (claude-sm-9d4e)

The maintainer chose **T-0006 before [T-0022](T-0022-adopt-clean-architecture-layering.md)**: the
MVP completes, then the layering refactor migrates what this ticket builds.

**So this ticket is written in the current shape**, deliberately — controller plus `DbContext`, as
[T-0004](T-0004-create-and-list-projects.md) and [T-0005](T-0005-create-and-read-issues.md) are.
Not because that shape is preferred; [ADR-0010](../../architecture/adr/ADR-0010-clean-architecture-layering.md)
says it is not. Writing it half-way toward the target would leave a third shape for T-0022 to
migrate, which is worse than either.

One thing this changes about how it should be built: **its tests are what will protect the
refactor.** T-0022's AC4 forbids modifying any test to accommodate the new structure, so every
test written here becomes part of the safety net for a change that touches everything. Tests that
assert behaviour through the HTTP boundary survive the refactor; tests coupled to how the
controller happens to work today would have to change, and under T-0022's AC4 that would register
as a defect in the refactor rather than in this ticket. Prefer the former.

- **Did:** Recorded the sequencing decision and its one implication for how this ticket is built.
- **Decided:** by the maintainer; this ticket is written in the current shape rather than partly
  migrated.
- **Remaining:** unchanged — `committed`, eligible once T-0005 reaches `done`.
- **Open questions / blockers:** none; the sequencing question is answered.
- **Test state:** n/a — not started.
