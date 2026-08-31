---
id: T-0021
title: Prove what migrations do to databases that already hold rows
type: technical
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0005]
adrs: []
created: 2026-08-31
updated: 2026-08-31
---

# T-0021: Prove what migrations do to databases that already hold rows

## Problem / Context

Found by independent review of [T-0005](T-0005-create-and-read-issues.md) (`claude-rev-5c14`,
2026-08-31), raised as blocking finding B1 and fixed there. This ticket is the **class** the fix
does not close.

T-0005's `AddIssues` migration added a `NextIssueNumber` column to the existing `projects` table
with `defaultValue: 0`, while the entity's CLR initialiser said `1`. Every project created *after*
the migration was fine, because EF wrote 1 on insert. Every project that already existed was
backfilled with 0, so its first issue came out as `GOTI-0` — violating the `key` pattern and
`number: minimum 1` the API's own contract declares, and unreadable through the only declared read
path, which rejects a zero with 400.

**The defect was invisible to 83 of 84 tests, and to both existing drift gates**, for two separate
structural reasons:

1. **Every test in the repository migrates an empty schema.** Nothing any of them do depends on
   what a migration does to rows that already exist, so the entire upgrade path was untested.
   T-0005 added `UpgradePathTests` in response — one test, at one hardcoded migration boundary,
   seeding one row in one table, asserting one ticket's behaviour. It closes the instance. The
   next migration is not covered by it unless someone remembers.
2. **Nothing compares the database the migrations produce to the model that describes it.** EF's
   `PendingModelChangesWarning` compares the *model* to the *migration snapshot*.
   `tools/check-drift.sh` compares the *spec* to *generated code*. Neither looks at the live
   schema. Before the fix, the model declared no default at all and the database column carried
   `DEFAULT 0` — a divergence sitting precisely in the gap between the two gates.

The second reason is the more general one: it would have caught B1 **without anyone thinking about
upgrades at all**, which is the property that makes a mechanism close a class rather than an
instance.

## Desired Outcome

A migration cannot silently change the shape or content of an existing database without a test
saying so — and the coverage extends to migrations written after this ticket, without depending on
anyone remembering.

## User / Business Value

This is the first migration in the project's history to touch an existing table, and it shipped a
defect that reached a live stack. Every subsequent ticket adds migrations to a schema that, by
then, holds real data: T-0006 adds lifecycle columns to `issues`, T-0007 and T-0008 follow. The
cost of getting one of those wrong rises with every row already in the database, and the failure
mode is silent — a 201 whose body violates the contract that produced it.

## Scope

### In Scope

- A mechanism that fails when the schema a migration actually produces diverges from the EF model
  it is supposed to implement — columns, nullability, and defaults at minimum.
- Extending upgrade coverage beyond T-0005's single hardcoded boundary
  (`BeforeIssues = "20260831162646_AddProjectsDropPlaceholder"`) so that later migrations inherit
  it rather than needing it written again.
- A recorded decision on where the responsibility sits: automated mechanism, a
  [DoD](../../governance/DEFINITION_OF_DONE.md) item for migrations that touch existing tables, or
  both. Deciding "a checklist item is the honest answer" is a legitimate outcome.
- Mutation evidence that the mechanism kills T-0005's original defect **for the schema reason**,
  not only through an issue-numbering assertion.

### Out of Scope

- Fixing any specific migration — B1 is already fixed on T-0005.
- Down-migration / rollback coverage. Related, larger, and nothing depends on it yet.
- Data-migration tooling, backfill scripts, or anything about migrating production data.
- Changing the migration boundary itself — that is [T-0013](T-0013-enforce-migration-boundary-with-db-privileges.md).

## Acceptance Criteria

- [ ] AC1: Given a migration whose column default disagrees with the model's declared default, when
      the suite runs, then it fails and names the column — with no test written specifically for
      that migration.
- [ ] AC2: Given a database holding rows written under an earlier migration, when it is migrated to
      the latest, then a test asserts those rows still satisfy the current model and the contract's
      declared constraints.
- [ ] AC3: Given a migration added after this ticket, when no bespoke test is written for it, then
      it is still covered by AC1's mechanism — or, if the honest answer is that it cannot be, the
      recorded decision says so and names the checklist item that replaces it.
- [ ] AC4: Given T-0005's original `defaultValue: 0` reinstated, when the suite runs, then the
      mechanism from AC1 fails on the schema divergence itself. Recorded as a mutation, stating
      what the mutant reaches.

## Examples / Scenarios

- Reinstate `defaultValue: 0` on `AddIssues`: AC1's mechanism reports that `projects.NextIssueNumber`
  has database default `0` where the model declares `1`. That is the defect named at its cause,
  rather than as a wrong issue number three layers away.
- Add a migration introducing a `NOT NULL` column to `issues` with a backfill value outside the
  contract's declared enum: AC2 catches it on rows that predate the migration.
- Add a migration that only creates a new table: nothing fails, and nothing new has to be written.

## Technical Notes

The two halves are independent and can be sized separately; the schema-conformance half is the one
that closes the class.

For the conformance check, EF exposes no first-class "compare model to live database" API. The
likely routes are `IMigrationsModelDiffer` against a model scaffolded from the live database, or a
direct query of `information_schema` compared against `IEntityType`/`IProperty` metadata. The
latter is cruder and probably sufficient for columns, nullability and defaults. **Which of these is
workable is the ticket's main unknown and should be measured, not assumed.**

For the upgrade half, the seam is that `UpgradePathTests` hardcodes its starting migration.
`context.Database.GetMigrations()` enumerates them, so a boundary-by-boundary form is expressible —
but each boundary needs representative seed data, and that is the part that does not generalise for
free. Refinement should decide whether "seed everything the model can express at each boundary" is
honest or is the kind of test that passes because nothing reaches it.

## Dependencies

- **T-0005** — the ticket that produced the defect, the fix, and `UpgradePathTests`, which this
  ticket generalises.

## Risks / Unknowns

- **The generic upgrade test may not be honestly writable.** Seeding representative rows at every
  historical boundary is real work that grows with the migration count, and a version that seeds
  nothing would pass vacuously — the exact failure mode [TESTING.md](../../standards/TESTING.md)
  warns about. If that is the answer, AC3's escape hatch (a recorded checklist item) is the right
  outcome and refinement should say so rather than forcing a mechanism.
- **The conformance check may produce false positives.** EF leaves artefacts in PostgreSQL that the
  model does not describe — the `AddColumn` default that caused B1 is itself one. A check that
  flags every such artefact is noise; one that ignores defaults would have missed B1 entirely.
  Where that line sits is a design question, not an implementation detail.
- **Sizing is genuinely uncertain** and depends on the Technical Notes unknown. If the conformance
  route turns out to be large, the seam is: ship the conformance check alone, and leave the
  boundary-by-boundary upgrade tests as a follow-on.

## Testing Notes

The mechanism's own coverage claim needs mutation evidence like any other (AC4), and the mutant
must be one the build and EF both accept — reinstating `defaultValue: 0` is such a mutant, verified
during T-0005's review to be accepted by both and to reach an assertion.

## Relevant ADRs & Documentation

- [TESTING.md](../../standards/TESTING.md) — the falsifiable-claims rule this ticket exists to
  extend to migrations
- [T-0005](T-0005-create-and-read-issues.md) — B1 and the review entry that recorded the class
- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — EF Core with code-first
  migrations, applied by an explicit step

## Definition of Ready

- [ ] Not yet evaluated — created from a review finding, to be refined before it enters a sprint.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`.

---

## Work Log

### 2026-08-31 — Software Engineer + Architect (claude-rev-5c14)

- **Did:** Created from T-0005's review. B1 (the counter backfilled to 0) was fixed on that ticket
  and its instance is closed by `UpgradePathTests`; this ticket carries the class, which one test
  at one hardcoded boundary does not close.
- **Decided:** Nothing — the mechanism is this ticket's to choose. Recorded the two candidate
  routes and the reason the schema-conformance one is the stronger of the two: it would have caught
  B1 without anyone thinking about upgrades.
- **Remaining:** refinement.
- **Open questions / blockers:** none blocking creation. The sizing unknown is named in Technical
  Notes.
- **Test state:** n/a — not started.
