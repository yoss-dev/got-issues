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

## Examples / Scenarios

- First request from a subject: one row in `users`, `first_seen_at` set.
- Second request from the same subject: still one row, `last_seen_at` advanced.
- A token carrying `sub` but no `name`: projection still created (T-0009 AC8's shape).

## Dependencies

**T-0010** — the identity host this changes.

## Risks / Unknowns

- **The grant type is the decision, and it is not obvious.** Resource-owner password is
  deprecated; authorisation code implies a browser and a login page; a test-only extension
  grant proves the projection without committing the product to a login model. Refinement
  should choose deliberately — this is the kind of decision that quietly becomes permanent.
- **It may meet the ADR bar.** "How people authenticate" is a system-shaping choice; if the
  answer is anything beyond a test-only grant, it likely needs an ADR.
- **Seeded people are still personal-data-shaped.** AC4 exists because a fake person with a
  realistic name is one careless copy from a real one.

## Testing Notes

The proof belongs in the smoke tier T-0015 builds, since it needs the real identity host —
that is precisely why T-0015 could not prove it.

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
