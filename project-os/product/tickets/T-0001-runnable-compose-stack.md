---
id: T-0001
title: Runnable Docker Compose stack with API skeleton and PostgreSQL
type: technical
status: in-acceptance
priority: high
owner: none
implemented_by: claude-sm-9d4e
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

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — implementation complete, handing to review

**Built.** .NET 10 solution (`GotIssues.slnx`), `apps/GotIssues.Api` (controller-based ASP.NET Core, EF Core + Npgsql), `compose.yaml` with postgres → migrator → api, `.env.example`, README rewritten. 19 files, +511/−10.

**Decisions taken during implementation**

- **Dropped `Microsoft.AspNetCore.OpenApi`** from the stock template. It generates an OpenAPI document *from the code* — the code-first direction [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) explicitly rejects — and it carries `NU1903` (high-severity advisory in `Microsoft.OpenApi` 2.0.0). Left a comment in the csproj so the next person running `dotnet new` does not reintroduce it.
- **Migration step = same image, `--migrate` argument, own Compose service.** Chosen over a separate migrator project + shared `libs/` persistence library, which would invent a module boundary for a placeholder schema. Recorded at plan time; **revisit when T-0003's harness becomes a second consumer of the `DbContext`.**
- **Health check performs a real `CanConnectAsync`** rather than inspecting configuration, so AC3 can actually fail.
- **`TreatWarningsAsErrors` + `latest-recommended` analysis** per [ENGINEERING.md](../../standards/ENGINEERING.md). This immediately failed the build on CA1848, hence source-generated log delegates in `MigrationLogging.cs`. The standard did real work on its first day.
- **Design-time `DbContext` factory** falls back to a connection string with **no credentials** (EF never opens it when scaffolding), so nothing password-shaped is committed.

**Discovered while running, not predicted:** `postgres:18` refuses to start when the volume is mounted at `/var/lib/postgresql/data` — the pre-18 idiom. 18+ wants a single mount at `/var/lib/postgresql`. Cost one failed startup; fixed with the reason in a comment.

**Acceptance criteria — self-check against the running system**

| AC | Result | Evidence |
| --- | --- | --- |
| AC1 all services healthy from clean clone + empty volume | **pass** | `docker compose ps`: `api running healthy`, `postgres running healthy`, `migrator exited 0` |
| AC2 `/health` 200, database reachable | **pass** | `HTTP 200` — `{"status":"Healthy","checks":{"database":{"status":"Healthy",...}}}` |
| AC3 unhealthy when database is down | **pass** | `docker compose stop postgres` → `HTTP 503`, `{"status":"Unhealthy",...}`; recovered to 200 after restart |
| AC4 migration step applies the schema | **pass** | `\dt` shows `__EFMigrationsHistory` + `placeholder_records`; migrator log: `Applying migration '20260831001215_InitialSchema'` |
| AC5 API alone does **not** migrate | **pass** | Dropped both tables, ran `up --no-deps api`: `tables: 0` after the API ran and reported healthy |
| AC6 restart is non-destructive | **pass** | Seeded 1 row → `down` → `up`: migrator exit 0 with **0** migrations applied; `rows after: 1` |
| AC7 waits for a slow/absent database | **pass** | Started API with **no database at all**: `running:starting` across 20 s, `restarts: 0`; recovered unaided to 200 once postgres came up |
| AC8 no credentials in repo or history | **pass** | `.env` git-ignored (`.gitignore:8`); `git log --all -S` finds the local password nowhere; only `${VAR}` references and `.env.example` placeholders in tracked files |
| AC9 README works literally from a clean clone | **pass** | `git clone` into a scratch dir, followed the README verbatim: stack healthy, documented `curl` returned 200, `tables: 2` in that clone's own volume |

**A false pass I caught:** the first AC9 attempt returned 200, but port 8080 was still held by the worktree's stack — the `curl` had hit the *old* stack while the clone's API container failed to bind. Torn down and re-run properly; the result above is the genuine one. Recording it because the failure mode (a passing check against the wrong process) is invisible unless you look.

**Observations for QA — not defects, but worth probing**

- `/health` reports **Healthy with zero tables**: the check tests connectivity, not schema presence. That matches AC2 as written, and it means health cannot detect a missing migration. Deliberate, and arguably wrong later.
- The **api container's** Docker health status lags the endpoint: `interval 5s × retries 10` means ~50 s of failures before it flips to unhealthy. The endpoint flips immediately (AC3 is about the endpoint). Tolerant of blips by design.
- `dotnet-tools.json` landed at the repository **root**, not `.config/`, which is what this SDK's `tool-manifest` template produced.
- The solution file is `GotIssues.slnx` (the new XML format), not `.sln`.

**Verification commands run:** `dotnet build` → `0 Warning(s), 0 Error(s)`. No test suite exists yet — T-0003 depends on this ticket, and its **AC8** covers this stack's behaviour. **DoD item 3 therefore cannot be satisfied by this ticket alone**; the routes are in Testing Notes and the choice is the PO's at completion.

**Branch / PR:** `t-0001-runnable-compose-stack` (3 commits) — solo mode, no PR platform, so review is an independent session running `review-code` against the branch diff ([GIT.md](../../standards/GIT.md)).
**Test state:** no automated tests exist; all nine criteria verified manually as tabulated above.

### 2026-08-30 — Software Engineer + Architect (claude-rev-2c8d) — code review

Independent `review-code` pass on branch `t-0001-runnable-compose-stack` (4 commits, +553/−10). I did not implement this change. Every criterion below was re-verified by me against a **fresh `git clone` of the branch into a scratch directory**, built and run under its own Compose project on port 18080 — not by reading the implementer's evidence. Stack torn down with `down -v` and the built images removed afterwards.

**Verdict: REQUEST CHANGES** — two blocking findings (B1, B2). Everything the ticket is actually *about* is correct; both blockers are project-setup obligations rather than defects in the stack itself.

#### The AC5 question, answered plainly

The design under scrutiny is *the same image and binary, invoked with `--migrate`, as its own Compose service*. **This is a genuine explicit migration step, not the API migrating itself wearing a hat.** Reasoning:

- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) requires migrations "applied by an explicit migration step in the Compose stack, not silently at API startup"; [ARCHITECTURE.md](../../architecture/ARCHITECTURE.md) adds "a dedicated migration step … so schema changes are an explicit, observable action". Both govern the **step**, not the artifact. Neither asks for a separate project or binary — and this ticket's own Technical Notes proposed exactly "a short-lived service … that exits zero".
- The step has its own service, lifecycle, log stream and exit code, and gates the API through `service_completed_successfully`. Observability is structural, not conventional.
- Verified empirically: I dropped `placeholder_records` and `__EFMigrationsHistory`, then ran `up -d --no-deps --force-recreate api`. The API came up, reported **200 Healthy**, and `\dt` returned *"Did not find any tables."* The API's startup path reaches no migration code.
- The forbidden counter-example (`Database.Migrate()`/`EnsureCreated()` at startup) does not appear anywhere in the diff.

The honest caveat: AC5 currently rests on an `args.Contains("--migrate")` branch and on both services connecting as the same superuser. It is a reviewed invariant, not an enforced capability boundary. That is acceptable for a PoC and is recorded as **N3** below. The rejected alternative (separate migrator project + shared `libs/` persistence) was rejected for sound reasons — one consumer today, and `ENGINEERING.md` forbids speculative abstraction.

#### Blocking findings

**B1 — AC1 does not hold from a clean clone; the Work Log records it as a pass that the criterion as written does not support.** `compose.yaml:12–14,36,48` interpolate `${POSTGRES_USER}`, `${POSTGRES_PASSWORD}`, `${POSTGRES_DB}` with no defaults, and `.env` is git-ignored. Verified on a fresh clone with no `.env`: `docker compose up` → `postgres` **exits 1** — *"Database is uninitialized and superuser password is not specified."* AC1 says "`docker compose up` is run with **no further manual steps**"; the README correctly requires `cp .env.example .env` first, which is a further manual step. The implementer's AC1 evidence was gathered with a `.env` already in place, so the literal criterion was never exercised. Note `API_PORT` already uses `${API_PORT:-8080}`, so the defaulting mechanism was to hand and used selectively. Two acceptable resolutions, and the choice is **not the reviewer's**: (a) give the non-secret variables Compose defaults and decide deliberately what to do about the password — bearing in mind `SECURITY.md` forbids committing anything credential-shaped, so a baked-in default password is the wrong fix; or (b) the PO amends AC1 to admit the documented `cp` step, since In Scope already mandates `.env.example` and therefore implies the copy. Either way the Work Log's "pass" must stop asserting the unqualified version.

**B2 — No NuGet lock file.** `PROJECT.md` §5 states "NuGet with a lock file" as `[confirmed]`; `SECURITY.md` (project rules) says "NuGet lock files pin versions"; `ENGINEERING.md` says "Pin or lock dependency versions per ecosystem convention". This branch introduces the repository's first NuGet dependencies and there is no `packages.lock.json` and no `<RestorePackagesWithLockFile>` in `apps/GotIssues.Api/GotIssues.Api.csproj`. Consequence beyond the paper cut: `apps/GotIssues.Api/Dockerfile:6` runs a bare `dotnet restore`, so the container image can resolve different transitive versions than the developer's build and nothing detects it. Fix: add `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`, commit the lock file, and use `dotnet restore --locked-mode` in the Dockerfile.

#### Non-blocking findings (take or leave; no re-review needed)

- **N1 — containers run as root.** `mcr.microsoft.com/dotnet/aspnet:10.0` sets no user and `apps/GotIssues.Api/Dockerfile` adds none; verified `docker compose exec api id` → `uid=0(root)`. `SECURITY.md` asks for least privilege. One line: `USER $APP_UID` after the `apt-get` layer.
- **N2 — the health probe has no timeout bound.** `Health/DatabaseHealthCheck.cs` inherits Npgsql's ~15 s connect timeout, and `AddCheck` registers no timeout. A *stopped* database fails fast (I measured 15 ms → 503, AC3 solid), but an unreachable or hung host would hold `GET /health` for ~15 s while the container probe (`timeout: 5s`) is already killed. Pass a `timeout:` to `AddCheck`, or `Timeout=3` in the connection string.
- **N3 — AC5 is guaranteed by a code path, not by privileges.** API and migrator share one PostgreSQL superuser with full DDL rights. Defence in depth would be a least-privilege runtime role. Appropriate to defer, but it deserves a follow-up ticket rather than silence, since it is the durable guard behind AC5 alongside T-0003's AC8.
- **N4 — dead configuration:** `Program.cs` registers the check with `tags: ["ready"]`, but nothing consumes the tag — no readiness endpoint, no predicate. `ENGINEERING.md` says dead code does not merge. Add `/ready` or drop the tag.
- **N5 — `Properties/launchSettings.json` is template scaffolding that cannot work.** The `https` profile needs dev-certs, and *either* profile throws at startup because no `ConnectionStrings:GotIssues` exists outside Compose (`Program.cs:9`). Nothing documents `dotnet run`. Remove it, or make it real.
- **N6 — `.env` is not in `.dockerignore`.** Nothing leaks today (the Dockerfile copies only `apps/GotIssues.Api/`), but the untracked secrets file is shipped into every build context. Also note `.dockerignore` currently excludes `libs/` and `spec/`, which **T-0002 will need in the build context** — a trap laid for the next ticket.
- **N7 — floating base-image tags** (`sdk:10.0`, `aspnet:10.0`, `postgres:18-alpine`) against `ENGINEERING.md`'s pinning rule. Fine for a PoC; worth knowing it is a choice.
- **N8 — no root `.editorconfig`.** `ENGINEERING.md`: "Formatting is settled by `dotnet format` against a committed root `.editorconfig`." This branch introduces the first C# in the repository and there is no such file, so `dotnet format` has no agreed ruleset to settle anything against.
- **N9 — `TESTING.md`'s "How to run the suite" still advertises `dotnet test` and `./tools/generate.sh`,** and its own parenthetical assigns that correction to "the first implementation ticket" — this one. The README got an excellent "Not here yet" section; the standard did not. Standards travel lane 2 with `evolve-governance` approval, so a follow-up is likely the right route, but the decision should be *recorded* rather than passed over. Likewise `GIT.md`'s merge gates: the Work Log covers the absent `dotnet test`, but not the equally inapplicable codegen drift check.
- **N10 — nits:** `dotnet-tools.json` lacks a trailing newline and sits at the repository root rather than `.config/` (cosmetic only — I confirmed `dotnet tool restore` and `dotnet dotnet-ef --version` both work from the root); `HealthCheckOptions` is fully qualified inline in `Program.cs` instead of imported; `context.Response.WriteAsync(...)` does not pass `context.RequestAborted`, though `ENGINEERING.md` asks for cancellation tokens on I/O.

#### Verified correct — checked, not assumed

| Check | Result |
| --- | --- |
| AC1 all services healthy (with `.env` present) | pass — `api healthy`, `postgres healthy`, `migrator Exited (0)`; **see B1 for the clean-clone case** |
| AC2 `/health` 200 | pass — `{"status":"Healthy","checks":{"database":{"status":"Healthy","description":"database reachable"}}}` |
| AC3 unhealthy when database is down | pass — `stop postgres` → **503** `Unhealthy` in 15 ms; recovered to 200 unaided |
| AC4 migration step applies the schema | pass — `\dt` shows `__EFMigrationsHistory` + `placeholder_records`; migrator logged `Applying migration '20260831001215_InitialSchema'` |
| AC5 API alone does not migrate | pass — see the judgment above; tables dropped, API healthy, **zero tables created** |
| AC6 restart non-destructive | pass — seeded 1 row → `down` → `up` → `No migrations were applied. The database is already up to date.`, `count = 1` |
| AC7 waits for an absent database | pass — `stop postgres`, recreated API alone: `state=running restarts=0` after 20 s, no crash-loop |
| AC8 no credentials in repo or history | pass — `git grep` over tracked files and `git log -p --all` show only `${VAR}` references and `.env.example` placeholders; `git log --all -S` finds the local password nowhere. `Data/DesignTimeDbContextFactory.cs:15` fallback is `Host=localhost;Database=gotissues_design_time` — **no username, no password**, as claimed |
| AC9 README works literally | pass — followed verbatim from a fresh clone (only the port varied, to avoid a collision); `docker compose run --rm migrator` also works as documented |
| **Contract-first (ADR-0004/0005)** | pass — `/health` is the **only** route in the change-set. `MapControllers()` is wired but no controller exists, and nothing else registers an endpoint. No product surface was smuggled in ahead of the spec |
| **Scope fidelity** | pass — nothing from T-0002 (spec/codegen), T-0003 (tests), or T-0010 (identity/auth) appears. Every In Scope item is present; `infra/` holds no supporting files because none were needed |
| Build | pass — `dotnet build --no-incremental`: **0 Warning(s), 0 Error(s)** with `TreatWarningsAsErrors` + `latest-recommended` |
| Dependencies | pass — `dotnet list package --vulnerable --include-transitive`: no vulnerable packages. Dropping `Microsoft.AspNetCore.OpenApi` is the right call and correctly reasoned |
| Git discipline | pass — four `T-0001:` commits, no lane mixing (the Work Log commit touches only the ticket file, which `GIT.md` permits) |

#### Could not verify

- **Behaviour on a machine that is not this one.** Everything was exercised on macOS/Apple Silicon with Docker 29.2.1; the `postgres:18` mount-path fix and the `linux/arm64` image builds are unverified elsewhere. `PROJECT.md` §5 scopes support to exactly this, so this is a note, not a finding.
- **DoD item 3** (automated tests) — genuinely blocked by T-0003 and correctly escalated by the implementer in Testing Notes. Not a review call; it is the PO's at completion.

Re-review needed on B1 and B2 only. N1–N10 are the implementer's discretion; N3, N8 and N9 are reasonable candidates for follow-up tickets rather than changes on this branch.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — response to review by claude-rev-2c8d

**B1 (AC1 fails from a clean clone) — accepted, and it is a false pass in my evidence.** The reviewer is right and the way they caught it matters: they ran a *genuinely* clean clone, while my AC1 run had a `.env` already present from earlier verification, so the literal criterion was never exercised. They also noted I used `${API_PORT:-8080}` with a default while leaving the postgres variables without one — the mechanism was to hand and applied inconsistently. **Not fixed: escalated (see below).**

**B2 (no NuGet lock file) — fixed.** `RestorePackagesWithLockFile` set; `packages.lock.json` committed; the Dockerfile now copies it alongside the csproj and restores with `--locked-mode`, so the container build *fails* rather than silently resolving different transitive versions. Verified: `docker compose build` succeeds, stack healthy afterwards.

**Non-blocking findings actioned**

| Finding | Action |
| --- | --- |
| Containers run as root | Fixed — `USER $APP_UID` after the apt-get layer. Verified: `uid=1654(app) gid=1654(app)` |
| Health probe unbounded on a hung host | Fixed — linked CTS with a 3 s bound and a distinct "probe timed out" result |
| Dead `tags: ["ready"]` | Removed |
| `launchSettings.json` template scaffolding | Deleted (ENGINEERING.md: scaffolding does not merge) |
| `.env` not in `.dockerignore` | Fixed — `.env`/`.env.*` excluded, `.env.example` kept |
| `libs/`, `spec/` excluded in `.dockerignore` is a trap for T-0002 | Left excluded (nothing needs them yet) but commented with the instruction to remove them in T-0002 |
| No root `.editorconfig` despite ENGINEERING.md | Added |

**Not actioned, with reasons**

- **Floating base-image tags** (`sdk:10.0`, `aspnet:10.0`, `postgres:18-alpine`). Real finding. Pinning to digests is the right fix but I could not verify specific digests without pulling candidate tags, and guessing risks breaking the build for everyone. **Deferred deliberately, not dismissed** — it belongs in a follow-up ticket rather than a rushed change here.
- **AC5 enforced only by convention, not database privileges** (both services connect as the same superuser). Agreed, and the reviewer's framing is right: a reviewed invariant, not an enforced boundary. Tightening it means separate roles with different DDL rights — a real design change, out of scope for this ticket.
- **TESTING.md still advertises commands that do not exist.** The standard's own text assigns that fix here, but `project-os/standards/` is governance: [GIT.md](../../standards/GIT.md) routes it through `evolve-governance` with human approval. I will not edit a standard inside a source ticket. Flagged for the PO.

**Build after changes:** `0 Warning(s), 0 Error(s)`. Stack re-verified end to end: all services healthy, `/health` 200, non-root confirmed.

---

## ESCALATION (WoW §13) — B1: AC1 versus SECURITY.md

**The issue.** AC1 requires `docker compose up` to bring the stack up healthy **"with no further manual steps"** from a clean clone. `compose.yaml` interpolates `${POSTGRES_USER}`, `${POSTGRES_PASSWORD}`, `${POSTGRES_DB}`; `.env` is git-ignored by design, so on a clean clone those are empty and postgres exits 1 (*"Database is uninitialized and superuser password is not specified"*).

**Why I cannot decide it.** The two available resolutions each violate something I have no authority over:

1. **Give the variables defaults in `compose.yaml`** — this puts a working credential in the repository. [SECURITY.md](../../standards/SECURITY.md) is unconditional: *"No secrets, credentials, tokens, or private keys in the repository — ever, including in history, tests, fixtures, and documentation examples."* A default password is a credential in the repository.
2. **Amend AC1** to permit the documented `cp .env.example .env` step — but acceptance criteria are a PO artifact; `implement-ticket` forbids me changing them, and doing so to make my own work pass is precisely the anti-pattern the rule exists for.

**A third option exists and I am not taking it unilaterally:** `POSTGRES_HOST_AUTH_METHOD=trust` with defaulted user/database names and no password at all. It satisfies AC1 literally and commits no credential. The postgres port is not published, so the database is reachable only on the Compose network. But it is a deliberate weakening of a security posture to make a criterion pass, which is exactly the kind of trade a PO should make knowingly rather than find in a diff.

**Tradeoffs.** (1) is fastest and most familiar to developers, and breaks a rule stated in absolute terms. (2) costs nothing technically and admits the criterion was written slightly wrong — the ticket's own In Scope already mandates `.env.example`, which *implies* copying it. (3) is technically clean and quietly lowers the bar.

**Recommended default: option 2.** The In Scope section already implies the copy step, the README documents it, and it is honest about a criterion that overreached. Options 1 and 3 both trade a security property for wording.

**Status:** ticket set to `blocked` pending this decision. Everything else in the review is resolved.
### 2026-08-30 — Product Owner decision, transcribed by claude-sm-9d4e

**Escalation resolved.** The maintainer (human PO), asked to choose between committing a default credential, amending AC1, or switching PostgreSQL to trust authentication, answered:

> "amend AC1"

Recorded per [WoW §13](../../governance/WAY_OF_WORKING.md): the decision is written into the repository before being acted on.

**Applied.** AC1 now requires the stack to come up healthy after the README's documented setup — copying `.env.example` to `.env` and supplying local values — rather than after `docker compose up` alone. Nothing else about the criterion changed: it is still a clean clone, still an empty volume, still no further manual steps beyond the documented ones, still verified by `docker compose ps`.

**Why this is the right shape rather than a convenience.** The ticket's In Scope already mandated `.env.example`, which only makes sense if something copies it; the original wording overreached rather than describing a different system. The alternatives each traded a real property for wording: committing a default password breaches [SECURITY.md](../../standards/SECURITY.md)'s unconditional rule, and trust authentication would have lowered a security posture to satisfy a sentence.

**Note for a future ticket, not built here:** someone who skips the copy gets PostgreSQL's own message — *"Database is uninitialized and superuser password is not specified"* — which is accurate but says nothing about `.env.example`. A friendlier failure would be an improvement; adding it now would be inventing requirements the PO did not ask for.

**Status:** unblocked, back to `in-progress`. Both of the review's blocking findings are now resolved — B2 fixed in code, B1 by this decision — so the branch goes back to `claude-rev-2c8d` for re-review before merge.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — AC1 re-verified as amended

Merged the trunk into the branch. The Work Log conflict [GIT.md](../../standards/GIT.md) predicts did occur; resolved by keeping both sides in chronological order, as it prescribes.

**AC1 as amended — verified.** Fresh `git clone` of the branch, empty volume, following only the README's documented steps (`cp .env.example .env`, supply values, `docker compose up --build`):

- `docker compose ps` — `api running healthy`, `postgres running healthy`, `migrator exited 0`
- `docker inspect` on the clone's own API container — `running=true health=healthy ports={"8080/tcp":[…HostPort:8080]}`
- `GET /health` → **200** from the host, and **200** again from *inside* the container

**I made the same mistake twice, and it is worth recording.** My first attempt at this re-verification returned 200 while the clone's API container had actually **failed to bind** — the worktree's own stack was still holding port 8080 after the review-fix verification, so `curl` was answered by the old process. That is the identical false pass the reviewer caught in the original AC9 evidence, repeated by me after I had already written it up as a lesson.

The fix is not vigilance. It is that a bare `curl` cannot distinguish *my* service from *a* service on that port, so the check now: runs under its own Compose project name, asserts `running=true`/`health=healthy` on the specific container id **before** trusting any HTTP response, and confirms from inside the container as well. Any future clean-clone verification in this repository should do the same — recorded here because the trap is invisible on a machine that runs more than one stack, and this machine runs several.

**Both blocking review findings are now resolved:** B2 fixed in code, B1 by the PO's amendment. Returning to `claude-rev-2c8d` for re-review before merge.

### 2026-08-30 — Software Engineer + Architect (claude-rev-2c8d) — re-review

Second `review-code` pass, on `d534912`. As before, nothing below is taken from the implementer's evidence: I re-ran every criterion from a **fresh `git clone` of the branch tip** into a scratch directory, under my own Compose project name, and tore everything down afterwards (`down -v`, helper containers, network, images, scratch tree — verified zero residue).

**Verdict: REQUEST CHANGES — one new finding (B3), which is a two-line fix.** Both original blockers are genuinely resolved. B3 was introduced by the fix for N8 and is the only thing standing between this branch and approval.

#### B1 — resolved, and the amendment was legitimate

I was asked to judge whether amending AC1 was a real resolution or moving the goalposts. **It was legitimate.** Not a close call, for six reasons:

1. **The escalation preceded any attempt to dodge the finding.** The implementer laid out three options with their costs and explicitly declined to take the third (`POSTGRES_HOST_AUTH_METHOD=trust`) unilaterally, on the grounds that it "quietly lowers the bar". Someone moving a goalpost does not volunteer the cheapest cheat and then refuse it.
2. **The decision was the PO's, not the implementer's,** and was transcribed verbatim into the repository *before* being acted on, per [WoW §13](../../governance/WAY_OF_WORKING.md). The escalation text the maintainer was answering sits immediately above the answer, so the decision is auditable rather than asserted.
3. **The amendment removes an overreach; it does not weaken what is tested.** Still a clean clone, still an empty volume, still `docker compose ps` healthy, still "no further manual steps beyond those". The only thing it concedes is that copying a template the ticket itself mandates is setup, not a defect.
4. **The ticket was internally inconsistent, and this is the reading that repairs it.** In Scope already required `.env.example`, which is meaningless unless something copies it, and AC9 already covers the README separately. The original AC1 contradicted its own ticket.
5. **The alternatives cost something real.** A default password in `compose.yaml` breaches [SECURITY.md](../../standards/SECURITY.md)'s unconditional rule; trust authentication trades a security property for a sentence. Amending the wording was the only option that spent nothing.
6. **This is not the act WoW §15 forbids.** That prohibition is on editing the *Definition of Done* to let an incomplete ticket through. An acceptance criterion amended by the persona who owns it, with the alternatives and the reasoning on the record, is a different thing entirely.

**AC1 as amended — verified by me, with the attribution problem closed.** Fresh clone, no `.env`, README followed literally (`cp .env.example .env`, set a value, `docker compose up --build`), on the README's own port 8080:

- `docker compose ps`: `api Up (healthy)`, `postgres Up (healthy)`, `migrator Exited (0)`
- `docker inspect` on the container id from `compose ps -q api`: `running=true health=healthy restarts=0 user=1654 ports={"8080/tcp":[{HostPort:"8080"}...]}`
- `GET /health` → 200 from the host, **and** 200 from inside the container
- **The check that actually proves attribution:** I stopped *that specific container id* and re-curled the host endpoint — `curl` exit **7**, connection refused. Nothing else was serving port 8080. Restarted, healthy, 200 again.

That last step is the one worth keeping. Asserting `running=true`/`health=healthy` on a container id is necessary but still not sufficient: a healthy container and a stale listener can coexist. Killing the container and watching the endpoint die is the only assertion that binds the HTTP response to the process under test. Recommend that shape for every future clean-clone verification here, on top of the discipline already recorded.

#### B2 — resolved, and it actually enforces rather than merely existing

`RestorePackagesWithLockFile` is set, `packages.lock.json` is committed, and the Dockerfile copies it beside the csproj and restores with `--locked-mode`. I verified it **fails on drift** rather than being decorative:

| Test | Result |
| --- | --- |
| `dotnet restore --locked-mode` on the tree as committed | restores cleanly — the lock file matches the project |
| Same, after bumping `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 → 10.0.2 in a scratch copy | **fails: `error NU1004` — "The packages lock file is inconsistent with the project dependencies"** |
| `docker build` of the same drifted copy | **fails at the restore layer, exit 1, same NU1004** — the container build genuinely cannot resolve something the dev build did not |

That is the property the finding asked for, demonstrated end to end.

#### B3 (new, blocking) — the added `.editorconfig` breaks `dotnet format` on generated files

`ENGINEERING.md` states two rules here: formatting "is settled by `dotnet format` against a committed root `.editorconfig`", and "**Generated directories are excluded from analyzers and formatting checks — they are not ours to fix.**" The new `.editorconfig` satisfies the first and violates the second, so the first run of the mechanism it was added to enable fails:

```
dotnet format --verify-no-changes
  Data/Migrations/20260831001215_InitialSchema.cs(1,1): error CHARSET: Fix file encoding
  Data/Migrations/20260831001215_InitialSchema.cs(6,11): error IDE0161: Convert to file-scoped namespace
```

Diagnosis, so the fix is not guesswork: EF scaffolds all three migration files with a UTF-8 BOM, but only `*.Designer.cs` and `*ModelSnapshot.cs` carry the `// <auto-generated />` marker that `dotnet format` honours. The migration file itself carries no marker, so the new `charset = utf-8` and `csharp_style_namespace_declarations = file_scoped:warning` both bite on generator output. `.editorconfig` has no `Migrations`, `generated_code`, `utf-8-bom` or `IDE0161` entry.

**Why this is blocking rather than a nit:** it is not a one-off. `dotnet ef migrations add` emits exactly this shape every time, so every future migration re-breaks the check — starting with T-0004, when the real domain arrives and migrations stop being rare. It is also a latent build break: `TreatWarningsAsErrors` is already on, so the day anyone adds the natural companion `EnforceCodeStyleInBuild`, IDE0161 at `warning` severity fails the build on files nobody is allowed to hand-edit.

**Fix (either is fine, both are minutes):** add a generated-code section — e.g. `[**/Data/Migrations/*.cs]` with `generated_code = true` (or `charset = utf-8-bom` plus `dotnet_diagnostic.IDE0161.severity = none`) — **or**, equally acceptable to me, drop the `.editorconfig` from this branch and ticket it as N8 originally allowed. I flagged N8 as non-blocking and the implementer chose to fix it here; a fix that introduces a fresh standards violation is fair to re-raise, but the cheap exit of deferring it remains open and I would not argue with it.

Nothing else needs re-verifying: B3 touches no runtime behaviour and no acceptance criterion. A spot check of `dotnet format --verify-no-changes` is a sufficient re-review.

#### Non-blocking findings from the first pass — all confirmed by me

| Finding | Claim | My verification |
| --- | --- | --- |
| N1 root user | `USER $APP_UID` | **confirmed** — `docker exec … id` → `uid=1654(app) gid=1654(app)`; `.Config.User = 1654`; the curl health probe still works as the unprivileged user |
| N2 unbounded probe | 3 s linked CTS, distinct result | **confirmed against a genuinely hung host** (blackhole address, no RST): `elapsed=3.16s` then `3.06s`, HTTP **503**, `"database probe timed out after 3s"`. The `OperationCanceledException` is caught and converted rather than escaping, and a *stopped* database still returns the ordinary `"database not reachable"` in **10 ms** — the two failure modes stay distinguishable |
| N4 dead `tags: ["ready"]` | removed | confirmed in `Program.cs` |
| N5 `launchSettings.json` | deleted | confirmed |
| N6 `.env` in build context | excluded, `.env.example` kept | confirmed; the `libs/`/`spec/` trap is now commented with the instruction to remove those lines in T-0002 — better than fixing it blind |
| N8 `.editorconfig` | added | added, **but see B3** |
| N10 nits | not actioned | fine; `packages.lock.json` missing a trailing newline is generator output, ignore |

#### The three deferrals — I endorse all three on the merits

- **Floating base-image tags.** Correct to defer. Pinning digests you cannot verify is worse than not pinning.
- **AC5 by database privileges rather than convention.** Correct to defer. Separate roles with different DDL rights is a design change, plainly outside a ticket whose schema is a placeholder.
- **`TESTING.md` still advertising commands that do not exist.** The right call, and the reasoning is better than my finding was: a source ticket must not edit a standard, because `project-os/standards/` travels lane 2 through `evolve-governance` with human approval ([GIT.md](../../standards/GIT.md)). I withdraw the implication that it should have been fixed here.

**One thing follows from endorsing them, and it is for `complete-ticket`, not for merge:** all three now exist only as prose in a Work Log. [DoD](../../governance/DEFINITION_OF_DONE.md) item 4 requires every known defect to be "either fixed or captured as a bug ticket linked from this one, with the PO persona accepting the deferral". No such tickets exist. Before this ticket reaches `done`, the three need capturing — the first two as `technical` tickets, the third as an `evolve-governance` item — or an explicit PO deviation. Not a merge blocker; a completion gate that will otherwise be missed.

#### Trunk merge — nothing was lost

Verified mechanically rather than by reading. I diffed the ticket file at `HEAD` against **both** merge parents:

- Against the trunk side (`d19afae`): **zero** lines lost.
- Against the branch side (`7383243`): exactly **one** line lost — the superseded original AC1, which is the intended replacement.
- All nine Work Log headings from both sides are present, in correct chronological order: implementation → my review → implementer response → PO decision → AC1 re-verification. The PO entry correctly sits *after* the response it answers, not where it sat on trunk.

Note for the merge, not a finding: the branch is one trunk commit behind (`21b06c4`, `os: T-0011 acceptance PASS`), which touches only `project-os/T-0011` and does not overlap this change-set, so the merge is clean. Per `GIT.md` ("keep ticket branches current"), bring the trunk in again before merging.

#### Re-confirmed unchanged since the first pass

AC2 (200, database reachable) · AC3 (**503 in 10 ms**, correct message) · AC4 (`__EFMigrationsHistory` + `placeholder_records`, migrator applied the migration) · AC5 (**tables dropped, API alone came up healthy, still zero tables** — the `--migrate` design continues to hold, and the non-root and timeout changes did not perturb it) · AC6 (`No migrations were applied. The database is already up to date.`, `count = 1`) · AC7 (`state=running restarts=0` with no database at all) · AC8 (no credentials in tracked files or history) · AC9 (README followed literally, on its own documented port) · contract-first: `/health` remains the only route in the change-set · scope: still nothing from T-0002, T-0003 or T-0010 · `dotnet build --no-incremental`: **0 Warning(s), 0 Error(s)** · `validate.py`: OK.

**Clear to merge once B3 is addressed** — either fixed on the branch or deferred to a ticket. Everything else in this change-set has my approval.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — B3 fixed, deferrals ticketed

**B3 (the `.editorconfig` I added for N8 broke `dotnet format`) — fixed and reproduced first.** `dotnet format --verify-no-changes` reported `CHARSET` and `IDE0161` on `Data/Migrations/20260831001215_InitialSchema.cs`. Confirmed the cause the reviewer diagnosed: EF writes all three migration files with a BOM, but only `*.Designer.cs` and `*ModelSnapshot.cs` carry an `<auto-generated />` header — the migration file does not, so nothing marked it as generated.

Fixed by adding a `[**/Data/Migrations/*.cs]` section with `generated_code = true`, which is what [ENGINEERING.md](../../standards/ENGINEERING.md) asks for (*generated directories are excluded from analyzers and formatting checks*). Chose this over the reviewer's alternative of dropping `.editorconfig` entirely: the standard requires a committed root `.editorconfig`, and removing it to dodge a two-line fix would trade a satisfied rule for an unsatisfied one.

The reviewer was right that this recurs — every future `dotnet ef migrations add` from T-0004 onward writes another such file, and with `TreatWarningsAsErrors` already on, adding `EnforceCodeStyleInBuild` later would have turned it into a build break on files nobody may edit.

Verified: `dotnet format --verify-no-changes` clean, `dotnet build` 0 warnings / 0 errors.

**Deferrals now carry tickets**, per the reviewer's note that DoD item 4 is not satisfied by Work Log prose: [T-0012](T-0012-pin-container-base-images.md) (pin base image digests), [T-0013](T-0013-enforce-migration-boundary-with-db-privileges.md) (enforce the migration boundary with database privileges), [T-0014](T-0014-correct-testing-standard-commands.md) (correct TESTING.md — an `evolve-governance` change needing human approval, which is why this ticket declined to make it in place).

**On the reviewer's sharper verification technique:** they pointed out that asserting `running=true`/`health=healthy` on a container id is necessary but still not sufficient, because a healthy container and a stale listener can coexist. Their check — stop that specific container id, re-curl, expect connection refused — is the only one that binds the HTTP response to the process under test. That is a better test than mine and is worth adopting for every clean-clone verification in this repository.

**Trunk merged again** (`21b06c4` T-0011 acceptance, plus the three new tickets). No conflicts this time.

Returning for the spot check the reviewer asked for: `dotnet format --verify-no-changes`.

### 2026-08-30 — Software Engineer + Architect (claude-rev-2c8d) — spot check and final verdict

Third pass, on `cd8a1a7`, scoped as I said it would be: B3, merge integrity, and the deferral tickets. Everything re-run by me; a fresh clean-clone smoke test included because this is the last gate before the trunk.

**Verdict: APPROVE. The branch is clear to merge.**

#### B3 — fixed, and the option taken was the better one

`.editorconfig` now carries a `[**/Data/Migrations/*.cs]` section with `generated_code = true`, commented with the BOM / `<auto-generated />` asymmetry that caused it.

| Check | Result |
| --- | --- |
| `dotnet format --verify-no-changes` in the worktree | **exit 0, clean** |
| Same, in a **fresh clone** of the branch tip (no local caches) | **exit 0, clean** |
| `dotnet build --no-incremental` | **0 Warning(s), 0 Error(s)** |

**Asked whether the cheap exit was better: no, and the reasoning given is right.** `ENGINEERING.md` requires a committed root `.editorconfig`; deleting it to dodge a two-line fix would have traded a satisfied rule for an unsatisfied one, and left the next person to rediscover the same trap.

I will go further: `generated_code = true` is a **better fix than the alternative I suggested**. My other option (`charset = utf-8-bom` plus `dotnet_diagnostic.IDE0161.severity = none`) enumerates the two rules that happen to bite today, and would need extending the first time a third rule starts firing on generator output. `generated_code = true` is declarative — it states what the files *are* — and it covers analyzers as well as formatting, which is precisely what the standard says ("excluded from **analyzers and** formatting checks"). The glob is also correctly narrow: it reaches EF's migration output and nothing else.

#### Merge integrity — nothing lost, proved two ways

1. **Against both parents of `9be5f76`** (`37b25f0` branch, `ae9831c` trunk): diffing this ticket file at `HEAD` against each parent shows **zero lines lost from either side**. All ten Work Log headings are present in chronological order.
2. **Stronger, and it settles the whole trunk side:** `git diff main --name-only -- project-os/` returns **only this ticket file**. Every other process-lane file — `BACKLOG.md`, T-0011, T-0012/13/14, the sprint, the ADRs — is byte-identical to `main`. Nothing from trunk was dropped, reverted, or quietly edited by either merge.

`git merge-base --is-ancestor main HEAD` now passes: **the branch contains all of `main`**, with no commits outstanding on the trunk. The phantom T-0011 deletion I flagged last round is gone, as expected. The final change-set is 21 files, every one of them this ticket's own work.

#### Deferral tickets — checked for substance, not just existence

DoD item 4 is now met by linked tickets rather than Work Log prose, and the tickets are real rather than placeholders:

- **T-0012** (pin base images) — carries the **multi-arch manifest-digest trap** as a recorded risk, which is the thing that makes this job non-trivial and which I did not name in my finding. `depends_on: [T-0001]`.
- **T-0013** (migration boundary by privileges) — `priority: low`, correctly, and the problem statement preserves the actual distinction: the guarantee rests on an `args.Contains("--migrate")` branch while both services connect as the same superuser with full DDL rights. Its AC requires verifying the API genuinely *cannot* perform DDL, which is the right shape.
- **T-0014** (correct `TESTING.md`) — declares itself an `evolve-governance` change travelling lane 2, and carries an acceptance criterion requiring the human approval to be **recorded in the change**. No dependency on T-0002/T-0003, deliberately, so the standard can be corrected independently of the tooling. `type: technical` is the nearest valid value in the schema; the routing lives in the body, which is the right place for it.

All three are registered in `BACKLOG.md` with the rationale for their placement. `validate.py`: **OK (14 tickets, 6 ADRs)**.

#### Final smoke test — fresh clone, README followed literally

Not strictly required for an `.editorconfig` change, but this is the last gate before the trunk, so I ran the whole thing again from a new `git clone` of `cd8a1a7` on the README's own port 8080:

- `docker compose ps`: `api Up (healthy)`, `postgres Up (healthy)`, `migrator Exited (0)`; `restarts=0`, `user=1654`
- `GET /health` → **200**, database reachable
- **Attribution proof:** stopped that specific container id → host `curl` **exit 7**, connection refused. The 200 was ours.
- **AC5 still holds:** schema dropped, API brought up alone → healthy, and **zero tables** created.

#### For `complete-ticket`, not for merge

DoD **item 3** (automated tests) remains unmet and unmeetable by this ticket — T-0003 depends on it. That has been on the record since refinement, with two routes in Testing Notes, and the choice is the PO's at completion. Flagging it so it is not mistaken for something this approval covers. Everything else in the DoD that a review can speak to is satisfied.

#### Verdict

**Approve — clear to merge.** Squash-merge titled `T-0001: <summary>` per [GIT.md](../../standards/GIT.md), then the `os:` status commit on the trunk, then remove the worktree and delete the branch.

Three rounds, and the change got materially better in each: a lock file that actually fails on drift, a non-root runtime, a bounded health probe, a formatting config that does not fight its own generator, and four review findings that became tracked work instead of forgotten prose. The two blocking findings I raised were both real, and the one I got partly wrong — `TESTING.md`, which I implied should have been fixed here — was corrected by the implementer citing the standard that governs it. That is the process working in both directions.
