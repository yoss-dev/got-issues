---
id: T-0005
title: Create and read issues within a project
type: feature
status: done
priority: high
owner: none
implemented_by: claude-sm-9d4e
accepted_by: claude-qa-8f52
depends_on: [T-0004]
adrs: [ADR-0004]
created: 2026-08-30
updated: 2026-08-31
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
- **Issue identity: `<PROJECT-KEY>-<n>`, numbered per project** (`GOTI-1`, `GOTI-2`), settled by the PO on 2026-08-31. The per-project counter and its allocation under concurrency are this ticket's central technical problem.
- Title and description handling, with validation declared in the spec.
- Unit and integration tests, including the unauthenticated case and the unknown-project case.

### Out of Scope

- Lifecycle fields — type, status, priority, assignee (T-0006). An issue created here carries whatever minimal defaults refinement agrees.
- Listing and filtering issues (T-0007).
- Comments (T-0008).
- Editing or deleting issues.

## Acceptance Criteria

- [x] AC1: Given an existing project and an authenticated caller, when they create an issue with valid input, then it is persisted against that project and returned with an identifier of the form `<PROJECT-KEY>-<n>`.
- [x] AC1b: Given a project with no issues, when the first issue is created, then it is numbered **1** — numbering starts per project, not globally.
- [x] AC1c: Given two projects, when each has issues created, then their numbers are independent — `GOTI-1` and `PROJ-1` can both exist.
- [x] AC1d: Given several issues created **concurrently** in the same project, when they are all persisted, then every identifier is distinct and no number is skipped or reused.
- [x] AC2: Given an issue that exists, when it is requested by its identifier, then the API returns it as the specification declares.
- [x] AC3: Given a project identifier that does not exist, when an issue is created against it, then the API returns 404 with an `application/problem+json` body — not a 500, and not an orphaned issue.
- [x] AC4: Given an identifier that does not correspond to any issue, when it is requested, then the API returns 404 with a problem document.
- [x] AC5: Given an unauthenticated or invalid-token caller, when they attempt either operation, then the API returns 401 and nothing is persisted.
- [x] AC6: Given the specification, when generation and the drift check run, then the diff is empty.

## Examples / Scenarios

- Create an issue in a project, read it back: fields round-trip intact, identifier `GOTI-1`.
- Create the first issue in a second project: `PROJ-1`, not `GOTI-2`.
- Ten simultaneous creates in one project: ten distinct identifiers, numbers 1–10, none repeated.
- Create against a deleted or nonexistent project: 404, nothing written.
- Create with an empty title: 400 with a problem document.
- Free-text description containing personal data: stored, but never written to logs ([SECURITY.md](../../standards/SECURITY.md)).

## Technical Notes

Spec first, then regenerate, then implement — as with every product ticket.

Issues are the entity everything else in the domain hangs off, so the shape chosen here is expensive to change later. Refinement should treat the schema as a decision, not an implementation detail.

## Dependencies

- **T-0004** — issues live inside projects; the project resource and its identity scheme must exist first.

## Risks / Unknowns

- ~~**Issue identity depends on T-0004's project-key decision.**~~ **Settled by the PO, 2026-08-31: `GOTI-123`, numbered per project.** What that leaves is not an unknown but a hard problem: **allocating a per-project sequence correctly under concurrency.**
- **The obvious implementation is wrong.** `SELECT MAX(number)+1` — or an in-memory counter — will duplicate identifiers under simultaneous creates, and every test written against a single-threaded harness will pass. The candidates are a per-project counter row updated in the same transaction, or a database sequence per project; both have costs and refinement is deliberately not choosing, because the implementer should measure. **AC1d exists to make the wrong answer fail.**
- **Numbers must not be reused.** If an issue is ever deleted, its number stays retired — `GOTI-7` pointing at a different issue than it did last week is worse than a gap. Deletion is out of scope here; the constraint is recorded so the ticket that adds it inherits it.
- Free-text fields (title, description) may contain personal data typed by users. `SECURITY.md` requires treating them as such: never logged, minimised. Refinement must name this concern per the DoR's security conditional.
- Whether an issue can exist without lifecycle fields at all, or needs defaults from the outset, is a boundary between this ticket and T-0006 that refinement should draw explicitly.

## Testing Notes

Integration tests against real PostgreSQL, covering the 404 paths in AC3/AC4 as first-class cases rather than afterthoughts — those are where an ORM-backed implementation most often returns a 500 instead.

## Relevant ADRs & Documentation

- [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md), [ENGINEERING.md](../../standards/ENGINEERING.md), [TESTING.md](../../standards/TESTING.md), [SECURITY.md](../../standards/SECURITY.md)
- [IDEA-001](../IDEAS.md) — the originating idea

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-31 during `refinement-session`. All nine universal items hold; the unknown that previously blocked it (issue identity) was settled by the PO and is now AC1–AC1d. Item 5: depends on T-0004, which is `ready` and must be `done` first. Conditional items: security/privacy named — free-text fields may carry personal data and are never logged; data-shape impact identified (the per-project sequence); no ADR-bar decision; no UX. No exceptions applied.

## Definition of Done

- [x] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — walked item by item on 2026-08-31 by `claude-qa-8f52`; no deviation required.

---

## Work Log

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** Created by promoting IDEA-001 during backlog seeding.
- **Decided:** Split creation/read from lifecycle fields (T-0006) so each ticket stays within the DoR sizing guideline and delivers observable behaviour on its own.
- **Remaining:** Refinement to Ready; issue identity is the decision to settle, and it depends on T-0004's.
- **Open questions / blockers:** none blocking creation.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — Business Analyst (claude-sm-9d4e) — refinement

Perspectives applied: Product Owner, Business Analyst, Software Engineer, Architect, QA, Security. (No UX — no user-facing UI.)

**The blocking unknown is gone, and what it leaves is harder than it looks.** The PO settled identity as `GOTI-123`, numbered per project (recorded in [T-0004](T-0004-create-and-list-projects.md)). That removes the design question and replaces it with an implementation problem: allocating a per-project sequence correctly when two creates arrive at once.

**The point of AC1d.** `SELECT MAX(number)+1`, or a counter held in memory, is the obvious implementation and it is wrong — and, critically, **every test written against a single-threaded harness will pass anyway**. This is the same shape as T-0004's key-uniqueness risk and as the retro's central finding: a check that reads as proof and is not. AC1d requires concurrent creates specifically, so the wrong answer fails rather than lurking until two people file issues in the same second.

Refinement deliberately does **not** choose between a counter row updated in the same transaction and a per-project database sequence. Both work, both have costs, and the implementer should measure rather than inherit my guess.

**Numbers must not be reused** — recorded now though deletion is out of scope, because the constraint belongs to whoever adds deletion. `GOTI-7` meaning a different issue than it did last week is worse than a gap in the sequence.

**Security:** unchanged and still applicable — issue titles and descriptions are free text typed by employees, so they may carry personal data, are never logged, and fall under `PROJECT.md` Q8's unanswered data-protection question.

**Sizing:** unchanged in scope, but the concurrency requirement makes it meaningfully harder than "create and read a row". Still within the guideline; if it overruns, the seam is read-by-identifier (trivial) versus create-with-allocation (the real work).

**DoR verdict: `ready`.**


### 2026-08-31 — A hazard inherited from T-0004, recorded before anyone copies it (claude-sm-9d4e)

[T-0004](T-0004-create-and-list-projects.md) added a `pattern` to the project `name` excluding
C0 control characters and DEL, after acceptance found `U+0000` producing an undeclared HTTP 500
with an empty body — PostgreSQL cannot store it, the failure escaped a deliberately narrow catch,
and nothing handled it.

**Do not copy that pattern wholesale onto this ticket's fields.** It does two different jobs, and
only one of them generalises:

| Concern | Applies to | Why |
| --- | --- | --- |
| **`U+0000` is unstorable** | **every** text column, including `description` | PostgreSQL rejects it in `text` outright (SQLSTATE 22021). Nothing can hold it, so nothing should accept it. |
| **The rest of C0, and DEL** | single-line fields only — a `title`, not a `description` | Tabs and line breaks have no place in a one-line field. A description is multi-line **by design**, and excluding `U+000A` from it would reject ordinary, correct input. |

So `title` can reasonably carry T-0004's constraint; `description` must exclude `U+0000` and
permit the rest. Getting this wrong in the permissive direction reproduces T-0004's 500; getting
it wrong in the strict direction rejects a description with a paragraph break in it, which is
worse in the way that annoys people daily rather than loudly.

Two related notes from the same review, worth having before implementation rather than after:

- **`U+0085` and `U+2028` are not excluded** by T-0004's pattern and are accepted today. That is
  a deliberate limit, not an oversight: the constraint is the narrow checkable one, not full
  Unicode line-break normalisation. If a title needs more, decide it here rather than inheriting
  a rationale that was scoped to a display name.
- **Whatever is decided must be declared in the specification**, not enforced in a controller
  ([ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md)). That
  is what makes it reach generated clients, and it is why T-0004's fix went into the contract
  rather than into a guard clause.

- **Did:** Recorded the constraint hazard T-0004 created for this ticket, with the storability
  and single-line concerns separated.
- **Decided:** nothing — the field constraints are this ticket's to choose at implementation.
- **Remaining:** unchanged; this ticket stays `ready`.
- **Open questions / blockers:** none.
- **Test state:** n/a — not started.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — claimed, with the implementation plan

Claimed at `4b66dc6`. Dependency verified in its own file: [T-0004](T-0004-create-and-list-projects.md)
is `done`.

#### The central problem: allocating a per-project number

The ticket's Risks name the trap — `SELECT MAX(number)+1` duplicates under concurrency and passes
every single-threaded test. Two candidates were left open for the implementer to measure. I am
choosing the **counter column on the project row**, and the deciding argument is not the one I
expected:

| | Counter column | Sequence per project |
| --- | --- | --- |
| Allocation | `UPDATE projects SET next_issue_number = next_issue_number + 1 … RETURNING` — the row lock serialises allocations for **that project only** | `nextval()` on a dedicated sequence |
| Requires DDL at runtime | **No** | **Yes** — `CREATE SEQUENCE` per project |
| Behaviour on rollback | Number is returned; no gap | **Gap** — sequences are deliberately non-transactional |

**The DDL requirement is what settles it.** [T-0013](T-0013-enforce-migration-boundary-with-db-privileges.md)
exists to strip DDL rights from the application role so the migration boundary is enforced by the
database rather than by convention. A design that needs the API to issue `CREATE SEQUENCE` on every
project creation would make that ticket impossible to implement — I would be spending a decision
that belongs to another ticket, in a direction it has already chosen against.

The rollback difference is secondary but points the same way: AC1d says *no number is skipped*, and
a sequence gives that only when nothing rolls back.

**The counter is the allocator, not the guarantee.** A **unique index on `(ProjectId, Number)`**
goes in alongside it. That is T-0004's lesson applied before the review round rather than after:
the constraint is the guarantee, the allocator is the mechanism, and the concurrent test is the
only one that can tell them apart.

**Numbers are never reused** because the counter only moves forward — deleting an issue cannot
hand its number to another. That is the property the ticket records for whoever adds deletion, and
`MAX(number)+1` would have quietly broken it.

#### Shape

| Step | What |
| --- | --- |
| 1 | `spec/openapi.yaml`: `POST /projects/{projectKey}/issues`, `GET /issues/{issueKey}`, schemas `Issue`, `CreateIssueRequest` |
| 2 | Generate; implement the generated contract |
| 3 | Migration: `issues`, unique index on `(ProjectId, Number)`, `next_issue_number` on `projects` |
| 4 | Tests, including the concurrent allocation |

**Two paths, deliberately.** Creation is scoped to a project (`/projects/{key}/issues`), because an
issue cannot exist without one and the project is where the number comes from. Reading is by the
human identifier (`/issues/GOTI-1`), because that string is the thing people paste into chat, and
requiring them to decompose it into a project and a number to fetch it would waste the identity
scheme the PO chose. [T-0007](T-0007-list-and-filter-issues.md) will add
`GET /projects/{key}/issues`, which is why creation lives on that path rather than at `/issues`.

#### The constraint question this ticket inherits — and a trap inside it

[T-0004](T-0004-create-and-list-projects.md) left a recorded hazard: its control-character pattern
does two jobs, and only `U+0000` (unstorable in any text column) generalises. So `title` takes the
single-line constraint and `description` must not.

But `description` cannot simply take a NUL-only pattern, and this is worth settling with evidence
rather than taste. `RegularExpressionAttribute` requires the match to span the **whole** value,
while `$` in .NET matches *before* a trailing newline — so `^[^\u0000]*$` **rejects a description
ending in a newline**, which is ordinary text. The options are: accept that (user-hostile in a way
that shows up daily), anchor with `\A…\z` (correct in .NET, not valid ECMA-262, so the published
contract would lie to other tooling), or drop the anchors (correct in .NET, meaningless to every
other reader).

I will measure which of these actually behaves how before choosing, and record the result. What I
will **not** do is leave `description` unconstrained and let a `U+0000` reach PostgreSQL — that is
exactly the acceptance failure T-0004 just closed.

#### Test plan

| AC | Test |
| --- | --- |
| AC1 | create in a project; `GOTI-1` returned; read back |
| AC1b | first issue in a fresh project is **1**, not a global counter |
| AC1c | two projects each start at 1 — `GOTI-1` and `PROJ-1` coexist |
| AC1d | **ten concurrent creates in one project → ten distinct identifiers, numbers 1–10, none repeated or skipped** |
| AC2 | read by identifier returns what the contract declares |
| AC3 | create against an unknown project → 404 problem document, nothing written |
| AC4 | read an unknown identifier → 404 problem document |
| AC5 | unauthenticated → 401 on both, nothing written |
| AC6 | `check-drift.sh` exit 0 |

**Mutate first**, and the mutants must be verified to reach their assertions — this ticket's
predecessor produced four that did not:

1. Replace the counter with `MAX(number)+1` **and drop the unique index** → the concurrent test
   must fail and the sequential ones must pass. That is the exact shape of T-0004's read-then-insert
   mutant, which was the only one that showed the concurrent test earning its place.
2. Keep the counter, drop the unique index → everything should still pass, which tells me the index
   is currently untested *by behaviour* and is there as a guarantee against a future change. Worth
   knowing rather than assuming.

#### Risks I am carrying

- **AC1d is the ticket.** Everything else is a repeat of T-0004's shape.
- **A ten-way concurrent test against one row will serialise**, so it proves correctness rather than
  throughput. That is the right trade here and worth stating so nobody reads the test as a
  benchmark.
- **The `description` question above** could expand if the honest answer is "the contract cannot
  express this" — in which case it becomes a recorded decision rather than a silent controller
  guard.

- **Did:** Claimed; chose the allocation strategy with the argument that decides it; planned the
  paths, the tests and the mutants.
- **Decided:** counter column over sequence, on the strength of T-0013's direction; unique index as
  the guarantee alongside it; two paths, creation scoped to the project and reading by identifier.
- **Remaining:** implementation.
- **Open questions / blockers:** none blocking; the `description` constraint is a decision to make
  with evidence during implementation.
- **Test state:** not started.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — built; the wrong answer passes twelve of thirteen tests

Branch `t-0005-issues`. **100 tests green** (17 unit, 83 integration), smoke 13/13, all gates
exit 0.

#### AC1d earned its place, exactly as the ticket predicted

The Risks section said the obvious implementation *"will duplicate identifiers under simultaneous
creates, and every test written against a single-threaded harness will pass"*. Measured:

| Mutant (build and EF both accept it; migration regenerated so it reaches the tests) | Result |
| --- | --- |
| `MAX(number) + 1` and no unique index — the implementation the ticket names as wrong | **12 of 13 pass.** Only `AC1d_ten_concurrent_creates…` fails |
| Correct allocator, unique index dropped | **All 13 pass** |

The first row is the ticket's own prediction, confirmed rather than assumed. Twelve tests —
including every other numbering criterion — cannot tell the wrong answer from the right one.

**The second row is worth stating plainly rather than hiding.** The unique index is *not*
currently proven by behaviour: with the correct allocator in place, dropping it changes nothing
observable. It is there as a guarantee against a future change to the allocator, which is the
role T-0004 established for it, and I would rather record that it is untested than let the table
imply otherwise. Making it fail would mean shipping a deliberately broken allocator to prove a
constraint, which is not worth it.

#### Why a counter column and not a sequence

Recorded at plan time and unchanged by implementation. A sequence per project needs
`CREATE SEQUENCE` at runtime, and [T-0013](T-0013-enforce-migration-boundary-with-db-privileges.md)
exists to take DDL rights away from the application role — choosing a sequence would spend a
decision that belongs to that ticket, against the direction it has already chosen. Sequences are
also non-transactional, so a rolled-back create leaves a gap, and AC1d asks for no skipped numbers.

`UPDATE projects SET "NextIssueNumber" = "NextIssueNumber" + 1 … RETURNING` in one statement, inside
the creating transaction: PostgreSQL's row lock serialises concurrent creates **for that project
only**, and no row comes back when the project does not exist — which is AC3's 404 without a
second query or a second race.

#### The `description` question: I predicted a problem that does not exist

The plan said `RegularExpressionAttribute` requires a whole-value match while `$` matches before a
trailing newline, so `^[^\u0000]*$` would reject a description ending in a newline — and offered
two uglier alternatives. **Measured, and the prediction was wrong.** All three candidate patterns
accept plain, multi-line and trailing-newline text and reject `U+0000`, because `[^\u0000]*`
consumes the newline (unlike `.`), so the match spans the whole value and `$` matches at the end.

Two minutes of measurement removed a decision I had planned to agonise over, and the honest
version of the ticket's own advice — *the implementer should measure* — applied to a question the
implementer invented. Recorded because the previous ticket's central lesson was exactly this:
**a prediction about a tool's behaviour is not an observation of it**, and it is as wrong when it
predicts a problem as when it predicts safety.

So the constraint split T-0004's review recorded holds cleanly: `title` carries the single-line
pattern, `description` excludes only `U+0000`, and a test asserts both directions — a title with
a newline is rejected, a description with paragraph breaks is accepted, and neither may carry NUL.

#### Decisions

- **Two paths.** Creation is scoped to the project (`POST /projects/{projectKey}/issues`) because
  the project is where the number comes from; reading is by the key people quote
  (`GET /issues/GOTI-1`). [T-0007](T-0007-list-and-filter-issues.md) will add
  `GET /projects/{key}/issues`, which is why creation lives there rather than at `/issues`.
- **Creating an issue is a `member` act.** `PROJECT.md` §5 names three administrative acts and
  this is not one of them (ADR-0008's shape: the restriction is declared in the contract's
  description and by a declared 403, and enforced by the policy attribute).
- **The foreign key is `Restrict`, not `Cascade`.** Nothing deletes projects yet; when deletion
  arrives it should be a decision someone makes rather than one this line already made.
- **Numbers are never reused** because the counter only moves forward. `MAX(number)+1` would have
  broken that quietly as well as duplicating — an issue deleted later would hand its number on.

- **Did:** Specified the issue resource, generated, implemented, migrated, and tested all six
  criteria; measured the regex question rather than choosing; ran both planned mutants.
- **Decided:** as above.
- **Remaining:** review, then acceptance.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0005-issues`.
- **Test state, measured in this worktree:** `dotnet test` **100/100** · `tools/smoke.sh` **13/13**
  exit 0 · build **0 warnings** · `dotnet format` exit 0 both · `validate.py` exit 0 ·
  `check-drift.sh` exit 0 after commit.
- **For QA to probe:** the unique index is unproven by behaviour (above); and whether anything
  can produce a gap in numbering — the counter returns numbers on rollback, but I have not tested
  a rollback path, because no request currently fails between allocation and insert.


### 2026-08-31 — Software Engineer + Architect (claude-rev-5c14) — review of `t-0005-issues` @ `b472465`

Independent review per [review-code](../../skills/review-code/SKILL.md). Reviewer is not the
implementer (`claude-sm-9d4e`). Personas: Software Engineer, plus Architect — this change adds the
entity everything else in the domain hangs off, and a numbering scheme that is expensive to change.

**Verdict: Request changes.** Two blocking findings. Neither is in the allocator: the allocator is
correct, and I tried hard to break it. B1 is one word in the migration — the counter's *initial*
value for projects that already exist — and it reproduces this project's signature defect exactly:
a 201 whose body violates the schema the same document declares, and an issue that can never be
read back through the only declared read path. B2 is an ADR-0008 half-declaration on `getIssue`.

#### Gates, all run in this worktree (`/Users/yoss/work/got-issues--t-0005`), exit codes read from each tool

| Gate | Exit | Result |
| --- | --- | --- |
| `dotnet test` | 0 | 100 passed — 17 unit, 83 integration |
| `dotnet build --no-incremental` | 0 | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | 0 | solution |
| `dotnet format --verify-no-changes` (SmokeTests csproj) | 0 | the project outside the solution |
| `./tools/check-drift.sh` | 0 | `libs/` clean beforehand, so 0 is a drift pass and not the dirty-tree 2 |
| `./tools/smoke.sh` | 0 | 13/13, 5m10s |
| `python3 tools/validate-project-os/validate.py` | 0 | 20 tickets, 8 ADRs |

Scope fidelity: clean. Every In Scope item is present; nothing from Out of Scope appears — no
lifecycle fields, no listing or filtering, no comments, no edit or delete. Every acceptance
criterion has a corresponding change and at least one test.

**Method note, so nobody mistakes me for a co-implementer.** I mutated implementation code five
times to verify coverage claims, ran the suite, and restored with `git checkout -- apps/` each
time; `git status --porcelain` is empty and `IssuesTests` is 13/13 on the restored tree. I fixed
nothing. I also stood up a full Compose stack under its own project name (`-p rev5c14`), asserted
each container healthy before trusting any response, confirmed attribution by stopping the API
container and seeing the endpoint stop answering (curl exit 7), and tore it down with `down -v`.

---

## Blocking

### B1 — The migration starts the counter at 0 for every project that already exists, so the first issue in such a project is `GOTI-0`

`apps/GotIssues.Api/Data/Migrations/20260831193427_AddIssues.cs:15` (`defaultValue: 0`), against
`apps/GotIssues.Api/Data/ProjectRecord.cs:37` (`public int NextIssueNumber { get; set; } = 1;`).

The model says the counter starts at 1. The migration backfills existing rows with 0. Those two
statements disagree, and the allocator — which is otherwise right — faithfully returns what it is
given:

```text
UPDATE projects SET "NextIssueNumber" = "NextIssueNumber" + 1 ... RETURNING "NextIssueNumber" - 1
        counter 0  ->  sets it to 1, returns 1 - 1 = 0
```

New projects are unaffected: EF writes the CLR default of 1 on insert, so a project created after
this migration numbers from 1 and every test in the suite passes. The defect is reachable only on
the upgrade path — which is the normal path. `compose.yaml:119-121` declares `postgres-data` as a
**named volume**, T-0004 is `done` and shipped project creation, and the migration runs against
whatever is already there.

**Reproduced end to end on a real stack**, not inferred:

1. Brought the stack up and created project `GOTI` through the API.
2. Rolled the database back to the pre-T-0005 schema (dropped `issues`, dropped the
   `NextIssueNumber` column, removed the `AddIssues` row from `__EFMigrationsHistory`) — the state
   a deployment was in with T-0004 shipped and T-0005 not yet applied. The `GOTI` row survived.
3. Ran the real migrator, exactly as Compose does on deploy: `docker compose run --rm migrator`.
   It applied `20260831193427_AddIssues` and left `GOTI."NextIssueNumber" = 0`.
4. Called the API:

```text
POST /projects/GOTI/issues   ->  201  {"key":"GOTI-0","number":0,...}
GET  /issues/GOTI-0          ->  400  application/problem+json
GET  /issues/GOTI-1          ->  404
POST /projects/GOTI/issues   ->  201  {"key":"GOTI-1","number":1,...}   (the *second* issue)
```

What that costs, precisely:

- **AC1b fails.** "Given a project with no issues, when the first issue is created, then it is
  numbered **1**." It is numbered 0.
- **The 201 body violates the contract that produced it.** `Issue.key` declares
  `^[A-Z][A-Z0-9]{1,9}-[1-9][0-9]{0,8}$` and `Issue.number` declares `minimum: 1`
  (`spec/openapi.yaml:319-336`). `GOTI-0` and `0` satisfy neither. This is T-0004's
  409-with-the-wrong-media-type and its undeclared 500 in a third costume: the document promising
  one thing and the system delivering another.
- **The issue is created and cannot be read.** `GET /issues/{issueKey}` carries the same pattern,
  so the only declared read path returns 400 for the key the create just handed out. Not a 404 —
  a 400, telling the caller their own key is malformed.
- **`GOTI-1` stops meaning the same thing across projects.** Numbering runs 0, 1, 2 in an upgraded
  project and 1, 2, 3 in a new one, so `GOTI-1` is the second issue in one and the first in the
  other — a softer version of the reuse the ticket says must never happen.

**Mutation evidence for the API-level claim** (mutant E, below): setting the CLR initialiser to 0 —
precisely the state the migration leaves a pre-existing row in — turns **5 of 13** tests red,
including AC1, AC1b, AC1c, AC1d and AC2, with `Expected: "GOTI-1" / Actual: "GOTI-0"`. So the suite
*would* catch this; nothing in it ever reaches a project row created before the migration.

**The fix is `defaultValue: 1`.** Two things worth doing alongside it, both cheap:

- Consider `HasDefaultValue(1)` on the property so the model and the database agree. Today the
  column is left as `integer NOT NULL DEFAULT 0` in PostgreSQL (verified against
  `information_schema.columns`) while the EF model declares no default at all — invisible to EF,
  and a trap for any future insert path that does not go through the entity.
- A test that exercises the upgrade path rather than a fresh schema. The whole defect lives in the
  gap between "migrate an empty database" and "migrate a database with rows in it", and every test
  in the suite is on the near side of it.

### B2 — `getIssue` declares a 403 and never says who is refused (ADR-0008)

`spec/openapi.yaml:186-193` (the operation description) against
`apps/GotIssues.Api/Controllers/IssuesController.cs:95`
(`[Authorize(Policy = AuthorizationPolicies.Member)]`).

[ADR-0008](../../architecture/adr/ADR-0008-role-restrictions-declared-in-the-contract-enforced-by-policy.md) requires
**both halves**: "Every operation whose access depends on a role says so in its `description`, and
declares `403` among its responses." `getIssue` has the second and not the first. Its access does
depend on a role — a token carrying `role: superuser` is refused, which is exactly what the
integration test `A_caller_with_an_unrecognised_role_is_refused` demonstrates for the sibling
operation.

`createIssue` gets this right ("Any caller holding a recognised role may create an issue; unlike
creating a project, this is not an administrative act"), and so does T-0004's member-level
`listProjects` ("Any caller holding a recognised role may list projects..."). `getIssue` is the
outlier. The rule *is* stated — in the controller's XML doc, "Reading an issue is open to any
recognised role" — which is the precise location ADR-0008 was raised to reject: a caller generating
a client from the contract sees a 403 with no account of what triggers it.

One sentence in the spec plus a regenerate. Blocking because it is an accepted ADR (precedence
level 3), not because it is large.

---

## The five things I was asked to judge rather than accept

### 1. Is the allocator correct under concurrency, and does the ten-way test earn its place?

**Yes to both, and the test is doing more work than the record claims.**

The allocation is right, and for the reason the plan gives. `UPDATE ... SET n = n + 1 ...
RETURNING n - 1` is a single statement, so under PostgreSQL's default READ COMMITTED the second
writer blocks on the row lock, **re-reads the row after acquiring it**, and increments the value
the first writer committed. There is no read-then-write window to lose an update in. I measured the
locking rather than assuming it, in a container of my own:

| Probe | Result |
| --- | --- |
| Session A holds an open transaction having allocated in project `AAAA`; session B allocates in `AAAA` | **Blocks** (killed at 3s, exit 124) — the serialisation AC1d depends on |
| Same, but session B allocates in **`BBBB`** | **Returns immediately, exit 0** — different rows, no contention |
| Allocate inside a transaction, then `ROLLBACK` | Counter back to its prior value — **the number is returned, not burned** |

So the case the test does not cover — **two concurrent creates in different projects must not block
each other** — holds, and holds for a structural reason (the lock is on the project row, and there
is one row per project). Worth a test only if someone later replaces this with a shared counter; I
would not block on it. And a **create that fails after allocation does not burn a number**, because
the increment is inside the transaction with the insert. See N1 for what is unproven there.

**Is the ten-way test passing incidentally?** No — and the sharpest evidence is a mutant the
implementer did not run. With `MAX(number)+1` substituted for the allocator but the unique index
**kept** (mutant C), **5 of the 10 concurrent requests came back 500** with
`application/problem+json`. Half the requests genuinely collided. `Task.WhenAll` over ten
`WebApplicationFactory` clients is producing real overlap, not ten requests that happen to queue.

The one honest limitation, which the plan already states: this proves correctness, not throughput.
Ten creates against one row serialise by design.

### 2. The recorded mutation evidence

**Both recorded rows reproduce exactly. The negative was the right call to record, and it is
understated.**

| # | Mutant | Build/EF accept it? | Reaches the assertion? | Result | What it proves |
| --- | --- | --- | --- | --- | --- |
| A | `MAX(number)+1`, no explicit transaction, **unique index dropped** (migration `unique: false`; the model snapshot untouched, so no `PendingModelChangesWarning`) | Yes | Yes — fails on `numbers.Distinct().Count()`, downstream of `Assert.All(Created)`, so all 10 returned **201** | **12 of 13 pass.** Only AC1d fails: `Expected: 10 / Actual: 4` — six duplicate numbers, silently | The ticket's own prediction, confirmed. Twelve tests, including every other numbering criterion, cannot tell the wrong answer from the right one |
| B | Correct allocator, **unique index dropped** | Yes | n/a — nothing fails | **All 13 pass** | The index is not proven by behaviour *while the allocator is correct*. The recorded negative is accurate |
| C | `MAX(number)+1`, **unique index kept** — my addition | Yes | Yes — fails on `Assert.All(... Created)` with five 500s | **12 of 13 pass**, AC1d fails differently | The index is not inert. Under a broken allocator it is what converts silent duplicates into loud 500s. Also: the concurrent test achieves real overlap (5/10 collided), and the **declared 500 is real and carries `application/problem+json`** on `POST /projects/{key}/issues` |
| D | Correct allocator, unique index kept, **explicit transaction removed** — my addition | Yes | n/a | **All 13 pass** | See N1: the transaction is as unproven as the index, and it guards a property the ticket states |
| E | `NextIssueNumber` initialiser `= 1` changed to `= 0` — the state the migration leaves a pre-existing row in | Yes | Yes — `Expected: "GOTI-1" / Actual: "GOTI-0"` | **5 of 13 fail**: AC1, AC1b, AC1c, AC1d, AC2 | B1's API-level consequence, and proof the suite would catch it if any test reached a pre-migration project |

**Was recording the unproven index the right call?** Yes, unambiguously. It is the honest form of a
claim this project has twice been burned by, and the alternative — shipping a deliberately broken
allocator to make a constraint fail — buys nothing. But mutant C shows the record is weaker than
the truth, and the stronger sentence is available for free: *the index is unobservable only while
the allocator is correct; the moment it is not, the index is the difference between a duplicate
identifier and a 500.* That is a better argument for keeping it than "guarantee against a future
change", and it is measured rather than asserted. Not blocking — the record is honest, just
understated. Worth amending while the branch is open.

### 3. The `description` versus `title` split, and the regex problem that was not there

**The patterns behave exactly as claimed.** I re-measured all three candidates directly against
`RegularExpressionAttribute.IsValid` rather than trusting either the plan or its correction. In the
table, `NUL` stands for the literal `U+0000` character:

| Input | `^[^NUL]*$` (shipped) | `\A[^NUL]*\z` | unanchored `[^NUL]*` | `title` pattern |
| --- | --- | --- | --- | --- |
| `para one` + blank line + `para two` | accept | accept | accept | REJECT |
| **ends with a newline** | **accept** | accept | accept | REJECT |
| ends with CRLF | accept | accept | accept | REJECT |
| a lone newline | accept | accept | accept | REJECT |
| empty | accept | accept | accept | REJECT (`+`, and `minLength: 1`) |
| contains `U+0000` | **REJECT** | REJECT | REJECT | REJECT |
| **`U+0000` as the last character** | **REJECT** | REJECT | REJECT | REJECT |
| `U+0000` then a newline | REJECT | REJECT | REJECT | REJECT |
| tab | accept | accept | accept | REJECT |
| DEL `U+007F` | accept | accept | accept | REJECT |

The predicted problem genuinely does not exist, and the recorded explanation is the right one:
`[^NUL]*` consumes the newline (unlike `.`), so the match spans the whole value and `$` matches at
true end-of-string. The trailing-`U+0000` case — the one where a naive reading of "`$` matches
before a trailing newline" would have produced a *false accept* rather than a false reject — is
also correctly rejected. Confirmed live on the stack: a description containing `U+0000` is 400, and
a description of `line1`-newline-`line2`-newline round-trips through create and read intact.

Choosing the ECMA-262-valid form over `\A...\z` was right for a published contract, and it costs
nothing here. Recording a wrong prediction as a wrong prediction is the correct disposition; see N2
for the one question in this area the ticket asked and the branch did not answer.

### 4. Two paths, and `LastIndexOf('-')`

**The split is right, and the parse is safe — verified rather than argued.**

Creation scoped to the project is correct on the merits the plan gives: an issue cannot exist
without a project, the number comes from the project row, and T-0007 will hang
`GET /projects/{key}/issues` off the same path. Reading by the quotable key is right for the same
reason the PO chose the identity scheme at all.

`LastIndexOf('-')` is safe, and safe for a stronger reason than the comment gives. The comment says
the shape is enforced before the method runs; the *structural* fact is that the project-key pattern
`^[A-Z][A-Z0-9]{1,9}$` admits no hyphen, so a key matching
`^[A-Z][A-Z0-9]{1,9}-[1-9][0-9]{0,8}$` contains **exactly one** hyphen and `LastIndexOf` and
`IndexOf` cannot disagree. `int.Parse` cannot overflow either: nine digits caps at 999,999,999,
well inside `int`.

I still probed the failure the comment depends on, because "a malformed key never reaches here" is
the kind of claim this project keeps finding to be false. Against the live stack, every malformed
shape returned **400 `application/problem+json`**, never a 500:

```text
/issues/GOTI1            400      /issues/GOTI-01           400
/issues/goti-1           400      /issues/GOTI-1234567890   400
/issues/GOTI-0           400      /issues/GOTI--1           400
/issues/A-1              400
```

`[ApiController]`'s model-state filter runs at order -2000, ahead of the generated
`[ValidateModelState]` action filter, which is why these come back as `ValidationProblemDetails`
with the declared media type rather than the `application/json` `SerializableError` the generated
filter would have produced. Worth knowing, since the generated filter is effectively dead here.
See N6 — none of this is covered by a test.

### 5. What the contract declares versus what the API delivers

**Every declared response on both new operations is real.** I checked each against a live Compose
stack rather than against the suite.

| Declared | `POST /projects/{projectKey}/issues` | `GET /issues/{issueKey}` |
| --- | --- | --- |
| 201 / 200 | 201 `application/json`, body round-trips including a multi-line description | 200 `application/json` |
| 400 | Real, `application/problem+json`: empty title, title with a newline, description with `U+0000`, description at 10 001 chars, malformed `projectKey`, non-JSON body, and a lone surrogate in the description | Real, `application/problem+json`: seven malformed key shapes, all 400 |
| 401 | Real, `application/problem+json` (no token; garbage token) | Real, `application/problem+json` |
| 403 | Real — `[Authorize(Policy = Member)]`, tested with `role: superuser` | Real by the same policy; **not tested** (N5) |
| 404 | Real, `application/problem+json`, `type: https://httpstatuses.io/404`, nothing written | Real, `application/problem+json`, for both an unknown number and an unknown project inside the key |
| 500 | **Real, `application/problem+json`** — observed under mutant C, five concurrent unique violations | Same handler; T-0004's smoke case covers the shape |

Nothing is declared that the API cannot produce. The converse — something delivered but not
declared — is where B1 lives: the 201 body itself, when the project predates the migration.

Two smaller notes in the same family. `description` comes back as `null` when omitted, matching the
nullable schema. And a lone surrogate in a description is rejected at 400 by `System.Text.Json`
before it can reach Npgsql, so the `U+0000` pattern is not the only thing standing between user
text and an encoding failure — good, and worth knowing it is the serialiser doing that one.

---

## Non-blocking

- **N1 — The explicit transaction is exactly as unproven as the unique index, and guards more.**
  Mutant D removed `BeginTransactionAsync`/`CommitAsync` entirely, leaving the bare
  `UPDATE ... RETURNING`: **all 13 tests pass**. `UPDATE ... RETURNING` is atomic on its own, so
  concurrency is unaffected; the transaction's only job is that a create failing *after* allocation
  returns the number instead of burning it — which is half of AC1d's "no number is skipped", and
  the implementer already flagged it for QA as untested. The record names the index as unproven and
  is silent on the transaction. Both belong in the same sentence. I verified the underlying
  property directly (`BEGIN; UPDATE ... RETURNING; ROLLBACK;` leaves the counter untouched), so the
  design is right — it is the *record* that is asymmetric.

- **N2 — A title may still contain `U+0085` and `U+2028`, and the ticket asked this ticket to
  decide that.** Verified live: a title of `a`+`U+0085`+`b`+`U+2028`+`c` returns **201** and echoes
  both characters back. The refinement entry above says plainly: *"`U+0085` and `U+2028` are not
  excluded ... If a title needs more, decide it here rather than inheriting a rationale that was
  scoped to a display name."* The branch inherits the pattern and records no decision. Keeping the
  narrow checkable constraint is a perfectly good answer — but it is an answer the ticket asked for
  and did not get. Related: the spec's title description reads "tabs and line breaks have no place
  in a title" (`spec/openapi.yaml:333-334`), which overstates what the pattern does, since `U+0085`
  and `U+2028` *are* line breaks and are accepted. One recorded sentence closes both.

- **N3 — No test covers cross-project independence under concurrency.** AC1c covers independent
  *numbering*; nothing covers independent *locking*. I verified it at the database level (above).
  It is structural, so I would not add a test today — but if the allocator is ever replaced with
  anything sharing a row, this is the property that disappears silently.

- **N4 — `CreateIssue` re-reads the project row it has just locked.** `IssuesController.cs:68-71`
  issues a second `SELECT` for `project.Id` and `project.Key` immediately after the `UPDATE`,
  inside the same transaction. `RETURNING "Id", "NextIssueNumber" - 1` would return both from the
  statement that already has the row. One round trip on the hot path; take it or leave it.

- **N5 — Nothing asserts the 403's body, and `getIssue`'s 403 has no test at all.**
  `A_caller_with_an_unrecognised_role_is_refused` asserts the status code only, and covers
  `createIssue` alone. T-0004's review recorded "the 403's body is asserted nowhere" as a carried
  gap; this ticket adds two more operations declaring 403 without narrowing it. The 403 body comes
  from `UseStatusCodePages`, a different mechanism from the controller's `Problem(...)` — so it is
  not covered by transitivity from the 404 assertions.

- **N6 — The malformed-`issueKey` 400 has no test, and it is the safety property the parse depends
  on.** `GetIssue`'s comment states the invariant that stops `LastIndexOf` returning -1; nothing
  enforces it. It holds today (I measured seven shapes), and it holds because of a `pattern` in the
  spec — one spec edit away from turning that line into an `ArgumentOutOfRangeException` and an
  undeclared 500. One `[Theory]` over the shapes above makes the contract's role explicit.

- **N7 — No unit tests were added.** Defensible: the allocator is inherently a database behaviour
  and belongs in the integration tier, and the only pure logic in the diff is the key split, which
  N6 covers better. Recorded so the 17/83 split reads as a noted choice rather than an oversight.

---

- **Did:** Reviewed the full diff of `t-0005-issues` @ `b472465` against `main`; ran every gate in
  this worktree; reproduced both recorded mutants and ran three more; measured the allocator's
  locking, rollback and cross-project behaviour directly in PostgreSQL; measured all candidate
  regex patterns against `RegularExpressionAttribute`; probed every declared response of both new
  operations against a live Compose stack; and reproduced the migration defect end to end through
  the real migrator.
- **Decided:** **Request changes** — B1 (the migration backfills the counter to 0, so the first
  issue in any pre-existing project is `GOTI-0`: AC1b fails, the 201 body violates the declared
  `Issue` schema, and the issue cannot be read back) and B2 (`getIssue` declares a 403 without the
  description ADR-0008 requires beside it). N1 to N7 are the implementer's to take or leave.
- **Remaining:** implementer addresses B1 and B2 on the branch and re-requests review.
- **Open questions / blockers:** none. B1's fix is `defaultValue: 1`; whether to add
  `HasDefaultValue(1)` and an upgrade-path test is the implementer's call, and I would take both.
- **Branch / PR:** `t-0005-issues` @ `b472465`.
- **Test state, measured in this worktree:** `dotnet test` **100/100** exit 0 · `dotnet build
  --no-incremental` **0 warnings** exit 0 · `dotnet format` exit 0 (solution and SmokeTests) ·
  `check-drift.sh` exit 0 · `smoke.sh` **13/13** exit 0 · `validate.py` exit 0. Working tree
  restored and clean after mutation; `IssuesTests` 13/13 on the restored tree.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — B1 was a real upgrade defect, and every test was blind to it

`claude-rev-5c14` found a defect that would have shipped, and found it by doing the one thing no
test in this repository does: **reverting a live stack to the previous schema and running the real
migrator against it.**

#### B1 — the first issue in every existing project would have been numbered zero

`NextIssueNumber` was declared with a CLR initialiser (`= 1`) and no database default, so the
migration backfilled existing rows with **0**. `UPDATE … +1 RETURNING …-1` then returns 0, and the
first issue in any project created before this ticket gets the key `GOTI-0` — which violates the
`^…-[1-9][0-9]{0,8}$` pattern and the `minimum: 1` **this same document declares**, and which the
read path therefore refuses with 400. The issue would exist and be unreachable.

Reproduced by the reviewer end to end: `201 {"key":"GOTI-0","number":0}`, then `GET /issues/GOTI-0`
→ 400. [T-0004](T-0004-create-and-list-projects.md) is `done` and `compose.yaml` uses a named
volume, so this is the ordinary upgrade path rather than a hypothetical.

**Why nothing caught it: every test in the suite migrates an empty schema.** Not one depended on
what a migration does to rows that already exist — so the entire class of upgrade defects was
invisible, and would have stayed invisible for every future migration too.

Fixed with `HasDefaultValue(1)` in the model, so the default lives in the database rather than only
in the CLR initialiser, and the migration regenerated to `defaultValue: 1`.

**`UpgradePathTests` closes the instance and part of the class — not the class.** *(Corrected after
re-review; the original sentence claimed it closed the class, and that overstatement is struck
here rather than edited away.)* It migrates to the schema as it stood *before* this ticket, inserts
a project, upgrades, and asserts the first issue is number 1 and readable. Mutation-proved by
restoring `defaultValue: 0`: `Expected: 1, Actual: 0`, and the mutant fails on the `number`
assertion *after* the 201, so the whole upgrade path executed.

What it does generalise: `MigrateAsync()` goes to latest, so that seeded project row is carried
through **every future migration**. What it does not: pre-existing rows in any other table —
[T-0006](T-0006-issue-lifecycle-fields.md) backfills lifecycle columns onto `issues`, the same
defect shape one table over, and there are no pre-existing issues here.

**The sharper statement of the class is the reviewer's, and it is what makes this a ticket rather
than a test:** B1 was a **model-versus-database divergence sitting in the gap between two gates
that both look elsewhere.** EF's `PendingModelChangesWarning` compares the model to the migration
*snapshot*; `check-drift.sh` compares the *spec* to generated code. **Nothing compares the model to
the database.** A gate that did would have caught B1 without anyone thinking about upgrades at all
— which is what distinguishes closing a class from covering a case.

That gate is now [T-0021](T-0021-prove-migrations-against-populated-databases.md), created by the
reviewer, carrying both candidate mechanisms and an acceptance criterion requiring the mutant be
killed *for the schema reason*.

#### B2 — a declared 403 with nothing saying who is refused

`getIssue` declared `403` and its description never said who receives it, which is exactly half of
[ADR-0008](../../architecture/adr/ADR-0008-role-restrictions-declared-in-the-contract-enforced-by-policy.md)
— the ADR raised on T-0004 *because* the rule was recorded only in a controller comment. I put the
rule in an ADR one ticket ago and then broke it in the next. Both operations now say who may call
them and what a caller lacking a role receives.

#### N2 — a decision refinement asked this ticket to make, which I had inherited instead

Titles accept `U+0085` and `U+2028`, and the specification said "line breaks have no place in a
title", which claims more than the pattern delivers. Decided rather than inherited: the constraint
stays the **narrow, checkable one**, and the document now says so and says what it excludes. A
title carrying an exotic separator is cosmetic; a title carrying `U+0000` cannot be stored at all,
and only the second is worth a constraint that has to be got right.

#### On the reviewer's amendment to my own record

I recorded that dropping the unique index changes nothing observable. The reviewer's addition is
sharper and I am adopting it: **the index is unobservable only while the allocator is correct.**
Break the allocator and it is the difference between a silent duplicate and a loud 500 — which the
reviewer measured, seeing 5 of 10 concurrent requests return 500 with `MAX+1` *and the index kept*,
where the index-dropped variant returned ten 201s and four issues. So the index is not decorative:
it converts a data-corruption bug into a visible failure. That is worth more than "untested".

- **Did:** Fixed the backfill default and regenerated the migration; added `UpgradePathTests` and
  mutation-proved it; put the 403 rule in both operations' descriptions; decided the title
  constraint instead of inheriting it; amended the index record.
- **Decided:** the database carries the default, not just the CLR initialiser; the title constraint
  stays narrow and says so.
- **Remaining:** re-review.
- **Open questions / blockers:** none.
- **Test state:** `dotnet test` **101/101** (17 unit, 84 integration) · build 0 warnings ·
  `dotnet format` exit 0 both · `validate.py` exit 0 · drift and smoke below.


### 2026-08-31 — Software Engineer + Architect (claude-rev-5c14) — re-review of `t-0005-issues` @ `b71d037`

Second pass. Reviewer still not the implementer (`claude-sm-9d4e`).

**Verdict: Request changes.** One blocking finding, and it is a sentence rather than a defect:
the Work Log claims `UpgradePathTests` "closes the class, not just the instance", and measurement
does not support that. **B1 and B2 are genuinely closed** — I re-ran the exact live reproduction
that found B1, against the fixed code, and it now behaves correctly. The code is done. What is left
is one paragraph, and it matters because it is the paragraph that would stop the next person
writing upgrade coverage for the next migration.

#### Gates, all run in this worktree, exit codes read from each tool

| Gate | Exit | Result |
| --- | --- | --- |
| `dotnet test` | 0 | **101 passed** — 17 unit, 84 integration |
| `dotnet build --no-incremental` | 0 | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | 0 | solution |
| `dotnet format --verify-no-changes` (SmokeTests csproj) | 0 | the project outside the solution |
| `./tools/check-drift.sh` | 0 | `libs/` clean beforehand, so a drift pass and not the dirty-tree 2 |
| `./tools/smoke.sh` | 0 | 13/13, 5m09s |
| `python3 tools/validate-project-os/validate.py` | 0 | 21 tickets, 8 ADRs |

Mutants restored each time; `git status --porcelain` empty, `git diff b71d037 HEAD -- apps/ libs/
spec/` empty.

---

## B1 — closed, verified by the method that found it

Fix: `HasDefaultValue(1)` on the property (`GotIssuesDbContext.cs:71`) and `defaultValue: 1` in the
regenerated migration. I did not take this on the test's word. I rebuilt the stack and re-ran the
**identical** reproduction — created project `GOTI`, rolled the database back to the pre-T-0005
schema, ran the real `docker compose run --rm migrator`, then called the API:

| | before the fix | after the fix |
| --- | --- | --- |
| `projects."NextIssueNumber"` after backfill | `0` | **`1`** |
| database column default | `0` | **`1`** |
| `POST /projects/GOTI/issues` | `201 {"key":"GOTI-0","number":0}` | **`201 {"key":"GOTI-1","number":1}`** |
| `GET` the created issue | `GOTI-0` → **400** | `GOTI-1` → **200** |
| second create | `GOTI-1` | `GOTI-2` |

**The fix is in the database, not only in the CLR initialiser — measured, not assumed.** Mutant G:
delete the `= 1` initialiser entirely, leaving only the store default. **All 84 integration tests
pass.** EF sees the CLR default, omits the column from the INSERT, and PostgreSQL's `DEFAULT 1`
supplies it. That is exactly what "in the database rather than only in the CLR initialiser" claims,
and it is now the thing holding the invariant up rather than a comment saying so.

**`UpgradePathTests` kills the defect cleanly and for the right reason.** Mutant F, reinstating
`defaultValue: 0`, run against the whole integration suite:

- **83 of 84 pass. Only `UpgradePathTests` fails**, with `Expected: 1 / Actual: 0`.
- The mutant **reaches the assertion**: the failure is on the `number` assertion, which sits after
  `Assert.Equal(HttpStatusCode.Created, ...)`. So the targeted migration ran, the seeded project
  landed, the upgrade ran, and the create succeeded — the whole path executed and the mutant is the
  cause. Not a build rejection, not an unrelated error, not a vacuous red.
- The 83 is the point. It is the same 83 that were blind to B1 in the first place.

The test is well made in a way worth naming: it is self-verifying at every step. A wrong
`BeforeIssues` constant throws, a failed seed makes the create 404, and a failed upgrade makes it
500. There is no arrangement in which it passes without having done what it says.

## B2 — closed

Both operations now carry the role sentence, and the 403 keeps its declared response. I checked
the half that actually matters for ADR-0008 — that the declaration **reaches a generated client**,
which is the whole reason the ADR rejects controller comments: `Requires a recognised role` appears
in `libs/GotIssues.Contracts/.../Controllers/IssuesApi.cs` (both operations), in
`libs/GotIssues.Client/.../Api/IssuesApi.cs`, and in the published `libs/GotIssues.Client/api/openapi.yaml`.
Drift exit 0, so those are reproducible from the spec rather than hand-placed.

## N2 — decided, and the document now matches the behaviour

The spec no longer says "line breaks have no place in a title" while accepting two of them. It
states the limit and the reason for it. I re-confirmed the behaviour is unchanged (a title carrying
`U+0085` and `U+2028` is still a 201) — the change is that the document is now true, which was the
finding. Keeping the narrow checkable constraint is the right call and the argument given for it is
the right argument.

---

## Blocking

### B3 — "closes the class, not just the instance" is a coverage claim measurement does not support

Work Log, this ticket, entry of 2026-08-31 (`claude-sm-9d4e`): *"**`UpgradePathTests` now closes the
class, not just the instance.**"*

It does not, and [TESTING.md](../../standards/TESTING.md) is explicit that this is the same defect
as an overstated assertion: *"The mutation record states what the mutant proves, not what you hoped
it proved."* The mutation record itself is accurate — it is the sentence around it that reaches too
far. Concretely, what the test does and does not close:

**What it genuinely closes, and I want to credit this properly:**

- The instance, decisively.
- More than one boundary, actually. `MigrateAsync()` with no argument migrates to *latest*, so the
  seeded `projects` row is carried through **every migration written from now on**. A future
  migration that breaks pre-existing project rows in a way that shows up in issue numbering or in
  the readability of `OLD-1` will fail this test without anyone touching it. That is real
  generalisation and the entry undersells it while overselling the rest.

**What it does not close:**

- **Pre-existing rows in any other table.** The test seeds one row in `projects` and nothing else.
  [T-0006](T-0006-issue-lifecycle-fields.md) adds `NOT NULL` lifecycle columns to `issues` — the
  same defect shape, one table over, with a backfill value that could sit outside the enum the
  contract declares. There are **no pre-existing issues** in this test, so it cannot see that. The
  blind spot moves; it does not close.
- **The schema divergence itself.** B1 was a disagreement between the EF model and the actual
  database, in the gap between two gates that both look elsewhere: EF's `PendingModelChangesWarning`
  compares the model to the *migration snapshot*, and `check-drift.sh` compares the *spec* to
  *generated code*. Nothing compares the model to the database. That gap is untouched, and it is
  the one that would have caught B1 **without anyone thinking about upgrades at all** — which is
  the property that distinguishes closing a class from closing an instance.
- **Anything that makes the next author think about it.** No DoD item, no standard, no failing
  test. The habit rests on someone remembering — and the reason B1 existed is that nobody
  remembered, because nobody knew the gap was there. A Work Log stating the class is closed
  recreates that ignorance in a more confident form, which is why this is worth a round trip
  rather than a shrug.

**Fix:** replace the claim with what the test does. Something close to: *"`UpgradePathTests` closes
the instance and establishes the pattern — and because it migrates to latest, its seeded project row
is carried through every future migration. It does not close the class: it seeds one table, asserts
this ticket's behaviour, and nothing compares the migrated schema to the model. T-0021 carries the
class."*

**The destination now exists**, so this is a pointer rather than a promise:
`T-0021` (`tickets/T-0021-prove-migrations-against-populated-databases.md`) — *Prove what migrations do to
databases that already hold rows* — created on the trunk (`39cc88e`; not a link here only
because the file is on `main` and this branch predates it) per the review-code skill's
rule that out-of-scope findings become tickets rather than review-time scope creep. It carries both
candidate mechanisms (schema conformance as the class-closer; boundary-by-boundary upgrade tests as
the follow-on), the sizing unknown, and AC4 requiring the mutant to be killed *for the schema
reason* rather than through an issue-numbering assertion. **So: it needed a ticket rather than a
test — and it also needed the test, which is already here.**

---

## N1 and N3 — you asked for my read, and I have changed my mind on one of them

### N1 (the transaction is unproven) — it can be tested honestly, and better than I first thought

I had this as "probably untestable". That was wrong, and the reason is worth stating because it is
the same reasoning error the standard warns about — I reached for what is *reachable through the
public surface* and stopped there.

A deterministic failure between allocation and insert does exist. Seed a project's counter to *N*
and insert an issue with `Number = N` directly, then `POST`. The allocator returns *N*, the insert
violates the unique index, the exception propagates, and `await using` rolls the transaction back.
Assert **two** things: the response is 500 `application/problem+json`, and `NextIssueNumber` is
**still *N*** — the number was returned, not burned.

That single test closes N1 *and* gives the unique index its first behavioural proof, which is the
other thing we have both been recording as unproven. I have verified each half separately and not
the composition: a duplicate insert does produce a 500 (mutant C, last pass — five of them), and a
rollback does restore the counter (measured in `psql`: `BEGIN; UPDATE … RETURNING; ROLLBACK;`
leaves the value untouched). So I would call it very likely rather than proven.

One honesty caveat to write **into** the test rather than leave implied: it constructs a state the
API cannot itself produce (an issue whose number is at or above the counter). That is the normal
way to test rollback and it is fine — but a reader who does not know that will read it as a
scenario, and it is a mechanism.

**Disposition:** worth doing, and cheap. Not a defect, so I am not blocking on it. If it does not
land on this ticket it should be a ticket rather than a Work Log note, because it is a test someone
can sit down and write, not an observation to preserve.

### N3 (cross-project non-blocking) — I agree with your disposition and not with the reason

Your instinct is right about the obvious formulation and wrong about the only good one, and the
distinction is worth keeping because "that would be flaky" outlives the case it was true for.

Do **not** assert "B finished within X ms". Assert **liveness**: hold a transaction open on project
A's counter row from the test, then create in project B through the API with a generous timeout, and
assert 201. The pass condition does not depend on speed — only on B not queueing behind A. It fails
only if the allocator ever shares a row across projects, in which case B blocks until the test's
transaction is released, deterministically. So it *can* be written without flakiness.

**But it is not worth writing now**, and here I land where you did. The property is structural —
one counter row per project — and no change breaks it without rewriting the allocator wholesale, at
which point AC1d fires and so would N1's test. The measurement in the Work Log is the right weight
for it today. I have recorded the non-flaky formulation above so the option survives if anyone ever
proposes a shared allocator; that is the part that would otherwise be lost.

---

## Non-blocking

- **N8 — the migration was renamed, which will break stale local volumes.**
  `20260831193427_AddIssues` became `20260831200135_AddIssues`. `main` never had either, so the
  trunk is fine and this is not a defect. But anyone who ran the *old* branch against a persistent
  volume — the Compose stack uses a named one — has the old ID in `__EFMigrationsHistory`, and the
  renamed migration will try to `ADD COLUMN "NextIssueNumber"` again and fail. Worth one line
  somewhere the acceptance session will see it: `docker compose down -v` first. (I hit exactly this
  state on my own review stack and tore it down.)

- **N9 — `CreateIssueRequest.title` still says only "Control characters are excluded".** The N2
  decision landed on `Issue.title` (the response schema) and not on the request schema, which is
  the one a generated client validates against before sending. The constraint is identical, so
  nothing behaves differently; it is the explanation that is in one place and not the other.

- **N10 — `HasDefaultValue(1)` makes `NextIssueNumber = 0` inexpressible through the entity.** The
  property is now `ValueGeneratedOnAdd`, so EF reads a CLR-default 0 as "not set" and writes 1
  instead. Demonstrated by mutant G above, which is the same mechanism seen from the useful side.
  Nothing wants 0 today. If anything ever does — a counter reset, a data migration — it will
  silently get 1, and the code doing it will look correct. One sentence near the property would
  cost nothing.

- **N4, N5, N6, N7 from the first pass stand**, unchanged and still non-blocking: the extra project
  read after the `UPDATE`; no assertion on any 403 body; no test for the malformed-`issueKey` 400
  the parse depends on; no unit tests added.

---

- **Did:** Re-reviewed `b71d037`; re-ran every gate in this worktree; verified B1's fix by
  rebuilding the stack and repeating the exact reproduction that found it; ran mutant F (reinstate
  `defaultValue: 0`) against the full integration suite and confirmed it reaches its assertion; ran
  mutant G (delete the CLR initialiser) to establish that the default now genuinely lives in the
  database; confirmed the ADR-0008 declarations reach the generated client; created
  `T-0021` (`tickets/T-0021-prove-migrations-against-populated-databases.md`) on the trunk for the class B3
  names.
- **Decided:** **Request changes** — B3 only, and only the Work Log. B1 and B2 are closed and
  verified independently; N2 is decided and the document now matches the behaviour; the index
  amendment is adopted and correct. B3 is one paragraph, with suggested text above and T-0021 as
  its destination.
- **Remaining:** correct the claim, then merge; then the `os:` status commit on the trunk.
- **Open questions / blockers:** none. My reads on N1 (testable, and the same test would prove the
  index) and N3 (agree it stays unwritten, but not because it would be flaky) are above and are the
  implementer's to take or leave.
- **Branch / PR:** `t-0005-issues` @ `b71d037`.
- **Test state, measured in this worktree:** `dotnet test` **101/101** exit 0 · `dotnet build
  --no-incremental` **0 warnings** exit 0 · `dotnet format` exit 0 (solution and SmokeTests) ·
  `check-drift.sh` exit 0 · `smoke.sh` **13/13** exit 0 · `validate.py` exit 0. Tree clean after
  mutation; `dotnet test` re-run green on the restored tree.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — B3: the blocking item was a sentence I wrote

`claude-rev-5c14`'s blocking finding is my own overstatement, and it is right to block on it.
[TESTING.md](../../standards/TESTING.md) makes a coverage claim falsifiable evidence, so a claim
that outruns the test is the same defect as an assertion that outruns its subject — and this one
was load-bearing in the worst way: *"closes the class"* is precisely the sentence that would stop
the next person writing upgrade coverage for [T-0006](T-0006-issue-lifecycle-fields.md)'s
migration. Corrected in place above rather than edited away, and the reviewer's sharper framing
adopted, because it is better than mine: **B1 lived in the gap between two gates that both look
elsewhere**, and nothing in this repository compares the model to the database.

That gap is now [T-0021](T-0021-prove-migrations-against-populated-databases.md).

#### N1 — I said it needed a broken allocator to prove. It did not

I recorded the transaction and the unique index as unprovable without shipping a deliberately
broken allocator. The reviewer's formulation needs neither: **seed the counter to 5 and an issue
already at 5**, then create. The allocator hands out a number that is already taken, and the
system's own behaviour does the rest.

`AllocationRollbackTests` asserts both properties at once, and both mutants kill it distinctly:

| Mutant | Result |
| --- | --- |
| Unique index dropped (migration regenerated) | **Killed** — `Expected: InternalServerError, Actual: Created`. The duplicate was written silently: two issues numbered 5 |
| Explicit transaction removed | **Killed** — `Expected: 5, Actual: 6`. The number was burned |

The first row is **the unique index's first behavioural proof**, and it settles the exchange we
had about it: the index is not decorative and not merely a guard against future change — it is
the difference between silent corruption of the identity this whole ticket is about and a loud
failure. The second row proves the transaction claim the ticket has been making since the plan.

Two properties I had written off as untestable, closed by one test, because someone asked how
rather than whether. Worth remembering next time I record something as unprovable.

#### The rest

**N9** — the explanation reached `Issue.title` and not `CreateIssueRequest.title`, so the schema a
client actually posts carried the bare rule without the reasoning. Fixed.

**N10 recorded, not fixed** — `HasDefaultValue(1)` makes `NextIssueNumber = 0` inexpressible
through the entity: EF reads 0 as unset and writes 1. Harmless now, because nothing wants a zero
counter, and it is the same shape as the defect just fixed, so it is worth naming: the database
default and the CLR default now disagree about what 0 means.

**N8 noted for acceptance** — the migration was regenerated and its timestamp changed, so a stale
local volume from an earlier build of this branch would carry the old identifier. `main` never had
either, so it affects nobody but a reviewer or acceptor reusing a volume; `docker compose down -v`
first.

**N3** — the reviewer's liveness formulation (hold a transaction on project A's row, create in
project B, assert 201 rather than timing anything) is recorded in their entry. Still not written:
the property is structural and any change breaking it also trips AC1d. But the formulation is
kept, because "flaky" was my reason and it was wrong — the obvious version is flaky, that one is
not.

- **Did:** Corrected the overstated coverage claim; added `AllocationRollbackTests` and
  mutation-proved both halves; applied N9; recorded N8 and N10.
- **Decided:** record the claim's correction in place rather than replace it, as with the invalid
  mutation row on T-0004.
- **Remaining:** re-review.
- **Open questions / blockers:** none.
- **Test state:** `dotnet test` **102/102** (17 unit, 85 integration) · build 0 warnings ·
  `dotnet format` exit 0 · `validate.py` exit 0 · drift and smoke below.


### 2026-08-31 — Software Engineer + Architect (claude-rev-5c14) — re-review of `t-0005-issues` @ `c89f416`

Third pass, second on this branch's fixes. Reviewer still not the implementer (`claude-sm-9d4e`).
Branch rebased onto `main` @ `39cc88e`, so it is current with the trunk and `T-0021` resolves.

**Verdict: Approve.** Merge may proceed. B1, B2 and B3 are closed, N1 is closed by a test that is
better than either property it proves, N2 and N9 are decided and documented, and N3, N8 and N10 are
recorded rather than quietly dropped. I verified every claim in the new entry by running it myself;
nothing here rests on the implementer's report.

#### Gates, all run in this worktree, exit codes read from each tool

| Gate | Exit | Result |
| --- | --- | --- |
| `dotnet test` | 0 | **102 passed** — 17 unit, 85 integration |
| `dotnet build --no-incremental` | 0 | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | 0 | solution |
| `dotnet format --verify-no-changes` (SmokeTests csproj) | 0 | the project outside the solution |
| `./tools/check-drift.sh` | 0 | `libs/` clean beforehand, so a drift pass and not the dirty-tree 2 |
| `./tools/smoke.sh` | 0 | 13/13, 5m43s |
| `python3 tools/validate-project-os/validate.py` | 0 | 21 tickets, 8 ADRs |

Two mutants run and restored; `git status --porcelain` empty, full suite re-run green on the
restored tree.

---

## B3 — closed, and corrected in the right way

The claim now reads *"closes the instance and part of the class — not the class"*, with the
original struck in place and a parenthetical saying why, rather than edited out of existence. That
is the same disposition T-0004 used for its invalid mutation rows, and it is the one that leaves a
reader able to see what was believed and what replaced it.

The replacement is accurate against the test, which is the part I checked rather than assumed: it
credits the generalisation that is real (`MigrateAsync()` goes to latest, so the seeded project row
rides through every future migration), names the one that is not (no pre-existing rows in any other
table, so T-0006's backfill onto `issues` is invisible), and carries the two-gates framing to
`T-0021` rather than leaving it as prose. Nothing overstates anything now.

## N1 — closed, and it proves more than either property on its own

`AllocationRollbackTests` is the formulation from the last entry, built as described: counter at 5,
an issue already at 5, then create. I ran both mutants against the **full integration suite**:

| Mutant | Build/EF accept it? | Result | What it proves |
| --- | --- | --- | --- |
| Unique index dropped (migration `unique: false`; snapshot untouched, so no `PendingModelChangesWarning`) | Yes | **84 of 85 pass.** Only `AllocationRollbackTests` fails: `Expected: InternalServerError / Actual: Created` | **The unique index's first behavioural proof.** The duplicate was written and the API said 201 — two issues numbered 5 in one project, silently |
| Explicit transaction removed | Yes | **84 of 85 pass.** Only `AllocationRollbackTests` fails: `Expected: 5 / Actual: 6` | The transaction returns the number instead of burning it |

Both reproduce the recorded results exactly. Three things worth drawing out that the entry does not
claim, and could:

- **The mutants prove the test reaches its own subject.** My standing worry about a bare
  `Assert.Equal(InternalServerError, …)` is that it is satisfied by anything that throws — including
  a throw *before* the allocation, which would leave the counter at 5 and pass all three assertions
  while proving nothing about rollback. The transaction mutant settles it: the counter moves to
  **6**, so the allocation demonstrably happened and the only difference is whether it rolled back.
  The assertion is evidence about the path because a mutant showed the path is reached.
- **It retires a question I could only reason about in the first pass.** Whether EF's
  `Database.SqlQuery` enlists in the ambient transaction was, until now, certain-by-documentation
  and unmeasured — and the whole "no number burned" property rests on it. If it did not enlist, the
  `UPDATE` would autocommit and the unmutated test would read 6. It reads 5. Measured at last.
- **84 of 85 in both rows is the honest measure of how unasserted these were.** Exactly one test in
  the repository now asserts each property, and before this ticket none did. That is a better
  statement of what was gained than "two mutants killed".

And the record now says the right thing about the index: not decorative, not merely a guard against
future change, but the difference between silent corruption of the identity this ticket exists to
define and a loud failure. That is the claim the measurement supports, and it is the one written
down.

The one thing I asked to be written **into** the test rather than implied — that it constructs a
state the API cannot itself produce — is there, twice: in the class comment and at the seed. A
reader will not mistake the mechanism for a scenario.

## N2, N9 — decided and propagated

`CreateIssueRequest.title` now carries the reasoning, not just the rule, and I checked the half that
matters: it reaches `libs/GotIssues.Contracts/.../Models/CreateIssueRequest.cs`,
`libs/GotIssues.Client/.../Model/CreateIssueRequest.cs` and the published
`libs/GotIssues.Client/api/openapi.yaml`, with drift exit 0 — so it is reproducible from the spec
rather than hand-placed. The request schema is the one a generated client validates against, so
this is where the explanation was actually needed.

## N3, N8, N10 — recorded, which was the right disposition for each

N3 stays unwritten with the liveness formulation preserved in both entries, and the correction to
the *reason* is recorded — which, as you say, matters more than the decision. N8 is noted for
acceptance. N10 is named rather than fixed, and named accurately: the database default and the CLR
default now disagree about what `0` means, which is worth a reader's attention precisely because it
is the same shape as the defect it came from.

---

## Non-blocking, and what is left after this merges

- **N4, N6, N7 stand**, unchanged and still not worth blocking: the extra project read after the
  `UPDATE`; no test for the malformed-`issueKey` 400 that `LastIndexOf` depends on; no unit tests
  added. Of the three, **N6 is the one I would take** — it is a `[Theory]` over shapes I have
  already measured, and it pins a safety property that is currently one spec edit away from becoming
  an undeclared 500.

- **N5 already has a home, and I checked rather than raising it a third time.**
  [T-0017](T-0017-automated-contract-conformance-tier.md) carries both the 403
  declaration-versus-enforcement check and the 403 media-type guard, folded in from T-0004's review.
  Nothing further is needed here.

- **What N5 does *not* cover, now recorded on T-0017 rather than lost** (trunk commit `d1684a8`):
  that ticket's AC1 validates *"a response from any endpoint exercised by the integration suite"*,
  so **a declared response no test ever produces is invisible to it**. Measured on this ticket's two
  operations: `getIssue`'s declared 403 has no test at all, and neither operation's declared 500 is
  exercised by the integration tier — only under mutation, and in the smoke tier by stopping the
  database. A conformance tier built exactly to AC1 would report green on a contract half of whose
  declared responses nothing has ever produced. That is coverage-shaped rather than
  validation-shaped, so it is a separate mechanism, and refinement should decide whether it belongs
  in T-0017 or its own ticket.

- **Provenance note on the rebase.** My two earlier entries name `b472465`, `296581c`, `b71d037` and
  `6e8b1da`; the rebase rewrote those to `6a3ffec`, `99f7ccc`, `57f4458` and `d2fbbc0`. The originals
  still resolve in this clone's reflog and are on no branch, so they will not survive a `gc` or reach
  a second clone. The rebase itself is right — [GIT.md](../../standards/GIT.md) asks for exactly it,
  and the squash-merge collapses all five into one trunk commit regardless, which is why the durable
  record is this Work Log's text and not its hashes. Recorded so nobody chases a hash that no longer
  exists.

---

- **Did:** Re-reviewed `c89f416` after the rebase; ran every gate in this worktree; read the B3
  correction against the test it describes; ran both of `AllocationRollbackTests`' mutants against
  the full integration suite and confirmed each kills only that test and reaches its assertion;
  confirmed N9's text reaches the generated client and the published contract; verified my two
  earlier entries survived the rebase intact; recorded the unexercised-response gap on T-0017.
- **Decided:** **Approve** — merge may proceed, then the `os:` status commit on the trunk. B1, B2,
  B3 and N1 are closed; N2 and N9 decided and propagated; N3, N8 and N10 correctly recorded. N4, N6
  and N7 remain open and are the implementer's to take or leave; N5's destination exists and has
  been widened.
- **Remaining:** merge, then acceptance. For QA: the two things this review could not close by
  measurement are N6's untested 400 path and the unexercised declared responses noted above —
  neither is a defect, both are places where a green suite is not evidence.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0005-issues` @ `c89f416`, rebased onto `main` @ `39cc88e`.
- **Test state, measured in this worktree:** `dotnet test` **102/102** exit 0 · `dotnet build
  --no-incremental` **0 warnings** exit 0 · `dotnet format` exit 0 (solution and SmokeTests) ·
  `check-drift.sh` exit 0 · `smoke.sh` 13/13, 5m43s exit 0 · `validate.py` exit 0 (21 tickets,
  8 ADRs). Tree clean after mutation; full suite re-run green on the restored tree.


### 2026-08-31 — QA / Test Engineer (claude-qa-8f52) — acceptance of `aea080c` on `main`: **FAIL**

Independent acceptance of the merged change, run in the primary checkout on `main`. I did not
implement this ticket and did not touch implementation code, tests, the specification or the
acceptance criteria. Requirements were read before the Work Log, so the checks below are derived
from the criteria rather than from the implementer's narrative.

**Verdict: FAIL — one blocking finding.** All nine acceptance criteria hold, verified against the
running software. The blocker is [DoD](../../governance/DEFINITION_OF_DONE.md) item 6: three
statements in the repository's own documentation became false when this merged, one of which names
this ticket as the thing that will bring what it has already brought. This is the same finding, at
two of the same three locations, that [T-0004](T-0004-create-and-list-projects.md)'s acceptance
made blocking (`9f89ddd`, Finding 2).

---

#### The gates, each exit code read from the tool itself and not from a pipeline

| Gate | Result |
| --- | --- |
| `dotnet test` | **102/102** — 17 unit, 85 integration, 0 skipped — exit **0** |
| `dotnet build --no-incremental` | **0 warnings, 0 errors** — exit **0** |
| `dotnet format --verify-no-changes` | exit **0** |
| `dotnet format apps/GotIssues.SmokeTests/GotIssues.SmokeTests.csproj --verify-no-changes` | exit **0** |
| `./tools/check-drift.sh` | `OK — generated code matches spec/openapi.yaml` — exit **0** (AC6) |
| `./tools/smoke.sh` | **13/13**, 6m16s — exit **0** |
| `python3 tools/validate-project-os/validate.py` | exit **0** |

Working tree clean before and after; `check-drift.sh` ran against a clean `libs/`, so its exit 0 is
a drift result and not the refuse-to-run guard.

**Attribution, per [TESTING.md](../../standards/TESTING.md).** Everything below was measured against
my own stack: `docker compose -p qa8f52` on ephemeral ports 18452/18453, its own volumes, started
after a `down -v` for stale state. Six unrelated Compose stacks were running on this machine
throughout, which is exactly the condition the rule exists for. Before trusting any response I
bound the port to my container: `qa8f52-api-1` `running healthy` → `/health` **200**; container
stopped → `curl` **exit 7**, connection refused; container restarted. Torn down with
`docker compose -p qa8f52 down -v --remove-orphans`, exit 0; no container, volume or network named
`qa8f52` survives, and the smoke tier left nothing either.

Tokens were genuine, obtained from the running identity host by client credentials — not synthetic.

---

#### The nine criteria

| AC | Verdict | Evidence (live stack unless stated) |
| --- | --- | --- |
| **AC1** | **Pass** | `POST /projects/GOTI/issues` → **201** `application/json`, body `{"key":"GOTI-1","projectKey":"GOTI","number":1,…}`; a multi-line `description` round-tripped intact. Persisted: the row is in `issues` joined to `projects` by `ProjectId` |
| **AC1b** | **Pass** | First issue in each of `GOTI`, `PROJ`, `RACE`, `LEGA`, `OLD2`, `RAWX`, `ABCDEFGHIJ` was **1**, including four projects that predate the migration (see F-closure 3) |
| **AC1c** | **Pass** | Three issues in `GOTI`, then the first in `PROJ` → **`PROJ-1`**, not `GOTI-4`. Counters read from the database: `GOTI|3`, `PROJ|2` |
| **AC1d** | **Pass** | **30 concurrent creates over real HTTP** (`xargs -P 30` against Kestrel, separate connections — a stronger arrangement than the suite's in-process 10): 30×201, numbers exactly `1..30`, 30 distinct, none skipped, none reused; database agrees (`count 30, distinct 30, min 1, max 30`), counter at 31. Separately, the guarantee is real in the schema: `IX_issues_ProjectId_Number` is `CREATE UNIQUE INDEX … ON public.issues USING btree ("ProjectId", "Number")`, read from `pg_indexes` on a migrated database |
| **AC2** | **Pass** | `GET /issues/GOTI-1` → **200** `application/json`, all fields matching what creation returned |
| **AC3** | **Pass** | `POST /projects/NOPE/issues` → **404** `application/problem+json`, `type: https://httpstatuses.io/404`. Nothing orphaned: `select count(*) from issues` unchanged, and no project row was created |
| **AC4** | **Pass** | `GET /issues/GOTI-99` → **404** `application/problem+json` |
| **AC5** | **Pass** | No token and a garbage token, on both operations → **401** `application/problem+json`; issue count unchanged afterwards |
| **AC6** | **Pass** | `./tools/check-drift.sh` exit **0** |

**AC1d's second half, verified by a real failure rather than a synthetic one.** With PostgreSQL
stopped underneath the live API, a create returned 500; after restart the project's counter was
still 2 and the next create got `GOTI-2`. The number was returned, not burned — which is the
property the explicit transaction exists for and the reason AC1d can ask for "no number skipped".

---

#### The three things review left for QA, closed by measurement

**1. The malformed-`issueKey` 400 path that `LastIndexOf('-')` depends on — closed.**
Fifteen shapes, all **400 `application/problem+json`** naming `issueKey`, none a 500:
`GOTI1`, `goti-1`, `GOTI-0`, `A-1`, `GOTI--1`, `GOTI-01`, `GOTI-1234567890`, `-`, `-1`, `GOTI-`,
`GOTI-%20`, `GOTI-1a`, `ABCDEFGHIJK-1`, `GOTI-1000000000`, and — the one I added because .NET's
`$` matches before a trailing newline and could have let one through — `GOTI-1%0A` and `GOTI%0A-1`,
plus `projectKey` as `GOTI%0A`. All 400. The trailing-newline hole is closed by
`RegularExpressionAttribute` requiring the match to span the whole value, not by the anchor, which
is worth knowing because the pattern alone does not carry that guarantee. `GOTI-999999999` (the
longest expressible number) reaches the handler and returns **404**, so the parse is exercised at
its bound. Still true that no *test* covers any of this (N6).

**2. Declared responses no test exercises — all produced, all correctly shaped.**
- `getIssue`'s **403**, which has no test anywhere: seeded a third identity carrying
  `role: superuser`, confirmed the claim in the token, and called `GET /issues/GOTI-1` →
  **403 `application/problem+json`**, 162-byte RFC 9457 body. Real, and its body matches `Problem`.
- **500 on both operations**: with PostgreSQL stopped under a live, already-authenticating API,
  both `POST /projects/GOTI/issues` and `GET /issues/GOTI-1` returned
  **500 `application/problem+json`** with a body, and nothing leaked — no `Password`, `Npgsql`,
  `Host=`, exception text or stack trace.

So every declared response on both new operations is now observed to be produced with its declared
media type. The gap is coverage, not correctness, and it already has a home on T-0017.

**3. Does anything else in this migration mistreat existing rows? No — and I checked rather than
reasoned.** I reverted a live stack to the previous schema (`DROP TABLE issues`, `ALTER TABLE
projects DROP COLUMN "NextIssueNumber"`, deleted the `AddIssues` row from
`__EFMigrationsHistory`), seeded it the way a real deployment already looks — five projects and a
`users` row — and ran **the real migrator** (`docker compose run --rm migrator`, exit 0,
`Applying migration '20260831200135_AddIssues'`). What it did:

| Check | Result |
| --- | --- |
| Existing project rows | All five got `NextIssueNumber = 1`. B1's fix confirmed by the method that found it |
| Column default persists in the database | `NextIssueNumber integer NOT NULL DEFAULT 1`. A **raw SQL** insert bypassing EF also got 1, so the guarantee is the database's, not the CLR initialiser's — recorded as enforcement rather than proved by a mutant, per the amended standard |
| Other `projects` columns | `Id`, `Key`, `Name`, `CreatedAt` identical in type, nullability and default before and after. Nothing else was touched |
| `users` rows | Untouched |
| New constraints | `PK_issues`, `IX_issues_ProjectId_Number` UNIQUE, `FK_issues_projects_ProjectId … ON DELETE RESTRICT` |
| Behaviour afterwards | Each pre-existing project's first issue was `<KEY>-1` and read back **200**. `GOTI-0` would have been 400 |

Nothing else in the migration mistreats existing rows.

---

#### F1 — Blocking. The repository documents issues as not existing, in the release that adds them (DoD item 6)

Three statements are false on `main` as of `aea080c`:

| Location | Text | Why it is false |
| --- | --- | --- |
| `README.md:7` | *"The first product resource — **projects** — is real and role-guarded (T-0004); **issues and comments come next.** See* Not here yet *for what does not exist."* | Issues do not "come next"; they are here, specified and role-guarded, and I created and read them |
| `README.md:113`, under **### Not here yet** | *"**Issues and comments.** Projects exist (T-0004); **T-0005** and T-0008 bring the rest."* | T-0005 is this ticket. The README lists this ticket's deliverable under a heading that says it does not exist |
| `project-os/architecture/ARCHITECTURE.md:5` | *"What remains intended rather than built: **issues** (T-0005) and everything that hangs off them."* | Same |

[DoD](../../governance/DEFINITION_OF_DONE.md) item 6 names *"README/setup instructions affected by
the change"*, and `README.md:113` is affected by name.
[DOCUMENTATION.md](../../standards/DOCUMENTATION.md) is more specific: *"A ticket that changes any
of those steps fixes the README in the same change."*

This is not a new judgement call. T-0004's acceptance made exactly this finding blocking at
`README.md:7`, `README.md:113` and `ARCHITECTURE.md:5` — the same three lines — and scored DoD item
6 **Fail** for it (`9f89ddd`). Applying it to that ticket and not this one would make the standard
depend on who is reading. The shape is the one that acceptor named: a reader arriving today is told
the product has no issues, by the same document that tells them how to run it.

**Not fixed here — acceptance does not edit the change under test.** The remedy is a few sentences.

#### F2 — Non-blocking, and a *specification* inconsistency rather than an implementation defect

`Issue.number` is declared `type: integer, minimum: 1` with **no maximum**. `Issue.key` is declared
`^[A-Z][A-Z0-9]{1,9}-[1-9][0-9]{0,8}$` — at most **nine** digits. Above 999,999,999 the two
declarations cannot both hold, and the API follows both faithfully:

```text
counter 999999999  → 201, key "BIGN-999999999"   → GET 200
counter 1000000000 → 201, key "BIGN-1000000000"  → GET 400  (violates the declared key pattern)
counter 2147483647 → 500 application/problem+json, counter unchanged at 2147483647
```

The middle row is B1's class exactly — a 201 whose body violates the contract that produced it, and
an issue that exists and cannot be read through the only declared read path. It survives in a second
instance nobody looked for, because B1 was found and fixed at the *bottom* of the range.

I am classifying this as **requirement ambiguity, not an implementation defect**, per the skill's
step 5: the code implements both declarations correctly and they disagree only at a bound the ticket
never stated. It is a Product Owner decision — declare `maximum: 999999999` on `number`, widen the
key pattern, or refuse allocation past the expressible range and say so. **I have not rewritten
anything.** Reaching it needs 10^9 issues in one project, so it is not why this fails; the top of the
range is also handled loudly (500 with a problem document, counter rolled back) rather than
silently, which is the right failure.

#### F3 — Non-blocking. A record on T-0017 is already stale against the merged tree

The table added by `d1684a8` says `500` on **either** operation is *"No — reached only under
mutation, and in the smoke tier by stopping the database."* On the merged tree
`AllocationRollbackTests.A_failed_insert_returns_the_number_instead_of_burning_it` asserts
`HttpStatusCode.InternalServerError` **and** `application/problem+json` for `createIssue`, in the
integration tier, with no mutation. The row was written before that test landed in the same change.
`getIssue`'s 500 and `getIssue`'s 403 remain genuinely unexercised, so the gap is real — it is one
row of three that is now wrong, and T-0017's refinement will read it.

---

#### Mutation, under the standard as amended today (`2006cf2`)

**I produced no mutants, deliberately, and this is the reasoning rather than an omission.**

- **The allocator (AC1d).** The implementer's mutant — `MAX(number)+1` killing only the concurrent
  test — is on record against code that has not changed shape since. *"Do not re-mutate an unchanged
  claim."* I had no reason to challenge it, and I obtained stronger evidence for the property
  itself: 30-way real-HTTP concurrency, and the counter surviving a genuine dependency failure.
- **The migration backfill.** The property is enforced by a **database column default**, which I
  read out of `information_schema` and confirmed applies to a raw insert that never touches EF.
  The standard says to *record the enforcement* instead, because it is stronger than a test.
- **The unique index and the transaction.** Both already have honest mutants from re-review, and
  I saw the index's effect directly: `IX_issues_ProjectId_Number` is `UNIQUE` in the live schema.

#### Definition of Done at this stage

| Item | Verdict |
| --- | --- |
| 1 Implementation complete | **Pass** — every In Scope item present. Scope fidelity clean: the spec adds exactly `POST /projects/{projectKey}/issues` and `GET /issues/{issueKey}`; no lifecycle fields (T-0006), no listing or filtering (T-0007), no comments (T-0008), no edit or delete |
| 2 All acceptance criteria verified | **Pass** — nine of nine, independently, above |
| 3 Automated tests exist and pass | **Pass** — 102/102, 0 skipped; every AC maps to a named test; smoke 13/13 |
| 4 No known unrecorded defects | **Pass with F2 recorded** — F2 needs a PO disposition before Done; F3 is a correction to another ticket's record |
| 5 Code quality | **Pass** — reviewed and approved by `claude-rev-5c14`; build warning-clean; both `format` runs exit 0; no TODO, FIXME, `Console.WriteLine` or debug scaffolding in the new files |
| 6 Documentation updated | **FAIL — F1** |
| 7 Work Log complete | **Pass** — a different agent could resume from repository state alone |
| 8 State updated | Handled by this entry and `complete-ticket` |
| — ADR recorded | **Pass** — ADR-0004 followed (constraints declared in the spec, not in the controller); ADR-0008's policy attributes present; ADR-0009 accepted separately and consistent with the controller keeping the DbContext |
| — Security | **Pass** — all new external input validated in the contract; no secrets added. Free text is never logged: I created an issue whose title and description carried marker strings and personal-data-shaped text, then grepped the API and migrator container logs — **0 occurrences of either**, satisfying the ticket's own Example and SECURITY.md. The 500 body leaks nothing |
| — Migrations | **Pass** — scripted, reversible (`Down` drops the table and the column), applied by the explicit migrator service, and now proved against a **populated** database rather than only an empty one |
| — Observability | **Pass** — unchanged by this ticket |
| — Deployment | **Pass** — smoke 13/13 through the real Compose stack |

**Does a deviation need recording? For F1, no — because none is available.** A DoD deviation is a
recorded PO or human decision to accept a gap; there is no gap worth accepting when the remedy is
three sentences of documentation inside this ticket's scope. **For F2, yes** — either a fix or a
PO-accepted deferral to a ticket whose scope actually takes it on must exist before item 4 can pass
at `complete-ticket`. There is no such destination today: T-0021 is about migrations against
populated databases and its Out of Scope disowns fixing specific migrations; T-0017 validates
responses the suite produces and would never produce this one.

---

- **Did:** Independent acceptance of `aea080c` on `main`. Ran all seven gates myself and read each
  exit code from the tool. Drove a private Compose stack (`-p qa8f52`, ephemeral ports) with real
  tokens: verified all nine criteria, 30-way HTTP concurrency, fifteen malformed-key shapes, sixteen
  body-boundary cases, `getIssue`'s untested 403, both operations' 500 with the database removed
  underneath, log leakage, and the real migrator against a database already holding rows.
- **Decided:** **FAIL** on F1 (DoD item 6). Status back to `in-progress`, owner restored to the
  implementer (see the state-model conflict below), sprint and backlog tables updated. F2 recorded as a specification ambiguity for the PO; F3 recorded as a correction
  another ticket's refinement should see.
- **Remaining:** fix the three documentation statements; obtain a PO disposition on F2; resubmit.
  The nine criteria need no rework — none of them failed.
- **Open questions / blockers:** F2 is a Product Owner decision (WoW §7): cap `number` in the
  contract, widen the key pattern, or refuse allocation beyond the expressible range.

**State-model conflict, recorded per WoW §3.** `acceptance-test` says a failed acceptance sets
`owner: none`; `validate.py` rejects `status: in-progress` without an owner. The validator is the
tool the trunk gate runs, and the repository has already resolved this once — `a3f27d1` restored the
owner for the same reason, and `9f89ddd` (T-0004's acceptance FAIL) followed it. I have done the
same: `owner: claude-sm-9d4e`, the implementer, so the ticket is picked back up by the session that
built it rather than re-claimed. Noting it rather than silently choosing, and noting that the skill
text and the validator still disagree — a candidate for `evolve-governance`, not for this ticket.

- **Branch / PR:** n/a — accepted against `main` @ `aea080c` in the primary checkout.
- **Test state, measured by me on `main`:** `dotnet test` **102/102** exit 0 · `dotnet build
  --no-incremental` **0 warnings** exit 0 · `dotnet format` exit 0 (solution and SmokeTests) ·
  `check-drift.sh` exit 0 · `smoke.sh` **13/13** exit 0 · `validate.py` exit 0. Nothing left running
  or stored: `docker compose -p qa8f52 down -v` exit 0, no `qa8f52` container, volume or network.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — acceptance FAIL addressed; F2 closed rather than deferred

`claude-qa-8f52` passed all nine criteria and failed the ticket on a Definition of Done item.
Both of its substantive findings are fixed here.

#### F1 — the same three lines, two tickets running

`README.md:7`, `README.md:113` and `ARCHITECTURE.md:5` said issues do not exist, and line 113
listed **this ticket's deliverable** under *Not here yet*. [T-0004](T-0004-create-and-list-projects.md)'s
acceptance made the identical finding at the identical lines (`9f89ddd`), and I fixed it there —
by rewriting those sentences to describe projects, which made them false again the moment issues
shipped.

That is the pattern rather than the incident: **these lines say what exists, so every ticket that
builds something falsifies them, and the fix that makes them true today guarantees the next
ticket makes them false.** T-0004's version of the fix was correct and still bought nothing beyond
one ticket. Worth the retro; the durable answer is probably that the banner names *what is not yet
built* by ticket rather than enumerating what is.

#### F2 — the GOTI-0 defect arriving from the other end of the range

`Issue.number` declared no maximum while `Issue.key` allows nine digits. Above 999 999 999 the API
returned **201 with a key its own published pattern rejects**, and `GET /issues/{key}` then refused
that key with 400 — an issue that exists and cannot be read. Structurally identical to the
backfill defect review caught, approached from the top instead of the bottom.

Fixed rather than deferred, because it is this ticket's own resource and its own identity scheme,
and because the acceptor established that **no existing ticket's scope accepts it** — deferring
would have meant inventing a destination, which DoD item 4 exists to prevent.

- `Issue.number` now declares `maximum: 999999999`, so the two fields cannot disagree, and the
  contract says *why* that number and not another.
- `createIssue` declares `409`, and the API refuses a project that has exhausted its numbers.
- The refusal happens **inside the allocating transaction**, so the number is returned rather than
  burned. The test asserts the counter reads 1 000 000 000 afterwards rather than 1 000 000 001 —
  which is the assertion that proves the rollback rather than assuming it.

Tested by seeding the counter to 999 999 999 — the technique `claude-rev-5c14` supplied for the
rollback test, reused because a billion issues is not a fixture. The boundary value itself is
asserted to work end to end (`FULL-999999999` creates **and reads back**), not merely to be
accepted; a bound that rejects one past the limit while breaking the limit itself would be a worse
defect than the one being fixed.

#### F3 — a claim of mine that was already false

The note I added to [T-0017](T-0017-automated-contract-conformance-tier.md) (`d1684a8`) said
neither issue operation's 500 is exercised in the integration tier. `AllocationRollbackTests`
exercises `createIssue`'s. Corrected on the trunk — a wrong claim in a ticket written *about*
unexercised responses is the kind that gets believed.

- **Did:** Corrected three stale documentation claims; bounded the issue number in the contract and
  enforced it inside the transaction; added a boundary test proving both the limit and one past it;
  corrected the T-0017 note.
- **Decided:** close F2 here rather than defer it — no destination existed, and inventing one is
  the failure DoD item 4 names.
- **Remaining:** review, then re-acceptance.
- **Open questions / blockers:** none.
- **Test state:** `dotnet test` **103/103** (17 unit, 86 integration) · build 0 warnings ·
  `dotnet format` exit 0 both · `validate.py` exit 0 · drift and smoke below.


### 2026-08-31 — Software Engineer + Architect (claude-rev-5c14) — review of `t-0005-acceptance-fixes` @ `c44456e`

Review of the acceptance-failure fixes, branched from `main` @ `33f022a`. Reviewer is not the
implementer (`claude-sm-9d4e`) and not the acceptor (`claude-qa-8f52`).

**Verdict: Approve.** F1 is complete — I checked for the ones the acceptor did *not* name as well
as the three it did. F2 is correct, and I ran four mutants against it, one of which changed my
answer to a question I was asked. F3 corrects a claim of mine that was wrong when I wrote it.

#### Gates, all run in this worktree (`/Users/yoss/work/got-issues--t-0005b`), exit codes read from each tool

| Gate | Exit | Result |
| --- | --- | --- |
| `dotnet test` | 0 | **103 passed** — 17 unit, 86 integration |
| `dotnet build --no-incremental` | 0 | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | 0 | solution |
| `dotnet format --verify-no-changes` (SmokeTests csproj) | 0 | the project outside the solution |
| `./tools/check-drift.sh` | 0 | `libs/` clean beforehand; re-run after my spec mutant and its regeneration, still 0 |
| `./tools/smoke.sh` | 0 | 13/13, 6m47s |
| `python3 tools/validate-project-os/validate.py` | 0 | 22 tickets, 10 ADRs |

Four mutants run and restored; tree clean, drift 0 after restoring the regenerated output.

**One deliberate omission, stated rather than skipped.** I did not stand up a Compose stack this
time. In the earlier passes it earned its place because B1 was invisible to every test; F2 is not —
mutant J reproduces it in the integration tier in seconds. The only thing Compose adds here is the
real JWT pipeline, which this guard does not touch. Saying so because "I ran the gates I thought
were worth running" is only honest if the reasoning is visible.

---

## F2 — correct, and the mutants say more than the entry claims

Four mutants, all accepted by the build (mutant J leaves `MaximumIssueNumber` unreferenced and the
build stays warning-clean, so it is a test kill and not a compiler kill):

| # | Mutant | Result | What it proves |
| --- | --- | --- | --- |
| J | Guard removed entirely | **85 of 86 pass**; only the exhaustion test fails: `Expected: Conflict / Actual: Created` | The acceptance defect reproduces exactly, and exactly one test catches it. The assertion is reached — it sits after the boundary create *and* its read-back, so the whole path ran |
| K | `>` becomes `>=` — an off-by-one rejecting the last usable number | **85 of 86 pass**; `Expected: Created / Actual: Conflict` | The boundary half is genuinely asserted. "A bound that rejects one past the limit while breaking the limit itself would be worse" is not just an intention — a test enforces it |
| L | Explicit transaction removed | **84 of 86 pass**; the exhaustion test gives `Expected: 1000000000 / Actual: 1000000001` | The rollback is asserted at the exhaustion boundary independently of the earlier rollback test, which also fails. Two tests now guard the transaction where none did two passes ago |
| M | **Spec** key pattern narrowed to eight digits and `Issue.number` maximum with it, regenerated, controller constant left at nine | **85 of 86 pass**; `Expected: OK / Actual: BadRequest` | See Q2 — this is the one that changed my answer |

A design property worth recording because it is not obvious and nothing states it: **the counter
cannot overflow `int`.** Once it reaches 1 000 000 000 every further create is refused and rolled
back, so the column pins there permanently rather than climbing toward `int.MaxValue` and raising
SQLSTATE 22003 as an undeclared 500. That falls out of the refusal being inside the transaction,
which the test asserts — so the property is guarded, just not named.

Placement is right too: the guard sits before the project re-read, so the refused path costs one
statement rather than two, and it returns while the transaction is open, which is what makes the
rollback happen. The 409's `detail` carries only the project key from the path — no user text,
nothing from an exception (SECURITY.md).

## Q1 — is 409 right for an exhausted project? **Yes.**

RFC 9110 §15.5.10 defines 409 as a conflict "with the current state of the target resource." The
target resource is the project's issue collection, and its state — every expressible number
consumed — is precisely what makes the request impossible. That is a closer fit than 409's other
use in this API, and the reasoning generalises rather than being chosen to suit.

The alternatives are worse, and for reasons worth writing down so this is not re-litigated:

| Candidate | Why not |
| --- | --- |
| **507** | Registered for *storage* (RFC 4918). The database has plenty of room; it is the identifier space that is finished. It is also 5xx, which says the server erred, when the server is behaving exactly as designed |
| **500** | Nothing unexpected happened, and the handler deliberately strips detail — the one response that cannot explain itself, for a condition the API can explain precisely |
| **400 / 422** | The request is well-formed and semantically valid. Nothing the caller changes about it helps |
| **503** | Implies retry later. This never resolves |

**One imperfection worth naming rather than hiding:** 409 conventionally implies the client can
resolve the conflict — refetch, pick another key, retry. Here nothing the caller does ever
succeeds; the project is permanently full. So the API now returns 409 from two operations with
opposite actionability: `createProject`'s means "choose a different key", `createIssue`'s means
"this project is finished forever". Both are problem documents and the `title` distinguishes them,
and the shared `Conflict` component's description was widened to say so — which is the honest
handling. There is no better-fitting registered code, and inventing one would be worse. Right call.

## Q2 — the duplicated constant: **you are not deferring something you should close**

I was ready to disagree with you here, and measurement changed my mind. Your premise is that
nothing checks the controller's `999_999_999` against the spec's nine-digit pattern. Mutant M tests
that premise directly: narrow the pattern in `spec/openapi.yaml` to eight digits, regenerate, leave
the constant alone — the drift direction that **reintroduces the defect you just fixed**, because
the API would then issue keys its own contract rejects.

The suite goes red. `Expected: OK / Actual: BadRequest`, on the read-back of `FULL-999999999`.

Three of the four drift directions are already caught:

| Drift | Caught? | By |
| --- | --- | --- |
| Pattern narrowed, constant unchanged — **the dangerous one** | **Yes** | The boundary read-back (mutant M) |
| Constant narrowed, pattern unchanged | **Yes** | The boundary create (mutant K) |
| Constant widened past the pattern | **Yes** | The one-past refusal (mutant J) |
| Pattern widened, constant unchanged | No | — and this is the safe direction: the API refuses numbers it could express, which is under-use, not corruption |

So the coupling *is* behaviourally asserted, by the assertion you added for a different reason —
that the boundary must work and not merely be accepted. That assertion is doing two jobs, and only
one of them is written down.

**That is the one thing I would change, and it is a comment.** Say in the test that the read-back
is load-bearing for the constant-versus-pattern agreement, not only for the boundary. Otherwise
T-0022's layering refactor is exactly the kind of change that deletes an assertion whose second
purpose nobody recorded — and it would delete it while moving the constant into the domain, which
is precisely when the coupling matters most. Non-blocking; a sentence.

And worth being clear about T-0022 itself: **it would not close this even if it landed.** Moving
the constant into the domain leaves a domain constant and a spec pattern — still two places. The
seam is between code and contract, and no layering removes it. Only a check or a derivation does,
and a check already exists. So the deferral is right, but not for the reason given: this is not
waiting on T-0022, it is already handled, and T-0022 should be told not to lose it.

## F1 — complete, and the durable answer is not the one proposed

The three lines are corrected and the corrections are accurate. I checked for the ones the acceptor
did not name: `grep` across `README.md`, `ARCHITECTURE.md` and `PROJECT.md` turns up no other claim
that issues do not exist, and no other place naming T-0005 as unbuilt. The two remaining mentions
of deleting issues are `PROJECT.md`'s and `ARCHITECTURE.md`'s authorisation rows, which describe an
intended *role* boundary rather than a shipped feature and are not falsified by this ticket.

**On where the durable fix belongs — you asked, so here is the disagreement.** Your proposal is
that the banner should name *what is not yet built* by ticket rather than enumerating what is. That
does not survive: when T-0006 ships, "not yet built: T-0006" is false in exactly the same way.
Whichever side is enumerated, a human has to remember, and the evidence is that humans do not.

The stronger evidence is that **the countermeasure has already been tried and has already failed.**
`ARCHITECTURE.md` line 7 says *"Updating this banner is part of any ticket that changes the state
above. It has repeatedly been left stale by the very ticket that falsified it."* That was written
because of the first occurrence. The second happened anyway. Anything in the same family — a
firmer reminder, a better-worded rule — is the same idea louder.

So: **the correction belongs here** (it is a DoD item and it blocks acceptance, correctly), and
**the durable fix belongs in the retro** — it is cross-cutting, it touches documents outside this
ticket's scope, and a recurring process failure with two data points is exactly what WoW §15 says
retros exist to convert into owned actions.

But the retro is at sprint end and **T-0006 lands before it**, so leaving it there guarantees a
third occurrence. WoW §15 also lets any agent record an improvement proposal at any time, so I have
recorded it in [`CURRENT_SPRINT.md`](../../delivery/CURRENT_SPRINT.md) Notes (trunk `eb1432a`) with
the evidence, the reason the proposed fix does not work, and three candidates for the retro to
choose between: delete the enumerations and point at `BACKLOG.md`, which is already authoritative
and already updated by `complete-ticket`; generate them from ticket frontmatter; or make
`validate.py` fail when a `done` ticket is named under a *Not here yet* heading. Not a
recommendation between them — that is the retro's call. The point of recording it now is that
T-0006's author should not rediscover this by failing acceptance.

## F3 — my error, and the correction is right

The T-0017 note was mine (`d1684a8`) and it was wrong when I wrote it. `AllocationRollbackTests`
exercises `createIssue`'s 500 — with a media-type assertion — and I had verified that myself, in
the same session, two paragraphs before writing that it was unexercised. A wrong claim inside the
ticket written *about* unexercised responses is the worst place to put one. Struck rather than
edited away on the trunk (`e1175ca`), which is the right handling, and `getIssue`'s 500 remains
genuinely unexercised, so the finding survives in the half that was true.

---

## Non-blocking

- **NF1 — the read-back assertion's second job is undocumented.** Q2 above. One sentence in
  `AllocationRollbackTests`, and a line in T-0022 saying not to lose it when the constant moves.

- **NF2 — the 409 now covers two conditions with opposite actionability.** Q1 above. The contract
  is honest about it; recorded so that whoever adds a third 409 asks whether the shared `Conflict`
  component is still telling the truth.

- **N4, N6, N7 from the original review still stand** and are still not worth blocking: the extra
  project read after the `UPDATE`; no test for the malformed-`issueKey` 400 that `LastIndexOf`
  depends on; no unit tests. **N6 has grown slightly more interesting** — the `issueKey` pattern is
  now load-bearing for the exhaustion guard as well as for the parse, so it is doing three jobs
  with a test for one of them.

---

- **Did:** Reviewed `c44456e` against `main` @ `33f022a`; ran every gate in this worktree; verified
  F1's completeness by searching for stale claims the acceptor did not name; ran four mutants
  against F2 — guard removed, off-by-one, transaction removed, and a spec-side pattern narrowing
  with regeneration — and confirmed each reaches its assertion; verified the 409 reaches the
  generated contract; recorded the F1 pattern as retro input on the trunk.
- **Decided:** **Approve** — merge may proceed, then re-acceptance. F1 is complete, F2 is correct
  and better guarded than its own entry claims, F3 corrects my error. Q1: 409 is right, with one
  imperfection named. Q2: the deferral is right, but the premise is not — measurement shows the
  coupling is already asserted, and T-0022 would not have closed it anyway.
- **Remaining:** merge, then re-acceptance by a session other than mine and other than
  `claude-qa-8f52`'s implementer counterpart.
- **Open questions / blockers:** none. NF1 is a sentence; the F1 durable fix is recorded for the
  retro rather than left in chat.
- **Branch / PR:** `t-0005-acceptance-fixes` @ `c44456e`.
- **Test state, measured in this worktree:** `dotnet test` **103/103** exit 0 · `dotnet build
  --no-incremental` **0 warnings** exit 0 · `dotnet format` exit 0 (solution and SmokeTests) ·
  `check-drift.sh` exit 0 · `smoke.sh` **13/13** exit 0 · `validate.py` exit 0 (22 tickets,
  10 ADRs). Tree clean after four mutants and a regeneration; full suite re-run green.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — approved; two notes taken, and a harness limit found by taking them

`claude-rev-5c14` approved and answered both questions I could not settle alone.

**Q1 (is 409 right):** yes — RFC 9110 §15.5.10 is "conflict with the current state of the target
resource", which is exactly a project whose issue numbers are spent. 507 is about storage, and
there is room; the *identifier space* is finished. Its caveat is recorded rather than waved off:
this API now returns 409 from two operations with **opposite actionability** — `createProject`'s
means "pick another key", `createIssue`'s means "this project is finished, permanently". The
widened `Conflict` description is the honest handling, and whoever adds a third 409 should recheck
it.

**Q2 (am I deferring the duplicated nine-digit limit to T-0022):** no — and the reviewer changed
its own mind by measuring rather than reasoning. It narrowed the spec's key pattern to eight
digits, left the constant alone, and **the suite went red on the boundary read-back**
(`Expected: OK, Actual: BadRequest`). Three of the four drift directions are already caught; the
fourth is under-use rather than corruption. The coupling is behaviourally asserted — by an
assertion added for a different reason entirely.

**NF1 taken, and it is the important one.** That read-back now carries a comment saying it is
load-bearing for constant-versus-pattern agreement, and telling [T-0022](T-0022-adopt-clean-architecture-layering.md)
not to lose it. The reviewer's reasoning is worth keeping verbatim: *a layering refactor is exactly
the change that deletes an assertion whose second purpose nobody wrote down, at the moment the
constant moves.* It also observed that T-0022 would not have closed this anyway — a domain constant
and a spec pattern are still two places, and only a check or a derivation removes the seam.

**N6 taken**, having grown: the `issueKey` pattern is now load-bearing for the parse, the read path
**and** the exhaustion guard, with a test for one of them. Seven malformed shapes are now asserted,
including `GOTI-0` — the key the backfill defect would have produced, which must be refused by the
contract rather than merely absent.

#### Taking N6 found a limit in the test harness

Adding a seven-case theory turned **three unrelated tests red at once**, including one in a class I
had not touched: `Npgsql.PostgresException 53300: sorry, too many clients already`. The suite
creates a database per test — over a hundred now — and PostgreSQL's default ceiling is 100 clients.
In isolation every failing test passed.

Bounded from both ends: `max_connections=500` on the container, and `MaxPoolSize=10` per test
database so no single factory can hoard the ceiling. Recorded because of the shape rather than the
fix — **it fails as an unrelated-looking defect in whichever test happens to run at the limit, and
it moves as tests are added.** Anyone who meets it while adding tests will suspect their own change
first, exactly as I did.

- **Did:** Took NF1 and N6; found and fixed a connection-limit ceiling in T-0003's harness that
  adding tests had reached.
- **Decided:** raise the ceiling *and* cap the pools — either alone leaves the failure reachable by
  adding tests.
- **Remaining:** re-acceptance.
- **Open questions / blockers:** none.
- **Test state:** `dotnet test` **110/110** (17 unit, 93 integration) · build 0 warnings ·
  `dotnet format` exit 0 · `validate.py` exit 0 · drift and smoke below.


### 2026-08-31 — QA / Test Engineer (claude-qa-8f52) — re-acceptance of `5649367` on `main`: **PASS**

Second acceptance, same acceptor, fresh stack and fresh ports. **Verdict: PASS.** F1 is fixed, F2 is
closed rather than deferred, F3 is corrected on the trunk, and all nine criteria hold. One new
finding, non-blocking, with a destination created: **F4**, below — the harness ceiling the
coordinator asked me to measure is a **leak**, and the diagnosis recorded beside the fix is wrong.

#### The gates, each exit code read from the tool itself

| Gate | Result |
| --- | --- |
| `dotnet test` | **110/110** — 17 unit, 93 integration, 0 skipped — exit **0** |
| `dotnet build --no-incremental` | **0 warnings, 0 errors** — exit **0** |
| `dotnet format --verify-no-changes` | exit **0** |
| `dotnet format …/GotIssues.SmokeTests.csproj --verify-no-changes` | exit **0** |
| `./tools/check-drift.sh` | `OK` — exit **0** (AC6) |
| `./tools/smoke.sh` | **13/13**, 7m17s — exit **0** |
| `python3 tools/validate-project-os/validate.py` | exit **0** |

Live work on `docker compose -p qa8f52r2`, ports 18462/18463, after a `down -v`. Attribution bound
both ways before anything was trusted: `qa8f52r2-api-1` `running healthy` → `/health` **200**;
stopped → `curl` **exit 7**. Torn down with `down -v` exit 0; no `qa8f52r2` container, volume or
network survives.

#### The nine criteria, re-verified against the running software

All nine **Pass**. AC1 `GOTI-1` with a multi-line description round-tripping; AC1b first issue **1**
in every project; AC1c `PROJ-1` after three issues in `GOTI`; **AC1d** again at **30-way real-HTTP
concurrency** — `1..30`, 30 distinct, contiguous, none skipped; AC2 read-back identical; AC3 **404**
`problem+json` with nothing orphaned; AC4 **404**; AC5 **401** on both operations with nothing
written; AC6 drift exit 0.

Regression sweep on everything I probed the first time, all unchanged: nine malformed key shapes all
**400 `problem+json`** (including `GOTI-1%0A`), `getIssue`'s 403 **403 `problem+json`** with a real
body, both operations **500 `problem+json`** with PostgreSQL stopped underneath and no leakage of
`Password`, `Npgsql`, `Host=` or exception text, and free text still absent from the logs — a title
and description carrying personal-data-shaped markers produced **0** log hits.

**The migration and data model are byte-identical to what I accepted against a populated database
last round** (`git diff 303fafb..5649367 -- apps/GotIssues.Api/Data/` is empty), so that verdict
carries rather than being re-run.

#### F1 — fixed, and fixed accurately

All three lines now describe what exists. `README.md:7` reads *"**Projects and issues** are real and
role-guarded"* and names the key format; `README.md:113` narrows *Not here yet* to lifecycle,
listing and comments, which is true; `ARCHITECTURE.md:5` names two built resources and three
intended ones. I grepped for any surviving claim that issues do not exist and found none. The
pattern itself was escalated to the retro with three candidate durable fixes and an honest note that
the existing countermeasure (`ARCHITECTURE.md:7`) had already failed once — the right handling.

#### F2 — closed, and the closure is better than the deferral would have been

Verified on the live stack, seeding the counter because a billion issues is not a fixture:

```text
counter 999999999 → 201  key "BIGN-999999999"  → GET 200
one past          → 409  application/problem+json,  counter stays 1000000000
repeated ×3       → 409 each time,               counter stays 1000000000, issue count still 1
```

Three things worth stating. The **bound itself works**, not just the refusal — the last expressible
number creates *and reads back*. The counter is **returned, not burned**: it holds at 1 000 000 000
across repeated attempts rather than climbing, which proves the refusal is inside the allocating
transaction. And that pinning makes the **`int` overflow I found last round unreachable through the
API** — the counter can no longer climb toward 2 147 483 647, so the 500 I recorded is now gone by
construction rather than by luck.

`AllocationRollbackTests.A_project_that_has_exhausted_its_numbers_is_refused_rather_than_given_an_unusable_key`
encodes exactly this and cannot pass vacuously. I also corroborated the reviewer's claim that its
read-back assertion is load-bearing for constant-versus-pattern agreement, without re-mutating it:
live, a 9-digit key reads **200** and a 10-digit key is **400**, so the pattern's boundary is
demonstrably nine — which is the fact that assertion depends on.

#### F3 — corrected, and struck rather than edited away

`e1175ca` splits the row and strikes the wrong half in place, with a dated note saying who found it
and why the strike is more useful than a tidy table. That is the right disposition.

---

#### F4 — Non-blocking, but the recorded cause is wrong: this is a leak, not pool contention

The coordinator asked whether the `53300: sorry, too many clients already` failure was a latent
ceiling that seven new tests crossed, or a connection leak. **It is a leak, and the ceiling was
latent because of it.** Measured, not reasoned: 100 samples of `pg_stat_activity` taken against the
Testcontainers instance while the integration suite ran.

| Elapsed | Connections | Idle | Distinct databases | Databases created |
| --- | --- | --- | --- | --- |
| 0 s | 3 | 2 | 2 | 1 |
| 5 s | 32 | 31 | 30 | 30 |
| 10 s | 60 | 58 | 57 | 60 |
| 16 s (end) | **104** | **103** | **92** | 95 |

- The count **never decreased once** — 0 decreases across 100 samples.
- **1.09 connections per database**, 103 of the final 104 `idle`. Every database ever created still
  holds a connection at the end, including those whose class finished ten seconds earlier.

**The mechanism recorded in the fix cannot be the real one.** The comment added to
`PostgresContainerFixture.CreateDatabaseAsync` says the pools multiply out *"while xUnit runs classes
in parallel"*. All nine integration classes carry `[Collection(PostgresFixtureDefinition.Name)]`, and
xUnit runs one collection's classes **sequentially** — at most one class is live at a time, so
parallel pool growth cannot occur. The data agrees: pools never approach their cap (1.09 against a
`MaxPoolSize` of 10), because the driver is *how many databases have been created*, not how deep any
one pool goes.

Three consequences:

1. **`MaxPoolSize = 10` binds nothing.** Peak real usage is ~1 per database.
2. **`max_connections=500` postpones rather than fixes.** Growth is linear with no reclamation, so
   the ceiling returns at roughly **455 tests**. The same arithmetic reproduces the original failure
   exactly: at the default 100 the limit lands at ~89 tests; the suite had 86 and T-0005 added 7.
3. **The next person to hit `53300` will hunt a parallelism problem that does not exist.**

**Disposition.** The leak itself is in `PostgresContainerFixture` / `ApiFactory` teardown, introduced
by [T-0003](T-0003-automated-test-harness.md), which is `done` — so per [WoW](../../governance/WAY_OF_WORKING.md)
§11 it is a new bug ticket, not a reopening, and per the `acceptance-test` skill it is a defect in
*adjacent existing behaviour* rather than in this change. I created
**[T-0023](T-0023-integration-tests-retain-a-connection-per-test-database.md)** with the full sample
data and the arithmetic; its In Scope takes on releasing the connections, re-deciding both
mitigations, and correcting the comment (AC4), and its **AC2 requires the suite to pass at
`max_connections=100`** so that raising a ceiling again cannot satisfy it. **DoD item 4 needs the PO
persona to accept that deferral** at `complete-ticket`; the destination now exists and its scope
genuinely covers the item.

**The one part that belongs to this ticket** is the two-line comment T-0005 introduced, which states
a false mechanism. Non-blocking, on the precedent T-0004's acceptance set for `Program.cs:164` — the
cleanest resolution is to correct it before `complete-ticket`, and T-0023 AC4 catches it otherwise.
The mitigation itself is sound and I am not asking for it to be reverted: it buys roughly four times
the current suite size, and the commit subject honestly calls it *"lift a harness ceiling"* rather
than a fix.

#### Mutation, under the amended standard

**No mutants again, deliberately.** The allocator's is on record against code whose shape has not
changed. The migration backfill is enforced by a database default, which I read out of
`information_schema` last round. The exhaustion guard's read-back assertion has a reviewer's mutant
on record, and I corroborated the property it rests on directly (9 digits → 200, 10 → 400) rather
than re-running theirs. F4 was found by measurement, which is the thing the amendment redirected
effort toward, and no mutant would have found it — every test in the suite passes while the leak is
happening.

#### Definition of Done

| Item | Verdict |
| --- | --- |
| 1 Implementation complete | **Pass** — the spec declares exactly `/projects`, `/projects/{projectKey}/issues`, `/issues/{issueKey}`. No lifecycle fields, listing, comments, edit or delete. The `ProjectsApi` regeneration is a consequence of widening the shared `Conflict` component, not scope creep |
| 2 All acceptance criteria verified | **Pass** — nine of nine, independently |
| 3 Automated tests exist and pass | **Pass** — 110/110, 0 skipped; N6's malformed-key theory now exists and the exhaustion boundary is covered at both ends |
| 4 No known unrecorded defects | **Pass, conditional** — F4 recorded, destination T-0023 created; needs PO acceptance of the deferral at `complete-ticket` |
| 5 Code quality | **Pass** — reviewed by `claude-rev-5c14`; build warning-clean; both `format` runs exit 0; no TODO, FIXME or debug scaffolding in the changed files |
| 6 Documentation updated | **Pass** — F1 closed at all three locations, verified by grep and by reading |
| 7 Work Log complete | **Pass** |
| 8 State updated | Handled here and by `complete-ticket` |
| — ADR recorded | **Pass** — ADR-0004 honoured: the bound and the 409 are declared in the spec and regenerated, not enforced only in a guard clause |
| — Security | **Pass** — new input validated in the contract; 0 log occurrences of marker text in title and description; the 500 body leaks nothing |
| — Migrations | **Pass** — unchanged since my first acceptance, where it was proved against a populated database |
| — Deployment | **Pass** — smoke 13/13 |

**Deviations requiring a record:** one — the F4 deferral to T-0023, which is a PO decision at
`complete-ticket`, not mine. Nothing else needs a deviation, because nothing else is unmet.

---

- **Did:** Re-accepted `5649367` on `main`. Ran all seven gates and read each exit code from the tool.
  Verified F1 at all three locations, F2 at and past the bound with the counter checked after each
  attempt, F3 on the trunk. Re-verified the nine criteria and my whole first-round probe set against
  a fresh stack. Instrumented a second suite run with 100 samples of `pg_stat_activity` to answer the
  question the coordinator could not.
- **Decided:** **PASS.** F4 recorded as a defect in adjacent pre-existing behaviour, with
  [T-0023](T-0023-integration-tests-retain-a-connection-per-test-database.md) created as its
  destination and registered in the backlog.
- **Remaining:** `complete-ticket` — which needs the PO persona to accept the F4 deferral, and should
  correct the two-line mechanism claim in `PostgresContainerFixture` on the way past.
- **Open questions / blockers:** none blocking.
- **Branch / PR:** n/a — accepted against `main` @ `5649367` in the primary checkout.
- **Test state, measured by me on `main`:** `dotnet test` **110/110** exit 0 · `dotnet build
  --no-incremental` **0 warnings** exit 0 · `dotnet format` exit 0 (solution and SmokeTests) ·
  `check-drift.sh` exit 0 · `smoke.sh` **13/13** exit 0 · `validate.py` exit 0 (23 tickets, 10 ADRs).
  Nothing left running or stored.


### 2026-08-31 — Software Engineer (claude-sm-9d4e) — my diagnosis was wrong; the fix was right for the wrong reason

Acceptance passed the ticket and answered the question I flagged as beyond me. It is **a leak**,
and the ceiling was latent *because* of the leak — not the dichotomy I offered.

`claude-qa-8f52` sampled `pg_stat_activity` 100 times during a run. **The count never decreased
once**: 3 connections at the start, 60 at ten seconds, 104 at the end — 103 of them idle, across
92 databases, including classes that had finished ten seconds earlier. Every database ever created
still holds a connection when the suite ends.

**Two things I committed are false, and both are now corrected in place:**

1. *"xUnit runs classes in parallel."* It does not. All nine integration classes share
   `[Collection(PostgresFixtureDefinition.Name)]`, and a collection is xUnit's unit of
   parallelisation — so they run **sequentially** and the multiplication I described cannot happen.
2. *`MaxPoolSize=10` bounds the total.* It binds nothing: actual usage is **1.09 connections per
   database**, because the growth is one leaked connection each, not pools filling up.

The fix works and the reasoning under it was invented. That is worse than it sounds: a plausible
wrong mechanism sitting beside a working fix is where the next person debugging this would start,
and it would send them to look for parallelism that does not exist. Corrected rather than deleted,
with the measurement that replaces it.

**And the fix postpones rather than solves.** At 1.09 connections per database the same failure
returns at roughly 455 tests. The acceptor's arithmetic also reproduces mine exactly: at the
default ceiling of 100 the limit lands at ~89 tests — I had 86 and added 7.

**No mutant would have found this.** The suite is green throughout the leak; it fails only when a
threshold is crossed. Both of this acceptor's substantive findings across two runs came from
exploration, which is the second data point for the standard the maintainer narrowed today.

#### The deferral, accepted as PO persona

**F4 — the connection leak — is deferred to [T-0023](T-0023-integration-tests-retain-a-connection-per-test-database.md), and I accept that deferral.**
Per [DoD](../../governance/DEFINITION_OF_DONE.md) item 4 I read the destination rather than
trusting the pointer: T-0023 exists, is registered in `BACKLOG.md`, carries the sampled evidence,
and its **AC2 requires the suite to pass at `max_connections=100`** — which is what makes the
deferral real rather than nominal, because raising a ceiling again cannot satisfy it. Its AC4
covers the comment, which I have instead corrected here since it is this ticket's text.

The leak is in [T-0003](T-0003-automated-test-harness.md)'s harness, which is `done`, so a new
ticket is the correct route (WoW §11) rather than reopening it.

- **Did:** Corrected two false claims I committed about the mechanism; accepted the F4 deferral
  after reading T-0023's scope.
- **Decided:** correct the comment rather than delete it — the wrong mechanism is what someone
  would act on.
- **Remaining:** `complete-ticket`.
- **Open questions / blockers:** none.
- **Test state:** `dotnet test` **110/110** · `dotnet format` exit 0 · build 0 warnings.

### 2026-08-31 — QA / Scrum Master (claude-qa-8f52) — Definition of Done walk, ticket closed

State assessed: `main` @ `73a1833`.

**Universal items**

1. **Implementation complete** — the issue resource, its migration, the per-project allocator and
   both operations. Review walked the diff against In Scope across three rounds; no scope drift.
2. **Acceptance criteria verified independently** — all nine, twice, against a live Compose stack
   with attribution bound both ways. AC1d was verified at **30-way real-HTTP concurrency**: numbers
   exactly 1–30, none skipped or reused.
3. **Automated tests** — 110 passing (17 unit, 93 integration), smoke 13/13, nothing skipped.
4. **No known unrecorded defects** — F1, F2 and F3 fixed; **F4 (the connection leak) deferred to
   [T-0023](T-0023-integration-tests-retain-a-connection-per-test-database.md)**, whose scope
   accepts it and whose AC2 requires the suite to pass at `max_connections=100` so that raising a
   ceiling cannot satisfy it. Deferral accepted by the PO persona, with the destination read rather
   than trusted.
5. **Code quality** — approved by `claude-rev-5c14` across three rounds; build 0 warnings under
   warnings-as-errors; `dotnet format` exit 0 for the solution and the out-of-solution smoke project.
6. **Documentation** — the item this ticket first failed on. README's banner and *Not here yet*,
   and ARCHITECTURE's state banner, now describe what exists; re-grepped for survivors, none.
7. **Work Log complete** — including two false claims I committed about the harness, corrected in
   place rather than edited away.
8. **State updated** — this commit.

**Conditional items**

- **Regression tests** — every defect found has a test that fails without the fix: the backfill
  (`UpgradePathTests`), the exhaustion bound, the rollback, the malformed keys.
- **ADR** — none required. [ADR-0010](../../architecture/adr/ADR-0010-clean-architecture-layering.md)
  was raised the same day and governs [T-0022](T-0022-adopt-clean-architecture-layering.md), not
  this ticket's shape.
- **Security** — free-text title and description are personal-data-shaped; acceptance created an
  issue carrying such text and found **0 occurrences** in API and migrator logs. `U+0000` is
  refused at the contract boundary rather than reaching PostgreSQL.
- **Migrations** — `AddIssues` creates `issues` with a unique index on `(ProjectId, Number)` and
  adds the counter to `projects` with a database default of 1, proved against a **populated**
  database by reverting a live stack and running the real migrator.
- **Observability, Accessibility, Deployment** — not applicable.

**Verdict: Done.** No deviation required.

**What this ticket cost, and what it bought.** Three review rounds and two acceptance runs, which
found: a migration that would have made every existing project's first issue unreadable, an
identifier bound that broke at the top of its range, a response declaring one thing and returning
another, and a connection leak that had been growing invisibly since T-0003. None of those was
found by mutation; all came from exercising the running system in states it was not built for.

- **Did:** Walked every universal item and every applicable conditional item against repository state.
- **Decided:** F4's deferral is genuine — the destination exists and its criteria make a ceiling
  raise insufficient.
- **Remaining:** none.
- **Branch / PR:** merged as `912448e`, `5649367`, `73a1833`; worktrees and branches removed.
- **Test state:** 110/110 · smoke 13/13 · build 0 warnings · format 0 (both) · drift 0 · validate 0.

### 2026-08-31 — Retroactive note: `73a1833` was a lane deviation, not a precedent (claude-sm-9d4e)

Added after the fact, at `claude-rev-7a03`'s request during [T-0006](T-0006-issue-lifecycle-fields.md),
and recorded here rather than in a retro so that the next person to find this commit finds it marked.

**`73a1833` — "T-0005: correct a false mechanism committed beside a working fix" — put
`apps/GotIssues.Api.IntegrationTests/Infrastructure/PostgresContainerFixture.cs` directly on the
trunk with no branch and no recorded review verdict.** That is source code —
[TESTING.md](../../standards/TESTING.md) treats test code as production code — and
[GIT.md](../../standards/GIT.md) lane 2 requires it to reach `main` through a reviewed merge. The
change itself was right (it corrected a diagnosis that had been asserted rather than measured); the
route it took was not.

**Why the note exists.** During T-0006 I cited this commit as precedent for putting a post-acceptance
correction straight on the trunk, and went to a branch anyway. The reviewer's objection is the
durable point: **citing an unmarked deviation as precedent launders it into a rule.** A single
instance that contradicts a written standard is evidence the standard was missed, not that it has an
exception — and the way to stop it recurring is to mark the instance, which nothing in the repository
had done.

The related conclusion, recorded so it is not re-decided per ticket: **the axis is lane, not size.**
A "small changes may skip review" exception was considered and rejected — the T-0006 change that
prompted this looked like a two-line comment fix and in fact carried a predicate change that altered
which columns the guard covered. What should be proportionate is the depth of the review, not
whether one happens.

- **Did:** marked `73a1833` as a deviation; recorded the lane-not-size conclusion.
- **Decided:** no exception carved for small changes.
- **Remaining:** nothing; T-0005 stays `done` and this note changes no outcome.
- **Open questions / blockers:** none.
- **Test state:** unchanged — no code touched by this entry.
