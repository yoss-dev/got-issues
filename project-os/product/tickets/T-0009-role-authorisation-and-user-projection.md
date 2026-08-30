---
id: T-0009
title: Role-based authorisation and the user projection from token claims
type: feature
status: backlog
priority: high
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0001, T-0003]
adrs: [ADR-0003]
created: 2026-08-30
updated: 2026-08-30
---

# T-0009: Role-based authorisation and the user projection from token claims

## Problem / Context

Promoted from [IDEA-004](../IDEAS.md), unblocked by the maintainer's answer to Q7 on 2026-08-30.

Two gaps are answered together here. First, [`PROJECT.md`](../../PROJECT.md) §5 now fixes the authorisation model: **global roles — `admin` and `member` — carried as a claim in the Duende token.** The API needs to read that claim and turn it into enforceable policies. Second, T-0006 (assignment) and T-0008 (comment authorship) both need users to be addressable, and nothing in T-0004 or T-0005 creates them; the API keeps a *thin projection* of users (subject, display name) and never their credentials or roles ([ARCHITECTURE.md](../../architecture/ARCHITECTURE.md)).

## Desired Outcome

The API derives a caller's global role from their token and enforces it through named policies, and every authenticated caller is represented by a local user record that issues can be assigned to and comments attributed to.

## User / Business Value

Unblocks assignment (T-0006) and comment authorship (T-0008), and makes "who may do this?" a single answered question rather than a per-endpoint improvisation. Without it, every product ticket invents its own authorisation and the first one to get it wrong has nothing behind it.

## Scope

### In Scope

- Reading the role claim from the validated token and mapping it to authorisation policies (`admin`, `member`) usable by any endpoint.
- A thin user projection: on an authenticated request, the caller's subject and display name are upserted into a local users table, so they can be referenced as an assignee or comment author.
- The EF Core migration introducing that table.
- Behaviour when the role claim is absent or holds an unrecognised value (see AC4 — this must be a deliberate decision, not an accident).
- Tests covering each role against a protected endpoint, including the refusal cases.

### Out of Scope

- **Role assignment or management through this API.** Roles live in Duende; changing someone's role is an administrative act performed there. No endpoint implements it (maintainer, 2026-08-30).
- Storing credentials, passwords, or secrets — Duende owns those and always will ([ARCHITECTURE.md](../../architecture/ARCHITECTURE.md)).
- A users API (listing or searching users). Add it when a ticket needs it.
- Applying admin-only rules to specific endpoints — that belongs to the tickets owning those endpoints (T-0004 for project creation).
- Per-project permissions. Roles are global; there is no membership concept ([GLOSSARY](../../governance/GLOSSARY.md)).

## Acceptance Criteria

- [ ] AC1: Given a valid token carrying the `admin` role, when a caller requests an endpoint guarded by the admin policy, then the request is permitted.
- [ ] AC2: Given a valid token carrying the `member` role, when a caller requests an endpoint guarded by the admin policy, then the API returns 403 — authenticated but not authorised, distinct from the 401 an invalid token produces.
- [ ] AC3: Given a valid token of either role, when a caller requests an endpoint guarded by the member policy, then the request is permitted.
- [ ] AC4: Given a valid token whose role claim is missing or holds an unrecognised value, when any guarded endpoint is requested, then the caller is treated as having no role and is refused — never silently promoted to `member` or `admin`.
- [ ] AC5: Given an authenticated caller with no local user record, when they make a request, then a record is created from their token claims; and when they return later, then the existing record is updated rather than duplicated.
- [ ] AC6: Given a user record, when it is inspected, then it holds no credential, secret, or role — the role is read from the token on every request and never persisted.
- [ ] AC7: Given the user projection, when a display name or email is written anywhere, then it does not appear in logs ([SECURITY.md](../../standards/SECURITY.md)).

## Examples / Scenarios

- `admin` token on an admin-only endpoint: 200. `member` token on the same: 403.
- No token at all: 401, not 403 — the distinction matters to clients.
- Token with `role: "superuser"`: refused (AC4), not treated as admin.
- Token with no role claim at all: refused.
- Same subject calling twice: one user record, updated, not two.
- A user's display name changes in Duende: the projection reflects it on their next request.

## Technical Notes

*Suggestion, not constraint:* ASP.NET Core's policy-based authorisation maps onto this directly; the point of the ticket is that policies are defined **once, centrally**, not re-derived per controller.

AC4 is the one most likely to be got wrong by accident. A default that treats an unknown or missing claim as an ordinary member is a plausible-looking line of code and a real authorisation hole; the refusal must be deliberate and tested.

The upsert in AC5 runs on authenticated requests, so it sits on a hot path — refinement should consider the cost rather than discovering it later.

## Dependencies

- **T-0001** — token validation and the identity host must exist, and the token must actually carry a role claim. **Configuring Duende to emit that claim may fall to T-0001 or here; refinement must decide which, or it will fall between them.**
- **T-0003** — the test harness, for the role matrix in AC1–AC4.

## Risks / Unknowns

- **Duende must be configured to include the role claim** in issued tokens. If T-0001 stands up the identity host without it, this ticket also carries that configuration — a dependency that is easy to miss until the first test fails for a confusing reason.
- How a person becomes an `admin` in Duende in the first place (seeded at startup? configured by hand?) is unresolved and sits on the T-0001 boundary.
- The user projection stores employees' names and email addresses — personal data in an internal tool, and the subject of `PROJECT.md` Q8. The DoR's security/privacy conditional applies, and Q8 should be answered before real employee data is loaded.
- Treating the role claim as authoritative means the API's authorisation is only as good as Duende's token issuance. That is the correct trade for this model, but it concentrates risk in one place.

## Testing Notes

The role matrix (admin/member/unknown/absent × admin-policy/member-policy endpoint) is the core of the suite, and the **refusal** cases are the ones that matter — a test suite that only proves permitted access proves nothing about authorisation. AC5 needs a repeat-request test to catch duplicate user records.

## Relevant ADRs & Documentation

- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — Duende as issuer, API as resource server
- [ARCHITECTURE.md](../../architecture/ARCHITECTURE.md) — global roles, data ownership, the thin user projection
- [SECURITY.md](../../standards/SECURITY.md) — negative-case tests are mandatory; never disable auth to make a test pass
- [PROJECT.md](../../PROJECT.md) §5 — the authorisation model
- [IDEA-004](../IDEAS.md) — the originating idea

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — checked during refinement; note applied exceptions here.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** Created by promoting IDEA-004, unblocked by the maintainer's Q7 answer: roles are `admin`/`member`, carried as a token claim.
- **Decided:** Combined the authorisation policies and the user projection into one ticket — both fall out of "the token is the source of truth about the caller", and splitting them would leave T-0006 and T-0008 blocked on the half that shipped second. Kept endpoint-specific rules out: those belong to the tickets owning those endpoints.
- **Decided:** Recorded that role *assignment* happens in Duende, not through this API. The maintainer selected it as an admin-only act while also choosing token-carried roles; the coherent reading is that it is administrative work outside this API's surface. Flagged to the maintainer 2026-08-30 for correction if that reading is wrong.
- **Remaining:** Refinement to Ready. The main boundary question is whether Duende's role-claim configuration belongs here or in T-0001.
- **Open questions / blockers:** none blocking. `PROJECT.md` Q8 (data protection) applies to the user projection but does not block implementation.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
