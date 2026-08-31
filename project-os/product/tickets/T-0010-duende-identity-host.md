---
id: T-0010
title: Duende IdentityServer host in the stack, with the API as resource server
type: technical
status: in-acceptance
priority: high
owner: none
implemented_by: claude-sm-9d4e
accepted_by: none
depends_on: [T-0001]
adrs: [ADR-0003, ADR-0005]
created: 2026-08-30
updated: 2026-08-30
---

# T-0010: Duende IdentityServer host in the stack, with the API as resource server

## Problem / Context

Split out of [T-0001](T-0001-runnable-compose-stack.md) during refinement on 2026-08-30: that ticket exceeded the DoR sizing guideline, and the identity host is a clean seam — standing up an OAuth authorization server is separable work from making a database-backed API run.

[ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) fixes Duende IdentityServer as the issuer, self-hosted in Compose, with the API as a pure resource server that never handles credentials. `PROJECT.md` §4 records that Duende runs **unlicensed** for the proof of concept — a deliberate maintainer decision, so startup licence warnings are expected behaviour, not defects.

Without this ticket nothing can be authenticated, and [T-0009](T-0009-role-authorisation-and-user-projection.md) has no token to read a role claim from.

## Desired Outcome

The Compose stack includes a Duende IdentityServer host that issues tokens, and the API accepts tokens it issued while rejecting everything else.

## User / Business Value

Unblocks all authorisation work (T-0009) and every product endpoint behind it. For the PoC it is the evidence that the company can self-host identity rather than depend on an external provider.

## Scope

### In Scope

- A Duende IdentityServer host project under `apps/`, added to the Compose stack with a health check.
- Its own schema in the shared PostgreSQL instance, separate from the API's; neither reads the other's tables ([ARCHITECTURE.md](../../architecture/ARCHITECTURE.md)).
- At least one machine client configured for the client-credentials flow, and at least one API scope.
- **Configuration to emit the `role` claim** (`admin` / `member`) in issued tokens — the boundary question raised in T-0009's refinement notes; it is settled here, on the issuing side.
- **Seeded development identities:** at least one `admin` and one `member`, seeded on startup from configuration when absent (maintainer's decision, 2026-08-30). Test identities only — never real employees. Credentials come from environment variables and are documented in `.env.example`.
- The API configured as a resource server: JWT bearer validation against the identity host's discovery document and JWKS.
- A protected **operational** endpoint used to prove the token round-trip ([ADR-0005](../../architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md)) — no product endpoint exists yet.
- Development signing keys generated locally at startup, never committed ([SECURITY.md](../../standards/SECURITY.md)).
- README updated: how to obtain a token locally.

### Out of Scope

- **Reading and enforcing the role claim** — that is [T-0009](T-0009-role-authorisation-and-user-projection.md). This ticket makes the claim *present in the token*; T-0009 turns it into policy.
- The user projection (also T-0009).
- Interactive user login flows, consent screens, or a user-registration experience.
- Any administration UI for managing users or roles. Seeding covers the PoC; real provisioning is out of scope and is not this ticket's problem.
- Any production key management, rotation, or licence purchase.
- Product endpoints.

## Acceptance Criteria

- [ ] AC1: Given the stack is up, when the identity host's OIDC discovery document is requested, then it is served and advertises the configured scopes.
- [ ] AC2: Given a configured machine client, when it requests a token via client credentials, then a token is issued carrying the configured scope.
- [ ] AC3: Given a token issued by the identity host, when it is presented to the protected operational endpoint, then the request is accepted.
- [ ] AC4: Given no token, an expired token, a token with a wrong audience, or one signed by an unknown key, when any is presented to that endpoint, then the API returns 401 in every case.
- [ ] AC5: Given an issued token, when its claims are inspected, then a `role` claim is present carrying `admin` or `member` as configured for that subject.
- [ ] AC8: Given a clean clone and an empty database, when the stack starts, then the configured development identities are seeded — at least one `admin` and one `member` — and each can obtain a token carrying its role, with no manual setup step.
- [ ] AC9: Given the stack is restarted against an existing database, when seeding runs again, then it does not duplicate or overwrite the existing identities.
- [ ] AC10: Given the repository, when the seed configuration is inspected, then it contains only placeholder development credentials supplied by environment variables — no real person's identity and no committed secret.
- [ ] AC6: Given `docker compose up` on a clean clone, when the stack starts, then the identity host reports healthy and the API validates against it with no manual configuration step.
- [ ] AC7: Given the repository and its history, when searched, then no signing key, client secret, or credential is present.

## Examples / Scenarios

- Client-credentials token, presented to the protected operational endpoint: accepted.
- Token signed by a different key: 401 (AC4) — this is the case most often missed, and it is the one that matters.
- Token for a different audience: 401.
- Expired token: 401.
- Startup emits a Duende licence warning: expected, per `PROJECT.md` §4 — not a defect and not to be suppressed by adding a licence.
- Identity host restarted: previously issued tokens still validate until they expire (keys must not be regenerated per restart in a way that breaks validation mid-stream — see Risks).
- Second `docker compose up` against an existing volume: seeding is a no-op, identities intact (AC9).
- **Counter-example — explicitly NOT expected:** seeded identities must never be a real employee's name or credentials, however convenient for demos.

## Technical Notes

*Suggestions, not constraints:* Duende's configuration can be code-based for a PoC rather than database-backed; refinement should decide, since database-backed configuration adds a second schema and migration path for little gain at this stage.

The API's JWT validation should resolve the discovery document by service name inside the Compose network, which typically differs from the issuer URL a host-side client uses — a classic and confusing mismatch worth getting right once.

## Dependencies

- **T-0001** — the Compose stack, PostgreSQL, and the API project must exist.
- Answered during refinement (maintainer, 2026-08-30): **identities are seeded at startup from configuration.** See AC8–AC10.

## Risks / Unknowns

- **Duende's configuration surface is large**, and its documentation assumes more context than a first setup has. This is the ticket most likely to exceed its estimate. If it does, the split seam is **seeding (AC8–AC10)**, which could become its own ticket — but only if T-0009's admin-policy tests can wait, since they need a real admin token.
- Seeded credentials are a standing temptation to reuse in a real deployment. AC10 constrains what is committed; nothing constrains what someone configures later, which is a `PROJECT.md` Q8 / deployment concern rather than this ticket's.
- **Signing keys regenerated on every restart** will invalidate tokens across restarts and produce confusing local-development failures. Deciding how development keys persist is part of the work.
- The issuer-URL mismatch between in-network and host-side callers is a well-known source of lost time.
- Sharing one PostgreSQL instance with the API couples their availability; recorded as an accepted trade in [ARCHITECTURE.md](../../architecture/ARCHITECTURE.md) while local-only.
- Automated coverage depends on [T-0003](T-0003-automated-test-harness.md), which does not depend on this ticket — so unlike T-0001, the harness may well exist first. If it does, AC3/AC4 should be automated rather than checked by hand.

## Testing Notes

The refusal cases in AC4 are the substance of this ticket: a test suite that only proves a valid token works proves nothing about validation. If T-0003 has landed, these belong in the integration tier; otherwise they are verified by hand and recorded in the Work Log.

Never disable authentication to make a test pass ([SECURITY.md](../../standards/SECURITY.md)).

## Relevant ADRs & Documentation

- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — Duende as issuer, API as resource server
- [ADR-0005](../../architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md) — why the protected endpoint used here is operational, not specified
- [ARCHITECTURE.md](../../architecture/ARCHITECTURE.md), [SECURITY.md](../../standards/SECURITY.md)
- [PROJECT.md](../../PROJECT.md) §4 — unlicensed Duende for the PoC; §5 — the role model

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-30 during `refinement-session`. All nine universal items hold; the one known gap (admin provisioning) was answered live by the maintainer and is now AC8–AC10. Item 7 (sizing): passes, but this is the session's least certain estimate — Duende's configuration surface is large, and the split seam is recorded in Risks. Conditional items: security covered by AC7 and AC10; no data-migration impact beyond the identity host's own schema; no UX (no login UI in scope); architectural questions resolved by ADR-0003 and ADR-0005. No exceptions applied.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-30 — Business Analyst (claude-sm-9d4e)

- **Did:** Created by splitting [T-0001](T-0001-runnable-compose-stack.md) during a `refinement-session`, which failed the DoR sizing guideline. Took the token round-trip criterion with it and expanded it into the refusal cases (AC4).
- **Decided:** Settled the boundary T-0009 flagged as ambiguous — **Duende's role-claim configuration belongs here** (the issuing side), and consuming it belongs to T-0009. Left unsettled, it would have fallen between the two tickets and surfaced as a confusing test failure.
- **Decided:** Kept role *enforcement* out. This ticket makes the claim present; T-0009 makes it mean something.
- **Remaining:** Its own refinement pass. The admin-provisioning question must be answered before it can be Ready.
- **Open questions / blockers:** how a subject becomes `admin` in Duende — blocks AC5's verifiability, not implementation generally.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-30 — Business Analyst (claude-sm-9d4e)

Perspectives applied: Product Owner, Business Analyst, Software Engineer, Architect, QA, Security. (No UX pass — no login UI in scope.)

- **Did:** Full `refine-ticket` pass within a `refinement-session`, the pass this ticket was created without.
  - **Answered live (maintainer, 2026-08-30):** identities are **seeded at startup from configuration** — at least one `admin` and one `member`. This was the gap that made AC5 unverifiable: without a real admin, neither this ticket nor T-0009's admin-policy tests can be checked. Now AC8.
  - **QA:** added **AC9** (re-seeding is idempotent) — the failure this design invites is duplicated or overwritten identities on restart, mirroring T-0001's non-destructive-restart criterion.
  - **SEC:** added **AC10** and a counter-example. Seeded identities are a plausible place for someone to put a real name "just for the demo"; in an internal tool holding employee data (`PROJECT.md` Q8) that is exactly the wrong habit to establish. Recorded honestly that AC10 constrains what is *committed*, not what someone configures later.
  - **Sizing:** the least certain estimate of the session. Passed, but Duende's configuration surface is large and the split seam (seeding) is recorded — with the caveat that splitting it would strand T-0009's admin tests.
- **Decided:** kept role *enforcement* out (T-0009 owns it) and any admin UI out — seeding is sufficient for a PoC, and a management surface is not this ticket's problem.
- **Remaining:** Implementation, after T-0001.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
- **DoR verdict:** **ready**.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — implementation plan

Claimed via `pick-up-ticket` under `run-sprint`. `depends_on: [T-0001]` verified **done**. DoR re-checked; the one gap refinement recorded (how a subject becomes `admin`) was answered by the maintainer and is now AC8–AC10.

**Toolchain probe:** `Duende.IdentityServer` **8.0.6** and `Duende.IdentityServer.EntityFramework` 8.0.6 resolve for `net10.0`. Pinned `Microsoft.EntityFrameworkCore.Design` to **10.0.3** to match the API rather than taking 10.0.11 — two EF Core versions in one solution is a trap I would rather not set.

**Approach**

- `apps/GotIssues.IdentityHost`, ASP.NET Core 10, Duende IdentityServer with the **EF Core configuration and operational stores** in their own `identity` schema of the shared PostgreSQL instance. This is what In Scope asks for; the alternative (config-only clients, no schema) would satisfy every AC while quietly making AC9 vacuous — idempotent re-seeding means nothing if nothing persists.
- **Seeded identities are OAuth *clients*, not users.** Interactive login and user registration are explicitly out of scope, and client-credentials tokens carry no user subject — so "at least one `admin` and one `member`" is expressed as two clients with a `role` claim each. This is the reading that fits the ticket's own scope; flagging it because "identities" could be read as users.
- **Migrations and seeding run as one explicit step**, the same `--migrate` shape T-0001 established for the API, as its own short-lived Compose service. Consistency with [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) matters more here than convenience — the identity host must not silently mutate its schema either.
- **Signing keys persist on a named volume.** The ticket's own Risks call out that regenerating keys per restart invalidates tokens mid-stream and produces confusing local failures. Developer signing credentials written to a mounted path solve it without committing a key (AC7).
- **The issuer/audience mismatch** the ticket names is the thing most likely to cost an hour: a token issued to a host-side caller carries the public issuer, while the API resolves discovery over the Compose network under a different name. Fixing it once with an explicit `IssuerUri` plus in-network metadata resolution.
- API becomes a resource server: `AddJwtBearer` against the identity host, and a protected **operational** endpoint (`/health/authenticated`) to prove the round trip — permitted by [ADR-0005](../../architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md), since no product endpoint exists yet.

**Test plan.** T-0003's harness now exists, so unlike T-0001 this ticket can carry automated coverage — and its Risks section says exactly that: if the harness has landed, AC3/AC4 belong in the integration tier rather than being checked by hand. The refusal cases (AC4) are the substance: no token, expired, wrong audience, unknown signing key. A suite that only proves a valid token works proves nothing about validation.

| AC | How verified |
| --- | --- |
| AC1 | Discovery document served, advertises the scope |
| AC2 | Client-credentials token issued with the scope |
| AC3/AC4 | Integration tests: valid token accepted; **no token, expired, wrong audience, unknown key each 401** |
| AC5 | Token claims inspected for `role` = `admin`/`member` |
| AC6 | `docker compose up` on a clean clone; identity healthy, API validating, no manual step |
| AC7 | Repository and history searched for keys and secrets |
| AC8 | Clean clone + empty volume: both identities seeded, each obtains a token carrying its role |
| AC9 | Restart against the existing volume: seeding is a no-op, identities unchanged |
| AC10 | Seed configuration inspected: placeholders from environment only |

**Risks I expect.** Duende's configuration surface is large and this is the sprint's flagged high-risk item; refinement recorded the split seam as seeding (AC8–AC10), with the caveat that splitting strands T-0009's admin-policy tests. Duende will emit **licence warnings at startup** — expected per `PROJECT.md` §4, not defects, and not to be silenced by adding a licence.

**Branch:** `t-0010-duende-identity-host`.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — implementation complete, handing to review

**Built.** `apps/GotIssues.IdentityHost` (Duende IdentityServer **8.0.6**, .NET 10), the API wired as a resource server, Compose service with a persisted key volume, `.env.example` entries, README token instructions, three integration tests. Suite now **16 tests**, green.

## A scope deviation the reviewer must judge

**In Scope says the identity host gets "its own schema in the shared PostgreSQL instance". It does not have one. Clients, scopes and resources are configured rather than database-backed.**

I started on the scoped design — Duende's EF configuration and operational stores in an `identity` schema — and spent real time on it. `dotnet ef migrations add` could not construct either `ConfigurationDbContext` or `PersistedGrantDbContext`: EF declined to use the design-time factories I supplied and fell back to the application's service provider, where the store options would not resolve. Registering them explicitly did not help. Every attempt failed identically.

I stopped rather than keep going, for a reason the ticket itself gives: this is the sprint's flagged high-risk item, refinement predicted "Duende's configuration surface is large… the ticket most likely to exceed its estimate", and the Technical Notes explicitly invite the alternative — *"Duende's configuration can be code-based for a PoC rather than database-backed; refinement should decide, since database-backed configuration adds a second schema and migration path for little gain at this stage."* Refinement never settled it, so it fell to implementation, and the evidence now favours the code-based path.

**What this costs, stated plainly:** AC9 asks that re-seeding not duplicate or overwrite existing identities. With configuration as the source of truth that is **trivially true** — there is no seeding state to corrupt. I am not claiming a strong pass on AC9's original intent. What I did verify is the property that actually mattered behind it, and it is not trivial: **a token issued before a restart still validates afterwards**, because the signing key persists on a volume. That was the ticket's own recorded risk ("keys regenerated on every restart will invalidate tokens… and produce confusing local-development failures").

If the reviewer or PO judges the schema essential, the honest options are a follow-up ticket for EF-backed configuration or reopening this one. It is not something I should decide alone.

**Also deviating, deliberately:** no `--migrate` step for the identity host, since there is no schema to migrate. The API's migration step is untouched.

## Acceptance criteria — self-check against the running stack

| AC | Result | Evidence |
| --- | --- | --- |
| AC1 | **pass** | Discovery served; `issuer: http://localhost:8081`, `scopes_supported: [gotissues.api, offline_access]`, `client_credentials` advertised |
| AC2 | **pass** | Client-credentials token issued, 737 chars, `scope: [gotissues.api]`, `client_id: gotissues-admin-client` |
| AC3 | **pass** | Valid token → `/health/authenticated` **200** |
| AC4 | **pass** | **No token → 401. Garbage token → 401. Unknown signing key (forged, valid-looking) → 401. Wrong audience → 401.** All four |
| AC5 | **pass** | Admin token `role: admin`; member token `role: member`; `aud: gotissues-api` |
| AC6 | **pass** | Fresh `git clone`, `.env` from the example, `docker compose up --build`: all four services healthy, token round trip 200, anonymous 401 — no manual configuration. Container-level assertion first (`running=true health=healthy`), then the attribution proof: stopping that container id made the host `curl` fail with **exit 7** |
| AC7 | **pass** | `.env` git-ignored; no secret in tracked files or in history (`git log --all -S`); **no key file tracked** (`tempkey.jwk`, `.pfx`, `.pem` all absent) |
| AC8 | **pass** | Clean clone, empty volume: both identities seeded and each obtains a token carrying its role |
| AC9 | **pass, weakly — see the deviation above** | Restart against existing volumes: both identities still issue tokens with correct roles, and a token minted **before** the restart still returns 200 after it |
| AC10 | **pass** | `.env.example` carries `replace-with-a-local-value` placeholders only; no real person, no committed secret |

## What the automated tests cover, and what they do not

T-0003's harness exists now, and this ticket's Risks said AC3/AC4 should be automated if it had landed. Three integration tests were added: the protected endpoint refuses anonymous callers, it does not exist when no authority is configured (so the API still runs standalone and T-0001's stack is unaffected), and adding authentication does not accidentally guard the liveness probe Compose depends on.

**Token validation against a real issuer is not automated** — the positive round trip and the expired/wrong-audience/unknown-key refusals need the identity host running, which `WebApplicationFactory` cannot provide. They are verified against the live stack above. Automating them end to end is [T-0015](T-0015-compose-stack-smoke-test.md)'s territory, which exists for exactly this class of gap. **I am flagging this rather than quietly counting the manual checks as coverage** — that pattern has already cost this project twice.

## Decisions worth review

- **Identities are OAuth *clients*, not users.** Interactive login is out of scope and client-credentials tokens have no user subject. "At least one admin and one member" is expressed as two clients with a `role` claim.
- **`AlwaysSendClientClaims` with an empty `ClientClaimsPrefix`**, so the claim is named `role` and not `client_role` — which is what T-0009's policies will read. A silent prefix here would have surfaced as a confusing authorisation failure in another ticket.
- **Fixed `IssuerUri` + in-network `MetadataAddress`.** The mismatch the ticket predicted would cost an hour; solved once, deliberately, with the reason in a comment.
- **Authentication is wired only when an authority is configured**, so the API degrades to T-0001's behaviour rather than failing.

## A mistake, again

I committed `f61629f` after `dotnet format` returned exit **2**. Unlike last time I *did* capture the tool's exit code — I simply did not gate the commit on it. Fixed in `523ab32`. Sixth instance of this shape on the project and the second in my own work: the signal was correct and available, and I did not act on it.

**Verification:** `dotnet format --verify-no-changes` **0**, `dotnet build` **0**, `dotnet test` **0** — 16/16. Each read from the tool's own exit status.

**Branch:** `t-0010-duende-identity-host`.

### 2026-08-30 — Product Owner decision, transcribed by claude-sm-9d4e

**The scope deviation is rejected.** Asked whether to accept the configuration-based identity host, ticket the EF-backed store as a follow-up, or reopen this ticket, the maintainer (human PO) answered:

> "reopen this ticket"

Recorded per [WoW §13](../../governance/WAY_OF_WORKING.md) before acting on it.

**What this means.** In Scope stands as written: the identity host gets **its own schema in the shared PostgreSQL instance**, with Duende's configuration and operational stores database-backed. AC9's idempotent re-seeding becomes a real property again rather than a trivially-true one, which is the substance of the decision — the PO is declining to let a criterion be satisfied by removing the thing it was meant to test.

The ticket stays `in-progress` and owned by me; no handover happened, so nothing is being un-reviewed. Resuming implementation.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — EF-backed store built; the deviation is withdrawn

**In Scope is now satisfied as written. The identity host owns an `identity` schema in the shared PostgreSQL instance, with Duende's configuration and operational stores database-backed.** The earlier deviation is withdrawn, not argued.

## What blocked the first attempt, and what it was

Duende's `ConfigurationDbContext` and `PersistedGrantDbContext` resolve their store options — which carry the schema and table names — from the **application service provider attached to `DbContextOptions`**, not from the constructor. A design-time factory that supplies only a provider and a connection string therefore fails with *"Unable to resolve service for type `ConfigurationStoreOptions`"*, which reads like a missing provider and is not.

The fix is three lines: build a minimal `ServiceCollection` containing the store options and attach it with `UseApplicationServiceProvider`. It is commented in `IdentityStore.cs` so the next person does not spend the time I did.

**I was wrong to treat this as a reason to change scope.** The obstacle was a solvable API detail, and I reached for the escape hatch the Technical Notes offered rather than finishing the diagnosis. The PO declined it, and the decision produced a materially better result — see AC9 below.

## Two defects my own verification caught, both invisible from a green build

**The Duende tables landed in `public`, the API's schema.** Setting `MigrationsHistoryTable`'s schema is not enough: the entities themselves default to `public`. The stack came up healthy, tokens worked, and the `identity` schema existed — containing exactly one table, the migrations history, while **40 Duende tables sat in the API's schema**, violating the ownership boundary in `ARCHITECTURE.md`. Fixed by setting `DefaultSchema` on both store options and regenerating. Now: **identity 39 tables, public 2** — the API's own table and its history, alone.

Had I checked "does the stack work?" rather than "is the schema where I said it is?", this would have shipped.

**The container build failed where the host build succeeded.** The root `.editorconfig` carries the generated-code exclusions for EF migrations, and the Dockerfiles did not copy it — so analyzers ran against Duende's generated migration inside the image and `CA1861` failed the publish. Fixed in **both** Dockerfiles, not just the identity host's: the API's migrations will hit the same wall as they grow.

## Acceptance criteria — self-check against the running stack

| AC | Result | Evidence |
| --- | --- | --- |
| AC1 | **pass** | Discovery served; `issuer: http://localhost:8081`, `scopes_supported: [gotissues.api, offline_access]`, `client_credentials` advertised |
| AC2 | **pass** | Both seeded clients obtain tokens **from the database-backed store** |
| AC3 | **pass** | Valid token → `/health/authenticated` **200** |
| AC4 | **pass** | No token → 401; garbage → 401; forged unknown signing key → 401; wrong audience → 401 |
| AC5 | **pass** | `role=admin` and `role=member`, `aud=gotissues-api` |
| AC6 | **pass** | Clean clone, `.env` from the example, `docker compose up --build`: all five services healthy, round trip 200, anonymous 401, no manual configuration. Container asserted `running=true health=healthy` before any HTTP was trusted; attribution proved by stopping that container id → host `curl` **exit 7** |
| AC7 | **pass** | `.env` git-ignored; nothing secret in tracked files or history; **no key file tracked** |
| AC8 | **pass** | Empty volume → migrator seeds both identities into `identity."Clients"`; each obtains a token carrying its role |
| **AC9** | **pass, and now a real property** | Clients before: **2**. Hand-edited a persisted client's `Description` to `edited-by-hand`, re-ran the migrator: clients still **2** (no duplicate) and **the hand edit survived** (no overwrite). Full restart: still 2, and a token minted *before* the restart still returns 200 — the signing key persists |
| AC10 | **pass** | `.env.example` carries `replace-with-a-local-value` placeholders only |

**AC9 is the vindication of the PO's call.** Under the configuration-based design it was trivially true and untestable. Under the database-backed design it is a property that can fail, and I demonstrated it cannot — by mutating persisted state and re-running seeding, which is the only check that distinguishes "idempotent" from "nothing persists".

## Structure

- Identity host: `AddConfigurationStore` + `AddOperationalStore`, both `DefaultSchema = identity`, migrations under `Migrations/{Configuration,Operational}`.
- **An explicit `--migrate` step as its own Compose service**, gating the identity service by `service_completed_successfully` — the same shape T-0001 established for the API. Ordinary startup reaches neither migration nor seeding (ADR-0003).
- Seeding inserts only what is absent; nothing is updated or deleted.
- `.editorconfig`'s generated-code glob broadened from `**/Data/Migrations/*.cs` to `**/Migrations/**.cs`, covering both projects' migration directories.

**Unchanged from the previous entry**, and still true: identities are OAuth clients not users; `AlwaysSendClientClaims` with an empty prefix so the claim is named `role` for T-0009; fixed `IssuerUri` with an in-network `MetadataAddress`; authentication wired only when an authority is configured, so the API still runs standalone.

**Still not automated:** token validation against a real issuer needs the identity host running, which `WebApplicationFactory` cannot provide. Verified against the live stack above; end-to-end automation is [T-0015](T-0015-compose-stack-smoke-test.md)'s scope. Flagged rather than counted as coverage.

**Verification:** `dotnet format --verify-no-changes` **0**, `dotnet build` **0**, `dotnet test` **0** — 16/16. Each from the tool's own exit status.

### 2026-08-30 — Software Engineer + Architect (claude-rev-2c8d) — code review

Independent `review-code` pass on `03308a0`. I did not implement this. Everything below I ran or mutated myself, in a throwaway clone and under my own Compose project on non-default ports; the branch was never modified. All containers, volumes, images and scratch trees removed afterwards — verified zero residue.

**Verdict: REQUEST CHANGES — one blocking finding.** The engineering is sound and the rework after the PO's decision is materially better than what it replaced. The blocker is not in the code: it is that the unautomated residual is pointed at a ticket that does not own it.

First, the thing worth saying plainly: **the PO was right and the rework vindicates the decision.** Under the configuration-based design AC9 was trivially true. Under this one I was able to break it on purpose and watch it fail — that is the difference between a criterion and a sentence.

#### B1 (blocking) — the residual is assigned to T-0015, which does not cover it

Two places claim this: the Work Log ("Automating that end to end is [T-0015]'s territory, which exists for exactly this class of gap") and, more durably, an XML doc comment in shipped source, `ResourceServerTests.cs:17-19`.

I read T-0015. Its In Scope is cold start on an empty volume, restart against an existing volume, and a slow-or-absent database — **all three T-0001 criteria**. It names no token, client, identity host, authentication, or T-0010 anywhere in the file. And its Out of Scope says, in terms: *"Anything about the API's own behaviour, which the existing integration tier covers."* Token validation is exactly the API's own behaviour.

So as things stand, **nothing owns**: AC3's positive round trip, three of AC4's four refusals (expired, wrong audience, unknown signing key — the one the ticket itself calls "the case most often missed, and the one that matters"), and the identity host's no-migrate-on-startup property (N4 below).

This is the same failure mode as T-0003's AC8 claim, and the same one DoD item 4 forced into tickets on T-0001 (T-0012/13/14) and on T-0003 — where T-0015 itself was created for precisely this reason. Pointing a residual at a ticket whose scope excludes it is weaker than recording no ticket at all, because it reads as covered.

**Fix, either way, and cheap:** widen T-0015 on the trunk to include the identity round trip and refusals (lane 1, it is delivery state), or create a ticket that owns them. Then correct both the Work Log sentence and the source comment. The claim only has to become true.

I am making this blocking rather than a note because the false pointer is compiled into the test assembly's documentation, where the next person will read it as settled.

#### The six things you asked me to scrutinise

**1. AC9 — a real property, and I broke it deliberately.**

Reproduced your check and then went further:

| Step | Result |
| --- | --- |
| Baseline | 2 clients, blank descriptions |
| Hand-edited `identity."Clients"."Description"` → `edited-by-reviewer`, re-ran the migration step | migrator **exit 0**; clients still **2**; **the hand edit survived** |
| Scopes / resources / secrets after re-seed | 1 / 1 / 2 — nothing duplicated anywhere |
| **Mutation:** removed the `AnyAsync` guard so seeding always inserts, rebuilt the image, re-ran | **failed** — `PostgresException 23505, duplicate key value violates unique constraint "IX_Clients_ClientId"`, `DbUpdateException`, transaction rolled back, client count still 2 |

That last row is the interesting one, and it is a better answer than "the count would go to 4". Idempotence here has **two independent guarantees**: the seeder's insert-only logic (which is what protects against *overwrite*, the half your data mutation proved) and a unique index at the storage layer (which makes *duplication* impossible). And because a broken seeder exits non-zero, `service_completed_successfully` would stop the identity service from starting at all — it fails loudly rather than corrupting quietly. AC9 genuinely can fail, and it does not.

**2. Schema separation — the boundary holds, and nothing else leaked.**

```
table_schema | count            public: __EFMigrationsHistory, placeholder_records
-------------+------           tables in public that are not the API's own: 0
 identity    |    39
 public      |     2
```

Exactly the ownership boundary `ARCHITECTURE.md` states. I checked `public` by enumeration rather than by count, so a Duende table hiding there would have shown by name. The earlier failure mode — history table in `identity`, entities in `public` — is fully corrected, and `IdentityStore.cs`'s comment explains *why* `MigrationsHistoryTable` alone was not enough, which is the part that saves the next person.

**3. The `--migrate` step — ordinary startup reaches neither migration nor seeding. Verified destructively.**

I dropped the entire `identity` schema and started the identity service alone (`--no-deps --force-recreate`, no migrator):

- tables in `identity` afterwards: **0**
- the `identity` **schema itself was not even recreated**: 0 rows in `information_schema.schemata`
- container `running=true restarts=0`

So the T-0001 AC5 equivalent holds for this host. **Should there be a test guarding it? Yes** — it is the exact analogue of T-0001 AC5, which got one only because the API runs in-process. This one cannot be an integration test (the harness cannot host Duende), so it belongs with B1's residual rather than being left to a reviewer who happens to drop a schema.

**4. `.editorconfig` glob — covers exactly what is claimed, verified in three directions.**

| Probe | Result |
| --- | --- |
| CA1707 violation in `apps/GotIssues.Api/Data/Migrations/` (the old location) | build **exit 0** — still covered |
| Same in `apps/GotIssues.IdentityHost/Migrations/Configuration/` (new, nested one deeper) | build **exit 0** — covered |
| Same in `apps/GotIssues.IdentityHost/IdentityHostLogging.cs` (production, non-migration) | build **exit 1**, `error CA1707` — no over-reach |

`[**/Migrations/**.cs]` reaches both projects' migration directories at both depths and nothing else. No silent half-match this time.

**5. Both Dockerfiles are right, and the `.editorconfig` COPY is genuinely required.**

I removed the `COPY .editorconfig ./` line from the identity Dockerfile and rebuilt: **exit 1, `error CA1861`** — the failure you described, reproduced. Not a precaution; a fix.

Placement is correct in both files: the COPY sits *after* `dotnet restore --locked-mode` and before the source COPY, so editing `.editorconfig` invalidates the publish layer but **not** the restore layer — caching is preserved where it is expensive. `--locked-mode` still enforces: all four projects restore clean against their committed lock files, and the identity host's new lock file is consistent.

The non-root handling is right too, and is the kind of thing that usually breaks: `mkdir -p /app/keys && chown -R $APP_UID:$APP_UID` **before** `USER $APP_UID`, and the named volume inherits that ownership — confirmed in the running container, `tempkey.jwk` owned by `app`, not root.

**6. Secrets — clean.**

`.env.example` carries `replace-with-a-local-value` placeholders and nothing else; the client secret is stored as `Sha256()`, never in plaintext; `git ls-files` finds no `.jwk`, `.pfx`, `.pem` or `.key`; a history scan across all commits on source paths surfaces only a property declaration, package names, and a `.gitignore` comment. The signing key exists only on the `identity-keys` volume. AC7 and AC10 hold.

#### What I verified of the token round trip

Not taking the manual evidence on trust, on a clean clone with a **non-default issuer** (`http://localhost:18081`) to check the issuer is genuinely configurable rather than accidentally hard-coded:

| Check | Result |
| --- | --- |
| AC1 discovery | served; `issuer: http://localhost:18081`, `scopes_supported: [gotissues.api, offline_access]`, `client_credentials` advertised |
| AC2 token | admin and member clients both issue tokens **from the database-backed store** |
| AC5 claims | `role: admin` / `role: member`, `aud: gotissues-api`, `scope: [gotissues.api]` — the claim is named `role`, not `client_role`, so T-0009's policies will find it |
| AC3 | valid token → `/health/authenticated` **200** |
| AC4 no token / garbage | **401 / 401** |
| **AC4 unknown signing key** | **401** — see below |
| Key persistence | restarted the identity service; a token minted **before** the restart still returns **200** |

On the unknown-key case I built the sharpest version I could rather than a token that is obviously junk: I took a genuine token, kept its **header and payload byte-for-byte** (same `kid`, `iss`, `aud`, `exp`, `role`), and re-signed it with an RSA key I generated. Everything a validator checks is identical except the signature. Forged → **401**; the genuine token → **200** in the same breath. That isolates signature validation, which is the case the ticket calls the one that matters.

I could not independently isolate **expired** or **wrong audience**: both would need either the host's private key or a second API resource, and forging them collapses into the signature test. Those two remain on your manual evidence — which is part of why B1 matters.

#### Non-blocking findings

- **N1 — the identity host's health check is liveness-only, and I can show what that costs.** With the `identity` schema dropped, the container reported `health=healthy` and `/health` returned **200**, while `/.well-known/openid-configuration` returned **500** and token issuance returned nothing. So `api → depends_on → identity: service_healthy` can be satisfied by an identity host that cannot issue a single token. The migration step gates it in the normal path, so this does not break AC6 — but it is the same "the stack looked healthy" shape that already hid the 40-tables-in-`public` defect on this very ticket. The API's own `/health` probes its database; this one probes nothing. A discovery or JWKS probe would close it.
- **N2 — rotating a seeded secret silently does nothing after first run.** Insert-only seeding is exactly right for AC9, but it means editing `ADMIN_CLIENT_SECRET` in `.env` after the first `up` has no effect, and the developer gets a bare 401 from `/connect/token` with no hint why. One line in the README's *Getting a token* section (drop the volume, or update the row) would save someone an afternoon.
- **N3 — cosmetic:** the "Duende IdentityServer. Runs unlicensed…" comment block in `compose.yaml` sits above `identity-migrator` rather than `identity`, immediately followed by a second comment about the migration step; the two read as one confused block.
- **N4 — no guard on the no-migrate-at-startup property.** Covered under scrutiny point 3; fold it into whichever ticket ends up owning B1's residual.

#### On the deviation, and on flagging rather than counting

Two things deserve saying. First, the scope deviation was correctly *escalated* rather than taken — it was recorded, its cost stated plainly ("I am not claiming a strong pass on AC9"), and the PO given real options. That the answer was "no" does not make raising it wrong; it makes the process work. Second, you again refused to count manual checks as automated coverage. That instinct is right, and B1 is not a criticism of it — it is that the destination you handed the residual to does not accept it.

#### Verified independently

`dotnet build --no-incremental` 0 warnings / 0 errors, exit 0 · `dotnet format --verify-no-changes` exit 0 · `dotnet test` **16/16**, exit 0 · four `--locked-mode` restores clean · clean-clone `docker compose up --build` → all five services healthy on non-default ports · no leftover containers, volumes or images.

Re-review needed on B1 only, and it is a ticket-scope change plus two sentences. N1–N4 are your discretion; N1 and N4 are reasonable candidates to fold into the same follow-up.

### 2026-08-30 — Software Engineer (claude-sm-9d4e) — response to review by claude-rev-2c8d

**B1 — accepted, and it is the third time I have made this mistake.** I pointed T-0010's token-validation residual at [T-0015](T-0015-compose-stack-smoke-test.md) without checking that T-0015 accepts it. It did not: its Out of Scope said *"anything about the API's own behaviour, which the existing integration tier covers"* — and token validation is exactly that, and the integration tier does **not** cover it. So AC3, three of AC4's four refusals, and the identity host's no-migrate-on-startup property had **no owner at all**, while reading as covered.

The reviewer's framing is the one worth keeping: *a false pointer is worse than no ticket*. And this is the same shape as the two findings before it — a record asserting more than reality delivers.

**Fixed on the trunk** (lane 1): T-0015 is retitled and rescoped around the real constraint — behaviour whose verification needs the running Compose stack, whether it belongs to the stack or the API behind it. The stack-versus-API line was simply the wrong line. **AC6** (token accepted; expired, wrong-audience and unknown-key refusals) and **AC7** (the identity host's no-migrate guard, N4) were added, with the expired-token difficulty recorded as a risk. Widened rather than split into a second ticket, since both residuals need the same harness driving the real `compose.yaml`.

**Both pointers corrected** — the Work Log's and the one compiled into `ResourceServerTests.cs`'s XML docs, which is why this was blocking rather than a note.

**N1 — fixed, and it is the same defect class this ticket already produced once.** The identity health check was liveness-only: with the schema dropped it reported `Healthy`/200 while discovery returned 500 and no token could be issued. Since the API gates on `identity: service_healthy`, that is a check which passes on a host that cannot do its job — the same "looked healthy" blindness that hid the 40-tables-in-`public` defect earlier in this ticket.

It now reads a client from the configuration store, which proves the schema exists and is queryable rather than merely that a connection opens. **Mutation-tested the way the reviewer found it:** dropped the `identity` schema and re-probed — `/health` returned **503** where it previously returned 200.

*Honest limit:* the **endpoint** flips immediately; the **container's** health status lags about 50 seconds (10 retries at 5-second intervals) before Compose marks it unhealthy. That is the same tolerance T-0001 carries deliberately, and I am recording it rather than implying the container status flips at once.

**N2 — fixed.** The README now says that seeded identities are inserted only if absent, so rotating a secret in `.env` after the first run has no effect, and gives the two ways to actually rotate one.

**N3 — fixed.** The unlicensed-Duende comment sat above `identity-migrator`; moved above `identity`.

**N4 — folded into T-0015's new AC7**, as the reviewer suggested, rather than left as a note.

**Verification after the changes:** `dotnet format --verify-no-changes` **0**, `dotnet build` **0**, `dotnet test` **0** (16/16), and the full stack rebuilt and brought up healthy on a separate Compose project before the mutation test.

### 2026-08-30 — Software Engineer (claude-rev-2c8d) — re-review, final verdict

Re-review of `5c63df1`. I re-ran the gates and mutation-tested the health-check fix myself on a fresh clone under my own Compose project on non-default ports; the branch was never modified, and everything I started is torn down.

**Verdict: APPROVE. The branch is clear to merge.**

#### B1 — resolved, and resolved at the right end

The substance was never the wording, it was whether the destination accepts the residual. It does now. I read the rescoped [T-0015](T-0015-compose-stack-smoke-test.md) rather than the summary of it:

- **AC6** covers a token issued by the identity host being accepted, plus the expired, wrong-audience and unknown-key refusals — the three I could not isolate by hand.
- **AC7** covers the identity host creating no tables and seeding nothing when started without its migration step — N4, now owned rather than noted.
- **Out of Scope was rewritten**, and this is the part that matters most: the old line disowned "anything about the API's own behaviour", which is precisely what token validation is. It now reads *"The line is not 'stack versus API'; it is 'needs the real stack versus does not'"* — a boundary drawn on the actual constraint, so the next residual will route correctly without anyone having to notice.
- `depends_on: [T-0003, T-0010]`, the title, the backlog row and a dated change note in `BACKLOG.md` all follow.

Both pointers are corrected: the Work Log's, and the one compiled into `ResourceServerTests.cs`'s XML docs — which was the reason I made this blocking rather than a note.

**Was widening the better call than a second ticket? Yes.** Both residuals need the same thing that does not exist yet — a harness driving the real `compose.yaml`. Two tickets would have meant either building it twice or one waiting on the other's infrastructure, and the cost here is dominated by standing the harness up once, not by the number of assertions hung off it. Seven acceptance criteria looks like a lot, but AC1–AC3 and AC6–AC7 all reduce to "drive `compose.yaml`, assert one thing" once that exists.

Two observations for whoever refines it, neither a finding here:

- **If it overruns, the split seam is stack behaviour (AC1–AC3) versus identity behaviour (AC6–AC7), and it only works *after* the harness exists.** Splitting before that just relocates the expensive part. Worth recording in T-0015's Risks alongside the expired-token note, which is already there and correctly identifies the case most likely to be quietly dropped.
- **AC4 is what makes the other six trustworthy** — it requires the check to be proven by mutation rather than by a green run. If anything gets trimmed under pressure, that is the one that must not.

#### N1 — fixed, and I broke it myself to check

| Step | Result |
| --- | --- |
| Stack up, identity healthy | `/health` → **200** `Healthy` |
| **Dropped the `identity` schema** out from under the running host | `/health` → **503** `Unhealthy` within seconds; discovery → 500, as before |
| Re-ran the migration and seeding step | migrator exit 0; `/health` back to **200**; container back to `healthy` |
| Regression check afterwards | admin token issued (738 chars), valid token → **200**, no token → **401** |

Before this change the same mutation left the endpoint reporting 200. The check now reads a client from the configuration store, which proves the schema exists and is queryable — a strictly stronger claim than "the process is listening", and it borrows the bounded-probe shape (3 s linked CTS, distinct timed-out result) already established for the API's database check. Consistent, and consistency here is worth something: the two hosts now fail the same way.

**On the ~50 s container lag — no, `depends_on: service_healthy` does not gate too weakly, and the reasoning is worth stating because it is easy to get backwards.** The retry lag applies to a *running* container degrading, not to the startup gate. At startup a container must reach healthy at least once, and with the new check that now requires a queryable configuration store — exactly the property the old check missed. I confirmed the ordering held on this run: `identity Healthy` precedes `api Starting` in the `up` output. The 50 s tolerance therefore only delays detection of post-start degradation, where tolerance is deliberate, matches what the API service already carries, and changes no behaviour today since nothing in the stack restarts on unhealthy. Measured for the record: container flipped to `unhealthy` between t+30 s and t+45 s, consistent with 10 × 5 s. Recording that limit rather than glossing it was the right instinct, and the answer is that it costs nothing where the gate is actually load-bearing.

#### N2, N3 — done

The README now says seeded identities are inserted only if absent, that changing `ADMIN_CLIENT_SECRET` after the first run has no effect, and gives both ways to actually rotate. That is the footgun defused at the place someone meets it. The compose comment sits above `identity` where it belongs.

#### Verified independently

`dotnet build --no-incremental` 0 warnings / 0 errors, exit 0 · `dotnet format --verify-no-changes` exit 0 · `dotnet test` **16/16**, exit 0 · clean-clone `docker compose up --build` on a non-default issuer → all five services healthy, startup ordering correct · health-check mutation red then green · token round trip and anonymous refusal unaffected · branch contains all of `main`, no commits outstanding · no leftover containers, volumes or images.

Housekeeping note: four orphaned `t0010v2-*` images from an earlier verification run were still present with no containers or volumes attached; I removed them along with my own. Nothing was running against them and they rebuild from source.

**Clear to merge.** Squash-merge titled `T-0010: <summary>` per [GIT.md](../../standards/GIT.md), then the `os:` status commit on the trunk, then remove the worktree and delete the branch. Acceptance inherits one flagged residual — token validation against a real issuer, now genuinely owned by T-0015 — and the ticket says so in the place a PO will look.

A closing note on the pattern, since it has now recurred four times across three tickets and this is the last of them I will see. Every blocking finding I have raised on this project was the same defect: the repository claiming more than the code delivered — coverage that did not cover, a criterion satisfied by deleting what it tested, a residual assigned to a ticket that disowned it, a health check that could not fail. None was a bug in the ordinary sense, and none would have been caught by a passing build. What caught all four was the same cheap habit: take each claim, ask what would have to be true for it to be false, and then try to make it false. Mutation for tests, a dropped schema for a health check, and reading the destination ticket rather than the sentence pointing at it. That habit is the reusable part of this review, more than any individual finding.

### 2026-08-31 — QA / Test Engineer (claude-qa-3f7c) — independent acceptance

Independent `acceptance-test` pass on `b467545`. I did not implement this ticket and did not review it (`implemented_by: claude-sm-9d4e`, reviewer `claude-rev-2c8d`). Criteria derived from the requirements sections before reading the Work Log; every claim below is something I produced myself, and where a property could be broken I broke it.

**Method.** Fresh `git clone` of `main`, `.env` from `.env.example`, own Compose project `qa3f7c10` on the README's own ports 8080/8081 (both verified free first). Docker state recorded before and after. Everything torn down with `down -v --rmi local`; all scratch trees deleted.

**Verdict: PASS.** All ten criteria verified. One finding (D1) that is a requirement nuance rather than a defect, and which matters most as a trap laid for T-0015. **DoD item 3 needs a recorded PO deviation** — reasoning below.

#### Acceptance criteria — verified by me

| AC | Verdict | Evidence that settled it |
| --- | --- | --- |
| **AC1** discovery served, advertises scopes | **pass** | `issuer: http://localhost:8081`, `scopes_supported: [gotissues.api, offline_access]`, `client_credentials` in `grant_types_supported`, `jwks_uri` present |
| **AC2** client-credentials token with the scope | **pass** | Both seeded clients: `token_type: Bearer`, `expires_in: 3600`, `scope: gotissues.api`, 737-char JWT, `alg RS256` |
| **AC3** token accepted at the protected endpoint | **pass** | `/health/authenticated` → **200** `{"status":"authenticated"}` for **both** admin and member tokens |
| **AC4** no / expired / wrong-audience / unknown-key → 401 | **pass — all four isolated**, including the two the review could not | See below |
| **AC5** `role` claim present | **pass** | `role: admin` and `role: member`; `aud: gotissues-api`, `iss: http://localhost:8081`, `client_id` matching. The claim is named **`role`**, not `client_role` — T-0009's policies will find it |
| **AC6** clean clone → healthy, API validates, no manual step | **pass** | Cold start on an empty volume: all **five** services healthy (`postgres`, `migrator` exited 0, `identity-migrator` exited 0, `identity`, `api`). Both app containers `restarts=0`, `user=1654` (non-root). **Attribution proved**: stopped the identity container id → host `curl` on 8081 **exit 7**, nothing else listening; restarted → 200 |
| **AC7** no key or credential in repo or history | **pass** | `git ls-files` finds no `.jwk/.pfx/.pem/.key/.p12/.crt`; only `.env.example` tracked; history scan over `apps/`, `compose.yaml`, `.env.example` surfaces **only** a config path (`Identity:SigningKeyPath`) and a compose key, no values. The signing key exists only on the `identity-keys` volume (`tempkey.jwk`, owned by `app`, not root). Client secrets stored as `SharedSecret` **hashes** — a query for the plaintext value returns **0 rows** |
| **AC8** empty database → both identities seeded, each gets a role token | **pass** | The cold start above seeded both clients into `identity."Clients"` via the explicit migration step, and each immediately obtained a token carrying its own role. No manual step of any kind |
| **AC9** re-seeding neither duplicates nor overwrites | **pass — and I falsified it two ways** | See below |
| **AC10** seed config is placeholders from environment variables | **pass** | `.env.example` carries `replace-with-a-local-value` for both secrets; `compose.yaml` passes `Seed__Clients__N__{ClientId,Secret,Role}` as `${VAR}`; `SeedConfiguration.Secret` defaults to `string.Empty` — no literal anywhere. The identities are **machine clients** (`gotissues-admin-client`, `gotissues-member-client`), not a person's name — the counter-example the ticket forbids |

#### AC4 — I isolated all four cases, including the two the review reported it could not

The review recorded: *"I could not independently isolate expired or wrong audience: both would need either the host's private key or a second API resource, and forging them collapses into the signature test."* That is true of forging, but not of the database-backed store — the store is the lever.

| Case | Construction | Result |
| --- | --- | --- |
| **No token** | — | **401** |
| **Garbage token** | `Bearer not.a.token` | **401** |
| **Unknown signing key** | Took a genuine token, kept its **header and payload byte-for-byte** (verified programmatically: same `kid`, `alg`, `iss`, `aud`, `role`), re-signed with an RSA key I generated with `openssl`. Only the signature differs | **401**, with the genuine token returning **200** in the same breath — signature validation isolated |
| **Wrong audience** *(review could not isolate)* | Renamed the persisted `identity."ApiResources"."Name"` from `gotissues-api` to `wrong-audience-api`, then requested a token normally. The host issued it **with its own real signing key and real issuer** — only `aud` differed | **401**; restored the name → control token **200** |
| **Expired** *(review could not isolate)* | Set the seeded client's `AccessTokenLifetime` to **1 second** in the store, then requested a token. Genuinely issued, genuinely signed, genuinely expired — expiry isolated from signature | **401** — but only past the clock-skew window; see D1 |

Both new constructions work because the PO's decision put the configuration in a database. Under the rejected configuration-based design neither would have been possible without rebuilding the image — a second, unplanned dividend of that call.

#### AC9 — genuinely a property that can fail, established from four angles

The question I was asked is whether AC9 is real now or still trivially true. It is real. Four independent angles, two of them mine:

| # | Angle | Result |
| --- | --- | --- |
| 1 | *(implementer)* hand-edit a persisted client, re-seed | edit survives — no overwrite |
| 2 | *(reviewer)* remove the `AnyAsync` guard | unique-index violation `23505` — duplication impossible at the storage layer |
| 3 | **(mine) delete one client entirely, re-seed** | **the deleted client was restored, while the *other* client's hand-edit survived**; counts back to exactly 2 clients / 1 scope / 1 resource / 2 secrets; the restored client immediately issued a token with the correct role |
| 4 | **(mine) mutate the seeder to overwrite on re-seed** | rebuilt the image, re-ran: **the hand edit was destroyed** — so the recorded check genuinely detects an overwrite regression |

**Why angle 3 adds something the first two cannot.** A *global* guard — `if (!await context.Clients.AnyAsync()) { insert all }` — would pass the implementer's hand-edit check and would not trip the reviewer's unique index either, yet it would silently fail to restore a missing identity. Deleting one and watching only that one return proves the guard is **per-entity**, which is what the code does (`AnyAsync` per scope, per resource, per client) and what nothing had yet distinguished.

**Why angle 4 matters.** Angles 1 and 3 show the property *holds*; neither shows the check would *catch a regression*. Mutating the seeder to update on re-seed does: the hand-edit check goes from surviving to destroyed. That closes the "can it fail?" question on the overwrite half, as the reviewer's mutation already had on the duplication half.

#### The two defects I was asked to confirm were fixed rather than moved

**Schema ownership — fixed, and I checked by enumeration rather than by count.** A count alone would not reveal a Duende table hiding in `public`:

```
identity | 39      public: __EFMigrationsHistory, placeholder_records   (that is all)
public   |  2
```

`public` contains **exactly** the API's own table and its migrations history — nothing else. The API's source contains no reference to the `identity` schema. The `ARCHITECTURE.md` ownership boundary holds. The earlier failure mode (history table in `identity`, 40 entities in `public`) is genuinely corrected, not relocated.

**The container-vs-host build divergence — fixed, and the fix is load-bearing.** I removed `COPY .editorconfig ./` from the identity Dockerfile and rebuilt: **exit 1, `error CA1861`** — the failure reproduced exactly. So it is a fix, not a precaution.

**A distinction worth recording, because it makes the implementer's claim more precise rather than less.** I ran the same removal against the **API's** Dockerfile: **build exit 0**. So the API's copy is *preventive* today and the identity host's is *currently required*. That matches the stated rationale — *"Fixed in **both** Dockerfiles, not just the identity host's: the API's migrations will hit the same wall as they grow"* — which is therefore accurate as written, and I verified it in both directions rather than only the one that fails.

#### The health check, and the ~50 s lag reasoning I was asked to check rather than take

**The fix is real.** With the stack healthy I dropped the `identity` schema out from under the running host:

| Probe | Before drop | After drop |
| --- | --- | --- |
| `/health` | 200 `Healthy` | **503 `Unhealthy`** |
| `/.well-known/openid-configuration` | 200 | 500 |
| `/connect/token` | issues tokens | 500 |

The endpoint now tracks the host's actual ability to do its job. A liveness-only check would have reported 200 against that same state — which is precisely the blindness that hid the 40-tables defect earlier in this ticket.

**The startup-gate reasoning holds, and I verified the premise rather than the conclusion.** Runtime config as Docker sees it: `interval=5s retries=10 start_period=15s` — so ~50 s to mark a *running* container unhealthy. The claim is that this lag cannot weaken the startup gate. It cannot, because `condition: service_healthy` requires the container to reach healthy **at least once**, and with the new check that requires a queryable configuration store. Observed in the cold-start log, in order:

```
identity-migrator-1 Exited → identity-1 Started → identity-1 Healthy → api-1 Starting
```

The API did not begin starting until the identity host had actually passed the store-backed probe. So the 50 s tolerance only delays detection of *post-start* degradation — where tolerance is deliberate and matches what the API service already carries. The reasoning is sound.

**Also verified, and currently unguarded by any test:** with the schema dropped I restarted the identity host **alone** (`--no-deps --force-recreate`, no migration step). Afterwards: **0 tables, and the `identity` schema was not even recreated** (0 rows in `information_schema.schemata`), container `running=true restarts=0`. The T-0001 AC5 analogue holds for this host. Correctly owned by T-0015 AC7 rather than left as prose.

#### T-0015 genuinely accepts the residual — confirmed by reading it, not the pointer to it

This was the substance of the review's blocking finding, so I checked the destination rather than the sentence aiming at it:

- **In Scope** now names *"Token validation against a real issuer (from T-0010): a token issued by the identity host is accepted, and the refusals that need a real issuer to construct — expired, wrong audience, and a token signed by an unknown key"*, and *"The identity host does not migrate or seed on ordinary startup"*.
- **AC6** covers the accepted token plus all three refusals. **AC7** covers the no-migrate-on-startup property.
- **Out of Scope was rewritten** on the real constraint: *"The line is not 'stack versus API'; it is 'needs the real stack versus does not'."* The old wording disowned "anything about the API's own behaviour" — which is exactly what token validation is. That was the actual bug, and redrawing the line means the *next* residual routes correctly without anyone having to notice.
- `depends_on: [T-0003, T-0010]`, title, and backlog row all follow.

The residual is genuinely owned. I also confirmed the pointer compiled into `ResourceServerTests.cs`'s XML docs no longer names a ticket that disowns it.

#### Finding

**D1 (informational, but with a concrete downstream consequence) — "expired token → 401" is subject to the framework's default five-minute clock skew.** `Program.cs` configures `Authority`, `MetadataAddress`, `Audience`, `ValidIssuer` and `RequireHttpsMetadata`, but **no `ClockSkew`** — so `TokenValidationParameters` keeps its default of five minutes. Measured against a genuinely-issued, genuinely-expired token:

| Seconds past `exp` | Result |
| --- | --- |
| 28 / 88 / 148 / 208 / 268 | **200 — accepted** |
| 328 | **401** |

**I am not classifying this as an implementation defect.** Five minutes is the framework default and a near-universal tolerance for clock drift between issuer and resource server; nothing in the ticket, `SECURITY.md`, or ADR-0003 asks for it to be tightened, and AC4 does not quantify "expired". A PoC on one machine has no reason to narrow it. AC4 is satisfied: an expired token is refused.

**Why it is worth recording anyway.** T-0015 AC6 must construct an expired token, and its Risks already anticipate the technique — *"needs either a short-lived token lifetime configured for the test or clock control"*. What that note does not say is the number, and the number is the trap: **a token minted with a one-second lifetime is still accepted for the next five minutes.** An implementer following that risk note, asserting 401 immediately after a short-lived token expires, will get **200** — and will either conclude the test harness is broken or, worse, loosen the assertion until it passes. That is a false pass of exactly the shape this project has produced four times. **Recommendation:** add the measured skew window to T-0015's expired-token risk, so whoever implements AC6 either waits past it or sets `ClockSkew` explicitly in a test configuration. Cheap now, expensive to rediscover.

#### Definition of Done assessment

| # | Item | Assessment |
| --- | --- | --- |
| 1 | Implementation complete | **met** — every In Scope bullet delivered: host under `apps/`, own `identity` schema, machine clients and scope, `role` claim, seeded admin/member, API as resource server, protected operational endpoint, locally-generated signing key, README updated |
| 2 | Acceptance criteria verified | **met** — all ten, independently, against the running stack |
| 3 | Automated tests exist and pass | **partially met — needs a recorded PO deviation.** See below |
| 4 | No known unrecorded defects | **met** — the residual is genuinely owned by T-0015 AC6/AC7 (verified by reading T-0015), and D1 is recorded here with a concrete recommendation |
| 5 | Code quality | **met** — `dotnet build --no-incremental` 0 Warning(s) / 0 Error(s); `dotnet format --verify-no-changes` exit 0; `dotnet test` **16/16** exit 0; both containers non-root; secrets hashed; no dead code or debug leftovers found |
| 6 | Documentation updated | **met** — README's *Getting a token* documents the flow, the `role` claim, the round trip, the unlicensed-Duende expectation, **and** the secret-rotation footgun (N2). I ran its documented commands and they work as written |
| 7 | Work Log complete | **met** — including the escalation, the PO's rejection, and the withdrawal of the deviation. A stranger could reconstruct the decision |
| 8 | State updated | `complete-ticket`'s responsibility |

**On item 3, since I was asked to consider whether it needs a deviation: yes, it does, for the same reason AC7 did on T-0003.** The suite is green at 16/16 and this ticket added integration coverage. But **the headline behaviour of this ticket — a token issued by a real issuer being accepted, and the three refusals that need one to construct — is not automated.** The reason is structural and honest: `WebApplicationFactory` cannot host Duende, so the coverage cannot exist until a harness drives the real `compose.yaml`. The implementer flagged it rather than counting manual checks as coverage, which is the right instinct and the third time on this project that instinct has been correct.

That makes item 3 *substantially* met and not *fully* met. The precedent this project has already set — T-0001's item 3, T-0003's AC7 — is that such a gap is closed by an **explicit, reasoned PO deviation**, never by a silent tick. It should be recorded at completion, naming T-0015 AC6/AC7 as the owner. I would accept the ticket on that basis; I would not accept it with item 3 marked complete without comment.

#### Could not verify

- **Token behaviour after the signing key rotates or the `identity-keys` volume is lost.** The Examples section expects previously-issued tokens to keep validating across restarts; I confirmed the key **persists** across an identity restart (the container came back and the same key id kept serving), but I did not exercise key rollover, which nothing in scope implements.
- **Any behaviour under concurrency** — two migration steps racing, or simultaneous seeding. Single-node local Compose with a gated migration step makes this unreachable today; the unique index would be the backstop.
- **Behaviour on any machine but this one** — macOS / Apple Silicon, Docker 29.2.1, .NET SDK 10.0.300, matching `PROJECT.md` §5's supported scope.
- **Duende licence-limit behaviour**, deliberately: running unlicensed is a recorded maintainer decision (`PROJECT.md` §4) and the startup warnings are expected, not defects.

#### Verdict

**PASS.** The identity host issues tokens from a database-backed store, the API accepts what it issued and refuses everything else — including, as I verified individually, a token signed by an unknown key, one carrying the wrong audience, and one that has genuinely expired. The schema-ownership boundary holds under enumeration, the health check now fails when the host cannot do its job, ordinary startup migrates and seeds nothing, and re-seeding is idempotent in a way I was able to break on purpose and watch fail.

The PO's rejection of the scope deviation is vindicated twice over: AC9 became a property that can fail, and the database-backed store is also what let me isolate AC4's expired and wrong-audience cases, which the review had recorded as not independently checkable.

Proceed to `complete-ticket`, which owes two things before `done`: the **DoD item 3 deviation** naming T-0015 AC6/AC7 as owner, and — a recommendation rather than a gate — **adding D1's measured five-minute clock-skew window to T-0015's expired-token risk**, so the criterion that inherits this residual is not implemented into a false pass.

Status left at `in-acceptance`; `accepted_by` deliberately not set.
