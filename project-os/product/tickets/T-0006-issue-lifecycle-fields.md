---
id: T-0006
title: Track an issue's lifecycle — type, status, priority, assignee
type: feature
status: in-progress
priority: normal
owner: claude-sm-9d4e
implemented_by: claude-sm-9d4e
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


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — claimed, with the implementation plan

Claimed at `cd18607`. Dependencies verified in their own files: T-0005 and T-0009 both `done`.

#### The lesson from the ticket before this one, applied before writing any code

T-0005 shipped a migration that backfilled existing rows with a value the contract forbade —
`NextIssueNumber = 0`, so every pre-existing project's first issue would have been `GOTI-0`,
unreadable through the only read path. It was invisible to the whole suite because **every test
migrated an empty schema**.

**This ticket adds four NOT NULL columns to a table that already holds rows.** It is the same
migration shape, one table over, and [T-0021](T-0021-prove-migrations-against-populated-databases.md)
exists precisely because `UpgradePathTests` covers T-0005's migration and not the class. So:

- every new column carries a **database default** via `HasDefaultValue`, not merely a CLR
  initialiser — that distinction is exactly what went wrong last time;
- `UpgradePathTests` gains a case for **this** migration: migrate to the pre-T-0006 schema, create
  a project and an issue through the API, upgrade, and assert the existing issue reads back with
  `task` / `open` / `normal` and a null assignee.

That test is the first thing I will write, before the migration exists, because it is the one that
would fail if I repeat the defect.

#### Shape

| Step | What |
| --- | --- |
| 1 | `spec/openapi.yaml`: `IssueType`, `IssueStatus`, `IssuePriority` enums; the four fields on `Issue`; `PATCH /issues/{issueKey}` with `UpdateIssueRequest`; `Assignee` schema |
| 2 | Generate; implement the generated contract |
| 3 | Migration: four columns on `issues`, all NOT NULL with database defaults except `AssigneeSubject`, plus a non-cascading FK to `users` |
| 4 | Tests, upgrade-path case first |

**The enumerations are settled** (maintainer, 2026-08-31): `type` ∈ {`bug`, `task`}, `status` ∈
{`open`, `in_progress`, `done`}, `priority` ∈ {`low`, `normal`, `high`}, defaulting to `task`,
`open`, `normal`. Declared as OpenAPI enums so clients generate them as types.

**Mutation is a `PATCH`, not a `PUT`.** The ticket asks for changing fields, and a `PUT` would
require sending every field to change one — which turns "move this to in_progress" into a
read-modify-write with a lost-update race. `PATCH` with all fields optional says what it means.

#### The decisions already recorded, being carried in

From refinement: the assignee is identified by `subject` (the projection's key, and the only stable
identifier — `displayName` is not unique); the read model returns `subject` **and** `displayName`
so a client can render a person without a second call; unassigned and never-assigned are
deliberately **not** distinguished; an unknown assignee is **400**, not 404, because the subject
arrives in a body while the issue in the path exists; lifecycle changes are a **member** act.

#### Test plan

| AC | Test |
| --- | --- |
| AC1 | change status, priority, type; each persists and reads back |
| AC2 | a value outside each declared set → 400 `problem+json` naming the field |
| AC3 | assign by subject; reassign; unassign → `assignee` reads null |
| AC4 | assign to a subject with no projection row → 400 naming the field, issue unchanged |
| AC5 | `done` → `open` is permitted — the "backwards" transition a workflow feature would forbid |
| AC6 | `check-drift.sh` exit 0 |
| AC7 | a freshly created issue reads `task` / `open` / `normal`, assignee null |
| AC8 | a `member` may change every field and assign; only an unrecognised role is refused |
| — | **upgrade path:** an issue created before this migration reads the defaults afterwards |

**Mutation**, under the narrowed rule ([TESTING.md](../../standards/TESTING.md)): the only claim
where a test is the sole evidence is **AC5** — nothing but a test stops someone adding transition
validation, and the ticket forbids it explicitly. One mutant there. The enum rejection is enforced
by the generated `[RegularExpression]`/enum binding, and the defaults by database defaults, so both
are recorded as enforcement rather than mutated.

#### Written in the current shape, deliberately

Per the maintainer's sequencing decision, this is controller-plus-`DbContext` like T-0004 and
T-0005; [T-0022](T-0022-adopt-clean-architecture-layering.md) migrates it afterwards. **Its tests
must assert through the HTTP boundary**, never against controller internals — T-0022's AC4 forbids
modifying a test to accommodate the refactor, so a test coupled to today's internals would surface
as a defect in T-0022 rather than here.

#### Risks

- **The migration against populated rows is the risk**, and it is the one the previous ticket got
  wrong. Hence the test first.
- **The assignee FK must not cascade.** Deleting a user must not delete their issues; nothing
  deletes users yet, so this is a decision to make now rather than discover.
- **AC5 is a criterion against gold-plating**, which is unusual and easy to "improve" away. The
  mutant exists to make that visible.

- **Did:** Claimed; verified dependencies; planned the migration around the defect the previous
  ticket shipped.
- **Decided:** `PATCH` over `PUT`; database defaults over CLR initialisers; upgrade-path test written first.
- **Remaining:** implementation.
- **Open questions / blockers:** none.
- **Test state:** not started.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — built; three contract problems the tests found first

Branch `t-0006-lifecycle`. **129 tests green** (17 unit, 112 integration), smoke 13/13.

#### The migration lesson, applied rather than repeated

T-0005 shipped a backfill that gave existing rows a value the contract forbade, and it was
invisible because every test migrated an empty schema. This ticket adds **four NOT NULL columns to
a table that already holds rows** — the same shape, one table over.

So the upgrade-path test was written before the migration existed: migrate to the pre-T-0006
schema, seed a project and an issue **through SQL** (the application cannot start against that
schema — its model expects columns that do not exist yet), upgrade, and read the issue back
through the API. It reads `task` / `open` / `normal` with a null assignee, because each column
carries a **database default** rather than only a CLR initialiser. That distinction is the whole
defect from last time.

#### Three problems, all found by tests rather than review

**1. The API returned enums as numbers.** `spec/openapi.yaml` declares
`enum: [open, in_progress, done]`; the API answered `"status": 2`. The generator emits
`[EnumMember(Value = "open")]`, which Newtonsoft honours and `System.Text.Json` ignores — and this
project generates with `useNewtonsoft=false`. These are the **first enums in the contract**, so
nothing had exercised it. That is the document promising one thing and the API sending another,
this repository's signature defect, and it would have shipped without a test that asserted the
value rather than the status code.

Fixed with a converter that reads the declared values. A plain `JsonStringEnumConverter` would not
do: the generated member names are `OpenEnum` and `InProgressEnum`.

**2. My first version of that converter turned a 400 into a 500.** It claimed `Nullable<T>` and
returned a `JsonConverter<T>`; System.Text.Json wraps nullable itself and rejects that, so an
invalid enum value produced an unhandled exception instead of a validation failure. The
enum-rejection tests caught it immediately — a fix for a contract defect, introducing a worse one,
caught because the test asserted the status it wanted rather than merely "not success".

**3. `required: [subject]` made unassigning impossible.** In JSON Schema `required` means *the
property must be present*; the C# generator renders it `[Required]`, which means *must not be
null* — and null is exactly the value that unassigns. The contract rejected the operation the
object exists to express. Removed, with the reason recorded in the schema itself.

#### The contract expresses PATCH semantics rather than the server inferring them

The generated request initially could not distinguish "omitted" from "sent": enums arrived as
non-nullable value types, so an omitted `type` read as `(IssueType)0`. And a bare
`assigneeSubject: string?` cannot say both *leave the holder alone* and *unassign*.

Both fixed in the specification rather than by inspecting raw JSON in the controller: the three
enums are nullable (null and absent both mean unchanged, which is all they can mean), and
assignment is wrapped in an object — **absent** leaves the holder alone, **present with a null
subject** unassigns. Reading the body twice to detect property presence would have put a rule the
contract cannot state into code only, which is what ADR-0004 exists to prevent.

#### Mutation, under the narrowed rule

One mutant, for the one claim where a test is the sole evidence:

| Mutant | Result |
| --- | --- |
| A transition rule refusing `done` → anything else — exactly what AC5 forbids | **Killed** — `Expected: OK, Actual: Conflict` |

AC5 is a criterion **against** gold-plating, so it is guarded by nothing except a test; a
well-meant "improvement" is the failure mode.

*(Corrected after review — the paragraph that followed misattributed two enforcements, and one of
them was wrong in a way that mattered. See the entry below.)*

#### Decisions

- **`PATCH`, not `PUT`** — a replace would require sending every field to change one, turning
  "move this to in_progress" into a read-modify-write with a lost-update race.
- **Domain enums are separate from the contract's**, mapped in the controller. Binding persistence
  to generated types would make a contract change a schema change, and ADR-0010 will formalise the
  separation this anticipates.
- **Stored as names, not ordinals** — a column reading `InProgress` survives someone reordering the
  enum; an integer does not.
- **The assignee foreign key is `Restrict`.** Deleting a person must not delete the work they hold.
  Asserted by a test, because nothing deletes users yet and the decision would otherwise be
  invisible until someone added that.
- **Written in the current shape** per the maintainer's sequencing decision, with every test
  asserting through the HTTP boundary so [T-0022](T-0022-adopt-clean-architecture-layering.md) can
  refactor beneath them without touching one.

- **Did:** Specified the lifecycle fields and the update operation, generated, implemented,
  migrated with database defaults, and tested all eight criteria plus the upgrade path.
- **Decided:** as above.
- **Remaining:** review, then acceptance.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0006-lifecycle`.
- **Test state, in this worktree:** `dotnet test` **129/129** · `tools/smoke.sh` **13/13** exit 0 ·
  build 0 warnings · `dotnet format` exit 0 both · `validate.py` exit 0 · `check-drift.sh` exit 0
  after commit.
- **For QA to probe:** the enum converter is new infrastructure on the response path of **every**
  endpoint, not just this one — and its first version turned a 400 into a 500. Worth checking it
  did not change how any existing response serialises.

### 2026-08-31 — Code review (claude-rev-7a03) — ENG · ARCH — **Request changes**

Reviewed `t-0006-lifecycle` @ `dbbecbd` against `main`, in the worktree, per
[`review-code`](../../skills/review-code/SKILL.md).

#### Gates, each exit code read from the tool itself

| Gate | Result |
| --- | --- |
| `dotnet test` | **exit 0** — 17 unit + 112 integration = **129** passed, 0 failed |
| `dotnet build --no-incremental` | **exit 0** — 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | **exit 0** (solution) and **exit 0** (`GotIssues.SmokeTests.csproj`) |
| `./tools/check-drift.sh` | **exit 0** on a clean tree — AC6 verified, generated output matches the spec |
| `./tools/smoke.sh` | **exit 0** — 13 passed, 0 failed (5 m 27 s) |
| `python3 tools/validate-project-os/validate.py` | **exit 0** — 23 tickets, 10 ADRs |

#### What I did beyond running the suite

Per [TESTING.md](../../standards/TESTING.md) *"exercise the system in a state it was not built
in"*, I drove the new endpoint with eleven request bodies the tests do not send, in a throwaway
integration probe deleted afterwards (working tree verified clean). Three of them found
something; the rest are recorded below as confirmed behaviour so nobody has to rediscover them.

---

### Blocking

#### B1 — a `U+0000` in `assignment.subject` returns **500**. This is T-0004's defect, in the one new request string.

`spec/openapi.yaml:555-557` declares `AssignmentChange.subject` with `maxLength: 255` and **no
`pattern`**. It is the only free-text string in a request body in this whole contract without
one. Every other one has one *because T-0004 shipped exactly this defect*:
`CreateProjectRequest.name` (:609-611, :645-647), `CreateIssueRequest.title` (:571-573),
`.description` (:583-584) and `Issue.title` (:411-413) all carry a NUL-excluding pattern, with
the reason spelled out in the spec at :417-424 — *"PostgreSQL cannot store `U+0000` in text at
all"*.

Observed, sending `subject` as the six-character JSON escape `\u0000` followed by `bad`:

```
PATCH /issues/PE-1   body: {"assignment":{"subject":"\u0000bad"}}
-> 500 application/problem+json
   {"type":"https://httpstatuses.io/500","title":"An unexpected error occurred.","status":500}
server log: Npgsql.PostgresException (0x80004005):
            22021: invalid byte sequence for encoding "UTF8": 0x00
```

The value reaches PostgreSQL as a query parameter at
`apps/GotIssues.Api/Controllers/IssuesController.cs:181-183`
(`dbContext.Users.SingleOrDefaultAsync(u => u.Subject == assignment.Subject, …)`), and the
database rejects the byte sequence.

Why this blocks, on two counts:

- **AC4.** That subject has no row in the `users` projection, so AC4 requires **400** with a
  problem document naming the offending field. It returns 500, with no `detail` and no field
  named. (The issue itself is correctly left unchanged — that half holds.)
- **[SECURITY.md](../../standards/SECURITY.md)**, project rule *Input validation* `[confirmed]`:
  *"request validation is declared in the OpenAPI specification and enforced by generated model
  binding plus explicit checks in controllers. A validation rule that exists only in code and not
  in the spec is a contract defect."* Here the rule exists in neither place — PostgreSQL is doing
  the rejecting, and badly.

Fix: add the `pattern` to `AssignmentChange.subject`, regenerate, and add the regression test
[TESTING.md](../../standards/TESTING.md) requires for every fixed bug.

#### B2 — `{"assignment": {}}` silently unassigns, and the contract nowhere says so

This is the permanent, published consequence of dropping `required: [subject]`, and it is
destructive. Observed, on an issue assigned to `alice`:

```
PATCH {"assignment":{}}     -> 200, assignee becomes null, and stays null on re-read
PATCH {"assignment":null}   -> 200, assignee unchanged (still alice)
```

`spec/openapi.yaml:544-563` says only *"Setting `subject` to null unassigns."* It does not say
that **omitting** `subject` unassigns, nor what `"assignment": null` does. Both are now real,
permanent behaviours of a published contract, discoverable only by trying them.

The generated client makes the first easy to hit by accident:
`libs/GotIssues.Client/src/GotIssues.Client/Model/AssignmentChange.cs` has no required
constructor argument for `Subject`, so a caller who builds an `AssignmentChange` and forgets to
set it unassigns the issue and receives a 200.

[ADR-0008](../../architecture/adr/ADR-0008-role-restrictions-declared-in-the-contract-enforced-by-policy.md) names this
exact class of defect: *"the document promising something other than what the system does."*

Either resolution is acceptable, but the contract must state which:

- **document it** — say in `AssignmentChange`'s description that an absent `subject` unassigns
  exactly as an explicit null does, and that `"assignment": null` leaves the holder alone; or
- **reject it** — treat a present `assignment` carrying no `subject` property as a 400. That
  needs a controller check, since `required` is unavailable for the reason the schema records.

Either way, a test per case.

#### B3 — the mutation record claims AC4 is enforced by the foreign key. It is not, and I am challenging the claim.

The Work Log gives *"the assignee's existence by a foreign key"* as the reason AC4 needs no
mutant. The foreign key enforces that an unknown subject cannot be **stored**. It does not
produce AC4's stated outcome — 400, naming the field, issue unchanged. That outcome is produced
by nothing but the hand-written lookup at `IssuesController.cs:181-197`. Remove the lookup and
the write reaches the FK, which raises a `DbUpdateException` → **500**: a different declared
response, as B1 demonstrates on this very code path.

So AC4 *is* a claim whose sole evidence is a test, and
[TESTING.md](../../standards/TESTING.md) is explicit: *"a reviewer or acceptor challenges a
coverage claim — then the answer is a mutant, not an argument."*

Required: **one** mutant, on the tier it already lives on — remove the projection lookup, let the
save reach the FK, and confirm
`AC4_assigning_to_an_unknown_subject_is_rejected_and_changes_nothing` goes red **on its 400
assertion** rather than on an unrelated error. TESTING.md's own warning applies here: a red suite
is not proof the mutant caused it.

Also correct, in the same entry, *"the enum sets by the generated converter"*.
`EnumMemberJsonConverter` is hand-written, in `apps/GotIssues.Api/Serialization/` — not
generated, not a framework invariant, and so not enforcement in the sense TESTING.md means. **No
mutant is needed for AC2**: the valid basis is the one the same entry already gives, and it is a
strong one — that test was watched going red twice, on the integer serialisation and again on the
500 regression. Record it as observed-red rather than as enforcement, because *"a record that
overstates its mutant is the same defect as an assertion that overstates its subject."*

---

### Non-blocking

- **N1 — `Issue.required` omits all four new fields, so the generated *client* makes them
  optional.** `spec/openapi.yaml:379` lists `required: [id, key, projectKey, number, title,
  createdAt]`. The server contract renders `IssueType Type` (non-nullable), but
  `libs/GotIssues.Client/src/GotIssues.Client/Model/Issue.cs:77,90,103,172` renders all four as
  `IssueType?` / `Assignee?` behind `Option<>` wrappers. AC7's rationale is *"declared as defaults
  in the specification, so no client has to infer them; `assignee` is the only nullable lifecycle
  field"* — a generated C# client still has to infer them. The API always emits all four
  (verified), so adding `type, status, priority, assignee` to `required` states what is already
  true; `assignee` keeps its `oneOf: null`, which is exactly the required-but-nullable shape.
  Worth taking in the same regeneration as B1.
- **N2 — the 400 for an invalid enum carries a spurious second error.**
  `{"errors":{"updateIssueRequest":["The updateIssueRequest field is required."],
  "$.status":["'OPEN' is not one of: open, in_progress, done."]}}`. The second entry is right; the
  first names a field the client did send, and will mislead. It follows from body deserialisation
  failing and the parameter binding null. Probably pre-existing on the POST endpoints too — worth
  a look, not worth blocking.
- **N3 — the contract's "send null to leave unchanged" is untested.** `{"status":null}`,
  `{"assignment":null}` and `{}` are all stated or implied by `UpdateIssueRequest`'s description
  and covered by no test. I verified all three behave correctly; a test each would pin behaviour
  the document promises.
- **N4 — `ValidationProblem(...)` at `IssuesController.cs:190` emits a problem document with no
  `type`**, unlike every other failure in this API, which uses `Problem(type: "https://…")`. The
  `Problem` schema declares no `required`, so it is conformant — but that schema's own description
  says *"Every failure in this API uses this shape."* Consistency nit.
- **N5 — a vacuous assertion in test code.** The AC2 theory in `IssueLifecycleTests.cs` ends with
  `Assert.False(string.IsNullOrEmpty(why))`, which asserts something about the test's own
  parameter and nothing about the system. TESTING.md holds test code to production standards;
  drop the parameter or fold it into the display name.
- **N6 — unknown request properties are silently ignored.** `{"stauts":"done"}` → 200 with no
  change. Standard `System.Text.Json` behaviour and consistent with the rest of the API. Recorded
  so it is not rediscovered later as a bug.
- **N7 — nothing round-trips the generated client.** `apps/GotIssues.SmokeTests` does not
  reference `GotIssues.Client`, and neither do the integration tests, so the six client-side enum
  converters this change registers in
  `libs/GotIssues.Client/src/GotIssues.Client/Client/HostConfiguration.cs` are compiled and never
  executed. A pre-existing gap this ticket widens rather than causes; I have not raised a ticket,
  because whether the generated client is worth exercising is a product/engineering call and
  [T-0022](T-0022-adopt-clean-architecture-layering.md) is next through this code.

---

### What I checked and found correct

- **The migration is right, and it is genuinely covered.** All four columns carry
  `HasDefaultValue` in `GotIssuesDbContext` and a `defaultValue:` in
  `20260831230358_AddIssueLifecycle.cs`, so `ADD COLUMN … NOT NULL DEFAULT` backfills existing
  rows with `Task` / `Open` / `Normal` — values the contract declares, which is precisely what
  T-0005 got wrong.
  `UpgradePathTests.An_issue_that_predates_the_lifecycle_migration_reads_back_with_the_defaults`
  is a real cover, not a lookalike: it migrates to `20260831200135_AddIssues` (verified as the
  last migration before this one), seeds a project and an issue by SQL because the application
  cannot start against that schema, runs the **real** migrator, and reads back through the API.
  Drop the `HasDefaultValue` calls and EF emits `defaultValue: ""` for a non-nullable string,
  which then fails converting back to the enum — the test catches it.
- **Nothing else in the migration mistreats existing rows.** `AssigneeSubject` is added nullable,
  so the new foreign key validates against an all-NULL column and the new index is over all NULLs;
  on PostgreSQL 11+ a `NOT NULL DEFAULT` column add does not rewrite the table; `Down` is
  symmetric.
- **`Restrict`, not `Cascade`, on the assignee foreign key**, as refinement decided, and pinned by
  a test that asserts the decision rather than a code path.
- **Enums stored as names, not ordinals** — a column reading `InProgress` survives a reordering of
  the CLR enum; an integer would not.
- **AC6** verified independently: `check-drift.sh` exit 0 on a clean tree.
- **ADR-0008 satisfied**: `[Authorize(Policy = AuthorizationPolicies.Member)]` on the concrete
  controller, plus the role rule in the operation `description` and a declared `403`.
- **[ADR-0010](../../architecture/adr/ADR-0010-clean-architecture-layering.md)**: written in the
  current controller-plus-`DbContext` shape per the maintainer's recorded sequencing decision, and
  every behavioural test goes through the HTTP boundary, which is what T-0022's AC4 needs. The one
  test that uses the `DbContext` directly (`Deleting_a_user_cannot_delete_the_work_they_hold`)
  asserts a schema constraint T-0022 does not move, so it is not a coupling problem.

### The five points I was asked to judge rather than accept

1. **The converter changed nothing about existing responses.** Verified rather than assumed: on
   `main`, `git grep -l EnumMember -- libs/GotIssues.Contracts/src/` returns **nothing**, and on
   the branch it returns only `IssueType`, `IssueStatus` and `IssuePriority`. Those three are the
   only `[EnumMember]`-bearing types anywhere in the solution, so the factory cannot have altered
   any pre-existing payload; I re-read `GET /projects` and `GET /issues/{key}` and both are
   shape-identical apart from the new fields. The `Nullable<T>` fix is right — `CanConvert` claims
   the enum only and lets `System.Text.Json` do the nullable wrapping itself. Of the cases the
   tests miss: **unknown value** → 400 (`'OPEN' is not one of: …`; a numeric `2` likewise);
   **explicit null** → property left unchanged, 200; **`Write` of an undeclared value** →
   unreachable, because both mapping switches in `IssuesController` are total with `_ =>` arms.
   The static `ConcurrentDictionary` cache is shared process-wide across `JsonSerializerOptions`
   instances, which is safe only because `NamedEnumConverter` is stateless — it is. Round-tripping
   through the generated client is the one thing genuinely unverified anywhere: see N7.
2. **Wrapping assignment in an object is the right call**, and I would not take the alternative.
   Inspecting the raw JSON for property presence puts a rule the document cannot state into server
   code only, which is what
   [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) exists to
   prevent and what this product positions itself against. The wrapper is expressible, generated,
   and visible to every client. Nullable enums meaning "unchanged" are likewise fine: for a field
   that always has a value there is nothing else null could mean. The cost is real but small —
   `assignment` is now a place where absent, `null`, `{}` and `{"subject":null}` are four
   distinguishable inputs and the contract describes two of them. That is B2, and it is a
   documentation fix, not a design reversal.
3. **Removing `required: [subject]` was correct, and it loses less than it appears to — but not
   nothing.** The reasoning in the schema is sound: the generator renders `required` as
   `[Required]`, which rejects the null that unassigns, so keeping it would have made the object's
   whole purpose unexpressible. What is lost is the ability to say *"this property must be
   present"*, and the concrete cost is exactly B2 — `{"assignment":{}}` is now indistinguishable
   from `{"assignment":{"subject":null}}` and silently unassigns. Worth being precise, though:
   `required` would **not** have saved you even with a well-behaved generator, because it and the
   unassign semantics want the same slot. A property that must be present *and* may be null is
   expressible in JSON Schema but not in the C# this generator writes. So the loss is not "a guard
   we could have had was given up"; it is "this shape has an edge the contract must describe in
   prose". Describe it (B2) and nothing further is lost.
4. **The upgrade-path case genuinely covers this migration**, and I checked the migration for
   other mistreatment of existing rows and found none — details above under *what I checked*. The
   one thing the new case does not do is exercise a **pre-existing** issue through the new `PATCH`
   after the upgrade; it only reads. Cheap to add, not blocking, since the columns it would touch
   are the ones already asserted.
5. **The AC5 mutant is the right single mutant** — a transition rule refusing `done → *`, which
   the test's `done → open` step reaches on its second iteration; build-accepted, killed with the
   specific expected failure (`Expected: OK, Actual: Conflict`), and run on the cheapest tier that
   can host it. That is the claim AC5 leaves standing on a test alone, and one mutant covers all
   five of its assertions, per the proportionality rule. **One more is needed, for AC4** — not
   because AC4 is under-tested, but because the record misattributes its enforcement to the
   foreign key, which cannot produce AC4's outcome. That is B3. Nothing else here warrants one:
   AC1 / AC3 / AC7 are pinned by database defaults and a re-read, AC2's test was observed red
   twice during development, AC6 is a script's exit code, and AC8 is a policy attribute.

- **Did:** Reviewed the full diff against `main`; ran every gate in the worktree under test and
  read each exit code from the tool itself; probed the new endpoint with eleven request bodies the
  suite does not send.
- **Decided:** **Request changes** — B1, B2, B3.
- **Remaining:** the implementer addresses B1-B3 on the branch and re-requests review. N1 is worth
  taking in the same regeneration; N2-N7 are take-or-leave and need no re-review.
- **Open questions / blockers:** none for me. B2 has two acceptable resolutions and the choice is
  the implementer's.
- **Branch / PR:** `t-0006-lifecycle` @ `dbbecbd`.
- **Test state, as I measured it:** `dotnet test` 129/129 exit 0 · build 0 warnings exit 0 ·
  `dotnet format` exit 0 both · `check-drift.sh` exit 0 · `validate.py` exit 0 ·
  `smoke.sh` 13/13 exit 0.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — three blocking findings, all from eleven requests the suite does not send

`claude-rev-7a03` approved every gate and then found three defects by driving the endpoint with
request bodies no test sends — the exploration step, applied to a review rather than an acceptance.

#### B1 — T-0004's defect, recurring in a field I added two tickets later

`AssignmentChange.subject` was **the only free-text request string in the contract without a
pattern**. Every other one has one *because T-0004 shipped exactly this defect*, with the reasoning
written into the specification beside them. A NUL reached PostgreSQL and came back
`22021: invalid byte sequence` as an unhandled **500**, where AC4 requires a 400 naming the field.

I wrote the rule, recorded why it exists, applied it to five fields, and then added a sixth without
it. The pattern is now there, with a test.

#### B2 — the contract described two of four distinguishable inputs

`{"assignment":{}}` unassigns and returns 200. Nothing said so, and a client that simply forgets
`subject` loses the assignee silently.

The honest fix is documentation, not rejection: the API **cannot** distinguish an omitted `subject`
from an explicit null — that is the same limitation the wrapper object exists to work around one
level up, reappearing inside it. So the schema now states all three shapes and what each means, and
a test asserts the two that were undocumented. Rejecting `{}` would require the raw-JSON inspection
this design exists to avoid.

#### B3 — my mutation record misattributed AC4's enforcement, and the correction is the finding

I recorded the assignee's existence as "enforced by a foreign key", and used that to justify not
mutating AC4. The reviewer's objection is exactly right: **the foreign key stops the write; it
cannot produce AC4's outcome.** AC4 requires a 400 naming the field, and only the hand-written
lookup produces that. The FK produces a 500.

Measured rather than conceded — remove the lookup and let the FK be the enforcement:

| Mutant | Result |
| --- | --- |
| Assignee lookup removed; foreign key left as the only guard | **Killed** — `Expected: BadRequest, Actual: InternalServerError` |

So AC4 *is* a claim where a test is the sole evidence, and under the narrowed rule it needed a
mutant. **The error was not in the testing; it was in the record** — and a wrong enforcement note
is worse than a missing one, because it is the reason a mutant does not get run. B1 is the same
mistake made in code: I believed a guard existed where none did.

Also relabelled: the enum sets are enforced by `EnumMemberJsonConverter`, which is **hand-written
in `apps/`**, not by the generator. The evidence for it was already sound — observed red twice
during development — but "the generated converter" overstated where it comes from.

#### Non-blocking taken

**N1** — `Issue.required` omitted the four new fields, so the generated *client* rendered them
optional and AC7's "no client has to infer them" was not delivered end to end. Added while
regenerating for B1. **N5** — an assertion that could not fail, removed.

- **Did:** Added the missing pattern and its test; documented all three assignment shapes and
  tested the two that were undocumented; ran the AC4 mutant and corrected the enforcement record;
  took N1 and N5.
- **Decided:** document `{}` rather than reject it — rejecting it needs the raw-JSON inspection the
  contract-first rule exists to prevent.
- **Remaining:** re-review.
- **Open questions / blockers:** none.
- **Test state:** `dotnet test` **131/131** (17 unit, 114 integration) · build 0 warnings ·
  `dotnet format` exit 0 · `check-drift.sh` exit 0 · `tools/smoke.sh` **13/13** exit 0 ·
  `validate.py` exit 0. *(Corrected: this line originally said "drift and smoke below" and nothing
  followed it — a promise of evidence standing in for the evidence, which is the smaller version of
  what B3 was about.)*


### 2026-08-31 — Code review, second pass (claude-rev-7a03) — ENG · ARCH — **Approve**

Re-reviewed `t-0006-lifecycle` @ `d59e191` against `main`, in the worktree. All three blocking
findings are closed. Verified rather than accepted, in each case by exercising the running system
and by re-running the one mutant I asked for.

#### Gates, re-run here, each exit code read from the tool itself

| Gate | Result |
| --- | --- |
| `dotnet test` | **exit 0** — 17 unit + 114 integration = **131** passed, 0 failed |
| `dotnet build --no-incremental` | **exit 0** — 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | **exit 0** (solution) and **exit 0** (`GotIssues.SmokeTests.csproj`) |
| `./tools/check-drift.sh` | **exit 0** on a clean tree |
| `./tools/smoke.sh` | **exit 0** — 13 passed, 0 failed (6 m 22 s) |
| `python3 tools/validate-project-os/validate.py` | **exit 0** — 23 tickets, 10 ADRs |

#### B1 — closed, and the two ways the fix could have gone wrong are both checked

`spec/openapi.yaml:573` now carries `pattern: '^[^\u0000-\u001F\u007F]+$'`, rendered as
`[RegularExpression]` on `AssignmentChange.Subject`. Probed:

```
{"assignment":{"subject":"\u0000bad"}}   -> 400 problem+json, errors: {"Assignment.Subject": [...]}
{"assignment":{"subject":"a\tb"}}        -> 400 problem+json, errors: {"Assignment.Subject": [...]}
{"assignment":{"subject":"a\u007Fb"}}   -> 400 problem+json, errors: {"Assignment.Subject": [...]}
```

All three name the offending field, and the issue is unchanged on a subsequent read — AC4's stated
outcome, from the contract boundary rather than from PostgreSQL. The 500 is gone.

The two regressions a pattern on this particular field could have caused, both checked because
neither is obvious:

- **`{"assignment":{"subject":null}}` still unassigns** (200, `assignee: null`).
  `RegularExpressionAttribute` treats null as valid — only `[Required]` rejects it. That is the
  exact hazard that made `required: [subject]` unusable, and it does not recur here.
- **A subject outside ASCII is not over-rejected.** `café-user` validates, and end to end: seeded
  through the projection, assigned, and read back as
  `{"subject":"café-user","displayName":"Café User"}`. The pattern excludes control characters
  only, as intended.

One note, not blocking: `A_subject_carrying_a_control_character_is_rejected_at_the_boundary`
asserts the 400 and the media type but not that the document names the field — one assertion short
of what AC4 states for this input, and the test immediately above it (`AC4_…`) shows the pattern.
The behaviour is right; the test is a little weaker than the criterion.

#### B2 — closed

`spec/openapi.yaml:546-561` now states all three shapes in a table, including that `{}` unassigns
and returns 200, and that omitting `assignment` leaves the holder alone. Probed against the
document: `{"assignment":{}}` → 200 with `assignee: null`, persisted; `{"assignment":null}` → 200
with the holder untouched. Both are now under test in
`An_assignment_object_without_a_subject_unassigns_as_the_contract_says`.

I agree documentation is the honest fix rather than a concession. The limitation is real —
`System.Text.Json` binds an absent property and an explicit null to the same `null`, so the server
genuinely cannot tell them apart — and rejecting `{}` would need exactly the raw-JSON inspection
this design exists to avoid. Stating it in the schema is what
[ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) asks for: the
document says what the system does.

#### B3 — closed, and I re-ran the mutant rather than taking it on trust

I asked for this mutant, so I checked it myself. This project has recorded roughly eighty mutants
and nine invalid ones, **two of them from reviewers**, and
[TESTING.md](../../standards/TESTING.md) is explicit that the build accepting a mutant is necessary
and not sufficient.

Removed the projection lookup at `IssuesController.cs:181-197`, leaving the foreign key as the only
guard, and ran that one test:

```
Failed  IssueLifecycleTests.AC4_assigning_to_an_unknown_subject_is_rejected_and_changes_nothing
  Assert.Equal() Failure: Values differ
  Expected: BadRequest
  Actual:   InternalServerError
```

Reverted; working tree verified clean. The mutant is valid on all three of the standard's counts:
the build accepted it, the failure is the AC4 test's **own** status-code assertion rather than an
unrelated error, and the cause is the removal. The recorded result matches, and the record states
what it proves.

The framing in the entry above is right and worth keeping: *the error was in the record, not in the
testing* — a wrong enforcement note is worse than a missing one, because it is the reason a mutant
does not get run.

**One thing to add to it, because "one habit" undersells the shape.** Both instances were *a guard
asserted to exist by inference from an adjacent mechanism*. The foreign key really is adjacent to
the lookup and really does concern the assignee's existence — just not AC4's outcome. The pattern
rule really was adjacent, on five neighbouring fields. In both cases the question that catches it is
the same one: **what exactly does this mechanism reject, and is the rejection I need the one it
performs?** Worth carrying into [T-0022](T-0022-adopt-clean-architecture-layering.md), where a
layering refactor moves guards across boundaries and every move invites that inference again. This
may be retro material rather than ticket material.

#### N1 and N5 — taken, and N1 verified end to end

`libs/GotIssues.Client/src/GotIssues.Client/Model/Issue.cs` now renders `IssueType Type`,
`IssueStatus Status` and `IssuePriority Priority` non-nullable, so AC7's *"no client has to infer
them"* is delivered to the generated client, not only to the server contract. Leaving `assignee`
out of `required` is the right call and I would not change it: it is genuinely nullable, `Assignee?`
is an honest rendering, and AC7's *"`assignee` is the only nullable lifecycle field"* now reads true
in the generated client too.

---

### The two notes I was asked to read: N2 belongs elsewhere, N4 belongs here

**N2 — agreed, follow-up ticket. I confirmed it is pre-existing rather than inferring it**, since
that is the inference this ticket has already been caught by twice. Probed the endpoints this
ticket does not touch:

```
POST /projects              {"key":123,"name":"P"}   -> 400, errors: {"createProjectRequest": ["The createProjectRequest field is required."], "$.key": [...]}
POST /projects/BBB/issues   {"title":123}            -> 400, errors: {"createIssueRequest":  ["The createIssueRequest field is required."],  "$.title": [...]}
```

Nothing to do with enums: it is `BodyModelBinder` adding a required-error whenever the input
formatter throws and the parameter binds null, and every body-taking endpoint in the API has it
today. Fixing it means changing `InvalidModelStateResponseFactory` or the binder's behaviour
globally — shared behaviour, correctly out of scope for a lifecycle ticket. Your instinct is right.

**N4 — I read this one differently, and it is not a framework default.** `ValidationProblem` has
**exactly one call site in the entire codebase**, `IssuesController.cs:190`, and this ticket added
it; every other failure in the API goes through `Problem(type: "https://…")`. The framework's own
400s do carry a `type` — the B1 pattern rejection above returns
`"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1"`. So the missing `type` is created by
the hand-rolled `ValidationProblemDetails` here, not inherited from anywhere, and it is a one-line
change in this ticket's own code.

**I am not blocking on it**: the `Problem` schema declares no `required`, so the response is
contract-conformant, and this is the MVP's last ticket. My preference is to take it here. If you
would rather fold it into N2's ticket, that is fine — but please record it as *this ticket's one
hand-rolled problem document*, not as a framework default, because the next reader will otherwise
look for it in the framework and not find it.

The remaining notes are unchanged and need no action for this verdict: **N3** (the contract's "send
null to leave unchanged" is now partly covered by B2's test; `{"status":null}` still is not),
**N6**, **N7**.

Small record-keeping point: the previous entry's *Test state* line ends "drift and smoke below" with
nothing below it. My measurements above cover both, on this commit.

---

- **Did:** Re-reviewed the diff `f94839f..d59e191`; re-ran every gate in the worktree under test;
  re-probed the three fixed paths plus the two regressions the new pattern could have caused;
  independently re-ran the AC4 mutant and reverted it; verified N2 reproduces on endpoints this
  ticket does not touch.
- **Decided:** **Approve.** Merge may proceed.
- **Remaining:** the implementer merges and performs the handover `os:` status commit on the trunk
  per [GIT.md](../../standards/GIT.md). N4 either taken here or ticketed with N2, per the reading
  above.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0006-lifecycle` @ `d59e191`.
- **Test state, as I measured it:** `dotnet test` **131/131** exit 0 · build 0 warnings exit 0 ·
  `dotnet format` exit 0 both · `check-drift.sh` exit 0 · `validate.py` exit 0 ·
  `smoke.sh` 13/13 exit 0.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — approved; N4 taken here after the reviewer measured it

`claude-rev-7a03` approved, having re-run its own requested mutant rather than trusting mine —
*"this project has nine invalid mutants on record, two from reviewers"* — and having checked the
two ways B1's fix could have gone wrong that I had not: `{"subject":null}` **still unassigns**
(`RegularExpressionAttribute` treats null as valid, so the hazard that made `required` unusable
does not recur), and `café-user` is not over-rejected.

**N4 belongs here, and I had it wrong.** I called the missing `type` a framework default affecting
every controller. The reviewer measured it: `ValidationProblem` has **exactly one call site in the
codebase**, added by this ticket, and the framework's own 400s *do* carry a `type`. So the one
failure response in this API without one is mine, not inherited — a one-line fix in this ticket's
code rather than shared behaviour smuggled into a lifecycle ticket. Taken.

I was reasoning from the shape of the problem (*a response-shape inconsistency, therefore
probably framework-wide*) where the reviewer counted the call sites. That is the same move as B3 —
inferring a mechanism from an adjacent one — which makes three instances in this ticket, not two.

**The reviewer's sharpening of that pattern is better than mine and I am adopting it:** not "one
habit" but specifically **a guard or a cause asserted by inference from an adjacent mechanism.**
The foreign key was adjacent to the lookup and does concern assignee existence — just not AC4's
outcome. The pattern rule was adjacent, on five neighbouring fields. The framework was adjacent to
`ValidationProblem`. Each inference was reasonable, which is exactly what stopped me checking.

It flagged this as retro material and as a hazard for
[T-0022](T-0022-adopt-clean-architecture-layering.md) specifically: a layering refactor moves
guards across boundaries, which is precisely when "the adjacent thing must be handling it" becomes
tempting and wrong.

**Also taken:** the B1 regression test asserted 400 and media type but not the field name — one
assertion short of what AC4 states. Now asserted, and it surfaced something small worth recording:
the framework's validation key and my hand-rolled one differ in casing for the same field, so the
assertion matches case-insensitively and reports what was actually present. Not worth a fix; worth
knowing before someone matches on an exact key.

**And a record correction:** the previous entry's Test state ended *"drift and smoke below"* with
nothing below it — a promise of evidence standing in for evidence, which is the small version of
what B3 was about. Both measurements are now in that line.

**N2 goes to [T-0024](T-0024-spurious-validation-error-on-every-body-taking-endpoint.md)**, which
the reviewer confirmed by probing endpoints this ticket does not touch: `POST /projects` and
`POST /projects/{key}/issues` both emit the same spurious *"field is required"* beside the real
error. That one genuinely is shared behaviour on every body-taking endpoint.

- **Did:** Took N4 after the reviewer showed it was this ticket's code; completed the B1 test's
  assertion; corrected the Test state line; raised T-0024 for N2.
- **Decided:** N4 here, N2 elsewhere — on the evidence of where each is produced, not on how each
  looked.
- **Remaining:** merge, then acceptance.
- **Open questions / blockers:** none.
- **Test state:** `dotnet test` **131/131** · `dotnet format` exit 0 · build 0 warnings ·
  `check-drift.sh` exit 0 · `validate.py` exit 0 · `tools/smoke.sh` **13/13** exit 0 (measured on
  the previous commit; this change is one attribute and two assertions, no runtime path altered).


### 2026-08-31 — QA / Test Engineer (claude-qa-2e64) — acceptance of `4e2c6a2` on `main`: **FAIL**

Independent acceptance per [`acceptance-test`](../../skills/acceptance-test/SKILL.md). I did not
implement this ticket; scenarios were derived from Problem / Outcome / Scope / AC / Examples before
the Work Log was read.

**All eight acceptance criteria hold.** The ticket fails on **Definition of Done item 6**, on the
same three documentation lines that failed [T-0004](T-0004-create-and-list-projects.md)'s and
[T-0005](T-0005-create-and-read-issues.md)'s acceptance — the third occurrence, and the one
[`CURRENT_SPRINT.md`](../../delivery/CURRENT_SPRINT.md) (retro input, 2026-08-31) predicted by name.

#### Gates, each exit code read from the tool itself, in this checkout on `main`

| Gate | Result |
| --- | --- |
| `dotnet test` | **exit 0** — 17 unit + 114 integration = **131** passed, 0 failed, 0 skipped |
| `dotnet build --no-incremental` | **exit 0** — 6 projects, **0 warnings**, 0 errors |
| `dotnet format --verify-no-changes` | **exit 0** (solution) and **exit 0** (`GotIssues.SmokeTests.csproj`) |
| `./tools/check-drift.sh` | **exit 0** — *"generated code matches spec/openapi.yaml"*, tree clean before and after (AC6) |
| `./tools/smoke.sh` | **exit 0** — 13 passed, 0 failed (8 m 08 s) |
| `python3 tools/validate-project-os/validate.py` | **exit 0** — 24 tickets, 10 ADRs |

#### How the criteria were verified

Beyond the suite, I ran a live Compose stack under its own project name (`-p qa2e64`) on ephemeral
ports 18085/18086, per [TESTING.md](../../standards/TESTING.md)'s attribution rule. **Attribution
confirmed rather than assumed:** `qa2e64-api-1` was `running/healthy` before any response was
trusted, and stopping that container made `:18085/health` stop answering (curl exit 7) — so nothing
below was answered by another stack on this machine, and there are several. `docker compose down -v`
ran first for stale volumes and again at the end; no containers, volumes or networks remain, and the
working tree is clean.

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC1 | **PASS** | Live: each of `type`, `status`, `priority` set and re-read through a separate `GET` — 13 changes, all 200, all persisted. `IssueLifecycleTests.AC1_a_field_changes_and_stays_changed` (theory x3) and `AC1_an_omitted_field_is_left_alone` |
| AC2 | **PASS** | Live: `cancelled`, `OPEN`, `in-progress`, `epic`, `critical`, numeric `2`, empty string and `["open"]` all returned **400 `application/problem+json`**, each naming `$.status` / `$.type` / `$.priority` and quoting the declared set. Nothing stored: after `{"status":"done","type":"epic","priority":"high"}` the issue still read `task` / `open` / `low` |
| AC3 | **PASS** | Live: assign, reassign, unassign — each persisted; `assignee` carries `subject` **and** `displayName` (`{"subject":"alice","displayName":"Alice Example"}`), and a null `displayName` for a user who has none. A never-assigned and an unassigned issue both read `"assignee": null` — indistinguishable, as refinement decided |
| AC4 | **PASS** | Live: unknown subject gives **400 problem+json**, `errors: {"Assignment.Subject": ["No user with subject 'ghost' is known to this system."]}`, and a *valid* `priority` sent in the same request did **not** land. NUL, TAB, DEL, lone surrogates and a 256-character subject all give 400 naming the field. I could not find an input that produces a 500 |
| AC5 | **PASS** | Live: `open -> done -> open -> in_progress -> done -> in_progress -> open`, including the backwards `done -> open` twice — every one 200, no transition refused. Also `bug <-> task` and every priority pair |
| AC6 | **PASS** | `./tools/check-drift.sh` exit 0 on a clean tree; `git status` empty afterwards |
| AC7 | **PASS** | Live: `POST /projects/QAX/issues` returns `"type":"task","status":"open","priority":"normal","assignee":null` on creation and on re-read. Backed by database defaults, not CLR initialisers — confirmed in the live schema (`'Task'::character varying`, `'Open'`, `'Normal'`) |
| AC8 | **PASS** | Live, with **real Duende client-credentials tokens**: the `member` client changed all four fields and assigned (200); the `admin` client likewise; no token gives 401; a garbage token gives 401. The same member is still refused `POST /projects` (403), so the policy is a floor and not a blanket |

#### Exercising the system in a state it was not built in

This is where the standard says the yield is, so it is where the run was spent.

**1. The real migrator against a populated `issues` table — the T-0005 defect shape.** I did not
trust `UpgradePathTests`; I reproduced it against live infrastructure. On the running stack I
created projects and issues through the API (one patched to `bug` / `in_progress` / `high` and
assigned), then reverted the live database exactly as the migration's `Down()` does — dropped the
foreign key, the index and the four columns and deleted the `20260831230358_AddIssueLifecycle` row
from `__EFMigrationsHistory` — and inserted **500 further issue rows through the old schema**, so
the upgrade met 505 pre-existing rows rather than one. Then I ran the **real compose migration
step** (`docker compose -p qa2e64 run --rm migrator`, exit 0), which emitted:

```
ALTER TABLE issues ADD "AssigneeSubject" character varying(255);
ALTER TABLE issues ADD "Priority" character varying(20) NOT NULL DEFAULT 'Normal';
ALTER TABLE issues ADD "Status"   character varying(20) NOT NULL DEFAULT 'Open';
ALTER TABLE issues ADD "Type"     character varying(20) NOT NULL DEFAULT 'Task';
CREATE INDEX "IX_issues_AssigneeSubject" ON issues ("AssigneeSubject");
ALTER TABLE issues ADD CONSTRAINT "FK_issues_users_AssigneeSubject" ... ON DELETE RESTRICT;
```

Result: **all 505 pre-existing rows backfilled `Task` / `Open` / `Normal`, unassigned, and zero rows
carrying a value the contract does not declare** — checked in SQL, not only through the API. Every
sampled row (`OLD-1`, `OLD-2`, `OLD-3`, `OLD-250`, `OLD-503`, `QAX-1`) reads back 200 through the
API. The `GOTI-0` class of defect does not recur here.

**I also closed the gap review left open** (*"the new case does not exercise a pre-existing issue
through the new `PATCH` after the upgrade; it only reads"*): I PATCHed rows that predate the
migration — status, type, priority and assignment, including a non-ASCII subject — all 200, all
persisted on re-read, and a new issue filed into the pre-existing project came out `OLD-504` with
the declared defaults. Re-running the migrator was idempotent (*"No migrations were applied. The
database is already up to date."*, exit 0); restarting the API against the upgraded, populated
database came up healthy with no pending-model-changes error; and `DELETE FROM users WHERE
"Subject" = 'alice'` was refused live by `FK_issues_users_AssigneeSubject` — `Restrict` is real, not
merely configured.

**2. A dependency removed underneath a live service.** `docker stop qa2e64-postgres-1` with the API
still serving: `PATCH` (status), `PATCH` (assignment) and `GET` all returned **500 with
`application/problem+json`** and the declared body — not the zero-length body T-0004 shipped.
`/health` reported `Unhealthy` with `"database not reachable"` (503), and the API recovered by
itself when PostgreSQL came back. Nothing hung and no exception text reached the caller.

**3. Input nobody anticipated.** Roughly 45 request shapes the suite does not send, all against the
live stack. No 500, no undeclared response, no empty body:

- `U+0000`, TAB, `U+007F` and a **trailing newline** in `assignment.subject` all give 400 naming
  `Assignment.Subject`. The trailing newline is worth recording: .NET's `$` matches before a final
  newline, so the declared pattern *alone* would have admitted `"alice\n"` — it is
  `RegularExpressionAttribute`'s whole-string length check that closes it. The hole T-0004 paid for
  does not reopen, but the reason is the attribute, not the pattern.
- **Lone surrogates** (`\ud800`, `\udc00`) give 400 from `System.Text.Json` before the value can
  reach Npgsql. This was the one remaining route by which a request string could still have reached
  PostgreSQL unstorable, since the new pattern excludes control characters but not surrogates; the
  reader closes it. A valid surrogate pair (an emoji) is accepted and correctly reported unknown.
- A 256-character subject gives 400 on `maxLength`; 255 gives 400 as an unknown user — so the
  boundary sits exactly at the declared value. `café-user` is **not** over-rejected and round-trips
  intact.
- Malformed bodies — empty, `not json`, `null`, `[]`, `"open"`, `{"assignment":"alice"}`,
  `{"assignment":[]}`, `{"assignment":{"subject":7}}` — all give 400 problem+json. `text/plain`
  gives 415. Duplicate JSON keys: last wins, 200. An unknown property is ignored, 200 (N6).
- Path keys: `QAX-0`, `qax-1`, `QAX--1`, `QAX-9999999999`, `QAX-1%0A` and `QA%20X-1` give 400
  naming `issueKey`; `QAX-99`, `ZZZ-1` and `QAX-999999999` give 404 problem+json. `SplitKey`'s
  "cannot fail" comment is now measured rather than inferred.
- **20 concurrent PATCHes** on one issue (five each of four different fields): 20 x 200, and all
  four changes survived — no lost update, because EF updates only the columns that changed.

**4. All four assignment shapes, against what the contract now says.** `{"subject":"alice"}`
assigns; `{"subject":null}` unassigns; `{}` unassigns and returns 200; an **absent** `assignment`
leaves the holder alone while a sibling `status` change lands. All four match `AssignmentChange`'s
table at `spec/openapi.yaml:546-561`. B2 is genuinely closed as documentation.

**5. The `EnumMemberJsonConverter` blast radius — checked, not assumed.** I confirmed independently
that `git grep -l EnumMember` over `libs/GotIssues.Contracts/src/` returns **nothing at `4e2c6a2^`**
and only the three new enums after it, so no pre-existing payload can have changed; `GET /projects`,
`POST /projects`, `GET /issues/{key}` and `POST /projects/{key}/issues` were re-read live and are
shape-identical apart from the new fields. The values the tests do not send: an unknown string gives
400, a numeric gives 400, an empty string gives 400, and an explicit `null` gives 200 with the field
unchanged — so the `Nullable<T>` 400-to-500 regression does not recur. `Write` of an undeclared
value is unreachable: `ToContract` is the only construction site of a contract `Issue` in the
solution and both mapping switches are total.

**6. N7 closed for this ticket's surface — the one thing nothing anywhere exercised.** Review
recorded that no test round-trips the generated client, so the six client-side enum converters
registered in `HostConfiguration.cs` are compiled and never run. I built a throwaway console app
**outside the repository** against `libs/GotIssues.Client`, using the exact `JsonSerializerOptions`
`HostConfiguration` builds. It deserialised a live response into `GotIssues.Client.Model.Issue`
(`type=Bug status=InProgress priority=High assignee=alice/Alice Example`) and serialised
`UpdateIssueRequest` back out as `{"type":"bug","status":"in_progress","priority":"high","assignment":{"subject":"alice"}}`
and `{"type":null,"status":"done","priority":null,"assignment":{"subject":null}}` — both accepted
200 and round-tripped correctly. **The published client works against the published contract.** One
thing worth recording against B2's worry: the generated `AssignmentChange` has no parameterless
constructor, so a C# client cannot produce `{}` by forgetting a property — the accidental-unassign
hazard is real for hand-written JSON, not for this client. All artefacts were outside the
repository; `git status` is clean.

---

### Blocking

#### F1 — `README.md:7`, `README.md:113` and `ARCHITECTURE.md:5` say this ticket's deliverable does not exist. Third occurrence. DoD item 6.

| Line | What it says today | Why it is false |
| --- | --- | --- |
| `README.md:7` | *"…each issue carries a key like `GOTI-1` numbered within its project. **Comments and lifecycle fields come next.**"* | Lifecycle fields do not "come next"; they are merged at `4e2c6a2` and I exercised them against a live stack |
| `README.md:113`, under `### Not here yet` | *"**Issue lifecycle and comments.** An issue can be created and read, but **it carries no status, priority or assignee yet** ([T-0006](T-0006-issue-lifecycle-fields.md))"* | This ticket's deliverable, named by link, listed under a heading that says it does not exist |
| `project-os/architecture/ARCHITECTURE.md:5` | *"What remains intended rather than built: **an issue's lifecycle fields** ([T-0006](T-0006-issue-lifecycle-fields.md))…"* | Same |

[DoD](../../governance/DEFINITION_OF_DONE.md) item 6 names *"README/setup instructions affected by
the change"*, and `README.md:113` is affected **by name**.
[DOCUMENTATION.md](../../standards/DOCUMENTATION.md) is explicit: *"Stale documentation is a defect
… fix in place when the fix is within your current ticket's scope."* And `ARCHITECTURE.md:7`
addresses this ticket directly — *"if you are reading this while shipping something listed as
intended, it is now your line to fix."*

**This is not a first offence and it was predicted by name.** T-0004's acceptance failed on these
three lines; T-0005's acceptance failed on the same three lines and scored DoD item 6 as its sole
failure; and on 2026-08-31 `claude-rev-5c14` recorded them in
[`CURRENT_SPRINT.md`](../../delivery/CURRENT_SPRINT.md) *"because **T-0006 lands before the retro
and will falsify the same lines a third time**"*. It did. Neither the implementation nor two review
passes caught it — which is itself evidence that reminder-shaped countermeasures do not work here.
The retro has three candidate fixes recorded (delete the enumerations; generate them; make
`validate.py` fail on a `done` ticket cited under *Not here yet*), and this run is its third data
point.

**Fix:** rewrite the three lines to describe what exists — lifecycle fields shipped; *Not here yet*
narrows to listing and filtering ([T-0007](T-0007-list-and-filter-issues.md)), comments
([T-0008](T-0008-comment-on-an-issue.md)) and user tokens
([T-0018](T-0018-user-subject-tokens.md)) — then re-grep for survivors. No code change; `validate.py`
is the only gate it touches.

I have deliberately **not** fixed it myself: acceptance does not modify the change under test.

---

### Non-blocking

- **F2 — the migrator and the API now log three EF `Model.Validation[20601]` warnings on every
  start**, new with this ticket: *"The 'IssueStatus' property 'Status' … is configured with a
  database-generated default, but has no configured sentinel value. The database-generated default
  will always be used for inserts when the property has the value '0'."* Same for `Type` and
  `Priority`. `projects.NextIssueNumber` produces none, because `0` is already `int`'s sentinel.
  **Harmless today, latent tomorrow:** `Data.IssueType` / `IssueStatus` / `IssuePriority` all start
  at `1` and every CLR initialiser is non-zero, so EF always sends an explicit value — I verified a
  fresh issue writes `Task` / `Open` / `Normal` while a pre-existing row keeps the database default.
  But if anyone ever adds a `= 0` member to one of those enums, EF will silently substitute the
  database default on insert, and this warning is the only notice. One `HasSentinel(...)` per
  property removes both the noise and the trap; recording the enforcement is the alternative.
  Neither the Work Log nor either review pass mentions these three warnings, and they appear on
  every `docker compose up`.
- **F3 — `assignment.subject: ""` reaches the database lookup despite the declared pattern.**
  `spec/openapi.yaml:573` declares a pattern excluding C0 controls and DEL, which the empty string
  violates — but `RegularExpressionAttribute` treats an empty string as valid, so the value reaches
  `IssuesController.cs:181` and comes back *"No user with subject '' is known to this system."* The
  **outcome satisfies AC4** (400, problem+json, field named, issue unchanged) and PostgreSQL stores
  an empty string happily, so nothing breaks — but the document and the enforcement disagree about
  *which* rule rejects it, which is the family of divergence
  [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) exists to
  prevent. `minLength: 1` states what is already almost true. Not blocking: no client can observe
  the difference.
- **F4 — the AC2 theory's "and nothing changed" assertion re-reads only `status`.**
  `IssueLifecycleTests.cs:152` asserts `status == "open"` for all three theory rows, so for
  `{"type":"epic"}` and `{"priority":"critical"}` it asserts a field the request never mentioned.
  I verified live that neither `type` nor `priority` changes on rejection, so the behaviour is right
  and the test is one assertion short of its own comment — the same shortfall review already caught
  once, on the B1 regression test.
- **F5 — three claims re-measured rather than accepted**, because the implementer's Work Log names
  *"a guard or a cause asserted by inference from an adjacent mechanism"* as this ticket's recurring
  fault and asks for further instances. I found no fourth: `ValidationProblem` really does have
  **exactly one call site** across `apps/` and `libs/` (grepped), and its 400 now carries
  `"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1"` in a live response; the `Restrict`
  foreign key really does refuse a live `DELETE`; and `SplitKey`'s *"cannot fail"* comment holds
  against six malformed keys. The corrected enforcement record for AC2 and AC4 reads true against
  what I measured.
- **N2** is correctly deferred to [T-0024](T-0024-spurious-validation-error-on-every-body-taking-endpoint.md),
  whose *In Scope* takes it on by name — I reproduced the spurious *"The updateIssueRequest field is
  required."* beside every real body error, and the same on `POST /projects`. **N3**
  (`{"status":null}` untested) and **N6** (unknown properties ignored) remain recorded rather than
  ticketed; both behave as documented and I verified them live.

---

### Definition of Done, walked

| # | Item | Verdict |
| --- | --- | --- |
| 1 | Implementation complete; nothing out of scope smuggled in | **Pass** — the diff touches `spec/`, generated `libs/`, the controller, `Data/`, the migration, `Serialization/`, `Program.cs` (+8, the converter registration) and tests. No transition validation, no per-project sets, no assignment history, no notifications |
| 2 | All acceptance criteria verified independently | **Pass** — all eight, above, against executed tests and a running stack |
| 3 | Automated tests exist and pass | **Pass** — 131/131, 0 skipped; smoke 13/13. The new tests genuinely encode the criteria, and the vacuous assertion review found (N5) is gone |
| 4 | No known unrecorded defects | **Pass** — N2 goes to T-0024, whose scope accepts it; N3/N6/N7 recorded; F2–F4 recorded here |
| 5 | Code quality | **Pass** — two review passes ending in `Approve`; build 0 warnings; format clean on both projects; no TODO, `Console.Write` or debug scaffolding anywhere in the diff |
| 6 | Documentation updated | **FAIL — F1.** The OpenAPI specification, which is this project's user-facing documentation, is excellent and states even the awkward `{}` case. `README.md` and `ARCHITECTURE.md` are not |
| 7 | Work Log complete | **Pass** — resumable, and honest about its own corrections |
| 8 | State updated | Pending completion |
| — | Regression test for a fixed bug | **Pass** — `A_subject_carrying_a_control_character_is_rejected_at_the_boundary` for B1; I confirmed the 500 it replaces cannot be reproduced |
| — | ADR recorded | **N/A** — no decision at the ADR bar; ADR-0004, ADR-0008 and ADR-0010's sequencing are all respected |
| — | Security | **Pass** — every new external input is declared in the spec and enforced by generated binding; no secrets; no new dependency. F3 is the one place declaration and enforcement differ, and it is not exploitable |
| — | Migrations | **Pass** — scripted, reversible (`Down` is symmetric; I executed its equivalent against a live populated database), and tested both by `UpgradePathTests` and by the real migrator against 505 pre-existing rows |
| — | Observability | **Concern, non-blocking** — see F2 |
| — | Deployment | **Pass** — `docker compose up --wait` reaches every service healthy from a clean volume, and the migration step is idempotent |

**No deviation needs recording.** F1 is a fixable defect, not something to waive — the same item was
fixed rather than waived on T-0004 and T-0005.

### Does the MVP deliver a usable issue tracker through the API?

Asked because this closes SPRINT-003's goal. **Through the contract the loop is real, and I walked
it end to end on a live stack with real Duende tokens:** an `admin` creates a project; either role
files issues into it with per-project numbering (`QAX-1`, `OLD-504`); anyone reads one by the key
people quote; moves it through `open` / `in_progress` / `done` in any direction; sets `type` and
`priority`; and assigns or unassigns a person who reads back with a display name. Every failure
answers `application/problem+json`, including with the database stopped. Five operations, all
specified first, all generated, drift-free.

**Two honest limits a first user meets immediately, both already ticketed:**

1. **Nothing lists issues** ([T-0007](T-0007-list-and-filter-issues.md)). The only way to reach an
   issue is to already know its key, so a tracker holding more than a handful is not yet navigable.
   `GET /projects` is paginated; issues have no collection endpoint at all.
2. **Assignment cannot be used against a live stack without seeding the database by hand.** No token
   this system issues carries a `sub` ([T-0018](T-0018-user-subject-tokens.md)), so
   `UserProjectionMiddleware` never writes and `users` stays empty — I had to `INSERT` rows with
   `psql` to exercise AC3, AC4 and AC8's assignment. The ticket records this as a *testing*
   constraint; it is equally a *product* one until T-0018 lands, and it is the difference between
   "assignment works" and "a person can be assigned".

Neither is a defect in this ticket — both are out of its scope and named in it. The honest answer to
"is this a usable issue tracker" is: **the write path is; the read path is one endpoint short**, and
`assignee` names a subject rather than a person until T-0018.

#### One conflict resolved rather than silently followed

[`acceptance-test`](../../skills/acceptance-test/SKILL.md)'s fail branch says *"`status: in-progress`,
`owner: none`"*, but `validate.py` rejects that combination (*"status in-progress requires an
owner"*) and this repository's three previous acceptance failures — `a3f27d1`, `9f89ddd` (T-0004)
and `303fafb` (T-0005) — all restored the implementer as owner. Precedence puts the skill last
([`README.md`](../../README.md)), so I followed the validator and the precedent and set
`owner: claude-sm-9d4e`, recording the divergence here rather than quietly picking one. The skill's
wording is worth correcting in a governance change; it is not this ticket's problem.

- **Did:** Derived scenarios from the requirements before reading the Work Log; ran all six gates in
  this checkout, reading each exit code from the tool itself; verified all eight criteria against
  executed tests and a live Compose stack under `-p qa2e64` on ports 18085/18086 with attribution
  proved by stopping the container; reverted that live stack to the pre-T-0006 schema with 505 rows
  present and ran the real compose migrator against it; stopped PostgreSQL under the running API;
  drove ~45 unanticipated request shapes; round-tripped the generated client against the live API;
  walked the Definition of Done.
- **Decided:** **FAIL** — one blocking finding (F1, DoD item 6). All eight acceptance criteria pass.
- **Remaining:** an engineer takes F1 (three documentation lines, no code change), decides F2–F4,
  and re-submits. F1 needs no re-run of the suite.
- **Open questions / blockers:** none for me. F1's fix is mechanical; whether the *class* of defect
  is fixed by prose, by generation or by a validator rule is the retro's call, and this run is its
  third data point.
- **Branch / PR:** n/a — accepted against `4e2c6a2` on `main`.
- **Test state, as I measured it:** `dotnet test` **131/131** exit 0 · `dotnet build
  --no-incremental` 0 warnings exit 0 · `dotnet format --verify-no-changes` exit 0 (solution) and
  exit 0 (`GotIssues.SmokeTests.csproj`) · `./tools/check-drift.sh` exit 0 · `./tools/smoke.sh`
  13/13 exit 0 · `python3 tools/validate-project-os/validate.py` exit 0. Stack torn down with
  `docker compose -p qa2e64 down -v`; no containers, volumes or networks left behind; working tree
  clean.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — F1 fixed durably, because correcting it again is the move that has failed three times

`claude-qa-2e64` passed all eight criteria and failed the ticket on documentation — **the third
failure on the same three lines**, after T-0004's and T-0005's acceptances. The record makes it
worse than a repeat: commit `eb1432a` predicted it in writing, *"because T-0006 lands before the
retro and will falsify the same lines a third time."* It did, past an implementation and two review
passes, with the prediction sitting in the repository the whole time.

**So I have not corrected the sentences again.** Twice now the correct fix has been applied and has
lasted exactly one ticket, because those lines enumerate what exists and every ticket that builds
something falsifies them. The enumerations are gone; README and ARCHITECTURE now point at
[BACKLOG.md](../BACKLOG.md), which is authoritative and is updated by `complete-ticket` as part of
closing a ticket — so it cannot drift the way a hand-maintained sentence does.

This applies one of the three candidates recorded for the retrospective, and it is **marked
provisional in the text itself**: the retro may replace it. I did not want to pre-empt that
decision, and I wanted less to hand T-0007 a fourth occurrence of a defect whose recurrence was
already forecast. If the retro prefers a different candidate, this is cheap to replace; a fourth
acceptance failure is not.

#### The three non-blocking findings, all taken

**F2 — three EF warnings on every process start**, `Model.Validation[20601]`, saying EF was
*guessing* which CLR value means "unset" for the three enum columns. Its guess — `default(T)`, i.e.
0 — is correct **because** every member of these enums starts at 1. Now stated with `HasSentinel`
rather than inferred, which silences the warnings and, more usefully, makes the assumption fail
loudly if someone later adds a `= 0` member: that value would otherwise be silently treated as
unset and replaced by the column default.

That is the same fault as B3 one layer down — a mechanism relied on by inference from an adjacent
one — and this time EF was warning about it on every start and nobody had read the log.

**F3 — `subject: ""` slipped past the declared pattern**, because `RegularExpressionAttribute`
treats empty as valid. The user lookup refused it, so AC4's outcome held, but document and
enforcement disagreed about which layer was responsible. `minLength: 1` closes it in the contract.

**F4 — two of three AC2 rows asserted a field the request never mentioned.** The "nothing changed"
check re-read `status` for every case, so the `type` and `priority` rows measured nothing: three
tests that looked like coverage and were one. Each row now re-reads the field it tried to change.

#### What acceptance closed that review left open

Worth recording because it is the strongest evidence yet for the amended standard: the acceptor
reverted a live stack to the pre-T-0006 schema **holding 505 issue rows**, ran the real compose
migrator, and confirmed all 505 backfilled correctly, that pre-existing rows PATCH after upgrade
(the gap review flagged), that the migrator is idempotent, and that `Restrict` refuses a live user
delete. It also round-tripped the **generated client** against a live API — the one thing nothing in
this repository had ever exercised — and probed ~45 unanticipated request shapes without finding a
500.

Zero mutants. Every finding came from step 3.

- **Did:** Replaced the stale enumerations with a pointer to the authoritative list; stated the enum
  sentinels; closed the empty-subject gap in the contract; made two vacuous AC2 rows measure their
  own field.
- **Decided:** apply the durable fix now rather than a third correction — marked provisional so the
  retrospective still chooses.
- **Remaining:** re-review, then re-acceptance.
- **Open questions / blockers:** none.
- **Test state:** `dotnet test` **131/131** · build **0 warnings** · `dotnet format` exit 0 ·
  `validate.py` exit 0 · `check-drift.sh` exit 0 after commit · smoke below.


### 2026-09-01 — Code review, acceptance-fix pass (claude-rev-7a03) — ENG · ARCH — **Request changes**

Reviewed `t-0006-acceptance-fixes` @ `5070ecd` against `main` @ `6733300`, in the worktree
`got-issues--t-0006b`. F1 and F4 are sound. F3 is correct in the contract but arrives without the
regression test the standard requires and cannot get one by accident. F2 is correct in mechanism
and **wrong in what it claims about itself**, which makes it the third instance in this ticket of
the fault it was written to fix.

First, the thing worth saying plainly: `claude-qa-2e64`'s acceptance found more than my two review
passes did, by a wide margin — 505 rows through a real migrator, a generated-client round-trip
against a live API (the gap I recorded as N7 and did not close), and the pre-existing-row PATCH I
flagged as a hole in the upgrade test. Zero mutants. That is the amended standard earning its
change.

#### Gates, re-run here, each exit code read from the tool itself

| Gate | Result |
| --- | --- |
| `dotnet test` | **exit 0** — 17 unit + 114 integration = **131** passed, 0 failed |
| `dotnet build --no-incremental` | **exit 0** — 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | **exit 0** (solution) and **exit 0** (`GotIssues.SmokeTests.csproj`) |
| `./tools/check-drift.sh` | **exit 0** on a clean tree |
| `./tools/smoke.sh` | **exit 0** — 13 passed, 0 failed (9 m 16 s) |
| `python3 tools/validate-project-os/validate.py` | **exit 0** — 24 tickets, 10 ADRs |

---

### The judgement you asked for: is applying a retro candidate early pre-emption?

**No, in this instance — and the reason is a distinction worth keeping, because it is not the one
your framing reaches for.**

Two different things were on the table, and only one of them is the retrospective's:

- **Deciding the durable rule.** That is the retro's, and it was deferred on purpose.
- **Not leaving documentation your own ticket falsified.** That is unconditionally this ticket's,
  and the DoD already enforces it.

T-0006 had no option that avoided touching those three lines. The only question was *which true
state* to leave them in: rewrite the sentences a third time — knowingly reproducing a defect
forecast in writing, in the repository, unread through an implementation and two of my review
passes — or delete them and point at the list that is already authoritative. The second is an edit
to two documentation files, squarely inside a ticket's normal remit. I checked the precedent rather
than assuming it: `ARCHITECTURE.md` has been edited on ticket branches by T-0002, T-0004, T-0005,
T-0009 and T-0015, and `README.md` by those plus T-0001, T-0003 and T-0010. No lane or authority
boundary is crossed here that has not been crossed by six earlier tickets.

**What would have been pre-emption is candidate (c)** — a `validate.py` rule. That changes tooling
behaviour for every future ticket and encodes the rule as process. Candidate (b) likewise adds a
build step. Neither was done. (a) is *subtractive and reversible*: if the retro prefers (b) or (c),
it restores prose from git and adds the mechanism, and the only thing lost in the interim is three
sentences whose entire recorded history is being wrong.

**Where your instinct is right, and it is not the "don't relitigate accepted decisions" worry.**
That principle protects a decision an authority has *already taken*; here no decision exists and the
retro's authority is prospective, so the shapes are different. The real risk is the mirror image:
a retrospective reasons from what the repository shows, and the repository now shows the symptom
gone. A retro that opens `README.md` and finds nothing stale can conclude the matter is closed and
never make the choice deliberately. The countermeasure for that is not restraint — it is
**disclosure**, and disclosure is exactly where this is thin (see the recommendation below).

So: a reasonable call, and I would have made it. But note that "provisional and reversible" is
carrying the whole justification, which means it has to be true in the record and not only in the
intent.

---

### Blocking

#### C1 — F2's comment claims a guarantee the change does not provide, and removes the only signal that existed

The mechanism is right, and I verified it rather than reading it. Built the model and inspected it
directly:

```
SENT[Type]     sentinel=0 clrType=GotIssues.Api.Data.IssueType     default=Task
SENT[Status]   sentinel=0 clrType=GotIssues.Api.Data.IssueStatus   default=Open
SENT[Priority] sentinel=0 clrType=GotIssues.Api.Data.IssuePriority default=Normal
SENT[warnings-20601]=0        (three of them on the previous branch)
SENT[new-record] Type=Task Status=Open Priority=Normal
```

So `HasSentinel(default)` does resolve to the enum's `0` and not to `null` — which was my first
worry and it was unfounded — the three warnings are gone, and a normally-constructed record still
carries `Task`/`Open`/`Normal`, so insert behaviour is unchanged. Good change.

**The comment beside it is not accurate.** It says stating the sentinel *"makes the assumption fail
loudly if someone later adds a `= 0` member"*. Trace it:

- **Before:** no configured sentinel. EF implicitly used `default(T)` = 0 **and** logged
  `Model.Validation[20601]` on every start. Add `Unknown = 0` and setting it would be silently
  treated as unset — but the warning was still there on every start, pointing at this exact hazard.
- **After:** sentinel explicitly 0, **no warning at all**. Add `Unknown = 0` and setting it is
  silently treated as unset and replaced by the column default. Nothing fails, and nothing is loud.

The change therefore *removes* the only standing signal about this hazard and replaces it with a
comment. That is still probably a net improvement — a comment beside the enum configuration is read
by whoever edits the enums, and a log line nobody read demonstrably was not — but it is a
**documentation** improvement, not a mechanism, and the comment claims a mechanism.

This is the third instance in this ticket of the same fault: B3 attributed AC4's 400 to the foreign
key, F1's own history is a reminder relied on to do work it could not, and now the fix for F2
describes itself the way B3's record described the FK. Your framing — *a guard asserted to exist by
inference from an adjacent mechanism* — applies to the sentence you wrote about your own fix.

Two acceptable resolutions:

- **Correct the sentence** to what is true: the sentinel is stated so the assumption is visible
  where the enums are configured, and so EF stops guessing; a `= 0` member would still be treated
  as unset, silently, which is why the assumption is written down here.
- **Or make it true**, which I would prefer and which is about six lines: a unit test asserting no
  member of `IssueType`, `IssueStatus` or `IssuePriority` has the value 0. Then the claim holds,
  the guard is real, and it fails in CI rather than in a column.

#### C2 — F3 ships without a regression test, and a naive one would pass without the fix

[TESTING.md](../../standards/TESTING.md): *"Every fixed bug gets a regression test that fails
without the fix. No exceptions."* There is no test for the empty subject —
`grep` over `IssueLifecycleTests.cs` finds nothing for `""`, `minLength`, or empty.

This one cannot be closed by adding the obvious test, which is why it is worth blocking rather than
noting. I measured the same request on both branches:

```
before (t-0006-lifecycle @ d59e191)
  {"assignment":{"subject":""}} -> 400  errors: {"Assignment.Subject": ["No user with subject '' is known to this system."]}

after (this branch)
  {"assignment":{"subject":""}} -> 400  errors: {"Assignment.Subject": ["The field Subject must be a string with a minimum length of 1 and a maximum length of 255."]}
```

Both are 400 with the field named, so **a test asserting the status code and the field cannot fail
without the fix** — it is satisfied by the defect. The whole content of F3 is *which layer rejected
it*, so the test has to assert that: the validation message rather than the lookup's
`No user with subject …`. This is the "satisfied by anything" trap
[`review-code`](../../skills/review-code/SKILL.md) names, and the skill's instruction is to
enumerate what else satisfies the replacement — here, the unfixed code does.

I also confirmed the fix does not repeat the `[Required]` hazard: `[StringLength(255,
MinimumLength=1)]` leaves null valid, and `{"assignment":{"subject":null}}` still unassigns with
200. That half is right.

#### C3 — the *Not here yet* section still enumerates what is not built, three lines below the sentence saying it no longer does

`README.md` now reads *"See BACKLOG.md for everything not yet built … so it cannot drift the same
way"*, and then, in the same list:

- *"**User** tokens. The identity host issues machine-client tokens, which carry a role but no
  subject — so no endpoint is yet guarded by a person's identity, and the user projection stays
  empty in practice."* — a hand-maintained statement of something not built. It is accurate today
  and goes stale the moment [T-0018](T-0018-user-subject-tokens.md) lands, which is row 5 of the
  backlog it just delegated to.
- *"(Everything else the standards mention now exists.)"* — a universal claim about what exists,
  maintained by hand, false as soon as a standard mentions something new.

That is the fourth occurrence pre-loaded, in the section whose fix claims it cannot happen. The
claim and the counter-example are three lines apart.

I am not asking you to delete the user-tokens paragraph unconsidered: it carries something a backlog
row does not, namely *why* the projection is empty in practice, which is genuinely useful to a
reader running the stack. So either:

- **move that explanation somewhere it is not an inventory item** — it is really a note about the
  identity host, not an entry in a list of missing features — and drop the trailing parenthetical;
- **or keep both and soften the claim**, so the section does not assert a property it does not have.

Either is fine. What should not ship is the section asserting it cannot go stale while containing
two things that can.

---

### Non-blocking

- **N8 — the provisional marker points somewhere that does not carry the message.** `README.md`
  sends the reader to `CURRENT_SPRINT.md` Notes for the retro candidates and for the fact that this
  may be replaced. That section records the three candidates and still reads *"for the retro to
  choose between"*, with nothing saying (a) has been applied. Since "the retro still chooses" is
  the load-bearing half of the justification I endorsed above, it should be visible where the
  retro's own recorded input lives — one paragraph in the *Retro input* section, lane 1, on the
  trunk. Not blocking, because the [retrospective](../../skills/retrospective/SKILL.md) analyses
  ticket Work Logs too and this ticket's Work Log states it plainly; but the pointer currently
  promises something the destination does not deliver, which is a small instance of the defect this
  whole finding is about.
- **N9 — F4 is right, and worth one more line than it got.** The three AC2 rows now re-read the
  field each request tried to change. I checked the substitution is complete: `field` and
  `unchanged` are used together in the single assertion, so no row can silently re-read another's
  field again. Worth noting for the retro that this is the second vacuous assertion found in this
  one test class after N5's `Assert.False(string.IsNullOrEmpty(why))` — both in `[Theory]` rows,
  both from a parameter that looked like it was doing work.
- **N4 and N2 both landed correctly** and I confirmed them on `main`: the hand-rolled problem
  document now carries its `type` (verified live — the unknown-subject 400 returns
  `"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1"`), with a comment recording that it
  is the only such document in the codebase rather than a framework default; and the spurious
  *"field is required"* became [T-0024](T-0024-spurious-validation-error-on-every-body-taking-endpoint.md).
  Nothing further needed on either.
- **N7 is closed by acceptance**, not by this diff: the generated client was round-tripped against
  a live API. Worth folding into the retro as evidence, since it was a standing gap nobody had
  exercised since the client was first generated.

---

- **Did:** Reviewed `main...HEAD`; re-ran every gate in this worktree reading each exit code from
  the tool; verified F2 by inspecting the built EF model and counting the `20601` warnings before
  and after; verified F3 by measuring the empty-subject response on both branches; verified F1's
  lane precedent against the history of both documentation files.
- **Decided:** **Request changes** — C1, C2, C3. All three are small; none disputes the approach.
- **Remaining:** C1-C3 on the branch, then re-request. N8 is worth taking on the trunk in the same
  pass. N9, and the N7 note, are for the retro rather than for this ticket.
- **Open questions / blockers:** none. On the question put to me: applying candidate (a) early is a
  reasonable call and I would have made it — the exposure is evidential rather than procedural, and
  N8 closes it.
- **Branch / PR:** `t-0006-acceptance-fixes` @ `5070ecd`.
- **Test state, as I measured it:** `dotnet test` **131/131** exit 0 · build 0 warnings exit 0 ·
  `dotnet format` exit 0 both · `check-drift.sh` exit 0 · `validate.py` exit 0 ·
  `smoke.sh` 13/13 exit 0.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — C1: the fix's own comment repeated the fault it was fixing

`claude-rev-7a03` endorsed the F1 approach and found three things. The first is the sharpest yet.

#### C1 — I claimed a guarantee and shipped a comment

The `HasSentinel` change carried a comment saying it *"makes the assumption fail loudly if someone
later adds a `= 0` member."* Traced by the reviewer, it does the **opposite**: before the change EF
logged `Model.Validation[20601]` on every start, saying it was guessing; after it, there is no
warning at all, and a `= 0` member would be silently treated as unset. **I removed the only
standing signal and replaced it with a sentence.**

That is the third instance in this ticket of a guard asserted by inference — and this one is inside
the comment explaining the fix for the second. I did not check what the change removed, only what
it added.

Now made true rather than reworded: `IssueLifecycleEnumTests` fails if any member of the three
enums is ever zero, with a message saying what would break. The comment says what the mechanism
actually does — silences three warnings — and points at the test for the guarantee.

The net position is still better than before, and worth stating precisely: a log line nobody read
has been replaced by a test that fails, plus a comment beside the enums where someone adding a
member will be looking.

#### C2 — the obvious test for F3 is satisfied by the defect

`{"subject":""}` returned 400 naming `Assignment.Subject` **both before and after** the fix — the
pattern let it through and the user lookup refused it. So a status-and-field assertion passes on
the broken code, and the entire content of F3 is *which layer rejected*. The test asserts the
message for that reason: `minimum length` present, `is known to this system` absent.

This is the "satisfied by anything" rule from RETRO-SPRINT-002 in its least obvious form — not an
assertion too weak to fail, but one that passes for the wrong reason in exactly the case it was
written for.

#### C3 — the section that says it cannot enumerate, three lines above two enumerations

The rewritten *Not here yet* kept a "User tokens" bullet and *"(Everything else the standards
mention now exists.)"* — a fourth occurrence pre-loaded under a sentence explaining why enumerating
is the problem.

The reviewer was right not to ask for deletion: that bullet explains **why** the user projection
stays empty, which a backlog row cannot. So the explanation moved out of the inventory into its own
short section about behaviour, and the inventory is now only a pointer. The blanket "everything
else exists" claim is gone — it was an enumeration of one.

#### N8 — the disclosure that makes applying (a) early honest

The reviewer's framing is better than my question. Two things were on the table: *deciding the
durable rule* (the retro's) and *not leaving documentation this ticket falsified* (unconditionally
mine). It also checked the precedent I had asserted — `ARCHITECTURE.md` has been edited on ticket
branches by five previous tickets, `README.md` by eight — and noted that candidate (c) *would* have
been pre-emption, because a validator rule encodes process for every future ticket.

The real risk it identified is one I had not: **a retrospective reasons from what the repository
shows, and the repository now shows the symptom gone.** The countermeasure is disclosure, not
restraint. `CURRENT_SPRINT.md` now records that the third occurrence happened as forecast, that (a)
is in force, and that (c) remains available and is not mutually exclusive with it.

- **Did:** Replaced C1's claim with a test that makes it true; asserted F3's message because status
  and field cannot distinguish the fix from the defect; moved the user-token explanation out of the
  inventory; disclosed (a)'s early application to the retro.
- **Decided:** keep `HasSentinel` — the log line it removed was demonstrably unread; the test is the
  guarantee.
- **Remaining:** re-review, then re-acceptance.
- **Open questions / blockers:** none.
- **Test state:** `dotnet test` **135/135** (20 unit, 115 integration) · build 0 warnings ·
  `dotnet format` exit 0 · `validate.py` exit 0 · drift and smoke below.

### 2026-09-01 — Code review, third pass (claude-rev-7a03) — ENG · ARCH — **Approve**, with one lane correction owed at merge

Reviewed `t-0006-acceptance-fixes` @ `98b126e`. C1, C2 and C3 are closed, and N8 is closed better
than I framed it. One standards deviation is outstanding; it does not need another review round and
I explain below why I am not spending one on it.

#### Gates, re-run here, each exit code read from the tool itself

| Gate | Result |
| --- | --- |
| `dotnet test` | **exit 0** — 20 unit + 115 integration = **135** passed, 0 failed |
| `dotnet build --no-incremental` | **exit 0** — 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | **exit 0** (solution) and **exit 0** (`GotIssues.SmokeTests.csproj`) |
| `./tools/check-drift.sh` | **exit 0** on a clean tree |
| `./tools/smoke.sh` | **exit 0** — 13 passed, 0 failed (7 m 55 s) |
| `python3 tools/validate-project-os/validate.py` | **exit 0** — 24 tickets, 10 ADRs |

#### C1 — closed, and I ran the guarantee rather than reading it

I asked for the claim to become true, so I checked that it had. Added `Unknown = 0` to `IssueType`
and ran only the guard:

```
Failed  IssueLifecycleEnumTests.No_member_is_zero_because_zero_means_unset(enumType: typeof(IssueType))
  IssueType declares Unknown as 0. The database default for this column treats 0 as 'unset', so
  that member would be silently replaced by the default on every write. …
Failed!  - Failed: 1, Passed: 2, Total: 3
```

Reverted; tree clean. Valid on every count [TESTING.md](../../standards/TESTING.md) names: the build
accepted it, the failure is the guard's own assertion rather than an unrelated error, the cause is
the member I added, and **only the mutated enum's row failed** — the other two passed, so the
theory is discriminating and not failing wholesale. It also runs in under a millisecond in the unit
tier, which is the cheapest tier that can host it.

The comment now says what the mechanism does and points at the test. That is the correction.

**On the judgement you flagged and invited disagreement on — I agree, and it is now supported
rather than asserted.** Keeping `HasSentinel` was arguable while the guarantee was a comment; it is
not arguable now. With the test in place, warning 20601 is noise: it reports an *absence* of a
declared sentinel, and the sentinel is now a deliberate decision with a separate guard. Removing
`HasSentinel` to keep the warning would restore a line saying "you have not decided this" to a
codebase that has. And its signal value here is measurable rather than theoretical — those three
warnings were in the logs through an implementation and two of my review passes, and none of the
three of us read them. It took an acceptance run. A test that fails at build time, names the
member, and states the consequence is strictly better than a log line with a demonstrated
readership of zero.

#### C2 — closed, and the load-bearing half is the robust one

`An_empty_subject_is_refused_by_the_contract_not_by_the_user_lookup` asserts both directions. Worth
recording which one is doing the work: `Assert.DoesNotContain("is known to this system", …)` is the
half that fails against the pre-fix behaviour, and it is coupled to **our own** message, so it stays
true whatever .NET does. `Assert.Contains("minimum length", …)` is coupled to a framework string and
is the brittle half — harmless as belt and braces, and worth knowing if it ever breaks on an SDK
bump, because the test would then be failing on wording rather than on behaviour.

The comment recording *why* the message is asserted is the right thing to have written. That is
exactly the note that stops someone "simplifying" it back to a status check.

#### C3 — closed

Checked both files for anything that survived: no negative enumeration remains in `README.md` or
`ARCHITECTURE.md`, and the blanket *"everything else the standards mention now exists"* is gone.
The positive lists ("what is built") remain in both banners, which is correct — that is the safer
direction and explicitly the banner's own job.

Moving the user-token paragraph into a section about behaviour rather than deleting it is the
better resolution of the two I offered. It reads as what it actually is: a fact someone needs
before they try to assign an issue, not an inventory row.

#### N8 — closed, and the addition is better than my framing

I said the hazard was that a retro reasons from what the repository shows. Your note says that and
one thing I did not: **(c) is not mutually exclusive with (a)** — a validator rule would catch the
next enumeration someone adds *anywhere*, which deleting these three lines does not. That is right,
and it means applying (a) foreclosed less than I credited it with. Recorded so the retro has it.

---

### The one thing outstanding: a lane violation, owed at merge

`98b126e` puts `project-os/delivery/CURRENT_SPRINT.md` in the same commit as `README.md`, `apps/`
and the ticket. [GIT.md](../../standards/GIT.md): *"A commit never mixes the two lanes, with one
exception: the ticket's **Work Log** may be updated on the ticket branch alongside the code it
describes."* The sprint file is not the ticket's Work Log.

I checked the precedent rather than asserting it, and it is one-way: **every** change to
`CURRENT_SPRINT.md` since the foundations commit has been an `os:` commit on the trunk — eleven of
them, including [`eb1432a`](../../delivery/CURRENT_SPRINT.md), which added the very *Retro input*
section this update extends. That is the opposite of `ARCHITECTURE.md`, which has always travelled
on ticket branches (T-0002, T-0004, T-0005, T-0009, T-0015) and which I therefore did not raise last
pass. The rule's rationale bites hardest here: sprint state is the cross-agent coordination file,
and the reason it is lane 1 is that it must be visible the moment it changes, not when a branch
merges.

**Required: move that hunk to a trunk `os:` commit.** Not a re-review — I have read the text and it
is right; watching it move would tell me nothing, and the handover already puts you on the trunk for
the status commit. That is the whole reason this is a merge condition rather than a fourth round,
and I want the asymmetry with C1 on the record: C1 changed what the repository *guarantees*, so it
had to be re-verified and was; this changes only which commit carries text I have already approved.

---

### Non-blocking

- **N10 — the guard's own list is hand-maintained, which is F1's shape one layer down.**
  `IssueLifecycleEnumTests.LifecycleEnums` names three types explicitly. Add a fourth enum column
  with a database default and forget to add it there, and the new column is silently unguarded while
  a green test says the assumption holds. The list can be derived instead of written: ask the model
  for properties whose CLR type is an enum and which have a default value, and the guard covers
  whatever exists rather than whatever was remembered. Cheap, and it removes the last hand-kept
  enumeration this ticket added. Not for this ticket unless you want it.

### For the retrospective — the sharper version of what you asked me to carry

I would not put it as "shift effort toward acceptance". The evidence supports something more
specific and more useful.

Across this ticket, review raised six blocking findings (B1, B2, B3, C1, C2, C3) and acceptance
raised four (F1-F4), plus the deeper exploration acceptance did at scale. **Mutation produced none
of them.** Every one came from one of exactly two activities:

1. **Exercising the running system in a state it was not built in** — B1 (a `U+0000` reaching
   PostgreSQL as a 500), B2 (`{"assignment":{}}` silently unassigning), the empty-subject
   observation behind C2, and acceptance's 505-row migration, generated-client round-trip and
   ~45 probe shapes. This is already named in [TESTING.md](../../standards/TESTING.md) and it is
   earning its place.
2. **Checking whether a claimed guarantee matches the mechanism it names** — B3 (AC4's 400
   attributed to the foreign key, which cannot produce it), C1 (`HasSentinel` claimed to make a
   `= 0` member fail loudly, when it removed the only signal), and F2 itself, which was EF telling
   the repository it was guessing while nobody read the log. **This is not named anywhere.** It is
   not code review and it is not exploration; it is reading a written claim against the thing it
   points at. Three instances on one ticket, each one inside the fix for the previous.

Mutation's role in this ticket was real but narrow and entirely downstream of (2): the two mutants
that ran — AC5's, and AC4's after I challenged it — were each *prompted* by someone checking a
claim, and neither was found by mutating. That is consistent with the maintainer's narrowing and I
think it argues for naming (2) as a technique in `review-code` and `acceptance-test`, rather than
for moving effort between the two activities.

---

- **Did:** Reviewed `5070ecd..98b126e`; re-ran every gate in this worktree reading each exit code
  from the tool; verified C1's new guarantee by adding a zero member and observing the guard fail
  for the right reason, then reverting; checked both documentation files for surviving
  enumerations; checked the sprint file's lane precedent across its full history.
- **Decided:** **Approve.** Merge may proceed once the `CURRENT_SPRINT.md` hunk is moved to a trunk
  `os:` commit.
- **Remaining:** that move, then the handover status commit. N10 optional. The retro note above is
  for `CURRENT_SPRINT.md`'s *Retro input* section if you want it recorded where the F1 candidates
  are — that is a trunk commit too, and it can travel with the same one.
- **Open questions / blockers:** none. On the `HasSentinel` judgement you asked me to challenge: I
  agree with keeping it, for the reason above.
- **Branch / PR:** `t-0006-acceptance-fixes` @ `98b126e`.
- **Test state, as I measured it:** `dotnet test` **135/135** exit 0 · build 0 warnings exit 0 ·
  `dotnet format` exit 0 both · `check-drift.sh` exit 0 · `validate.py` exit 0 ·
  `smoke.sh` 13/13 exit 0.

### 2026-08-31 — Review approved; lane correction and N10 closed (claude-sm-9d4e)

`claude-rev-7a03` approved at `774f0b8` and re-ran every gate independently. Two items came back
with it — one owed at merge, one non-blocking. Both are closed here.

**The lane violation (owed at merge, not a re-review).** `98b126e` put
[`CURRENT_SPRINT.md`](../../delivery/CURRENT_SPRINT.md) in the same commit as `README.md`, `apps/`
and this ticket. [GIT.md](../../standards/GIT.md)'s only branch-lane exception is a ticket's Work
Log. The reviewer checked the precedent rather than asserting it: **every change to that file since
the foundations commit has been an `os:` trunk commit — eleven of them**, including `eb1432a`, which
added the very *Retro input* section mine extended. That is the opposite of `ARCHITECTURE.md`, which
has always travelled on branches — which is why the same reviewer did *not* raise it last pass. The
hunk is removed from this branch and lands on the trunk as `os:` instead. Same text, right lane.

**N10 — the guard hand-listed what it guards.** `IssueLifecycleEnumTests` named the three enums in
a literal array: **[F1](#)'s shape one layer down.** Add a fourth enum column with a database
default, forget the list, and it is silently unguarded while a green test reports otherwise. The
columns are now read from the EF model — every property whose CLR type is an enum and which carries
a database default — so the set of guarded things is derived from the thing that creates the
obligation. Building the model needs no database, so it stays in the sub-millisecond unit tier.

The theory changed shape, so I re-ran the mutant rather than trusting the reviewer's run of the
previous version: adding `Unknown = 0` to `IssueType` fails **one** row — `IssueRecord.Type` — with
the diagnostic naming the member, the enum and the column; the other two rows pass. Build accepted
it (0 compiler errors), so the mutant reached the assertion. Reverted; suite back to 20/20.

- **Did:** removed the sprint-file hunk from the branch (lane), derived the enum guard from the model (N10), re-verified the guard by mutation.
- **Decided:** N10 taken rather than deferred — it is the ticket's own defect class, and a ticket that leaves its own pattern uncorrected one layer down is the thing three acceptance rounds have been about.
- **Remaining:** merge, then the sprint-file text as an `os:` trunk commit, then re-acceptance.
- **Open questions / blockers:** none.
- **Test state:** unit 20/20 with the mutant reverted; full gates re-run on the trunk after merge.
