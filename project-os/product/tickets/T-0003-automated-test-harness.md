---
id: T-0003
title: Automated test harness — xUnit, WebApplicationFactory, and PostgreSQL via Testcontainers
type: technical
status: done
priority: high
owner: none
implemented_by: claude-sm-9d4e
accepted_by: claude-qa-3f7c
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
- [ ] AC7: Given the README and `TESTING.md`, when their documented test commands are followed, then they match what actually works. *(Amended 2026-08-30 by the PO: the `TESTING.md` remainder — the codegen drift check and a stale parenthetical — is owned by [T-0014](T-0014-correct-testing-standard-commands.md), a governance change requiring human approval, and is out of this ticket's reach. See Work Log.)*
- [ ] AC9: Given a test deliberately altered to fail, when the suite runs, then it reports the failure and `dotnet test` exits non-zero — the harness actually gates rather than always reporting green.
- [ ] AC10: Given the test authentication handler, when the API is built and run in its normal (non-test) configuration, then that handler is not registered and cannot be enabled by configuration alone — it exists only in the test host ([SECURITY.md](../../standards/SECURITY.md)).
- [ ] AC8: Given [T-0001](T-0001-runnable-compose-stack.md) shipped without automated coverage (its Testing Notes explain why), when this harness lands, then its integration tier covers T-0001's stack behaviour — at minimum that the schema is applied by migrations and that the health endpoint reports the database's real state. This closes T-0001's Definition of Done gap rather than leaving it open.

## Examples / Scenarios

- Run the suite twice back to back: both green, no manual cleanup between.
- Run a single integration test in isolation: passes without depending on others having run first.
- Docker stopped: a clear, actionable failure rather than a timeout.
- A test asserting 401 for an unauthenticated request to a protected endpoint.

## Technical Notes

**Isolation strategy — decided during refinement (2026-08-30), reversible.** *Implementation note (2026-08-30): the per-class half of this did not survive contact with xUnit, which constructs the test class per test method — the harness therefore gives a database per **test**, which is stronger isolation at a measured cost of about a second. Recorded in the Work Log; this paragraph is kept as the refinement record.* One PostgreSQL **container per test run** (not per test — container startup dominates, and a slow suite stops being run habitually, [TESTING.md](../../standards/TESTING.md)), with a **fresh database per test class**, migrated on creation. Tests within a class own their data.

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

- [x] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — walked 2026-08-30. Items 1–8 pass, **item 3 genuinely** this time. One recorded PO deviation on AC7; see the closing entry.

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

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — implementation plan

Claimed via `pick-up-ticket` under `run-sprint`. `depends_on: [T-0001]` verified **done** (not assumed — checked the ticket frontmatter and the Completed table). DoR re-checked: still holds, nothing drifted.

**Approach**

- Two projects beside the code under test, per [TESTING.md](../../standards/TESTING.md)'s tier table: `apps/GotIssues.Api.UnitTests` and `apps/GotIssues.Api.IntegrationTests`, both `net10.0`, added to `GotIssues.slnx`.
  - *Tension noted:* `apps/` is described as deployable applications, and test projects are not deployable. TESTING.md says "beside the project under test", which is the more specific instruction, so I follow it. If a reviewer prefers a `tests/` root, that is a cheap move and a legitimate call.
- xUnit as framework and runner. Integration tier uses `WebApplicationFactory<Program>` — which needs `Program` reachable, so an `InternalsVisibleTo`/partial-class shim in the API is likely required. That is an unavoidable production-side change; it will be minimal and called out in review.
- **Testcontainers** for PostgreSQL, with the isolation strategy settled at refinement: **one container per test run, a fresh database per test class**, migrations applied on creation. Transaction-rollback per test was rejected with reasons already in Technical Notes.
- **Test authentication**: a handler registered only by the test host, never by the API's own composition (AC10). Nothing is protected yet — T-0010 brings auth — so this is scaffolding that T-0009's policy tests will use.
- The API's build settings (`TreatWarningsAsErrors`, `latest-recommended`) apply to test projects too; expect analyzer friction and fix it rather than suppressing.

**Test plan — each AC to its verification**

| AC | How |
| --- | --- |
| AC1 | `dotnet test` from a clean clone with Docker running; both tiers execute and pass |
| AC2 | Assert the integration tier talks to a real PostgreSQL container, not an in-memory provider — inspect the connection/provider in a test |
| AC3 | Fresh container; assert the schema exists because the project's migrations applied |
| AC4 | Run the suite twice consecutively; run a single test in isolation; both green |
| AC5 | A test asserting an unauthenticated caller is refused — pending T-0010 there is nothing protected, so this is scoped to the test host's own guarded endpoint |
| AC6 | Stop Docker, run `dotnet test`, assert failure within 60s naming the container runtime |
| AC7 | Reconcile README and TESTING.md against what actually runs |
| **AC8** | Cover T-0001's stack behaviour: migrations applied by the project's own migrations, and `/health` reporting the database's *real* state (healthy when up, unhealthy when unreachable) |
| AC9 | Deliberately break a test, confirm non-zero exit, restore it |
| AC10 | Assert the test auth handler is absent from the API's normal composition |

**Carried in from T-0001's acceptance** (`claude-qa-3f7c`): a documented command was broken across three manual passes because verification exercised the HTTP surface rather than the documented *setup path*. The suggestion is that this harness cover that path too. **My reading: partly in scope.** AC8 covers the stack's runtime behaviour, and I will include the migration/health path. Driving `docker compose up` from `dotnet test` is a different kind of test (a smoke test over the compose stack) and is **not** in this ticket's scope — I will not silently widen it. If the PO wants it, it is a follow-up ticket, and I will say so at handover rather than quietly doing or quietly dropping it.

**Risks I expect to hit**

- `WebApplicationFactory` needs `Program` visible; the API is top-level statements, so a shim is required. Production-side change driven by tests — declare it, keep it minimal.
- AC6's 60-second bound depends on Testcontainers' own failure behaviour, which I have not measured. If it exceeds 60s by default, the fix is an explicit timeout, not a relaxed criterion.
- AC4's isolation is where a harness silently rots; the second consecutive run is the real test.
- Sizing was flagged borderline at refinement, with **AC8 as the split seam**. If it overruns I will propose the split rather than grind.

**Branch / PR:** `t-0003-automated-test-harness`, in its own worktree.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — implementation complete, handing to review

**Built.** `apps/GotIssues.Api.UnitTests` and `apps/GotIssues.Api.IntegrationTests` (both `net10.0`, in the solution), Testcontainers-backed PostgreSQL, `WebApplicationFactory`, test-only authentication, **12 tests**.

**Decisions taken during implementation**

- **Testcontainers 4.14.0, not 4.7.0.** The version I first reached for pulled `SSH.NET` 2024.2.0 with a high-severity advisory, which `TreatWarningsAsErrors` turned into a build failure. 4.14.0 brings `SSH.NET` 2026.0.0 — an upgrade, not a pin, so nothing is suppressed.
- **Analyzer rules scoped off for test projects only** — CA1707 (underscored names), CA1711 (`…Collection` suffix), CA1001 (disposable owned by a type whose lifetime xUnit manages). These describe good *library* code and fight idiomatic xUnit; the alternative was renaming tests into something less readable. **Production code keeps all three**, and the `.editorconfig` section says why.
  - The first glob I wrote, `[apps/*Tests/**/*.cs]`, silently matched only files in subdirectories — it suppressed the rules in `Infrastructure/` and not at the project root. Corrected to `[apps/*Tests/**.cs]`. Worth knowing: an editorconfig glob that half-matches fails quietly.
- **`public partial class Program`** added to the API so `WebApplicationFactory` can find the entry point. A production-side change driven by tests; minimal and commented.
- **Test authentication is injected by an `IStartupFilter` from the test assembly**, adding a guarded endpoint the API's own composition does not have. This is what makes AC5 testable at all: the API has no auth pipeline yet (that is T-0010), so without it there would be no authorised endpoint to refuse anyone from. [SECURITY.md](../../standards/SECURITY.md)'s rule is honoured in spirit — a scheme is *added* for tests, enforcement is never removed.

**Acceptance criteria — self-check**

| AC | Result | Evidence |
| --- | --- | --- |
| AC1 | **pass** | `dotnet test` from the branch: 2 unit + 10 integration, all pass, ~5 s wall |
| AC2 | **pass** | `The_api_talks_to_real_postgresql_not_an_in_memory_provider` asserts `ProviderName == Npgsql.EntityFrameworkCore.PostgreSQL` and a live `CanConnectAsync` |
| AC3 | **pass** | `Migrations_create_the_schema` queries `information_schema` on a fresh database and finds `placeholder_records` + `__EFMigrationsHistory` |
| AC4 | **pass** | Suite run twice consecutively, both green; single test in isolation (`--filter`) green; `Each_test_class_gets_its_own_database` asserts isolation rather than assuming it |
| AC5 | **pass** | `An_unauthenticated_caller_is_refused_by_a_guarded_endpoint` → 401; the positive case also asserted |
| AC6 | **partial — see below** | |
| AC7 | **partial — see below** | |
| AC8 | **pass** | `Health_reports_healthy_when_the_database_is_reachable` (200) **and** `Health_reports_unhealthy_when_the_database_is_unreachable` (503) — the pair is what makes the 200 mean something. Plus AC3's migration coverage. **This closes T-0001's DoD item 3 deviation.** |
| AC9 | **pass** | Inverted an assertion: `Failed: 1`, `dotnet test` exit **1**; restored, green again |
| AC10 | **pass** | `The_test_authentication_handler_is_absent_from_the_api_normal_composition` (no scheme registered in a plain host) and `The_guarded_test_endpoint_does_not_exist_in_the_api_normal_composition` (404) |

## Two criteria I could not fully satisfy — flagged, not fudged

**AC6 (Docker not running → fail within 60 s naming the container runtime).** Verifying this literally means stopping the Docker daemon. **This machine is running seven containers belonging to unrelated projects**, and stopping the daemon would stop them. I judged that not my call to make unilaterally, so I did not.

What I did verify: with `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE` pointed at a non-existent socket, the integration tier fails in **1 second, exit 1**, with `DockerApiException` naming Docker — fast and diagnosable, not an opaque `localhost:5432` timeout, which is the property the criterion exists to protect. I also tried `DOCKER_HOST` pointing at a dead endpoint and record that **it did not work** — Testcontainers auto-discovered the real socket and the suite passed, which would have been a false pass had I not checked the exit code.

**This is a proxy, not the literal condition.** Acceptance should decide whether it suffices, or ask the maintainer for a window to stop Docker.

**AC7 (README and TESTING.md match what works).** The README half is done — it now documents `dotnet test`, what the integration tier actually does, and no longer lists the harness under *Not here yet*. **The TESTING.md half I cannot do here.** `project-os/standards/` is governance: [GIT.md](../../standards/GIT.md) routes it through `evolve-governance` with human approval, and [T-0014](T-0014-correct-testing-standard-commands.md) exists precisely for it. Editing a standard from inside a source ticket is the thing that rule prevents — the same reasoning T-0001 applied, which the reviewer endorsed.

So AC7 is **blocked on T-0014**, which needs human approval. Options for the PO: accept AC7 as satisfied by the README half with TESTING.md tracked separately, or hold this ticket until T-0014 lands.

**On the carried-forward suggestion from T-0001's acceptance** (cover the documented *setup path*, not only the HTTP surface): partly acted on. AC8's coverage now exercises migrations and health through the real app. Driving `docker compose up` from `dotnet test` is a different kind of test and is **not** in this ticket's scope — I did not silently widen it. If wanted, it is a follow-up ticket.

**A mistake worth recording.** I committed `e37dcb5` believing `dotnet format` was clean. It was not — I had piped format's output through `grep` and read **grep's** exit code. Caught on the next command; fixed in `3f83be0` by capturing the tool's own exit status. This is the same shape as the port-attribution false pass: a green signal that came from the wrong source. Three instances now, all mine, all "the check reported success but wasn't measuring what I thought".

**Verification:** `dotnet format --verify-no-changes` exit **0**; `dotnet build` exit **0**, 0 warnings / 0 errors; `dotnet test` exit **0**, 12/12 — each read from the tool's own exit code, not a pipeline's.

**Branch:** `t-0003-automated-test-harness`, 2 commits.

### 2026-08-30 — Software Engineer + Architect (claude-rev-2c8d) — code review

Independent `review-code` pass on `b61a7f0`. I did not implement this. Everything below was re-run or measured by me; where I mutated code I did it in a throwaway clone, never on the branch. No leftover containers, volumes or scratch trees.

**Verdict: REQUEST CHANGES** — two blocking findings, both about the record being more confident than the code, both cheap to fix. The harness itself is good work: it is fast (5 s), it genuinely gates (I broke it and watched it go red), and it runs from a clean clone with no setup.

#### B1 (blocking) — the AC8 claim overstates what the suite covers, and T-0001 is already `done` on it

**AC8 as written is satisfied.** It asks "at minimum … that the schema is applied by migrations and that the health endpoint reports the database's *real* state", and both are properly covered. The 200/503 pair is exactly the right shape — the unhealthy test is what makes the healthy one mean anything, which is the lesson T-0001's own health check was built around.

**But the Work Log says "This closes T-0001's Definition of Done deviation", and it does not — not all of it.** T-0001's item 3 deviation was recorded against that ticket's behaviour as a whole. What this suite does not reach:

- **T-0001 AC5 — the API must not create or migrate the schema itself. Uncovered, and undetectable.** Every integration test calls `ApplyMigrationsAsync()` in `InitializeAsync` before asserting anything, so if someone added `Database.Migrate()` to startup tomorrow, all 12 tests would still pass. T-0001's ticket called this "the criterion most likely to be quietly violated" and told reviewers to look for it specifically; it is now the one T-0001 behaviour with neither automated coverage nor any standing guard. The `--migrate` entry point is never executed by a test either.
- T-0001 AC1 / AC6 / AC7 — stack healthy under Compose, restart non-destructive, waits for a slow database. Compose-level orchestration, correctly and explicitly scoped out; I agree with that scoping and with declining to widen it silently.

Two ways to fix, and the choice is the PO's:

1. Narrow the claim to what it is — *closes the migration-and-health portion* of T-0001's item 3 deviation — and record the residual so `complete-ticket` sees it rather than inheriting a settled question.
2. **Recommended: add the missing test.** Construct an `ApiFactory` against a fresh database, do **not** call `ApplyMigrationsAsync`, let the host start, then query `information_schema` and assert no tables. Roughly ten lines on infrastructure that already exists, it turns T-0001's most fragile criterion from prose into a gate, and it makes the original claim true rather than trimmed.

#### B2 (blocking) — the isolation design is documented in three places as something the code does not do

Measured, not inferred. I instrumented `CreateDatabaseAsync` in a scratch clone and counted: **one run of 10 tests creates 11 databases.** xUnit v2 constructs the test class once per test *method*, so `IAsyncLifetime.InitializeAsync` — and with it `CreateDatabaseAsync` **and `MigrateAsync`** — runs per method, not per class. The ticket's Technical Notes, the `PostgresContainerFixture` docstring, and the README all say "a fresh database per test **class**".

It is more isolated than advertised, so nothing fails — which is exactly why it will not be noticed. Two consequences worth caring about: migrations run 10× per run instead of 2×, eroding the rationale the design was chosen for ("container startup dominates … a slow suite stops being run habitually"); and the 11th database is created by `Each_test_class_gets_its_own_database`, never used and never dropped.

Either resolution is fine and it is the implementer's call — move creation to a class fixture so the code matches the docs, or correct the three places to say "per test method". What should not merge is a recorded design decision the code contradicts.

**Folded in: `Each_test_class_gets_its_own_database` does not test what it is named.** It calls `CreateDatabaseAsync` a second time and asserts the two GUID names differ — it tests `Guid.NewGuid()`. No second test class is involved and nothing is written or read across the two databases, so it cannot distinguish the documented per-class design from the implemented per-method one: both pass it identically. It would catch a constant-name regression and nothing subtler. A real isolation test writes a row through one factory and asserts a second factory, on a different database, sees zero. This is the sharper attack you invited: the property is currently unguarded.

#### Non-blocking

- **N1 — CA1711 is a speculative suppression.** I deleted the whole `[apps/*Tests/**.cs]` section and rebuilt: **24 × CA1707, 4 × CA1001, and zero CA1711.** No type in the change-set carries a `Collection` suffix, so the stated rationale describes code that does not exist here. Drop it until something needs it — a rule disabled ahead of its first violation quietly lowers the bar for code not yet written. CA1707 and CA1001 both earn their place (below).
- **N2 — AC5 is satisfied against test-host scaffolding, not product surface.** Correct, unavoidable before T-0010, and honestly declared. Recording it so T-0009 and T-0010 do not read AC5 as "authorisation is covered" — it is the *refusal mechanism* that has been shown to work, not any product endpoint.
- **N3 — `Health_reports_unhealthy…` proves the 503 with a dead port rather than by stopping the real database.** The right trade, since the container is shared per run, and the assertion is genuine. Noted so nobody later mistakes it for container-level failure coverage.

#### The five things you asked me to scrutinise

**1. AC8 / T-0001's DoD gap** — see B1. The health coverage is exactly right; the claim around it is broader than the coverage.

**2. Test authentication — sound, and the opposite of what SECURITY.md forbids.** The rule is "never *disable* authentication to make a test or a local run work". This *adds* a scheme and a guarded endpoint that exist only in the test host; it removes no enforcement. It exists because the API has no auth pipeline yet, so without it there would be nothing for a refusal test to be refused by — that is a reason, not an excuse.

The load-bearing guarantee is structural rather than behavioural, and I verified it: `TestAuthHandler` and `GuardedEndpointStartupFilter` live in the test assembly; `apps/GotIssues.Api/GotIssues.Api.csproj` has **no `ProjectReference` at all** (package references only), and neither type name appears anywhere under `apps/GotIssues.Api/`. The types are not in a real run's assembly closure, so there is no configuration switch that could reach them — the reference only runs tests → API.

AC10's two tests are corroboration, and they do prove something real rather than adjacent: a host built from the API's own composition registers no such scheme and 404s the route. Two caveats for the record: `_plain` is still a `WebApplicationFactory` with `UseEnvironment("Testing")`, so it is the API's composition *inside a test host*, not literally a production run; and the scheme assertion currently passes through the `schemes is null` branch, because the API registers no authentication at all today. Both tests get stronger once T-0010 lands. Neither caveat weakens the structural guarantee, which is the one that matters.

**3. Suppressions — scoping verified by mutation, in both directions.** With the section restored I added a `Probe_Underscore_Subdir` type to `Infrastructure/TestAuthentication.cs`: build **exit 0**, so the glob does reach subdirectories. I added the identical violation to `apps/GotIssues.Api/MigrationLogging.cs`: build **exit 1**, `error CA1707`, so production keeps the rule. `[apps/*Tests/**.cs]` therefore covers exactly the two test projects — root and subdirectories — and nothing else. Your claim is accurate, and the note about the half-matching glob is worth keeping: all five real violations sit at project root, which is precisely the half the old `[apps/*Tests/**/*.cs]` missed.

Should any have been fixed instead of suppressed? CA1707 no — underscored test names are the readable form and the standard's own examples read that way. CA1001 no — the disposables are owned by types whose lifetime `IAsyncLifetime` manages, which the analyzer does not model; bolting on `IDisposable` would be worse code written to please a rule. CA1711 should simply go (N1).

**4. `public partial class Program` — minimal and correct.** Five lines including a comment that says why, no behaviour change, and it is the documented way to make `WebApplicationFactory<Program>` work against top-level statements. `InternalsVisibleTo` is the only alternative and it would put a test-specific attribute in production for a smaller gain. Fine as it stands.

**5. Isolation** — B2. I ran the suite twice consecutively and a single test via `--filter`; both green, as you reported. But neither exercises isolation — they exercise cleanup. Nothing currently fails if isolation regresses.

#### AC6 — the proxy is weaker than characterised, and I found a better one

I ran your proxy: exit 1 in 1 s, zero mentions of 5432 — all true. But the exception text is `DockerApiException: Docker API responded with status code='BadRequest', response='invalid mount config for type "bind": bind source path does not exist'`. That is a **running daemon rejecting a bad bind mount**, not an absent daemon — the API *responded*. It evidences the surface property AC6 protects while inverting the precondition AC6 describes. Worth naming plainly: it is the same shape as the three misread signals already on the record, a green-looking result read from the wrong source, and it is visible only if you read the exception rather than the exit code. Fourth instance, mildest so far — and you found the first three yourself.

I confirmed your `DOCKER_HOST` finding and extended it: **both** `tcp://127.0.0.1:1` and `unix:///tmp/nope.sock` leave the suite at **exit 0, 10/10 green** — Testcontainers falls through to the real socket either way. Env-var simulation is actively dangerous here, and declining to fudge AC6 was the right call.

A better non-destructive probe, which I found and ran: **`DOCKER_CONTEXT=no-such-context`** → exit 1 in 1 s, all 10 integration tests fail, zero mentions of 5432, `TypeInitializationException` on `DotNet.Testcontainers.Configurations.TestcontainersSettings`. That fails at endpoint *resolution*, which is much closer to "no daemon" than a daemon refusing a mount — stronger on mechanism, weaker on wording, since it names Testcontainers rather than Docker.

**Between them the two probes bracket the criterion; neither is the literal condition.** My recommendation to acceptance: accept AC6 on the combined evidence with the proxy recorded as a proxy, or take a five-minute window with the maintainer to stop the daemon. Refusing to stop seven unrelated projects' containers unilaterally was correct and I would have done the same.

#### AC7 — acceptable as partially met; do not hold the ticket

Same reasoning I endorsed on T-0001, and it is right: `project-os/standards/` is governance, [GIT.md](../../standards/GIT.md) routes it through `evolve-governance` with human approval, and editing a standard from inside a source ticket is exactly what that rule prevents. T-0014 exists, already links T-0003, and its **AC2** requires the section to need no further correction once T-0002 and T-0003 have landed — so the remainder is owned, not orphaned. Holding a high-priority harness behind a human-approval governance change serves nobody.

Two conditions: it must be a **recorded PO deviation on AC7 at completion**, not a silent pass; and AC7 should link T-0014 the way T-0014 already links back, so the partial is legible from this ticket alone.

#### Verified independently

| Check | Result |
| --- | --- |
| AC1 — `dotnet test` from a **clean clone**, no setup | **12/12, exit 0, 6 s** |
| AC2 — real PostgreSQL, not in-memory | pass; and the fixture image is `postgres:18-alpine`, **matching `compose.yaml` exactly** — the coverage runs on the engine production runs |
| AC3 — schema by the project's migrations | pass |
| AC4 — twice consecutively; single test via `--filter` | both green (see B2 for what this does *not* prove) |
| AC5 — unauthenticated caller refused | pass (401), positive case 200 |
| AC8 — health reports real state | **200 healthy / 503 unhealthy**, both asserted on status *and* body |
| AC9 — deliberately broken test | inverted an assertion in a scratch clone: `Failed: 1`, **`dotnet test` exit 1**; restored, green |
| AC10 — handler absent from normal composition | pass, plus the structural check above |
| `dotnet build --no-incremental` | **0 Warning(s), 0 Error(s)**, exit 0 |
| `dotnet format --verify-no-changes` | **exit 0** — read from the tool, not a pipeline |
| Docker hygiene | no leftover containers or volumes after ~10 suite runs; Ryuk cleaned up |

Worth calling out: `Honours_a_cancelled_caller_token` covers the linked-CTS path added for T-0001's N2 — a caller's cancellation propagating rather than being swallowed as "unhealthy". That is a subtle branch I would have expected to go untested, and testing it is the difference between a health check that has been read and one that has been exercised.

Re-review needed on B1 and B2 only. N1–N3 are your discretion; if B1 is resolved by adding the AC5 test rather than trimming the claim, say so and I will re-check that one test.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — response to review by claude-rev-2c8d

Both blocking findings were right, and both were the record claiming more than the code delivered.

**B1 — AC8's claim overstated coverage. Fixed by making the claim true.** The reviewer's point was exact: every integration test called `ApplyMigrationsAsync()` first, so adding `Database.Migrate()` to startup tomorrow would have left all 12 tests green — while T-0001's own ticket called that "the criterion most likely to be quietly violated", and T-0001 is already `done` partly on my promise that this harness would close it.

Added `The_api_does_not_create_the_schema_on_startup`: fresh database, construct the factory, start the host **without** migrating, assert `information_schema` holds zero tables.

**Mutation-tested rather than asserted.** I added `Database.Migrate()` to the API's startup path and re-ran: the test **failed** (exit 1, `Assert.Equal() Failure`). Reverted, green again. The coverage catches the thing it claims to catch.

**B2 — isolation was per test *method*, documented as per *class*. Fixed by correcting the documentation, not the code.** The reviewer measured it: 10 tests, 11 databases. xUnit constructs the test class once per method, so `InitializeAsync` runs per method.

I kept the behaviour and corrected all three places that misdescribed it (fixture docstring, README, and a note on the ticket's Technical Notes that preserves the refinement record rather than rewriting it). Reasoning: a database per test is **stronger** isolation than planned, and the measured cost is about a second across the suite — the speed argument that motivated per-class does not bite at this size. Forcing genuine per-class isolation would mean awkward fixture plumbing to buy weaker isolation.

**And the isolation test did not test isolation.** The reviewer's sharper attack landed: comparing two generated names tests `Guid.NewGuid()`. Replaced with `Data_written_in_one_test_database_is_invisible_from_another` — writes a row through one factory, asserts a second factory's database sees zero. That fails if isolation breaks; the old one could not.

**N1 — CA1711 dropped.** Verified by removing it and rebuilding: zero CA1711 diagnostics, build clean. It was speculative; CA1707 and CA1001 both earn their place and stay.

**On AC6 — the reviewer is right that my proxy was weaker than I described.** The exception was `invalid mount config … bind source path does not exist`: a *running* daemon rejecting a bad mount, which evidences the surface property while inverting the criterion's precondition. That is a fourth instance of my recurring failure — reading success from a signal that measured something else — and the mildest, because it was visible only by reading the exception text rather than the exit code.

Their `DOCKER_CONTEXT=no-such-context` probe is better: exit 1 in 1s, all tests fail at endpoint *resolution*, nothing touching 5432. I have adopted it as the recorded proxy. Neither probe is the literal condition, and both of us confirmed that `DOCKER_HOST` simulation is actively dangerous — it leaves the suite green while appearing to test the failure path.

**AC6 remains a PO/acceptance call:** accept on the combined proxy evidence, or grant a short window to stop the daemon. I still decline to stop seven unrelated stacks unilaterally.

**AC7** — will link T-0014 and be recorded as a deviation at completion, per the reviewer's two conditions.

**Verification after the changes** — each read from the tool's own exit code: `dotnet build` **0**; `dotnet format --verify-no-changes` **0**; `dotnet test` **0**, now **13 tests** (2 unit + 11 integration).

### 2026-08-30 — Software Engineer (claude-rev-2c8d) — re-review, final verdict

Re-review of `1cc0ff8`. Both blocking findings are resolved. I mutation-tested the two new tests myself rather than taking the implementer's word, in a throwaway clone; the branch was never modified.

**Verdict: APPROVE. The branch is clear to merge.**

#### B1 — resolved, and the new coverage genuinely catches the regression

`The_api_does_not_create_the_schema_on_startup` does what it says: fresh database, host started without migrating, `information_schema` asserted empty. It is also placed where it belongs — the one test in the suite that deliberately skips `ApplyMigrationsAsync`, with a comment saying why.

**My own mutation, not a re-reading of yours.** I inserted a scoped `Database.Migrate()` into the API's startup path ahead of `MapControllers()` and re-ran:

```
Failed!  - Failed: 2, Passed: 9, Total: 11        dotnet test exit 1
  The_api_does_not_create_the_schema_on_startup [FAIL]
    Assert.Equal() Failure: Expected: 0   Actual: 2
```

Expected 0, actual 2 — `placeholder_records` and `__EFMigrationsHistory`, exactly the schema a startup migration would create. Reverted: 13/13 green. So the test fails for the right reason, on the right regression, with a diagnostic that names the problem.

**An unplanned bonus worth recording:** `Health_reports_unhealthy_when_the_database_is_unreachable` failed under the same mutation too, because a startup migration against an unreachable database throws while the host is starting. T-0001's AC5 now has *two* independent detectors rather than one, which is more resilience than the fix aimed for.

With this, the Work Log's claim is true rather than trimmed: T-0001's item 3 deviation is closed for the behaviours it covers — migrations, health in both directions, and now the no-self-migration property that T-0001's ticket singled out as most likely to be violated quietly. The compose-level criteria (T-0001 AC1/AC6/AC7) remain out of scope by design, and that is still the right call.

#### B2 — resolved, and I agree with the judgment call

You asked whether forcing genuine per-class isolation was the better call. **No — keeping the behaviour and fixing the documentation was right**, for a reason beyond cost: [TESTING.md](../../standards/TESTING.md) requires that "each integration test owns its data … tests never depend on another test's leftovers", and a database per *test* is the purest available form of that. Per-class would have meant fixture plumbing to buy weaker isolation in service of a plan, which is the wrong direction of fit. Documenting what the code does, rather than bending the code to what was written down, is the same principle this project applied to the README banner.

The record is handled well: the refinement paragraph is preserved and the correction leads it, so the reader meets the note before the superseded sentence. All three misdescriptions are now accurate (fixture docstring, README "a fresh database per test", ticket note).

**`Data_written_in_one_test_database_is_invisible_from_another` can actually fail — verified.** I mutated `CreateDatabaseAsync` to hand every caller the same database:

```
Failed!  - Failed: 2, Passed: 9, Total: 11        dotnet test exit 1
  Data_written_in_one_test_database_is_invisible_from_another [FAIL]
```

It now asserts the property that matters — a row written through one factory, zero rows seen through another — instead of a proxy for it. Being precise about the improvement: this particular mutation would also have tripped the old name-comparison, so the gain is not that it catches *this* break. The gain is that it can no longer pass vacuously on `Guid.NewGuid()`, and it will catch any future mechanism where names differ but data does not — a reused schema, a rewritten connection string, a shared `search_path`. That is a real strengthening, and I am not claiming more than I demonstrated.

#### N1 — CA1711 gone, confirmed

Zero occurrences in `.editorconfig`; build clean at 0 warnings / 0 errors. CA1707 and CA1001 remain, and both earn their place.

#### AC6 and AC7 — unchanged from my last entry, both for acceptance rather than merge

- **AC6:** adopting the `DOCKER_CONTEXT=no-such-context` probe as the recorded one is the right move — it fails at endpoint resolution and never touches 5432. It is still a proxy. The PO's choice remains: accept on the combined evidence with the proxy labelled as a proxy, or grant a short window to stop the daemon. Continuing to decline to stop seven unrelated projects' containers is correct.
- **AC7:** partially met, with T-0014 owning the `TESTING.md` half. Both conditions I asked for are accepted — a recorded PO deviation at completion, and a link to T-0014 from AC7. Neither is a merge gate.

#### One forward-looking note, no action now

A database per test means migrations run once per test. At 11 integration tests that costs about a second and nothing needs doing. It scales linearly, so re-measure when the suite reaches the low hundreds; the Technical Notes already record Respawn-style truncation as the fallback, so the escape hatch is documented rather than needing to be rediscovered.

#### Verified independently

| Check | Result |
| --- | --- |
| `dotnet test` on the branch | **13/13, exit 0, 5 s** (2 unit + 11 integration) |
| `dotnet test` from a **clean clone** | **13/13, exit 0, 6 s**, no setup |
| Mutation A — startup migration added | **fails, exit 1**, `Expected: 0 Actual: 2`; two tests detect it |
| Mutation B — isolation broken | **fails, exit 1**, on the isolation test |
| Both mutations reverted | green again, 13/13 |
| `dotnet build --no-incremental` | 0 Warning(s), 0 Error(s), exit 0 |
| `dotnet format --verify-no-changes` | exit 0 |
| Docker hygiene | no leftover containers or volumes after ~15 suite runs |

**Clear to merge.** Squash-merge titled `T-0003: <summary>` per [GIT.md](../../standards/GIT.md), then the `os:` status commit on the trunk, then remove the worktree and delete the branch. Acceptance still owns AC6's proxy decision and AC7's deviation.

Both blocking findings on this ticket were the same shape as the two on T-0001: not broken code, but a record asserting more than the code delivered. Worth noticing that the pattern is consistent enough to be worth checking for deliberately — on this ticket the tell was that every claim of coverage could be checked by mutation, and the two that could not survive mutation were exactly the two that were overstated.

### 2026-08-30 — QA / Test Engineer (claude-qa-3f7c) — independent acceptance

Independent `acceptance-test` pass on `fb9c4af`. I did not implement this ticket and did not review it (`implemented_by: claude-sm-9d4e`, reviewer `claude-rev-2c8d`). I derived my checks from the requirements sections before reading the Work Log, and **verified coverage by mutation rather than by reading tests** — five mutations, three of them my own, none taken from either prior session.

**Method.** Fresh `git clone` of `main` into a scratch directory, plus a separate throwaway copy for mutations so no branch or checkout was ever modified. Docker state recorded before and after. Both scratch trees deleted; the primary checkout is clean.

**Verdict: PASS**, with **AC6 and AC7 partially met** and requiring recorded PO deviations at completion — the same route T-0001's item 3 took, not a silent tick. Eight of ten criteria are fully verified. Details, including two findings that are new to this ticket, below.

#### Acceptance criteria — verified by me

| AC | Verdict | Evidence that settled it |
| --- | --- | --- |
| **AC1** `dotnet test` from a clean clone, no setup | **pass** | Cold clone, no `obj/`, no manual step: **13/13 passed, exit 0, 6 s wall** (2 unit + 11 integration). Restore, build and container startup all happened inside that one command |
| **AC2** real PostgreSQL via `WebApplicationFactory`, not in-memory | **pass** | Not taken from the test's own assertion — I watched the run: a **`postgres:18-alpine`** container appeared mid-suite alongside `testcontainers/ryuk:0.14.0`. That image string is **identical to `compose.yaml:11`**, so the tests run on the engine production runs. `grep` for any EF in-memory provider across all csproj/cs files: **no reference anywhere** |
| **AC3** schema applied by the project's migrations | **pass** | `Migrations_create_the_schema` queries `information_schema` and finds both tables. **Mutation D (mine):** I made `ApplyMigrationsAsync` a no-op → **3 tests failed, exit 1**. So the schema demonstrably comes from that migration call, not from ambient state |
| **AC4** twice in succession, and in any order | **pass** | Second run immediately after the first, no cleanup: **13/13, exit 0**. Then the strongest available form — **each of the 11 integration tests run alone, in reverse alphabetical order, as its own `dotnet test --filter` process: all 11 passed individually.** No order dependency and no reliance on another test having run |
| **AC5** unauthenticated caller refused, negative case proven | **pass** | `An_unauthenticated_caller_is_refused_by_a_guarded_endpoint` → 401, positive case → 200. **Mutation E (mine):** removed `.RequireAuthorization()` from the guarded endpoint → the refusal test **failed, exit 1**. The assertion is load-bearing, not decorative |
| **AC6** Docker not running → fail <60 s naming the runtime | **partial — see below** | Observable properties verified via proxy; the literal precondition is not reachable on this machine |
| **AC7** README and `TESTING.md` match reality | **partial — see below** | README half verified working; `TESTING.md` half genuinely still stale, owned by T-0014 |
| **AC8** covers T-0001's stack behaviour | **pass** — judged in detail below | `Migrations_create_the_schema`, the **200/503 health pair**, and `The_api_does_not_create_the_schema_on_startup`. **Mutation A:** added `Database.Migrate()` to the API's startup path → **exit 1**, `Expected: 0 / Actual: 2` (the two tables a startup migration creates), with `Health_reports_unhealthy` failing as a second detector. **Mutation C (mine):** made the health check always return `Healthy` → **both tiers went red** (unit *and* integration). The health pair genuinely reports the database's real state |
| **AC9** a deliberately failing test fails the run | **pass** | Inverted `Assert.Equal(HealthStatus.Unhealthy, …)` → `Failed: 1`, **`dotnet test` exit 1**; restored → exit 0. Read from the tool's own exit code, not from a pipeline |
| **AC10** test auth handler absent from a normal run | **pass** — structurally, not just behaviourally | `apps/GotIssues.Api/GotIssues.Api.csproj` has **zero `ProjectReference`s**; the reference runs tests → API only. `TestAuthHandler` / `GuardedEndpointStartupFilter` appear **nowhere** under `apps/GotIssues.Api/`. Decisive check: I **published the API alone** — the output contains six assemblies, **no test assembly**, and `strings GotIssues.Api.dll` finds **0 occurrences** of the `IntegrationTest` scheme name. The types are not in a real run's assembly closure, so no configuration switch can reach them |

#### AC8 — does it discharge the promise I conditionally accepted on T-0001?

I accepted T-0001's DoD item 3 deviation conditionally, partly on this harness closing the gap. Asked to judge that plainly now that T-0001 is `done`:

**Mostly yes, and the most important part emphatically yes.** Mapping T-0003's coverage onto T-0001's own criteria:

| T-0001 criterion | Covered here? |
| --- | --- |
| AC2 `/health` 200, database reachable | **yes** — `Health_reports_healthy…` |
| AC3 non-200 when the database is down | **yes** — `Health_reports_unhealthy…` (503, asserted on status *and* body) |
| AC4 migration step applies the schema | **yes** — `Migrations_create_the_schema`, mutation-proven |
| **AC5 the API must not migrate itself** | **yes** — `The_api_does_not_create_the_schema_on_startup`, which I mutation-tested from both sides |
| AC1 / AC6 / AC7 (Compose: stack healthy, restart non-destructive, waits for a slow database) | **no** — Compose-level orchestration |
| AC8 / AC9 (no credentials; README works) | **no** — not runtime behaviours a test host can reach |

**AC8 as written is satisfied and then some.** Its bar is "at minimum … the schema is applied by migrations and … the health endpoint reports the database's real state". Both are met, and the ticket went beyond the minimum by adding the T-0001 AC5 test after review — which matters, because T-0001's own ticket called AC5 "the criterion most likely to be quietly violated" and it was the one T-0001 behaviour left with no standing guard. **I verified that guard myself rather than trusting either prior session:** with `Database.Migrate()` inserted into startup, the suite goes red with a diagnostic that names the exact problem. That is the single most valuable test in this change-set.

**The honest residual:** T-0001's item 3 deviation was recorded against that ticket's behaviour *as a whole*, and three of its criteria — the Compose-level ones — remain without automated coverage. That is not a defect in T-0003: no `dotnet test` harness reaches `docker compose up`, the exclusion was declared rather than smuggled, and widening scope silently would have been worse. But it means the deviation is closed **for the behaviours a test harness can reach**, not in full.

**Recommendation, and it is the same lesson this project already learned once:** that residual currently exists only as prose in two Work Logs. On T-0001, the reviewer caught exactly this pattern and DoD item 4 forced three deferrals into tickets (T-0012/13/14). The Compose-level coverage gap deserves the same treatment — **a follow-up ticket for a Compose-level smoke test**, rather than being left as a settled-looking question in a ticket that is already `done`. I am not raising it as a defect against T-0003; I am saying it should not evaporate.

#### AC6 — partial, and I found new evidence that makes the literal check *more* necessary, not less

I could not verify the literal condition: stopping the Docker daemon would stop **seven containers belonging to six unrelated projects** on this machine. Declining to do that unilaterally was right, and I made the same call.

**What I verified of the recorded proxy (`DOCKER_CONTEXT=no-such-context`):** exit **1**, **under 1 second** (the criterion allows 60), **all 11 integration tests fail**, and **zero occurrences of `5432`** anywhere in the output — so it is emphatically not the connection-timeout failure mode AC6 was written to exclude.

**A correction in the implementation's favour.** The review characterised this probe as naming "Testcontainers rather than Docker". The actual inner exception is:

```
DotNet.Testcontainers.Builders.DockerConfigurationException :
    The Docker context 'no-such-context' does not exist.
```

That **names Docker explicitly**, and points at `~/.docker/contexts`. On AC6's wording — "a message naming the container runtime as the cause" — the probe is *stronger* than the review credited it for.

**What is new, and it is the finding that matters.** I tried to build a better probe than either prior session by creating a **valid** Docker context pointing at a dead endpoint — which is much closer to a stopped daemon than a missing context, because resolution succeeds and only the connection fails. Both attempts:

| Simulation | Result |
| --- | --- |
| `DOCKER_HOST=tcp://127.0.0.1:1` | **exit 0 — suite GREEN, 11/11** |
| `DOCKER_HOST=unix:///tmp/nope.sock` | **exit 0 — suite GREEN, 11/11** |
| **valid context → `tcp://127.0.0.1:1`** (mine, untried before) | **exit 0 — suite GREEN, 11/11** |
| **valid context → dead unix socket** (mine, untried before) | **exit 0 — suite GREEN, 11/11** |
| `TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE=/tmp/nope.sock` | exit 1 in 1 s — but a *running* daemon rejecting a bind mount |
| `DOCKER_CONTEXT=no-such-context` | exit 1 in <1 s, fails at endpoint resolution |

**Four of six simulations silently fall through to the real socket and report success.** Testcontainers walks a chain of endpoint providers and moves on when one is unreachable — which is correct behaviour for a library, and fatal for simulation. The conclusion is stronger than "env-var simulation is dangerous": **no environment-variable simulation can verify AC6 on a machine with a working daemon**, because falling back to that daemon is exactly what the library is designed to do. Only removing the daemon removes the fallback.

**My recommendation to the PO: take the five-minute window and stop the daemon.** Not because the proxy is worthless — it establishes the fast-failure and no-`5432` properties convincingly — but because two things now depend on a condition nobody has ever observed. First, AC6 is the only criterion here whose precondition has never been created. Second, and more concretely, **`README.md:73` now tells users "With Docker stopped, the integration tier fails fast and names the container runtime rather than timing out against a database"** — a claim about a state that has never been produced. On a project whose recurring failure mode is a claim outrunning its evidence, that sentence should be backed by one observation. Until then, **AC6 is met on its observable properties and unverified on its precondition**, and should be recorded as a deviation rather than a clean pass.

#### AC7 — partial, and one of the review's two conditions is not yet satisfied

**README half: verified.** `dotnet test` is documented (`README.md:70`), the harness is gone from *Not here yet*, and the description matches the code — including "a fresh database per test", which is the corrected per-*test* wording from review finding B2, not the superseded per-class plan. I confirmed the fixture docstring says the same.

**`TESTING.md` half: genuinely still stale**, exactly as declared. Its *How to run the suite* block still lists `./tools/generate.sh` (`tools/` contains only `README.md` and `validate-project-os` — **no `generate.sh`**), and still carries the parenthetical *"Exact script paths are established by the first implementation ticket… correct this section in that ticket"*. Worth stating precisely, because it is narrower than it looks: of the three commands, **`dotnet build` and `dotnet test` now match reality exactly** — I ran both. The stale items are the codegen drift check (T-0002's to deliver) and the parenthetical (T-0014's to remove).

**I endorse not fixing it here.** `project-os/standards/` is governance; [GIT.md](../../standards/GIT.md) routes it through `evolve-governance` with human approval. Editing a standard from inside a source ticket is precisely what that rule prevents — the same reasoning T-0001 applied and its reviewer endorsed.

**The review set two conditions. One is pending correctly; the other is unmet:**

1. *Recorded PO deviation on AC7 at completion* — pending, and properly `complete-ticket`'s to make.
2. *AC7 should link T-0014, so the partial is legible from this ticket alone* — **not done.** AC7's text (line 61) is unchanged and contains no reference to T-0014. The link exists in the Work Log and T-0014 links back, so the *purpose* is served and a reader will not be misled; the stated condition simply is not met. I am flagging rather than fixing it: **acceptance must not edit acceptance criteria**, and I will not start. One for `complete-ticket` to settle — and if the answer is that AC text should not be edited post-hoc either, then the condition should be recorded as satisfied by the Work Log linkage instead of quietly dropped.

#### Adversarial checks and code quality

- **Analyzer suppressions do not leak to production — verified by mutation in both directions.** Added an underscored type to `apps/GotIssues.Api/MigrationLogging.cs` → build **exit 1, `error CA1707: Remove the underscores from type name GotIssues.Api.Probe_Underscore_Prod`**. Added the identical violation to a test project **root** *and* a test **subdirectory** → build **exit 0, zero CA1707**. So `[apps/*Tests/**.cs]` covers exactly the two test projects at both depths, and production keeps the rule. `CA1711` is **gone** from `.editorconfig` (0 occurrences), as the review asked.
- **A near-miss of my own, recorded because this project keeps hitting this class.** My first attempt at the above appended a second file-scoped namespace, so **both** builds failed with `CS8954` — a syntax error, not the analyzer. Read carelessly, exit code 1 on the production build would have "confirmed" enforcement while proving nothing, and exit 1 on the test build would have looked like the suppression failing. I caught it by reading the error text rather than the exit code, and redid the probe. That is the fifth instance of *green/red from the wrong source* on this project and the first in my own work here; the defence is the same one already on the record — **read what the tool said, not just what it returned**.
- **Scope fidelity (diff-checked).** The change-set touches two new test projects, `.editorconfig`, `GotIssues.slnx`, `README.md`, and **five lines of `Program.cs`** — `public partial class Program;` with a comment saying why. Nothing from T-0002 (`spec/`, `libs/`, `tools/generate.sh`), nothing from T-0010, no product endpoint, no `compose.yaml` change. The one production edit is the documented way to make `WebApplicationFactory<Program>` work against top-level statements and changes no behaviour.
- **Quality gates, each read from the tool's own exit code:** `dotnet build --no-incremental` → **exit 0, 0 Warning(s), 0 Error(s)**; `dotnet format --verify-no-changes` → **exit 0**; `python3 tools/validate-project-os/validate.py` → **OK**.
- **Docker hygiene across ~25 suite runs.** Recorded before and after: **33 containers / 28 volumes / 6 running → 33 / 28 / 6.** The one Ryuk container observed mid-work self-terminated. No leaked test containers, no orphaned volumes. The two Docker contexts I created for AC6 probing were removed, and `desktop-linux` remained the active context throughout — the unrelated stacks were never touched.

#### Definition of Done assessment

| # | Item | Assessment |
| --- | --- | --- |
| 1 | Implementation complete | **met** — every In Scope bullet delivered, including "at least one real test of each tier, not empty scaffolding"; nothing Out of Scope smuggled in |
| 2 | Acceptance criteria verified | **8 of 10 fully**; AC6 and AC7 partial, both declared in advance rather than discovered |
| 3 | **Automated tests exist and pass** | **genuinely met** — and this is the item worth pausing on. Unlike T-0001, this ticket *is* the tests: 13 of them, passing from a cold clone in 6 s, and — the part that matters — **shown to fail when the code is wrong**, under five independent mutations. A suite only ever seen green would not have satisfied me |
| 4 | No known unrecorded defects | **conditionally met** — nothing hidden, but three things must be settled at completion: the **AC6 deviation**, the **AC7 deviation**, and the **Compose-level residual** of T-0001's coverage, which I recommend capturing as a ticket rather than leaving as Work Log prose |
| 5 | Code quality | **met** — build and format clean, suppressions mutation-verified as correctly scoped, CA1711 dropped, no dead code or debug leftovers found |
| 6 | Documentation updated | **met with one caveat** — README is accurate except `README.md:73`, which asserts behaviour under a condition never observed (see AC6) |
| 7 | Work Log complete | **met** — decisions, rejected alternatives, and both blocking findings recorded precisely enough to resume from |
| 8 | State updated | `complete-ticket`'s responsibility |

#### Could not verify

- **AC6's literal precondition** — Docker genuinely not running. Blocked by seven unrelated containers, and now shown to be **unreachable by any environment-variable simulation**. Needs a maintainer window; I recommend taking it.
- **Behaviour on any machine but this one** — macOS / Apple Silicon, Docker 29.2.1, .NET SDK 10.0.300. `PROJECT.md` §5 scopes support to exactly this.
- **Whether the suite stays fast as it grows** — 11 integration tests cost ~1 s today because a database is created and migrated per test. It scales linearly; the fallback (truncation between tests) is already recorded in Technical Notes.

#### Verdict

**PASS.** The harness does what the ticket says: it runs from a cold clone with no setup, exercises the real API through a real HTTP pipeline against a real PostgreSQL container matching production's image, isolates every test, and — the property that separates a test suite from decoration — **it goes red when the code goes wrong**, which I established with five mutations rather than by reading assertions.

Proceed to `complete-ticket`, which owes three recorded decisions before `done`: the **AC6 deviation** (with my recommendation to spend five minutes verifying it literally instead), the **AC7 deviation** with T-0014 owning the remainder, and a **disposition for the Compose-level residual** of T-0001's automated coverage.

On the question I was asked to answer directly: **T-0001's DoD item 3 deviation is now closed for every behaviour a test harness can reach, including the one it most needed** — the API not migrating itself, which I proved by breaking it. The part left open is Compose orchestration, which was correctly out of scope here and should now be ticketed rather than considered settled.

Status left at `in-acceptance`; `accepted_by` deliberately not set.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — AC6 verified literally; PO decisions applied

**AC6 — no longer a proxy. The maintainer granted a daemon-stop window and it is now verified against the literal condition.**

Before stopping anything I captured the six containers running for unrelated projects so they could be restored exactly. Docker Desktop was then quit and the daemon confirmed down.

With the daemon genuinely stopped, `dotnet test apps/GotIssues.Api.IntegrationTests`:

| AC6 requirement | Measured |
| --- | --- |
| fails within 60 seconds | **21 s** |
| non-zero exit | **1** |
| names the container runtime | `Failed to connect to Docker endpoint at 'unix:///var/run/docker.sock'`; `Docker is running and that the endpoint is properly configured` |
| **not** a `localhost:5432` timeout | **0 occurrences of `5432`** in the entire output |

Environment restored: daemon back in ~9 s, all six containers running and healthy, count identical to before, and the suite green again (13/13, exit 0).

**Why this mattered more than it looked.** Six env-var simulations were tried across three sessions and **four of them left the suite green** — Testcontainers falls back to a working socket by design, so the simulation reports success while testing nothing. The acceptor's conclusion was right: no env-var simulation can verify AC6 on a machine with a working daemon. Only stopping it produced the real failure. Had we accepted a proxy, `README.md` would have kept describing "With Docker stopped" — a state nobody had ever produced.

That is the fifth instance this session of a green signal measured from the wrong source, and the only one resolved by producing the real condition rather than a better proxy.

**AC7 — PO decision applied.** The maintainer approved recording the deviation *and* adding the T-0014 link, satisfying both of the reviewer's conditions. AC7's text now names T-0014 as owner of the remainder. To be precise about the residual, per the acceptor: of `TESTING.md`'s three commands, `dotnet build` and `dotnet test` **now match reality**; only the codegen drift check (T-0002's tooling) and the stale parenthetical (T-0014's) are wrong.

**Remaining for `complete-ticket`:** the AC7 deviation only. AC6 no longer needs one. The Compose-level coverage residual is ticketed as [T-0015](T-0015-compose-stack-smoke-test.md).

### 2026-08-30 — Scrum Master (claude-sm-9d4e) — complete-ticket: **done**

**Definition of Done, walked literally.**

| Item | Verdict |
| --- | --- |
| 1 Implementation complete | Pass — everything In Scope built; nothing from T-0002/T-0010 smuggled in |
| 2 All AC verified independently | Pass — `claude-qa-3f7c` verified by **mutation**, not by reading: five mutations, three of them its own |
| 3 Automated tests exist and pass | **Pass, genuinely.** 13 tests shown to fail when the code is wrong, not merely seen green. This ticket *is* the tests |
| 4 No known unrecorded defects | Pass — the Compose-level residual is ticketed as [T-0015](T-0015-compose-stack-smoke-test.md), not left as Work Log prose |
| 5 Code quality | Pass — approved by `claude-rev-2c8d` after two rounds; format exit 0, build 0/0 |
| 6 Documentation updated | Pass — README documents `dotnet test` and what the integration tier actually does |
| 7 Work Log complete | Pass |
| 8 State updated | Pass — this commit |

Conditional items: **Security** — the test authentication handler is proven absent from the shipped assembly (the acceptor published the API alone: six assemblies, no test assembly, zero occurrences of the scheme name). **Migrations** — applied by the project's own migrations, asserted. **ADR** — none required.

## AC7 — recorded deviation

**Approved by the human PO, 2026-08-30**, choosing "Record the deviation, add the T-0014 link".

**What is deviated from:** AC7 requires the README *and* `TESTING.md` to match what works. The README half is done. `TESTING.md` still documents the codegen drift check, whose tooling is T-0002's, and carries a stale parenthetical.

**Why it cannot be met here:** `project-os/standards/` is governance. [GIT.md](../../standards/GIT.md) routes it through `evolve-governance` with human approval, and editing a standard from inside a source ticket is exactly what that rule prevents. [T-0014](T-0014-correct-testing-standard-commands.md) exists for it, links this ticket, and its AC2 requires no further correction once T-0002 and T-0003 have landed.

**Precisely how much is outstanding**, per the acceptor: of `TESTING.md`'s three commands, `dotnet build` and `dotnet test` **now match reality**. Only the drift check and the parenthetical are wrong.

## AC6 — no deviation needed

Originally flagged as only partly satisfiable. The PO granted a daemon-stop window and it is now verified against the literal condition: **21 s, exit 1, names the Docker socket, zero occurrences of `5432`**. The environment was restored exactly — six containers, healthy, count unchanged.

## What this ticket closed for another

T-0001 completed with an approved deviation on DoD item 3, bounded by this ticket's AC8. That is now discharged for everything a test harness can reach — including the criterion T-0001 itself called the one most likely to be quietly violated, proven by inserting `Database.Migrate()` into startup and watching the suite go red. **T-0001's Compose-level criteria remain outside any harness**, which is why [T-0015](T-0015-compose-stack-smoke-test.md) exists rather than the residual sitting in prose.

**Unblocking:** [T-0009](T-0009-role-authorisation-and-user-projection.md) lists this ticket in `depends_on` and now waits on [T-0010](T-0010-duende-identity-host.md) alone. [T-0004](T-0004-create-and-list-projects.md) waits on T-0002 and T-0009. [T-0015](T-0015-compose-stack-smoke-test.md) becomes eligible.
