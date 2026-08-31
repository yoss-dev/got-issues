---
id: T-0010
title: Duende IdentityServer host in the stack, with the API as resource server
type: technical
status: ready
priority: high
owner: none
implemented_by: none
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
