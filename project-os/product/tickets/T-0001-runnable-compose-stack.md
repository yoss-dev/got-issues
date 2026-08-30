---
id: T-0001
title: Runnable Docker Compose stack with API skeleton, PostgreSQL, and identity host
type: technical
status: backlog
priority: high
owner: none
implemented_by: none
accepted_by: none
depends_on: []
adrs: [ADR-0003]
created: 2026-08-30
updated: 2026-08-30
---

# T-0001: Runnable Docker Compose stack with API skeleton, PostgreSQL, and identity host

## Problem / Context

Nothing runs yet. [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) fixes the stack — .NET 10 / ASP.NET Core, PostgreSQL via EF Core, Duende IdentityServer, all under Docker Compose — but no solution, no container, and no `compose.yaml` exist. Until this ticket lands, no other work can be verified end to end, and the README's *Getting started* section describes commands that do not work.

[`PROJECT.md`](../../PROJECT.md) §4 makes the Compose constraint hard: no component may require host-installed infrastructure beyond Docker and the .NET SDK.

## Desired Outcome

`docker compose up` from a clean clone brings up a healthy API, a PostgreSQL instance with the schema applied, and a Duende IdentityServer host that issues a token the API accepts.

## User / Business Value

This is the proof-of-concept's first real evidence: it shows the company *can* run this tooling in-house. It also unblocks every other ticket — nothing else can be verified against a real system until it exists. Serves Sam (running the stack) directly.

## Scope

### In Scope

- A .NET 10 solution with the API project under `apps/` and the identity host under `apps/`.
- `compose.yaml` at the repository root wiring API, PostgreSQL, and identity host, with supporting files under `infra/`.
- EF Core `DbContext` and an initial migration establishing the schema (entities themselves may be minimal — a placeholder is acceptable; the *mechanism* is the deliverable).
- Migrations applied by an explicit migration step in the stack, **not** at API startup ([ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md)).
- Health checks and startup ordering so the API waits for a ready database.
- Duende IdentityServer configured with at least one client (client-credentials) and one API scope; the API configured as a resource server validating its tokens.
- A `.env.example` documenting every required variable; no secrets committed.
- README *Getting started* updated so the documented commands actually work.

### Out of Scope

- Any product endpoint (projects, issues, comments) — those follow the contract-first pipeline in T-0002.
- The OpenAPI specification and code generation (T-0002).
- The test harness (T-0003).
- Any deployment target beyond local Compose.
- The global role set — the identity host needs only enough configuration to issue a token (see `PROJECT.md` Q7).

## Acceptance Criteria

- [ ] AC1: Given a clean clone and Docker running, when `docker compose up` is run, then the API, PostgreSQL, and identity host all reach a healthy state without manual intervention.
- [ ] AC2: Given the stack is up, when the API's health endpoint is requested, then it returns 200 and reports the database as reachable.
- [ ] AC3: Given the stack is up, when a client-credentials token is requested from the identity host and presented to a protected API endpoint, then the request is accepted; when no token or an invalid token is presented, then the API returns 401.
- [ ] AC4: Given a database with no schema, when the stack starts, then the migration step applies the schema and the API does not itself run migrations at startup.
- [ ] AC5: Given the repository, when it is searched for secrets, then none are committed — every credential comes from environment variables, and `.env.example` documents each one with a placeholder.
- [ ] AC6: Given the README's *Getting started* section, when its commands are followed literally on a clean clone, then they work as written.

## Examples / Scenarios

- Cold start with an empty volume: the database has no schema, the migration step creates it, the API starts healthy.
- Restart with an existing volume: migrations are a no-op, nothing is destroyed.
- Database slow to accept connections: the API waits rather than crash-looping.
- Token from the identity host presented to the API: accepted. Expired or wrong-audience token: 401.

## Technical Notes

*Suggestions, not constraints:* Compose health checks with `depends_on: condition: service_healthy`; the migration step as a short-lived service or an init container so it is observable and rerunnable. Duende needs signing keys — generate them locally at startup for development; never commit them ([SECURITY.md](../../standards/SECURITY.md)).

Both the API and the identity host use the same PostgreSQL instance in separate schemas, and neither reads the other's tables ([ARCHITECTURE.md](../../architecture/ARCHITECTURE.md)).

## Dependencies

Docker and the .NET 10 SDK on the developer machine (both verified present 2026-08-30). No external services, credentials, or human input required.

## Risks / Unknowns

- Duende IdentityServer's configuration surface is large; a minimal working setup may take longer than expected, and its documentation assumes more context than a first-time setup has.
- Container startup ordering is a known source of friction; naive `depends_on` without health conditions produces intermittent failures that look like application bugs.
- Duende runs unlicensed here — a deliberate, informed decision for the PoC (`PROJECT.md` §4). If it emits licence warnings at startup, that is expected, not a defect.
- The schema is a placeholder at this stage; the real entity model arrives with the product endpoints and will churn.

## Testing Notes

Primarily verified by running the stack: cold start, restart, and the token round-trip in AC3. Automated coverage is thin by nature at this stage — the test harness arrives in T-0003, which will then cover this ticket's behaviour properly. State explicitly in the Work Log what was verified by hand.

## Relevant ADRs & Documentation

- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — the stack, and why migrations run as an explicit step
- [ARCHITECTURE.md](../../architecture/ARCHITECTURE.md) — component boundaries and data ownership
- [SECURITY.md](../../standards/SECURITY.md) — secret handling
- [PROJECT.md](../../PROJECT.md) §4 — the Compose constraint

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — checked during refinement; note applied exceptions here.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-30 — Scrum Master (claude-sm-9d4e)

- **Did:** Created during `bootstrap-project` step 8 (seed the delivery pipeline). Scope derived from ADR-0003 and the Compose constraint in `PROJECT.md` §4.
- **Decided:** Kept the entity model out of scope — this ticket delivers the *mechanism* (stack, migrations, auth round-trip), not the domain, which belongs behind the contract-first pipeline in T-0002.
- **Remaining:** Refinement to drive to Ready; sizing is unverified and this may need splitting (identity host vs. API+database are separable).
- **Open questions / blockers:** none blocking. `PROJECT.md` Q7 (global role set) is deliberately not needed here — issuing and validating one token does not require the role model.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
