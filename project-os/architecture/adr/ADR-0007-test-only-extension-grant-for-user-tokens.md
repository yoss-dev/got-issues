# ADR-0007: Issue user-subject tokens through a test-only extension grant, deferring a real login model

## Status

Accepted

Chosen by the maintainer in a `refinement-session` on 2026-08-31, with all three candidates and their consequences put to them in writing (see [T-0018](../../product/tickets/T-0018-user-subject-tokens.md)'s Work Log). Accepted rather than Proposed because the decision is the maintainer's own, not an agent's recommendation awaiting review.

## Date

2026-08-31

## Context

Every token this system can issue is a **client-credentials** token. [ClientFactory.cs](../../../apps/GotIssues.IdentityHost/Configuration/ClientFactory.cs) sets `AllowedGrantTypes = GrantTypes.ClientCredentials`, and that flow authenticates a *client*, not a person — so the token carries no `sub`. Decoded from the running stack during [T-0015](../../product/tickets/T-0015-compose-stack-smoke-test.md):

```json
{"aud":"gotissues-api","client_id":"…","exp":…,"iat":…,"iss":"…","jti":"…",
 "role":"member","scope":["gotissues.api"]}
```

This is not a defect. It is what the grant type means, and it leaves a standing hole:

- [T-0009](../../product/tickets/T-0009-role-authorisation-and-user-projection.md)'s user projection never populates from a real request. Its AC5 and AC8 are proven only in an in-process test host, and acceptance confirmed the gap from both ends — seven real tokens decoded with no subject, and zero rows in `users` after a full traffic run. **That blind spot is what hid T-0009's claim-mapping bug behind forty green tests.**
- [T-0015](../../product/tickets/T-0015-compose-stack-smoke-test.md) AC8 is deferred to [T-0018](../../product/tickets/T-0018-user-subject-tokens.md) for exactly this reason.
- [T-0008](../../product/tickets/T-0008-comment-on-an-issue.md) cannot start: comment authorship is taken from the token, so without a subject the author is structurally null.
- [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md)'s assignee is testable against seeded rows but means nothing in a running system where nobody can be a person.

Meanwhile `PROJECT.md` §3 names a web or mobile UI as a **confirmed non-goal** for now: the API is the deliverable. So the decision is forced from two sides — the product needs people to exist, and it has explicitly declined to build the thing that normally makes them exist.

## Decision

**Add a test-only extension grant to the identity host that issues access tokens carrying a `sub` for seeded, fictional people, and gate it behind explicit configuration that is off by default.**

The grant is a deliberate stand-in for a login model, not an approximation of one. It exists so that identity-shaped behaviour — projection, assignment, authorship — is exercised by the running system rather than only by test doubles, while the question of how real people authenticate stays open until a UI makes the answer obvious.

Two constraints are part of the decision, not implementation detail:

1. **It must be refused unless explicitly enabled.** A configuration flag, off by default, so the grant cannot be reached by accident in anything resembling production. "Temporary" mechanisms that issue tokens for people outlive their justification; this one is designed so that outliving it requires someone to have switched it on.
2. **It must not create a second validation path.** Tokens it issues are validated by the resource server exactly as client tokens are — same issuer, audience, signing key and lifetime rules ([T-0018](../../product/tickets/T-0018-user-subject-tokens.md) AC5). A new token *shape* must not become a new token *path*; a parallel branch is precisely where T-0009's claim-mapping bug lived.

## Options Considered

**1. Test-only extension grant — chosen.** Days rather than weeks. Unblocks T-0006 and T-0008, closes T-0015's AC8, and commits the product to nothing about real login. *Rejected reason if it had been:* it issues tokens for people through a mechanism built for tests, and that is uncomfortable however carefully it is flagged.

**2. Authorization code with a login page.** The right answer eventually, and the one every real deployment ends at. Rejected **now** because it contradicts a confirmed non-goal — it requires a browser flow, a login UI and session handling, and `PROJECT.md` §3 says the API is the deliverable. It would also overrun T-0018's sizing and force a split. Choosing it would mean deciding the UI question by way of an authentication ticket, which is the wrong order.

**3. Resource-owner password grant.** Cheapest of the three that involve real credentials: users authenticate with username and password directly against the identity host. Rejected because it is a **deprecated** OAuth flow, and adopting a deprecated flow in a system whose stated premise is doing identity properly with Duende would undercut the reason Duende is here at all. Cheap now, embarrassing later, and harder to remove than option 1 because it looks legitimate.

## Consequences

### Positive

- The user projection, assignment and comment authorship become verifiable against the real stack — closing a blind spot that has already concealed one production-class bug.
- [T-0008](../../product/tickets/T-0008-comment-on-an-issue.md) can start; [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md)'s assignee becomes meaningful; [T-0015](../../product/tickets/T-0015-compose-stack-smoke-test.md) AC8's deferral closes.
- The login decision is deferred to the point where a UI exists and the answer follows from it, rather than being guessed now.

### Negative

- **The system gains a way to mint tokens for people that was built for testing.** The configuration gate makes that a deliberate act rather than an accident, but it does not make it pleasant.
- **It is a second thing to remove later.** When a real flow arrives, this grant must be deleted, not left disabled — a disabled mechanism is one configuration change from being an enabled one.
- Seeded people are personal-data-shaped even when fictional. [T-0018](../../product/tickets/T-0018-user-subject-tokens.md) AC4 keeps real names and addresses out of the repository, and that criterion is load-bearing rather than ceremonial.

## Risks

- **The gate is the whole safety argument, so the gate must be tested.** A flag that is documented as off-by-default and is in fact on-by-default would be worse than no flag, because the safety claim would be believed. This is the coverage-versus-claim failure [RETRO-SPRINT-002](../../delivery/retrospectives/RETRO-SPRINT-002.md) is about, and it should be mutation-proved: enable it in the test, confirm a token is issued; leave it default, confirm the request is refused.
- **The grant may quietly become the login model** by nobody ever revisiting it. The mitigation is that it cannot be used without being switched on, and that this ADR names its removal as the expected end state rather than an aspiration.
- Duende extension grants are a supported extension point, but this project runs Duende **unlicensed** for the PoC (`PROJECT.md` §4) — licence warnings are expected and are not evidence of a problem here.

## Follow-up Actions

- Implement via [T-0018](../../product/tickets/T-0018-user-subject-tokens.md), which now has this ADR and can be refined to `ready`.
- When a real authentication flow is chosen, supersede this ADR and **delete** the grant rather than disabling it.

## Related ADRs

- [ADR-0003](ADR-0003-initial-technology-stack.md) — establishes Duende IdentityServer as the identity provider; this decision extends how it issues tokens without changing that.

## Related Tickets

- [T-0018](../../product/tickets/T-0018-user-subject-tokens.md) — implements this decision
- [T-0015](../../product/tickets/T-0015-compose-stack-smoke-test.md) — its AC8 is the deferral this closes
- [T-0009](../../product/tickets/T-0009-role-authorisation-and-user-projection.md) — the projection that has never been exercised by a real request
- [T-0008](../../product/tickets/T-0008-comment-on-an-issue.md) — blocked entirely without a subject-carrying token
- [T-0006](../../product/tickets/T-0006-issue-lifecycle-fields.md) — its assignee becomes meaningful
