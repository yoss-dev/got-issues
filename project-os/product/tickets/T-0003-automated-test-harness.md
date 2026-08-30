---
id: T-0003
title: Automated test harness — xUnit, WebApplicationFactory, and PostgreSQL via Testcontainers
type: technical
status: backlog
priority: high
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0001]
adrs: [ADR-0003]
created: 2026-08-30
updated: 2026-08-30
---

# T-0003: Automated test harness — xUnit, WebApplicationFactory, and PostgreSQL via Testcontainers

## Problem / Context

[`TESTING.md`](../../standards/TESTING.md) sets the bar — every endpoint exercised against a real PostgreSQL instance, `dotnet test` as a merge gate in [`GIT.md`](../../standards/GIT.md) — but no test project exists, so `dotnet test` currently proves nothing. Every ticket that follows will claim "tests pass" against an empty suite unless this lands early.

The standard deliberately rules out the EF Core in-memory provider: it enforces no constraints and translates no real SQL, so a test passing on it says little about production behaviour.

## Desired Outcome

`dotnet test` runs unit and integration tests, the integration tier exercising the running API through its real HTTP pipeline against a real PostgreSQL instance in a container, and it is fast enough to run habitually.

## User / Business Value

Turns the merge gates in `GIT.md` from prose into something mechanical. Without it, "the full suite passes" is unverifiable and the Definition of Done cannot honestly be met by any later ticket. Value is to the delivery process rather than to a product persona.

## Scope

### In Scope

- A unit test project and an integration test project, placed per [TESTING.md](../../standards/TESTING.md)'s tier table.
- xUnit as framework and runner.
- `WebApplicationFactory` wiring so integration tests exercise the real ASP.NET Core pipeline.
- Testcontainers starting a PostgreSQL container per run, with migrations applied to it, and each test owning its data.
- A test authentication approach for tests that are not *about* authorisation, and a way to obtain real tokens for those that are — **never** globally disabling authentication ([SECURITY.md](../../standards/SECURITY.md)).
- At least one real unit test and one real integration test proving the harness works end to end — not empty scaffolding.
- The exact commands documented in the README and reconciled with [TESTING.md](../../standards/TESTING.md)'s *How to run the suite* section.

### Out of Scope

- Broad test coverage of product behaviour — tests arrive with the features they cover.
- Contract tests validating responses against the specification's schemas (belongs with T-0002's pipeline once it exists).
- CI execution — no CI exists (`PROJECT.md` Q6); the suite runs locally per [GIT.md](../../standards/GIT.md).
- Coverage measurement or targets; the standard sets no numeric target.
- Load or performance testing.

## Acceptance Criteria

- [ ] AC1: Given Docker is running, when `dotnet test` is run from a clean clone, then unit and integration tests execute and pass with no manual setup.
- [ ] AC2: Given the integration tier, when it runs, then it exercises the API through `WebApplicationFactory` against a PostgreSQL container — not an in-memory provider, and not a developer's local database.
- [ ] AC3: Given a fresh PostgreSQL container, when the integration tier starts, then the schema is applied by the project's migrations.
- [ ] AC4: Given the integration tests, when they are run twice in succession and when run in any order, then they pass both times — no leftover state, no order dependency.
- [ ] AC5: Given a test covering an authorised endpoint, when an unauthenticated caller is simulated, then the test asserts the request is refused — the negative case is proven, not assumed.
- [ ] AC6: Given Docker is **not** running, when `dotnet test` is run, then the failure message makes the cause obvious rather than surfacing an opaque connection error.
- [ ] AC7: Given the README and `TESTING.md`, when their documented test commands are followed, then they match what actually works.

## Examples / Scenarios

- Run the suite twice back to back: both green, no manual cleanup between.
- Run a single integration test in isolation: passes without depending on others having run first.
- Docker stopped: a clear, actionable failure rather than a timeout.
- A test asserting 401 for an unauthenticated request to a protected endpoint.

## Technical Notes

*Suggestions, not constraints:* share one PostgreSQL container across a test collection rather than starting one per test — per-test containers are correct but slow, and a slow suite stops being run habitually ([TESTING.md](../../standards/TESTING.md)). Isolate data per test with transactions or per-test schemas.

Do not test the generated code — testing a generator's output tests the generator. Test the behaviour behind the generated contract.

## Dependencies

- **T-0001** — needs the API project and its EF Core migrations.
- Docker on the developer machine (verified present 2026-08-30). Note the ordering constraint: Testcontainers needs a Docker daemon, so the suite cannot run in an environment without one.

## Risks / Unknowns

- Container startup makes the integration tier meaningfully slower than a pure unit suite; if it becomes slow enough to skip, the gate erodes. Splitting the tiers so the habitual run stays fast is the mitigation the standard already anticipates.
- Test isolation strategy (transaction rollback vs. per-test schema vs. fresh container) is unsettled and has real consequences for both speed and reliability; refinement should choose deliberately rather than defaulting.
- The test authentication approach must not become a hole that hides real authorisation defects — an over-permissive test handler makes every authorisation test vacuous.

## Testing Notes

This ticket's own verification is meta: the deliverable is the harness, so acceptance means running it, deliberately breaking a test to confirm it fails, and confirming the reruns in AC4. A harness that has only ever been seen green has not been shown to work.

## Relevant ADRs & Documentation

- [TESTING.md](../../standards/TESTING.md) — tiers, the real-PostgreSQL rule, the gate
- [GIT.md](../../standards/GIT.md) — `dotnet test` as a merge gate
- [SECURITY.md](../../standards/SECURITY.md) — never disable authentication to make a test pass
- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — the stack under test

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — checked during refinement; note applied exceptions here.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-30 — Scrum Master (claude-sm-9d4e)

- **Did:** Created during `bootstrap-project` step 8. Scope derived from `TESTING.md`'s project-specific section and the merge gates in `GIT.md`.
- **Decided:** Required at least one real test of each tier rather than empty scaffolding — a harness proven only by its own existence is the failure mode this ticket exists to prevent.
- **Remaining:** Refinement to drive to Ready; the test-isolation strategy is the main open design choice.
- **Open questions / blockers:** none blocking.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
