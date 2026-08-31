---
id: T-0001
title: Runnable Docker Compose stack with API skeleton and PostgreSQL
type: technical
status: in-progress
priority: high
owner: claude-sm-9d4e
implemented_by: none
accepted_by: none
depends_on: []
adrs: [ADR-0003, ADR-0005]
created: 2026-08-30
updated: 2026-08-30
---

# T-0001: Runnable Docker Compose stack with API skeleton and PostgreSQL

## Problem / Context

Nothing runs yet. [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) fixes the stack — .NET 10 / ASP.NET Core, PostgreSQL via EF Core, all under Docker Compose — but no solution, no container, and no `compose.yaml` exist. Until this lands, no other work can be verified end to end, and the README's *Getting started* section describes commands that do not work.

[`PROJECT.md`](../../PROJECT.md) §4 makes the Compose constraint hard: no component may require host-installed infrastructure beyond Docker and the .NET SDK.

The identity host was split out to [T-0010](T-0010-duende-identity-host.md) during refinement — see the Work Log.

## Desired Outcome

`docker compose up` from a clean clone brings up a healthy API and a PostgreSQL instance whose schema has been applied by an explicit migration step.

## User / Business Value

The proof of concept's first real evidence that the company can run this tooling in-house. It also unblocks everything else: no other ticket can be verified against a real system until it exists.

## Scope

### In Scope

- A .NET 10 solution with the API project under `apps/`.
- `compose.yaml` at the repository root wiring the API and PostgreSQL, with supporting files under `infra/`.
- EF Core `DbContext` and an initial migration. The schema may be minimal — a single placeholder table is acceptable; the *mechanism* is the deliverable, not the domain.
- Migrations applied by an explicit migration step in the stack, **not** at API startup ([ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md)).
- A health endpoint reporting database reachability, implemented directly and **not** declared in the specification ([ADR-0005](../../architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md)).
- Compose health checks and startup ordering so the API waits for a ready database rather than crash-looping.
- A `.env.example` documenting every required variable; no secrets committed.
- README *Getting started* updated so the documented commands actually work, and the health endpoint documented there (not in the spec).

### Out of Scope

- **The Duende identity host and all authentication** — split to [T-0010](T-0010-duende-identity-host.md).
- The OpenAPI specification and code generation ([T-0002](T-0002-contract-first-codegen-pipeline.md)).
- The automated test harness ([T-0003](T-0003-automated-test-harness.md)) — see Testing Notes for how this ticket is verified in the meantime.
- Any product endpoint (projects, issues, comments).
- Any deployment target beyond local Compose.

## Acceptance Criteria

- [ ] AC1: Given a clean clone, Docker running, and no pre-existing volume, when the setup documented in the README is followed — copy `.env.example` to `.env`, supply local values, then run `docker compose up` — then every service reports a healthy status in `docker compose ps`, with no further manual steps beyond those. *(Amended 2026-08-30 by the PO; see Work Log.)*
- [ ] AC2: Given the stack is up, when `GET /health` is requested, then it returns 200 with a body indicating the database is reachable.
- [ ] AC3: Given the database container is stopped, when `GET /health` is requested, then it returns a non-200 status indicating unhealthy — the check actually probes the database rather than always reporting success.
- [ ] AC4: Given an empty database volume, when the stack starts, then the migration step applies the schema and the API reports healthy afterwards.
- [ ] AC5: Given the migration step is not run (started alone, or disabled), when the API starts against an empty database, then the API does **not** create or migrate the schema itself.
- [ ] AC6: Given an existing volume with the schema already applied, when the stack is restarted, then the migration step completes as a no-op and no data is destroyed.
- [ ] AC7: Given a database that is slow to accept connections, when the stack starts, then the API waits for it and eventually reports healthy, rather than exiting or crash-looping.
- [ ] AC8: Given the repository and its history, when searched for credentials, then none are present — every credential comes from an environment variable, and `.env.example` lists each one with a placeholder value.
- [ ] AC9: Given the README's *Getting started* section, when its commands are followed literally against a clean clone, then they work as written.

## Examples / Scenarios

- Cold start, empty volume: migration step creates the schema, API becomes healthy.
- Restart with an existing volume: migrations no-op, data intact (AC6).
- Database stopped while the API runs: `/health` turns unhealthy (AC3).
- API started without the migration step against an empty database: no schema appears (AC5).
- Database slow to accept connections: API waits (AC7) — the common failure is a container that exits before the database is listening.
- **Counter-example — explicitly NOT expected:** the API must not create the schema on startup as a convenience, even when it would make the stack easier to run.

## Technical Notes

*Suggestions, not constraints:* Compose health checks with `depends_on: { condition: service_healthy }` address AC7 directly; a naive `depends_on` without a health condition produces intermittent failures that look like application bugs. The migration step is most observable as a short-lived service or init container that exits zero — it is rerunnable and its success or failure is visible in `docker compose ps`.

ASP.NET Core's built-in health-check middleware with a database probe covers AC2/AC3 without extra dependencies.

## Dependencies

None. Docker (29.2.1) and the .NET 10 SDK (10.0.300) are verified present on the maintainer's machine (2026-08-30). No external services, credentials, or human input required.

## Risks / Unknowns

- **AC5 is the criterion most likely to be quietly violated.** `Database.Migrate()` at startup is the path of least resistance and makes the stack easier to run; ADR-0003 rules it out precisely because silent schema changes are invisible. Reviewers should look for it specifically.
- Container startup ordering is a known source of friction. AC7 exists because the failure mode looks like an application defect rather than a sequencing problem.
- The schema here is a placeholder and will churn once the domain arrives (T-0004 onward). The initial migration is scaffolding, not a design commitment.
- **Automated coverage arrives only with T-0003** — see Testing Notes. This is a sequencing consequence, not an omission, and it has a Definition of Done implication recorded there.

## Testing Notes

The test harness does not exist yet ([T-0003](T-0003-automated-test-harness.md) depends on this ticket), so **every acceptance criterion here is verified by hand**, and the Work Log must record exactly what was run and observed for each — per [TESTING.md](../../standards/TESTING.md), where automation is not available the ticket says how verification happened instead.

**Definition of Done implication:** DoD item 3 ("automated tests exist and pass") cannot be satisfied by this ticket in isolation. Two routes exist and the choice belongs to the PO at completion time: either T-0003 lands first in the same sprint and covers these behaviours, or T-0001 completes with a **recorded PO deviation** for item 3. T-0003 carries an acceptance criterion covering this ticket's stack behaviour so the gap closes rather than lingering.

## Relevant ADRs & Documentation

- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — the stack, and why migrations run as an explicit step
- [ADR-0005](../../architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md) — why `/health` is not in the specification
- [ARCHITECTURE.md](../../architecture/ARCHITECTURE.md) — component boundaries and data ownership
- [SECURITY.md](../../standards/SECURITY.md) — secret handling
- [PROJECT.md](../../PROJECT.md) §4 — the Compose constraint

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-30 during `refinement-session`. All nine universal items pass. Conditional items: architectural question resolved by ADR-0005 (Accepted); security concern (secrets) named with AC8; migration impact identified (AC4–AC6). No UX item — no user-facing UI. No exceptions applied.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval. **See Testing Notes: item 3 needs either T-0003 first or a recorded deviation.**

---

## Work Log

### 2026-08-30 — Scrum Master (claude-sm-9d4e)

- **Did:** Created during `bootstrap-project` step 8 (seed the delivery pipeline). Scope derived from ADR-0003 and the Compose constraint in `PROJECT.md` §4.
- **Decided:** Kept the entity model out of scope — this ticket delivers the *mechanism* (stack, migrations, auth round-trip), not the domain, which belongs behind the contract-first pipeline in T-0002.
- **Remaining:** Refinement to drive to Ready; sizing is unverified and this may need splitting (identity host vs. API+database are separable).
- **Open questions / blockers:** none blocking. `PROJECT.md` Q7 (global role set) is deliberately not needed here — issuing and validating one token does not require the role model.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-30 — Business Analyst (claude-sm-9d4e)

Perspectives applied: Product Owner, Business Analyst, Software Engineer, Architect, QA, Security. (No UX pass — no user-facing UI.)

- **Did:** Full `refine-ticket` pass within a `refinement-session`.
  - **ARCH:** found that the ticket could not be implemented as written — it required a health endpoint and a protected endpoint while `ENGINEERING.md` forbids any endpoint absent from the specification, which T-0002 delivers and which depends on this ticket. Circular. Escalated live to the maintainer; **ADR-0005 accepted**, exempting operational endpoints. AC2 survives on that basis.
  - **Sizing:** failed the DoR guideline (≤ 2–3 focused days) — Compose plus PostgreSQL plus Duende plus migrations plus health checks. **Split along the identity seam:** the Duende host and all authentication moved to [T-0010](T-0010-duende-identity-host.md), taking the old AC3 (token round-trip) with them. What remains is the runnable core.
  - **BA/QA:** rewrote acceptance criteria for verifiability. The old AC4 asserted the API "does not itself run migrations", which no QA persona could check — replaced by AC5, which is observable by running the API without the migration step. Added AC3 (health must actually fail when the database is down — a health check that always returns 200 is the classic defect), AC6 (restart is non-destructive), AC7 (slow-database startup), and a counter-example forbidding startup migration.
  - **ENG:** added pointers on health-condition `depends_on` and the migration step as a short-lived service.
  - **SEC:** AC8 sharpened to cover history, not just the working tree.
- **Decided:** DoD item 3 cannot be met in isolation — the harness (T-0003) depends on this ticket. Recorded the two routes in Testing Notes rather than pretending the gap does not exist, and added a covering criterion to T-0003. **This is a PO decision at completion time, deliberately not pre-empted here.**
- **Remaining:** Implementation. T-0010 needs its own refinement before it is plannable.
- **Open questions / blockers:** none. Nothing blocks implementation starting.
- **Branch / PR:** n/a
- **Test state:** n/a — not started. Verification will be manual per Testing Notes.
- **DoR verdict:** **ready** — all universal items hold, conditionals addressed.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — implementation plan

Claimed via `pick-up-ticket` under `run-sprint`. Dependencies: none (verified — `depends_on: []`). DoR re-checked and still holds; nothing has drifted since refinement earlier today.

**Toolchain probe (before planning, in a scratch directory):** .NET 10 SDK `10.0.300` builds `net10.0`; NuGet is reachable; `Npgsql.EntityFrameworkCore.PostgreSQL` resolves at **10.0.3**. No blockers.

**Conflict found and resolved before coding (WoW §3):** the stock `dotnet new webapi` template adds `Microsoft.AspNetCore.OpenApi` — *code-first* OpenAPI document generation. That is the exact approach [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) rejected ("it inverts the required direction"). It also triggers `NU1903` (a known high-severity advisory in `Microsoft.OpenApi` 2.0.0). **The package will not be included**, and the project is created with `--use-controllers` per [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md). Recording rather than silently deleting it, because the next person running `dotnet new` will hit the same default.

**Approach**

- `GotIssues.sln` at the repository root; `apps/GotIssues.Api/` — ASP.NET Core 10, controller-based, no code-first OpenAPI package.
- `Data/GotIssuesDbContext.cs` with a single **placeholder** entity. Scope says the mechanism is the deliverable, not the domain.
- **Migration step:** the same API image invoked with an explicit `--migrate` argument as its own short-lived Compose service, which applies migrations and exits zero. Normal startup does not migrate, satisfying AC5.
  - *Alternative considered:* a separate `GotIssues.Migrator` project sharing a `libs/GotIssues.Persistence` library. Rejected for now — it invents a module boundary for a placeholder schema, and `ARCHITECTURE.md` currently reserves `libs/` for generated contracts. **If a second consumer appears (T-0003's harness may want the `DbContext`), extract the library then**, when two real consumers justify it per [ENGINEERING.md](../../standards/ENGINEERING.md).
- **Health:** a custom check calling `Database.CanConnectAsync()` rather than pulling the EF health-check package — fewer dependencies, and it makes AC3 (unhealthy when the database is down) obviously true. `/health` is implemented directly and **not** specified, per [ADR-0005](../../architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md).
- **Compose:** `postgres` with a `pg_isready` health check; `migrator` gated on `service_healthy`; `api` gated on `service_healthy` **and** the migrator's `service_completed_successfully`. This is what makes AC7 true by construction rather than by retry loops.
- `.env.example` with placeholder credentials; `infra/` for any supporting files; README *Getting started* rewritten so AC9 holds.

**Test plan** — the harness does not exist yet (T-0003 depends on this ticket), so every criterion is verified by hand and the evidence recorded here, per the ticket's Testing Notes.

| AC | How verified |
| --- | --- |
| AC1 | `docker compose up -d` on a clean clone + empty volume; `docker compose ps` shows all services healthy |
| AC2 | `curl -i localhost:PORT/health` → 200, body reports the database reachable |
| AC3 | `docker compose stop postgres`, re-`curl` → non-200 |
| AC4 | Fresh volume; confirm the migrator applied the schema (`\dt` in psql) and the API is healthy after |
| AC5 | With the schema dropped, run the `api` service **alone**; confirm no tables appear |
| AC6 | `docker compose down && up` against the existing volume; migrator exits zero as a no-op, data intact |
| AC7 | Start with the database deliberately delayed; confirm the API waits rather than exiting |
| AC8 | `git log -p` / grep for credentials; confirm all come from environment variables |
| AC9 | Follow the README literally from a clean clone |

**Risks carried into implementation:** AC5 is the one an implementer breaks by habit (`Database.Migrate()` on startup is the convenient path) — the `--migrate` design exists specifically to make that impossible without noticing. AC7's correctness depends on Compose conditions, not on application retry logic.

**Branch / PR:** `t-0001-runnable-compose-stack`, in its own worktree per [GIT.md](../../standards/GIT.md).

### 2026-08-30 — Product Owner decision, transcribed by claude-sm-9d4e

**Escalation resolved.** The maintainer (human PO), asked to choose between committing a default credential, amending AC1, or switching PostgreSQL to trust authentication, answered:

> "amend AC1"

Recorded per [WoW §13](../../governance/WAY_OF_WORKING.md): the decision is written into the repository before being acted on.

**Applied.** AC1 now requires the stack to come up healthy after the README's documented setup — copying `.env.example` to `.env` and supplying local values — rather than after `docker compose up` alone. Nothing else about the criterion changed: it is still a clean clone, still an empty volume, still no further manual steps beyond the documented ones, still verified by `docker compose ps`.

**Why this is the right shape rather than a convenience.** The ticket's In Scope already mandated `.env.example`, which only makes sense if something copies it; the original wording overreached rather than describing a different system. The alternatives each traded a real property for wording: committing a default password breaches [SECURITY.md](../../standards/SECURITY.md)'s unconditional rule, and trust authentication would have lowered a security posture to satisfy a sentence.

**Note for a future ticket, not built here:** someone who skips the copy gets PostgreSQL's own message — *"Database is uninitialized and superuser password is not specified"* — which is accurate but says nothing about `.env.example`. A friendlier failure would be an improvement; adding it now would be inventing requirements the PO did not ask for.

**Status:** unblocked, back to `in-progress`. Both of the review's blocking findings are now resolved — B2 fixed in code, B1 by this decision — so the branch goes back to `claude-rev-2c8d` for re-review before merge.
