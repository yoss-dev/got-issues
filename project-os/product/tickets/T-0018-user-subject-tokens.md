---
id: T-0018
title: Issue tokens that carry a user subject, so the projection has something to project
type: technical
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0010]
adrs: [ADR-0003]
created: 2026-08-31
updated: 2026-08-31
---

# T-0018: Issue tokens that carry a user subject, so the projection has something to project

## Problem / Context

Created by [T-0015](T-0015-compose-stack-smoke-test.md) as the **named successor its AC8
requires**. T-0015 AC8 asks that the user projection be proven against a token carrying a
real subject, and says in as many words that if no such token can be issued, the criterion
is deferred with a named successor rather than silently passed. This is that successor.

The identity host issues **client-credentials tokens only** ([ClientFactory.cs](../../../apps/GotIssues.IdentityHost/Configuration/ClientFactory.cs):
`AllowedGrantTypes = GrantTypes.ClientCredentials`). Such a token has no `sub` by
construction — the OAuth flow authenticates a *client*, not a person. Decoded from the
running stack during T-0015 (2026-08-31), a genuine member token carries exactly:

```json
{"aud":"gotissues-api","client_id":"smoke-member-client","exp":…,"iat":…,
 "iss":"http://localhost:8081","jti":"…","nbf":…,"role":"member","scope":["gotissues.api"]}
```

No `sub`. This is not a defect in the identity host; it is what the grant type means.

The consequence is a **standing blind spot**, already recorded twice. [T-0009](T-0009-role-authorisation-and-user-projection.md)'s
AC5 and AC8 are proven only in its in-process test host, and acceptance confirmed the gap
from both ends: seven real tokens decoded, none with a subject; the `users` table held zero
rows after a full traffic run. That blind spot is what hid T-0009's claim-mapping bug — the
policies read a claim type the JWT handler never produced, behind forty green tests.

## Desired Outcome

The stack can issue a token that identifies a person, so the user projection, assignment,
and comment authorship are exercised by the system rather than only by test doubles.

## User / Business Value

Three committed product tickets need a user identity to mean anything:
[T-0004](T-0004-create-and-list-projects.md) (admin-only creation),
[T-0006](T-0006-issue-lifecycle-fields.md) (assignee) and
[T-0008](T-0008-comment-on-an-issue.md) (comment authorship). Until a token carries a
subject, "who did this" is unrepresentable end to end, and every test of it is a test of
the test host.

## Scope

### In Scope

- A way for the stack to issue a token carrying a `sub` that identifies a person.
- Whatever minimum user representation that requires in the identity host.
- Proving **T-0015 AC8** with such a token: a projection is created on first request and
  updated, not duplicated, on return.

### Out of Scope

- A login UI. The API is the product for now (`PROJECT.md` §4).
- Real employee data — `PROJECT.md` Q8 gates that, and it is not needed to prove the shape.
- Provisioning policy: who may exist, how they are invited, deprovisioning.

## Acceptance Criteria

- [ ] AC1: Given the Compose stack, when a token is requested for a seeded test person, then the token carries a `sub` claim identifying that person.
- [ ] AC2: Given such a token, when an authenticated request is made, then a user projection is created for that subject; and when the same token is used again, then the record is updated rather than duplicated (T-0015 AC8, T-0009 AC5/AC8).
- [ ] AC3: Given a token carrying a subject and a role, when a guarded endpoint is requested, then the existing role policies permit or refuse it exactly as they do for a client token — the subject must not change the authorisation outcome.
- [ ] AC4: Given the seeded test people, when the repository is inspected, then no real employee's name or email address is present (`PROJECT.md` Q8, [SECURITY.md](../../standards/SECURITY.md)).
- [ ] AC5: Given a token carrying a subject, when the API validates it, then the **existing** validation applies unchanged — same issuer, audience, signing key and lifetime rules as a client token. A new token *shape* must not become a new token *path*.
- [ ] AC6: Given the smoke tier, when it runs, then [T-0015](T-0015-compose-stack-smoke-test.md)'s AC8 is satisfied by a real token rather than deferred — this ticket closes that deferral or it has not delivered its stated purpose.

## Examples / Scenarios

- First request from a subject: one row in `users`, `first_seen_at` set.
- Second request from the same subject: still one row, `last_seen_at` advanced.
- A token carrying `sub` but no `name`: projection still created (T-0009 AC8's shape).

## Dependencies

**T-0010** — the identity host this changes.

## Risks / Unknowns

- **The grant type is the decision, and it is not obvious — and refinement did not settle it.**
  See the Work Log: it meets the ADR bar and is the maintainer's call, not an agent's. The
  three candidates and their consequences are recorded there.
- **It may meet the ADR bar.** "How people authenticate" is a system-shaping choice; if the
  answer is anything beyond a test-only grant, it likely needs an ADR.
- **Seeded people are still personal-data-shaped.** AC4 exists because a fake person with a
  realistic name is one careless copy from a real one.

## Testing Notes

The proof belongs in the smoke tier T-0015 builds, since it needs the real identity host —
that is precisely why T-0015 could not prove it.

**The specific test to add** is T-0015's AC8, already written and waiting: a token carrying a
subject, one request, assert a projection row exists; a second request, assert the row is
updated and not duplicated. `apps/GotIssues.SmokeTests` already has the harness, the token
factory and the database access to do it.

**Mutate first** ([TESTING.md](../../standards/TESTING.md)): remove the subject claim from the
issued token and confirm the projection test fails. This is the exact blind spot that hid
[T-0009](T-0009-role-authorisation-and-user-projection.md)'s claim-mapping bug behind forty
green tests — the projection silently does nothing when the subject is absent, which is
indistinguishable from success unless something asserts the row.

**And the harder mutation, because it is the one that matters:** rename the subject claim to
something the API does not read, keeping everything else valid. If the test still passes, it is
asserting that *a* row exists rather than that *this caller's* row does.

## Relevant ADRs & Documentation

- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — Duende as the identity provider
- [T-0009](T-0009-role-authorisation-and-user-projection.md) — the projection this finally exercises
- [T-0015](T-0015-compose-stack-smoke-test.md) — AC8, the criterion deferred to here

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

### 2026-08-31 — Product Owner (claude-sm-9d4e)

- **Did:** Created as T-0015 AC8's named successor, with the decoded token recorded as evidence rather than asserted.
- **Decided:** scoped to making a subject-carrying token *possible and proven*, not to a provisioning model — the grant-type choice is left to refinement because picking it here would smuggle a system-shaping decision into a deferral.
- **Remaining:** refinement.
- **Open questions / blockers:** the grant type; whether it meets the ADR bar.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — Refinement (claude-sm-9d4e) — PO · BA · ENG · ARCH · QA · SEC

**Not ready, and the reason is the point: this ticket turns on a decision at the
[ADR bar](../../architecture/adr/README.md).** "How people authenticate" shapes the identity
host, the API's token validation, and anything that later puts a UI in front of either. The DoR's
architectural conditional says such a ticket is Ready only once the ADR exists — at least
`Proposed` — or the ticket is explicitly a spike. Neither is true yet, and writing a Proposed ADR
whose decision I invented would satisfy the letter of the DoR while defeating it.

**The three candidates, with what each commits us to:**

| Option | What it costs | What it commits |
| --- | --- | --- |
| **Test-only extension grant** — a custom Duende grant that issues a token for a seeded test person | Smallest. Days, not weeks. | Nothing about the product's real login model. It proves the projection, assignment and authorship end to end and leaves the question open. The risk is the usual one: "temporary" test-only mechanisms outlive their justification, and this one issues tokens for people. |
| **Authorisation code + a login page** | Largest. Implies a browser flow, a UI, and session handling — and `PROJECT.md` §3 names a web UI as an explicit **non-goal** for now. | The real answer eventually, and a direct contradiction of a confirmed non-goal today. |
| **Resource-owner password** | Middling. | A deprecated flow, in a system whose whole premise is doing identity properly with Duende. Cheap now, embarrassing later. |

**My recommendation: the test-only extension grant**, with its temporariness written into the
ADR as a constraint rather than an intention — for example, that it is refused unless an explicit
configuration flag is set, so it cannot be enabled by accident in anything resembling production.
It unblocks [T-0006](T-0006-issue-lifecycle-fields.md) and
[T-0008](T-0008-comment-on-an-issue.md), closes [T-0015](T-0015-compose-stack-smoke-test.md)'s
AC8, and defers the login question to when a UI actually exists — which is the point at which
the answer becomes obvious rather than speculative.

**What I refined regardless of the decision.** Added **AC5** — a new token shape must not become
a new token path; the same issuer, audience, key and lifetime rules apply, and an implementer
adding a parallel validation branch would be introducing precisely the kind of second code path
where [T-0009](T-0009-role-authorisation-and-user-projection.md)'s claim-mapping bug lived. Added
**AC6** so the ticket is accountable for the deferral it was created to close. Recorded the two
mutations, including the one that distinguishes "a row exists" from "this caller's row exists".

**Security.** Seeded people are personal-data-shaped even when fictional; AC4 keeps real names
and addresses out of the repository. Whatever grant is chosen touches token issuance, so
[SECURITY.md](../../standards/SECURITY.md) requires a Security review at refinement and at
acceptance — this entry is the refinement half, and it cannot complete until the grant is known.

**Sizing.** Option 1 fits the guideline comfortably; option 2 does not and would need splitting.
Another reason the decision comes first.

- **Did:** Applied every perspective; added AC5 and AC6; recorded the candidates, their
  consequences and a recommendation.
- **Decided:** nothing about the grant — deliberately. Refinement's job here was to make the
  decision cheap to take, not to take it.
- **Remaining:** the maintainer chooses a grant; then `create-adr`, then this is `ready`.
- **Open questions / blockers:** **one, blocking** — the grant type, at the ADR bar.
- **DoR verdict:** **not ready** — the architectural conditional fails. Every universal item
  holds; if the answer is the recommended option, this becomes `ready` as soon as the ADR is
  `Proposed`.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
