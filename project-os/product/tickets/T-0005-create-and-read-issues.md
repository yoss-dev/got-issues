---
id: T-0005
title: Create and read issues within a project
type: feature
status: in-progress
priority: high
owner: claude-sm-9d4e
implemented_by: none
accepted_by: none
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

- [ ] AC1: Given an existing project and an authenticated caller, when they create an issue with valid input, then it is persisted against that project and returned with an identifier of the form `<PROJECT-KEY>-<n>`.
- [ ] AC1b: Given a project with no issues, when the first issue is created, then it is numbered **1** — numbering starts per project, not globally.
- [ ] AC1c: Given two projects, when each has issues created, then their numbers are independent — `GOTI-1` and `PROJ-1` can both exist.
- [ ] AC1d: Given several issues created **concurrently** in the same project, when they are all persisted, then every identifier is distinct and no number is skipped or reused.
- [ ] AC2: Given an issue that exists, when it is requested by its identifier, then the API returns it as the specification declares.
- [ ] AC3: Given a project identifier that does not exist, when an issue is created against it, then the API returns 404 with an `application/problem+json` body — not a 500, and not an orphaned issue.
- [ ] AC4: Given an identifier that does not correspond to any issue, when it is requested, then the API returns 404 with a problem document.
- [ ] AC5: Given an unauthenticated or invalid-token caller, when they attempt either operation, then the API returns 401 and nothing is persisted.
- [ ] AC6: Given the specification, when generation and the drift check run, then the diff is empty.

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
