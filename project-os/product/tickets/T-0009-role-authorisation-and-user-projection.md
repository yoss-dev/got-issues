---
id: T-0009
title: Role-based authorisation and the user projection from token claims
type: feature
status: in-progress
priority: high
owner: claude-sm-9d4e
implemented_by: none
accepted_by: none
depends_on: [T-0003, T-0010]
adrs: [ADR-0003, ADR-0005]
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
- Tests covering each role against a guarded endpoint, including the refusal cases. **The guarded endpoint used for these tests is registered in the test host, not shipped** — see Technical Notes.

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
- [ ] AC7: Given a request that creates or updates a user projection, when the log output emitted during that request is inspected, then it contains neither the display name nor the email address ([SECURITY.md](../../standards/SECURITY.md)).
- [ ] AC8: Given a token whose subject is present but whose display-name claim is missing, when the caller makes a request, then the projection is still created and the caller is usable as an assignee — a missing optional claim does not fail the request.

## Examples / Scenarios

- `admin` token on an admin-only endpoint: 200. `member` token on the same: 403.
- No token at all: 401, not 403 — the distinction matters to clients.
- Token with `role: "superuser"`: refused (AC4), not treated as admin.
- Token with no role claim at all: refused.
- Same subject calling twice: one user record, updated, not two.
- A user's display name changes in Duende: the projection reflects it on their next request.
- Two different subjects sharing a display name: two distinct records — identity is the subject claim, never the name.
- **Counter-example — explicitly NOT expected:** the API must never write a role onto the user record as a cache, however convenient. The token is the only source (AC6).

## Technical Notes

**Where the guarded endpoint for AC1–AC4 comes from — decided during refinement (2026-08-30).** This ticket defines policies before any product endpoint uses them: T-0004 is the first admin-guarded endpoint and it depends on *this* ticket. Rather than inventing product surface to test against, the policy tests use a **guarded endpoint registered in the test host** ([T-0003](T-0003-automated-test-harness.md)'s `WebApplicationFactory`), which by T-0003's AC10 cannot exist outside the test configuration. An admin-only *operational* endpoint would also be permitted under [ADR-0005](../../architecture/adr/ADR-0005-operational-endpoints-outside-the-api-contract.md), but it would ship surface that exists only to be tested.

*Suggestion, not constraint:* ASP.NET Core's policy-based authorisation maps onto this directly; the point of the ticket is that policies are defined **once, centrally**, not re-derived per controller.

AC4 is the one most likely to be got wrong by accident. A default that treats an unknown or missing claim as an ordinary member is a plausible-looking line of code and a real authorisation hole; the refusal must be deliberate and tested.

The upsert in AC5 runs on authenticated requests, so it sits on a hot path. A naive write on every request costs a database round trip per call; writing only when something actually changed is the obvious mitigation, but the implementer should measure rather than assume.

## Dependencies

- **T-0010** — the identity host must exist and must emit the `role` claim. **That boundary is now settled: T-0010 configures Duende to issue the claim; this ticket consumes it** (decided during T-0001's refinement, 2026-08-30).
- **T-0003** — the test harness, for the role matrix in AC1–AC4.

## Risks / Unknowns

- How a person becomes an `admin` in Duende in the first place (seeded at startup? configured by hand?) is unresolved and now sits with [T-0010](T-0010-duende-identity-host.md), which must answer it before this ticket can be verified against a real admin.
- The user projection stores employees' names and email addresses — personal data in an internal tool, and the subject of `PROJECT.md` Q8. **Implementing and testing against seeded test identities is unaffected; loading real employee data is what Q8 gates.** The distinction matters: this ticket is not blocked, but the first real deployment is.
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

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-30 during `refinement-session`. All nine universal items hold. Item 5: depends on T-0003 and T-0010; **T-0010 is not yet refined**, which constrains sequencing but does not make starting pointless. Item 9: no blocker — `PROJECT.md` Q8 gates real employee data, not implementation against test identities. Conditional items: security/privacy named and covered by AC4, AC6, AC7; data-shape impact identified (the projection's migration); architectural questions resolved (`PROJECT.md` §5 fixes the model, ADR-0005 covers the endpoint question); no UX. No exceptions applied.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-30 — Product Owner (claude-sm-9d4e)

- **Did:** Created by promoting IDEA-004, unblocked by the maintainer's Q7 answer: roles are `admin`/`member`, carried as a token claim.
- **Decided:** Combined the authorisation policies and the user projection into one ticket — both fall out of "the token is the source of truth about the caller", and splitting them would leave T-0006 and T-0008 blocked on the half that shipped second. Kept endpoint-specific rules out: those belong to the tickets owning those endpoints.
- **Decided:** Recorded that role *assignment* happens in Duende, not through this API. The maintainer selected it as an admin-only act while also choosing token-carried roles; the coherent reading is that it is administrative work outside this API's surface. Flagged to the maintainer 2026-08-30 for correction if that reading is wrong.
- **Remaining:** Refinement to Ready. (The role-claim boundary question was settled on 2026-08-30 — see the later entry.)
- **Open questions / blockers:** none blocking. `PROJECT.md` Q8 (data protection) applies to the user projection but does not block implementation.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-30 — Business Analyst (claude-sm-9d4e)

- **Did:** Updated during T-0001's refinement. Dependency moved from T-0001 to [T-0010](T-0010-duende-identity-host.md) (the identity host, split out of T-0001).
- **Decided:** The role-claim boundary this ticket flagged as ambiguous is settled — **T-0010 configures Duende to emit the claim; this ticket reads and enforces it.** The admin-provisioning question moved with it.
- **Remaining:** Its own `refine-ticket` pass.
- **Open questions / blockers:** none blocking.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-30 — Business Analyst (claude-sm-9d4e)

Perspectives applied: Product Owner, Business Analyst, Software Engineer, Architect, QA, Security.

- **Did:** Full `refine-ticket` pass within a `refinement-session`.
  - **ARCH:** found the same shape of circularity T-0001 hit — AC1–AC4 need a *guarded endpoint* to test against, but the first one (T-0004's project creation) depends on this ticket. Resolved by testing policies against an endpoint registered **in the test host** rather than shipping product surface that exists only to be tested. Recorded the rejected alternative (an admin-only operational endpoint, which ADR-0005 would permit) and why it loses.
  - **QA:** AC7 was unverifiable as written — "does not appear in logs" names no observation point. Rewritten to scope it to the log output of a request that touches the projection. Added **AC8** for a missing display-name claim: the realistic case a strict implementation rejects and a user experiences as a broken request.
  - **BA:** added a counter-example forbidding the role being cached onto the user record. AC6 already implies it; the counter-example puts it where an implementer looking for a shortcut would read.
  - **SEC:** sharpened the Q8 relationship. The previous wording made the ticket look blocked; the accurate statement is that **test identities are unaffected and real employee data is what Q8 gates**.
  - **ENG:** noted the AC5 upsert sits on a hot path and that "write only when changed" is a thing to measure, not a design instruction.
- **Decided:** kept the ticket whole — policies and the user projection both fall out of "the token is the truth about the caller", and splitting them would leave T-0006 and T-0008 blocked on whichever half shipped second.
- **Remaining:** Implementation, after T-0003 and T-0010.
- **Open questions / blockers:** none blocking. **Sequencing:** T-0010 must be refined before this can be sensibly planned — it supplies the role claim this ticket reads.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
- **DoR verdict:** **ready**.
