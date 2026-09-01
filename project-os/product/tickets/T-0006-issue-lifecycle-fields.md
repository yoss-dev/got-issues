---
id: T-0006
title: Track an issue's lifecycle — type, status, priority, assignee
type: feature
status: in-acceptance
priority: normal
owner: none
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
