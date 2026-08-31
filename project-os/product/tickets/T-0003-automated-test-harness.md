---
id: T-0003
title: Automated test harness — xUnit, WebApplicationFactory, and PostgreSQL via Testcontainers
type: technical
status: in-progress
priority: high
owner: claude-sm-9d4e
implemented_by: none
accepted_by: none
depends_on: [T-0001]
adrs: [ADR-0003, ADR-0005]
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
- A test authentication approach for tests that are not *about* authorisation, and a way to obtain real tokens for those that are — **never** globally disabling authentication ([SECURITY.md](../../standards/SECURITY.md)). It must be impossible to enable outside the test host (AC10).
- The data-isolation mechanism described in Technical Notes.
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
- [ ] AC6: Given Docker is **not** running, when `dotnet test` is run, then the integration tier fails within 60 seconds with a message naming the container runtime as the cause — not a connection timeout to `localhost:5432`.
- [ ] AC7: Given the README and `TESTING.md`, when their documented test commands are followed, then they match what actually works.
- [ ] AC9: Given a test deliberately altered to fail, when the suite runs, then it reports the failure and `dotnet test` exits non-zero — the harness actually gates rather than always reporting green.
- [ ] AC10: Given the test authentication handler, when the API is built and run in its normal (non-test) configuration, then that handler is not registered and cannot be enabled by configuration alone — it exists only in the test host ([SECURITY.md](../../standards/SECURITY.md)).
- [ ] AC8: Given [T-0001](T-0001-runnable-compose-stack.md) shipped without automated coverage (its Testing Notes explain why), when this harness lands, then its integration tier covers T-0001's stack behaviour — at minimum that the schema is applied by migrations and that the health endpoint reports the database's real state. This closes T-0001's Definition of Done gap rather than leaving it open.

## Examples / Scenarios

- Run the suite twice back to back: both green, no manual cleanup between.
- Run a single integration test in isolation: passes without depending on others having run first.
- Docker stopped: a clear, actionable failure rather than a timeout.
- A test asserting 401 for an unauthenticated request to a protected endpoint.

## Technical Notes

**Isolation strategy — decided during refinement (2026-08-30), reversible:** one PostgreSQL **container per test run** (not per test — container startup dominates, and a slow suite stops being run habitually, [TESTING.md](../../standards/TESTING.md)), with a **fresh database per test class**, migrated on creation. Tests within a class own their data.

Rejected: *transaction-rollback per test*, because integration tests go through real HTTP into `WebApplicationFactory` and the application opens its own connections — a rollback in the test cannot wrap work the app did on another connection. This is the approach most likely to be reached for by habit, and it does not survive contact with an out-of-process HTTP boundary. *Respawn-style truncation between tests* is the viable fallback if per-class databases prove slow; swapping is a contained change.

This choice is deliberately **not** an ADR: it is reversible, confined to the test projects, and touches no architecture or public interface ([ADR bar](../../architecture/adr/README.md)).

*Suggestion, not constraint:* do not test the generated code — testing a generator's output tests the generator. Test the behaviour behind the generated contract.

## Dependencies

- **T-0001** — needs the API project and its EF Core migrations. AC8 exists because T-0001 cannot carry its own automated coverage: the harness depends on it, not the other way round.
- Docker on the developer machine (verified present 2026-08-30). Note the ordering constraint: Testcontainers needs a Docker daemon, so the suite cannot run in an environment without one.

## Risks / Unknowns

- Container startup makes the integration tier meaningfully slower than a pure unit suite; if it becomes slow enough to skip, the gate erodes. Splitting the tiers so the habitual run stays fast is the mitigation the standard already anticipates.
- The isolation strategy is now chosen (see Technical Notes), but its **cost is unmeasured**: if a fresh database per test class proves slow enough to discourage running the suite, the gate erodes and the fallback is truncation between tests.
- The test authentication approach must not become a hole that hides real authorisation defects — an over-permissive test handler makes every authorisation test vacuous. Worse, a test handler reachable in a real deployment is an authentication bypass; AC10 exists for that reason.
- **Sizing is borderline.** Two projects, Testcontainers wiring, the auth handler, and AC8's coverage of T-0001 sit at the top of the DoR guideline. If implementation finds it exceeds 2–3 focused days, the seam to split on is AC8 — T-0001 coverage can become its own ticket.

## Testing Notes

This ticket's own verification is meta: the deliverable is the harness, so acceptance means running it, deliberately breaking a test to confirm it fails, and confirming the reruns in AC4. A harness that has only ever been seen green has not been shown to work.

## Relevant ADRs & Documentation

- [TESTING.md](../../standards/TESTING.md) — tiers, the real-PostgreSQL rule, the gate
- [GIT.md](../../standards/GIT.md) — `dotnet test` as a merge gate
- [SECURITY.md](../../standards/SECURITY.md) — never disable authentication to make a test pass
- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — the stack under test

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-30 during `refinement-session`. All nine universal items hold. Item 7 (sizing) passes but is **borderline** — the split seam is recorded in Risks. Item 9: no blocker prevents starting, though T-0001 must be implemented first (a sequencing constraint for `plan-sprint`, not a blocker). Conditional items: security addressed by AC10; no decision at the ADR bar (isolation strategy judged below it, with reasons in Technical Notes); no UX; no data-migration impact beyond T-0001's. No exceptions applied.

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

### 2026-08-30 — Business Analyst (claude-sm-9d4e)

Perspectives applied: Product Owner, Business Analyst, Software Engineer, Architect, QA, Security. (No UX pass — no user-facing UI.)

- **Did:** Full `refine-ticket` pass within a `refinement-session`.
  - **ENG/ARCH:** settled the isolation strategy that was an open unknown — container per run, fresh database per test class. Recorded *why transaction-rollback per test fails here*: integration tests cross a real HTTP boundary and the app uses its own connections, so a test-side rollback cannot wrap the app's writes. That is what an implementer would most likely reach for by habit, so the rejection is written down rather than left to be rediscovered. Judged **below the ADR bar** — reversible, confined to test projects — and said so explicitly rather than silently.
  - **QA:** AC6 was subjective ("makes the cause obvious"); two implementers could both claim compliance. Rewritten with an observable bound. Added **AC9**: a deliberately failing test must actually fail the run — a harness only ever seen green has not been shown to gate anything.
  - **SEC:** added **AC10**. The original Risks noted that an over-permissive test handler makes authorisation tests vacuous; the sharper danger is a test handler reachable in a real deployment, which is an authentication bypass. Now a criterion, not a note.
  - **Sizing:** borderline against the DoR guideline. Passed, but the split seam (AC8) is recorded so a future implementer has an option rather than an overrun.
- **Decided:** Kept AC8 (covering T-0001's behaviour) here rather than splitting it out — splitting now would leave T-0001's Definition of Done gap owned by nothing.
- **Remaining:** Implementation, after T-0001.
- **Open questions / blockers:** none. **Sequencing note for `plan-sprint`:** this ticket cannot start until T-0001 is implemented, and T-0001's DoD depends on this one — they belong in the same sprint, in that order.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
- **DoR verdict:** **ready**.
