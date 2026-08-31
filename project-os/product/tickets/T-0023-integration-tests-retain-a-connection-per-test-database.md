---
id: T-0023
title: Integration tests retain a database connection per test for the whole run
type: bug
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: []
adrs: []
created: 2026-08-31
updated: 2026-08-31
---

# T-0023: Integration tests retain a database connection per test for the whole run

## Problem / Context

Found during [T-0005](T-0005-create-and-read-issues.md)'s re-acceptance by `claude-qa-8f52`, who was
asked to determine whether T-0005's harness fix addressed a cause or a symptom. It addressed a
symptom, and the recorded diagnosis of the cause is wrong.

**Reproduction.** Run `dotnet test apps/GotIssues.Api.IntegrationTests` and sample the Testcontainers
PostgreSQL instance once per ~150 ms:

```sql
select count(*) from pg_stat_activity where backend_type = 'client backend';
select count(distinct datname) from pg_stat_activity where backend_type = 'client backend';
select count(*) from pg_database where datname not in ('postgres','template0','template1');
```

**Observed**, over a full run of the 93 integration tests (100 samples):

| Elapsed | Connections | Idle | Distinct databases | Databases created |
| --- | --- | --- | --- | --- |
| 0 s | 3 | 2 | 2 | 1 |
| 5 s | 32 | 31 | 30 | 30 |
| 10 s | 60 | 58 | 57 | 60 |
| 16 s (end) | **104** | **103** | **92** | 95 |

- The connection count **never decreased once** across the whole run (0 decreases in 100 samples).
- It tracks the number of databases created almost exactly: **1.09 connections per database**, and
  103 of the final 104 are `idle`.
- Every database that has ever been created still holds a connection at the end of the run,
  including those whose test class finished ten seconds earlier.

**Expected:** a connection belonging to a finished test class is returned to the server when its
`ApiFactory` is disposed, so the steady-state count reflects the tests currently running (a
single-digit number), not every test that has ever run.

**Severity: normal.** Nothing is wrong with the product; the suite is green today. But the failure
mode is the one this project keeps paying for — invisible until it crosses a limit, and then
surfacing as an unrelated-looking red test.

### Why the recorded diagnosis is wrong, and why that matters

`PostgresContainerFixture.CreateDatabaseAsync` carries this comment, added by T-0005:

> *"every test class builds its own ApiFactory against its own database **while xUnit runs classes in
> parallel**. That multiplies out past the container's max_connections"*

**xUnit does not run these classes in parallel.** All nine integration test classes carry
`[Collection(PostgresFixtureDefinition.Name)]`, and xUnit runs the classes of a single collection
sequentially. At most one class is live at a time, so parallel pool growth cannot be the mechanism.
The measurement agrees: pools never approach their cap (1.09 connections per database, against a
`MaxPoolSize` of 10), because the count is driven by *how many databases have been created*, not by
how deep any one pool goes.

This matters for three reasons:

1. **`MaxPoolSize = 10` binds nothing.** Actual peak usage is ~1 connection per database. The cap
   could be 1 or 100 with no effect on the observed totals.
2. **`max_connections=500` postpones rather than fixes.** Growth is linear at ~1.09 connections per
   test with no reclamation, so the ceiling returns at roughly **455 tests**. The arithmetic also
   explains the original failure exactly: at the default ceiling of 100, the limit lands at ~89
   tests — the suite had 86 integration tests, T-0005 added 7, and three unrelated classes went red.
   It was **both** latent and a leak: latent *because of* the leak.
3. **The next person to hit `53300` will read the comment and look for a parallelism problem that
   does not exist.**

### Likely mechanism (to be confirmed by whoever fixes it)

Each test gets a fresh database and therefore a fresh connection string, and `ApiFactory` registers
`UseNpgsql(connectionString)`. EF Core caches the internal service provider — and with it the
`NpgsqlDataSource` and its pool — keyed by the options, so disposing the `WebApplicationFactory`
does not dispose the data source. Its idle connection then survives to the end of the test process.
Stated as a hypothesis, not a finding: the measurement above establishes *that* connections are
never reclaimed, not *why*.

## Desired Outcome

A test run's connection count reflects the tests currently running rather than every test that has
run, so the suite's demand on PostgreSQL stops growing with the size of the suite.

## User / Business Value

The engineers and agents who run this suite. Today, adding tests silently consumes a shared resource
until the suite fails somewhere unrelated to the change that caused it — the most expensive kind of
failure to diagnose, and one this repository has now paid for once.

## Scope

### In Scope

- Releasing each test database's connections when the test or class that owns it finishes.
- Correcting the diagnosis recorded in `PostgresContainerFixture.CreateDatabaseAsync`, and
  re-deciding `MaxPoolSize` and `max_connections=500` once the cause is fixed — either may become
  unnecessary, and a mitigation kept without a reason is the next reader's confusion.
- A check that fails when connection use grows with the number of tests rather than with
  concurrency — otherwise this returns the same way, invisibly. Asserting a ceiling on
  `pg_stat_activity` at the end of a run is one cheap form.
- Deciding whether per-test databases should be dropped after use; 95 abandoned databases per run is
  the same accumulation in another resource, even though it does not currently break anything.

### Out of Scope

- Changing the per-test database isolation strategy itself ([T-0003](T-0003-automated-test-harness.md)
  chose it and it is sound; this is about releasing what it allocates).
- The smoke tier, which drives Compose rather than Testcontainers.
- Product code. Nothing here is reachable by a deployed API.
- Test parallelism strategy — the single collection is a deliberate choice, and this bug is not an
  argument against it.

## Acceptance Criteria

- [ ] AC1: Given the integration suite running, when connection counts are sampled throughout, then
      the count does not grow monotonically with the number of completed tests, and connections
      belonging to a finished test class are released.
- [ ] AC2: Given the suite, when it runs against a PostgreSQL container with `max_connections` at its
      **default of 100**, then it passes — demonstrating the fix rather than the raised ceiling is
      what makes it pass.
- [ ] AC3: Given the mitigations T-0005 added (`max_connections=500`, `MaxPoolSize=10`), when the
      cause is fixed, then each is either removed or kept with a recorded reason that its own
      measurement supports.
- [ ] AC4: Given the comment in `PostgresContainerFixture`, when this ticket is done, then it
      describes the mechanism that was actually measured, and no longer claims xUnit runs these
      classes in parallel.
- [ ] AC5: Given a future change that reintroduces the growth, when the suite runs, then something
      fails and names the resource — not an unrelated test in whichever class ran at the limit.

## Examples / Scenarios

- Run the suite with `max_connections=100`: today it fails at ~89 tests with
  `53300: sorry, too many clients already`; after the fix it passes with headroom.
- Add fifty trivial integration tests: connection peak should be unchanged, not fifty higher.

## Technical Notes

The sampling used to find this is three `psql` queries in a loop against the Testcontainers instance
(`docker ps --filter ancestor=postgres:18-alpine`) while `dotnet test` runs; the full sample table is
in T-0005's Work Log under `claude-qa-8f52`'s second acceptance entry.

## Dependencies

None. T-0003 introduced the harness and is `done`; per [WoW](../../governance/WAY_OF_WORKING.md) §11
a defect found later is a new bug ticket rather than a reopening.

## Risks / Unknowns

- The mechanism above is a hypothesis. If EF's provider-level caching is the cause, the fix may be
  an explicitly constructed and disposed `NpgsqlDataSource` per factory, or an explicit pool clear on
  teardown — both cheap, but the measurement should come before the choice.
- AC2 is the criterion that makes this falsifiable. Without it, raising a ceiling again would pass.

## Testing Notes

The bug is in the harness, so the test for it is a property of a run rather than of a case. AC2 is
the strongest available check, because it removes the mitigation that currently hides the problem.

## Relevant ADRs & Documentation

- [TESTING.md](../../standards/TESTING.md) — *"These rules bind the test infrastructure too"*, and the
  SPRINT-002 precedent of a harness leaking resources invisibly on every run
- [T-0003](T-0003-automated-test-harness.md) — introduced the fixture
- [T-0005](T-0005-create-and-read-issues.md) — crossed the threshold, mitigated it, and recorded the
  diagnosis this ticket corrects

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet evaluated; created during
      acceptance with reproduction, measurements and expected-vs-observed already recorded.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`.

---

## Work Log

### 2026-08-31 — QA / Test Engineer (claude-qa-8f52)

- **Did:** Created from T-0005's re-acceptance. The coordinator asked whether T-0005's harness fix
  addressed a latent ceiling or a leak; measurement says it is a leak, and that the ceiling was
  latent because of it. Recorded the sample data, the arithmetic that reproduces the original
  failure point, and the reason the committed comment's mechanism cannot be the real one.
- **Decided:** Nothing about the fix — the mechanism is a hypothesis and belongs to whoever
  implements this. AC2 is deliberately written so that raising a ceiling again cannot pass it.
- **Remaining:** Refinement to `ready`.
- **Open questions / blockers:** none. Not urgent — the suite is green and has roughly four times its
  current size in headroom.
- **Test state:** n/a — not started.
