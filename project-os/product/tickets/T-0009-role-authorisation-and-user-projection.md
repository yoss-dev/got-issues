---
id: T-0009
title: Role-based authorisation and the user projection from token claims
type: feature
status: done
priority: high
owner: none
implemented_by: claude-sm-9d4e
accepted_by: claude-qa-7c21
depends_on: [T-0003, T-0010]
adrs: [ADR-0003, ADR-0005]
created: 2026-08-30
updated: 2026-08-31
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

- [x] AC1: Given a valid token carrying the `admin` role, when a caller requests an endpoint guarded by the admin policy, then the request is permitted.
- [x] AC2: Given a valid token carrying the `member` role, when a caller requests an endpoint guarded by the admin policy, then the API returns 403 — authenticated but not authorised, distinct from the 401 an invalid token produces.
- [x] AC3: Given a valid token of either role, when a caller requests an endpoint guarded by the member policy, then the request is permitted.
- [x] AC4: Given a valid token whose role claim is missing or holds an unrecognised value, when any guarded endpoint is requested, then the caller is treated as having no role and is refused — never silently promoted to `member` or `admin`.
- [x] AC5: Given an authenticated caller with no local user record, when they make a request, then a record is created from their token claims; and when they return later, then the existing record is updated rather than duplicated.
- [x] AC6: Given a user record, when it is inspected, then it holds no credential, secret, or role — the role is read from the token on every request and never persisted.
- [x] AC7: Given a request that creates or updates a user projection, when the log output emitted during that request is inspected, then it contains neither the display name nor the email address ([SECURITY.md](../../standards/SECURITY.md)).
- [x] AC8: Given a token whose subject is present but whose display-name claim is missing, when the caller makes a request, then the projection is still created and the caller is usable as an assignee — a missing optional claim does not fail the request.

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

- [x] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — walked item by item on 2026-08-31 by `claude-qa-7c21`; no deviation required.

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

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — implementation plan

Claimed via `pick-up-ticket` under `run-sprint`. `depends_on: [T-0003, T-0010]` — both verified `done` in the Completed table. DoR re-checked; still holds, and the sequencing note it carried ("T-0010 must be refined before this can be sensibly planned") is satisfied since T-0010 shipped.

**Approach**

- **Two authorisation policies**, `admin` and `member`, registered **once, centrally**, reading the `role` claim T-0010 emits. `admin` implies `member` — an admin can do anything a member can (`PROJECT.md` §5), so the member policy accepts either role rather than requiring an exact match.
- **AC4 is the criterion to design around, not to satisfy afterwards.** A missing or unrecognised claim must refuse. The plausible-looking bug is a policy that checks `role == "admin"` for admin and treats everything else as a member; that silently promotes an unknown claim. Both policies will therefore require the claim to be *one of the known values* — an allow-list, not a fallback.
- **The user projection**: a `users` table keyed by the token's subject, upserted on authenticated requests, holding subject and display name. **No role column** (AC6) — the role is read per request and never stored. A migration adds it.
- **Where the upsert runs.** Middleware after authentication is the obvious home. It sits on a hot path (T-0010's Risks noted this), so it writes only when something changed rather than on every request.
- **AC7 (no personal data in logs)** shapes the code, not just the tests: nothing logs the display name or email, and the log statements are source-generated as elsewhere.

**Test plan** — the harness exists, so this is automated, and per [TESTING.md](../../standards/TESTING.md) every coverage claim is verified by mutation:

| AC | How verified |
| --- | --- |
| AC1–AC3 | Role matrix through the test host's guarded endpoints: admin→admin ✓, member→admin **403**, either→member ✓ |
| AC4 | Tokens with a missing claim and with `role: superuser` — both refused. **Mutated** by making the policy fall back to member and confirming the test fails |
| AC5 | Two requests from one subject → one record, updated not duplicated |
| AC6 | Inspect the entity and the schema: no role, no credential |
| AC7 | Assert the captured log output for a projection-creating request contains neither value |
| AC8 | Token with subject but no display-name claim → projection created, caller usable |

**The 401-versus-403 distinction (AC2) is the subtle one.** Both are "refused" to a careless test. AC2 exists because they mean different things to a client, and T-0002 showed how easily a status-only assertion passes while the substance is wrong — there, a 401 that carried no body. Tests here assert the specific code, and the problem-document shape now applies to both.

**Risks I expect.** `PROJECT.md` **Q8** (data protection) is unanswered and this ticket is what first stores employee names — refinement recorded that seeded test identities are unaffected and only real employee data is gated, so it does not block. The upsert's cost on a hot path is real but measurable. And T-0010's own note stands: the API's authorisation is only as good as Duende's token issuance, which concentrates risk in one place by design.

**Branch:** `t-0009-roles-and-user-projection`.

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — implementation complete, handing to review

**Built.** Two authorisation policies reading the `role` claim, a `users` table holding the thin projection, middleware that maintains it, and 16 new tests. Suite is now **40**.

## The design decision that matters

**AC4 shaped the implementation rather than being satisfied after it.** The plausible implementation — *"admin if the claim says admin, otherwise member"* — reads correctly, passes every positive test, and silently promotes a caller whose claim is missing, empty, or unrecognised. So both policies use an **allow-list**: a role satisfies a policy only if it is one of the two the system knows. There is no fallback branch to get wrong.

**Mutation-proven, not asserted.** I replaced the member policy with that fallback and re-ran: **all four AC4 cases failed** — missing claim, `superuser`, empty string, and `Admin` with a capital A (the comparison is ordinal, so a case-different value is unrecognised rather than helpfully matched). Restored, all four pass.

## A structural problem I hit, and did not paper over

The projection tests failed at first: `Program.cs` registered the middleware inside the authentication block, and the integration test host builds its **own** front-of-pipeline through an `IStartupFilter`, which terminates before the app's middleware runs. The projection never executed there.

The tempting fix was to add the middleware to the test filter. **That is precisely the trap T-0002 hit** — a test host made to agree with the test rather than with the application, producing a fix that looks verified while production is broken.

Instead the ordering now lives in **one** method, `AuthenticationPipeline.UseGotIssuesAuthentication()`, called by both `Program.cs` and the test host. The two cannot drift, because there is only one definition of what the order is. The method carries a comment saying why it exists.

## Acceptance criteria — self-check

| AC | Result | Evidence |
| --- | --- | --- |
| AC1 | **pass** | Admin token → admin endpoint **200** |
| AC2 | **pass** | Member token → admin endpoint **403**, asserted as 403 *and* asserted not-401. The codes mean different things to a client, and a test asserting only "refused" would pass while the API said the wrong thing |
| AC3 | **pass** | Both roles → member endpoint 200. Admin satisfies the member policy: a floor, not an exact match (`PROJECT.md` §5) |
| AC4 | **pass** | Missing / `superuser` / empty / `Admin` all **403**, on both policies. **Mutation:** fallback implementation → all four fail |
| AC5 | **pass** | Two requests, one subject → one record, name updated to the token's current value. **Mutation:** remove the existing-record lookup → the test fails |
| AC6 | **pass** | Asserted against the EF model, not the class: properties are exactly `DisplayName`, `FirstSeenAt`, `LastSeenAt`, `Subject`. The migration contains **zero** role columns |
| AC7 | **pass** | Log output captured during a projecting request contains neither the display name nor an email. **Mutation:** add a `LogInformation("Projecting user {Name}")` → the test fails |
| AC8 | **pass** | Token with a subject and no display-name claim → 200, projection created, `DisplayName` null |

Per [TESTING.md](../../standards/TESTING.md), every coverage claim above that could be mutated, was — three mutations, each seen failing and then restored.

## Other decisions

- **The upsert writes only when something changed** — a name difference, or a last-seen timestamp older than five minutes. T-0010 recorded the hot-path cost as a risk; an unconditional write would be a database round trip on every authenticated request.
- **The test auth handler passes the role claim through verbatim**, without normalising or validating it. A test asking for `role: superuser` must produce exactly that, so the API's allow-list is what gets exercised rather than the test host's idea of a sensible value.
- **No role is stored.** Caching it would create a second source of truth that could disagree with the token a caller just presented.

## Known and flagged

`PROJECT.md` **Q8** (data protection) remains unanswered, and this ticket is what first stores employee names. Refinement established that seeded test identities are unaffected and only *real* employee data is gated, so it does not block — but this is the ticket that makes the question concrete rather than theoretical.

**Verification, each from the tool's own exit status:** `dotnet build` **0 warnings / 0 errors**, `dotnet format --verify-no-changes` **0**, `dotnet test` **0** (40 tests), `validate.py` **OK**, `check-drift.sh` **0**.

**Branch:** `t-0009-roles-and-user-projection`.

### 2026-08-31 — Software Engineer + Architect (claude-rev-8b4f) — code review

Independent `review-code` pass on `t-0009-roles-and-user-projection` @ `e9b085a`. I did not implement this. Docker under `-p gi-rev9` on ports 19080/19081, torn down with `down -v --rmi local`; the six unrelated stacks on this host were untouched.

**Verdict: REQUEST CHANGES.** The allow-list is right, and it is right for the reason the ticket said it had to be — I tried to break it the ways you asked and could not. But **the policies never see a real token's role claim at all**: against the identity host this project actually runs, both policies refuse every caller, including a genuine admin. The suite is green because the test host manufactures a claim shape the JWT pipeline never produces.

#### Blocking

**1. `HasRole` looks up a claim type the real pipeline does not emit. Both policies deny every real caller.**

`AuthorizationPolicies.HasRole` reads `user.FindAll("role")`. `Program.cs` calls `AddJwtBearer` without setting `MapInboundClaims`, so inbound claim mapping is left at its default and the token's `role` claim is rewritten before any policy sees it.

Evidence, in three steps, none of it from reading the code:

- A real admin token from the running identity host carries `"role": "admin"` in its JWT payload — confirmed by decoding it.
- Run through a `JwtBearer` pipeline configured **exactly** as `Program.cs` configures it (same authority, metadata address, audience, `RequireHttpsMetadata`, `ValidIssuer`; nothing added, nothing removed), the resulting principal carries the claim as type `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`. Against that principal:

  ```
  user.FindAll("role").Count()             -> 0     <- the expression HasRole uses
  user.FindAll(ClaimTypes.Role).Count()    -> 1
  ```

- The token itself is fine: `GET /health/authenticated` with that admin token returns **200** against the running API, and **401** anonymously. Authentication works; the failure is confined to the policy layer.

The tests pass because `TestAuthHandler` builds `new Claim("role", …)` — the short type, which the JWT pipeline never produces. So AC1 and AC3 are false against the real identity host, and AC4 passes for the wrong reason: everything is refused, including the callers who should be admitted.

It fails **closed**, and no shipped endpoint consumes the policies yet — `Program.cs` still guards `/health/authenticated` with a bare `RequireAuthorization()`. So nothing is broken in production today. That is luck of sequencing, not a property of the change: T-0004 is the first consumer, and it would find that its admin-only endpoint refuses admins.

Either fix works — `options.MapInboundClaims = false` on the JwtBearer options, which would also align the pipeline with the middleware's own primary lookups of `"sub"` and `"name"`; or have `HasRole` accept both `"role"` and `ClaimTypes.Role`. I have a mild preference for the first, because it makes the claim names in the token and the claim names in the code the same strings, which is what everyone will assume when reading either.

**What matters more than the fix is the test.** A regression test that uses `TestAuthHandler` cannot catch this, because the handler is the thing that is wrong. The test has to exercise real inbound claim mapping — a `JwtBearer`-configured host, or at minimum a principal built the way the handler builds one — or the next bug of this exact shape will be just as invisible.

**2. AC7's guard has a hole: a leak below `Information` is invisible to it.**

`CapturingLoggerProvider.IsEnabled` returns `true`, but the logging factory applies `Logging:LogLevel:Default` — `Information` in `appsettings.json` — *before* any provider is consulted. Demonstrated by mutation, both directions:

```
LogDebug("leak {Name}", displayName)        -> AC7 test PASSES   (leak invisible)
LogInformation("leak {Name}", displayName)  -> AC7 test FAILS    (your mutation)
```

The test's own comment claims "a future log statement that leaks one fails this test instead of passing review". That is true only for `Information` and above. AC7 is worded as "the log output emitted during that request", and a `LogDebug` is log output — one that will be enabled in exactly the environment where someone is debugging a projection problem with a real name in front of them. One line in `WithLogCapture` closes it: `builder.ConfigureLogging(l => l.SetMinimumLevel(LogLevel.Trace))`.

The current code logs nothing at all in the projection path, so there is no live leak. This is a weak guard, not a breach — but AC7 exists to be a durable guard, and a guard that silently covers half the levels is the kind of green check this project has now been bitten by repeatedly.

#### Answers to what you asked me to scrutinise

**The allow-list (item 1): I looked for a promotion hole and did not find one.** `ClaimsPrincipal.FindAll` matches the claim *type* case-insensitively, so `Role` and `ROLE` are found — but values are still filtered through `Known` with an ordinal comparer, so `"Admin"`, `" admin"`, `""` and `"superuser"` all fail. Multiple `role` claims behave correctly: any recognised value wins, which is right, since a caller genuinely holding `admin` alongside junk is an admin. A `roles` (plural) claim, a JSON-array value, and `ClaimTypes.Role` are all simply not found — refused. Every shape I tried fails closed. **The allow-list is sound. The defect is the opposite direction: the lookup never finds the real claim in the first place.**

**The shared pipeline (item 2): it genuinely removes ordering drift, but not the drift that bit you.** One definition, both callers, and I confirmed the test host really does run the projection middleware through it — that part is a real improvement and the right instinct. But the test host still *manufactures its own principal*, so it agrees with the test rather than with the token the application will actually see. Finding 1 is precisely that drift, surviving in the one dimension `UseGotIssuesAuthentication()` does not cover. The lesson generalises: sharing the wiring is necessary and not sufficient; what also has to be shared, or else independently pinned, is the shape of the identity flowing through it.

**AC6 (item 3): holds, and the assertion is stronger than it needed to be.** Asserting the exact property set rather than the absence of three names means a future `Role` property fails the test whatever it is called. The migration agrees: four columns, `Subject`/`DisplayName`/`FirstSeenAt`/`LastSeenAt`, no role, no credential.

**AC7 (item 4): the capture sees a subset — finding 2.**

**The upsert's write-avoidance (item 5): the staleness logic cannot skip a needed write.** A name change always writes regardless of timestamp; the only thing the five-minute window suppresses is a `LastSeenAt` refresh, which is what it is for. Two adjacent concerns instead — findings 3 and 4.

**AC2 (item 6): correct.** 403 asserted specifically, and the not-401 assertion is redundant but harmless.

#### Non-blocking

**3. Two concurrent first requests from one subject will 500.** Both find `existing is null`, both `Add`, and the second `SaveChangesAsync` violates the `users` primary key — `DbUpdateException`, unhandled in the middleware, surfacing as a 500 on a request that should have succeeded. It is a first-appearance-only race, so it is rare and self-healing, but a client opening two parallel connections on first use is not exotic. Catching the update exception and re-reading, or an `ON CONFLICT DO NOTHING` upsert, resolves it. **Analysis, not demonstration** — I could not exercise it live for the reason in finding 5.

**4. `LastSeenAt` can be five minutes stale and only the middleware knows.** The imprecision is a deliberate, correct trade, but `UserRecord.LastSeenAt` carries no XML doc saying so, and it is a public property on a projection other tickets will read. Whoever writes "last active" against it will believe it is exact. One sentence on the property.

**5. AC5 and AC8 are proven only in the test host, because no token this system can currently issue has a subject.** The identity host issues client-credentials tokens; the real payload I decoded carries `client_id` and **no `sub`**. `UserProjectionMiddleware` requires `ClaimTypes.NameIdentifier` or `"sub"`, finds neither, and correctly skips — so against the running stack no projection is ever created. The middleware's behaviour is right (a machine client is not a user), and this is downstream of T-0010's still-open admin-provisioning question rather than a defect here. But it is worth recording plainly, because *that blind spot is what hid finding 1*: every verification of this ticket ran against a synthetic principal, and the one thing nobody could do was watch a real token flow through.

**6. `Assert.NotEqual(HttpStatusCode.Unauthorized, …)` is redundant** after asserting equality with `Forbidden`. The intent is already carried by the comment. Take or leave.

#### On `PROJECT.md` Q8 — I agree it does not block

Refinement's distinction is the right one and I would not overturn it: Q8 gates loading *real employee data*, and implementing and testing against seeded identities does not do that. I would add one observation that strengthens rather than weakens the position — per finding 5, no token this system can presently issue carries a subject, so the `users` table cannot receive any personal data at all today. The question becomes concrete when T-0010's provisioning answer produces tokens with real subjects, which is the right moment for it.

#### Gates, each read from the tool's own exit status

`validate.py` **OK** (17 tickets, 6 ADRs) · `dotnet build --no-incremental` **0 warnings / 0 errors** · `dotnet format --verify-no-changes` **clean** · `dotnet test` **40/40** (2 unit, 38 integration) · `check-drift.sh` **0** · working tree clean.

Every gate is green, and finding 1 is a working authorisation layer that refuses everyone. That is the point of the finding.

- **Did:** Read the change against scope and the ACs. Hunted the allow-list for a promotion hole across claim type case, multiple claims, whitespace, case-different values, array values, plural `roles` and `ClaimTypes.Role`. Decoded a real admin token, then ran it through a `JwtBearer` pipeline configured identically to `Program.cs` and inspected the resulting claim types. Confirmed against the running API that the token is accepted, isolating the failure to the policy layer. Mutated the AC7 log capture at two levels. Read the migration and the EF model. Ran every gate.
- **Decided:** the allow-list design is correct and should not change; the fix belongs at the claim-mapping boundary, not in the policy logic.
- **Remaining:** findings 1 and 2 on the branch, with a regression test for 1 that does not rely on `TestAuthHandler`. Findings 3–6 are take-or-leave.
- **Open questions / blockers:** none for the reviewer.
- **Branch / PR:** `t-0009-roles-and-user-projection` @ `e9b085a` — **not merged**.
- **Test state:** 40/40 green; finding 1 is invisible to the suite by construction, which is the substance of the finding.
- **Review verdict:** **Request changes** — ENG + ARCH (`claude-rev-8b4f`).

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — response to review by claude-rev-8b4f

**Finding 1 is the deepest instance of this project's recurring failure, and it was mine.**

The policies read `user.FindAll("role")`. `AddJwtBearer` maps inbound claims by default, so a real token's `role` arrives as `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` — and **the policies would have refused every real caller, including a genuine admin.** The 40 tests passed because `TestAuthHandler` constructs `new Claim("role", …)`, the short type the JWT pipeline never produces.

So AC1 and AC3 were **false against the real identity host**, and AC4 passed for the wrong reason — everything was refused, including the things that should have been allowed. My suite agreed with my test host rather than with reality. That is the same shape as T-0002's 401 and T-0009's own pipeline seam, and the most consequential version of it so far: an authorisation layer that refuses everyone, verified green.

It failed **closed**, and no shipped endpoint consumes the policies yet — [T-0004](T-0004-create-and-list-projects.md) is the first. That is sequencing luck, not design.

**Fixed at the boundary and defensively.** `MapInboundClaims = false`, so the identity host's claim survives as written — that is the actual fix. The policies additionally read `ClaimTypes.Role`, so re-enabling mapping later, or adding a scheme that maps, degrades authorisation to *working* rather than silently refusing everyone.

**The regression tests deliberately use no test authentication handler**, because the handler was the thing that was wrong. `AuthorizationPolicyTests` builds principals itself under **both** claim types and asks the real policies through `IAuthorizationService`.

**Mutation-proven both ways:** reverting to the short-claim-only read fails **3** unit tests; reverting *both* halves fails the same 3. And when a `git checkout` during that work accidentally discarded the fix, the suite went red immediately and told me — which is what these tests are for.

**Finding 2 — the AC7 guard was blind below `Information`.** The logging *filter* runs before any provider, so `IsEnabled => true` in a capturing provider buys nothing: a leak written with `LogDebug` passed while the same leak at `LogInformation` failed. `SetMinimumLevel(LogLevel.Trace)` in `WithLogCapture` fixes it. A guard that cannot see the most likely place a leak would appear — a debug diagnostic — was worth less than it looked.

**Non-blocking, actioned:**
- **Concurrent first requests from one subject returned 500.** Both see no record, both insert, the second violates the primary key. Now caught: the other request created the projection, which is the outcome wanted, so it is a race lost rather than a failure to report. Test added asserting both requests get 200 and exactly one record exists. Same shape as [T-0004](T-0004-create-and-list-projects.md)'s recorded risk — a read-then-insert that passes every single-threaded test.
- **`LastSeenAt`'s five-minute imprecision** is now documented on the property itself, since other tickets will read it.
- **The redundant not-401 assertion** removed; `Assert.Equal(Forbidden, …)` already excludes it.

**On the reviewer's Q8 strengthening — accepted and worth recording:** no token this system can currently issue carries a `sub`, so the `users` table cannot receive personal data at all today. Q8 becomes concrete when T-0010's admin-provisioning answer produces real subjects. That is a better statement of the position than refinement's.

**Also accepted:** AC5 and AC8 are proven only in the test host, for the same reason — and that blind spot is exactly what hid finding 1.

**Verification, each from the tool's own exit status:** `dotnet build` **0 warnings / 0 errors**, `dotnet format --verify-no-changes` **0**, `dotnet test` **0** (**54 tests**), `validate.py` **OK**, `check-drift.sh` **0**.

### 2026-08-31 — Software Engineer + Architect (claude-rev-8b4f) — re-review

Second `review-code` pass on `t-0009-roles-and-user-projection` @ `b309930`. Docker under `-p gi-rev9b` on ports 19180/19181, torn down with `down -v --rmi local`; the six unrelated stacks were untouched.

**Verdict: REQUEST CHANGES — one finding, and it is the one you asked me to confirm rather than assume.** Finding 1 is genuinely fixed and I verified it the way it should have been verified originally. **Finding 2 is not fixed:** `SetMinimumLevel(LogLevel.Trace)` does not do what it looks like it does, and the AC7 guard is still blind to a `LogDebug` leak. I have the working one-line remedy below, proven.

#### Finding 1 — fixed, and confirmed against a real token

I rebuilt the check that originally caught it, but pointed at the branch's **real policy code** this time: a host with a `ProjectReference` to `apps/GotIssues.Api`, calling the real `AddGotIssuesPolicies()`, endpoints guarded by the real `AuthorizationPolicies.Admin` / `.Member`, JwtBearer configured exactly as `Program.cs` now configures it — against the running identity host, with tokens it actually issued.

The claim now survives as the identity host wrote it — `role = admin`, not the WS-Federation URI — and the full matrix is correct:

```
admin token  -> /admin   200        member token -> /admin   403
admin token  -> /member  200        member token -> /member  200
no token     -> /member  401        bad token    -> /member  401
```

AC1–AC4 now hold against reality rather than against the test host. That is the check that was missing, and it passes.

**The defensive half works too, and I tested it rather than reasoning about it.** I re-enabled `MapInboundClaims = true` in the probe to simulate the misconfiguration: the claim arrived as `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` and the admin token still got **200** on both endpoints. It degrades to working, exactly as the comment claims.

#### On whether the defensive half is wrong — keep it

You put the counter-argument well, and it is the right instinct applied to the wrong failure. Three reasons it does not apply here:

1. **The failure it prevents is not loud — it is the quietest one available.** Without the fallback, a misconfiguration produces blanket `403`s, which is indistinguishable from a policy working correctly. That is not "failing loudly"; it is precisely the failure that just consumed two review rounds. Removing the net does not convert a silent failure into a noisy one.
2. **It widens where a role may be carried, not what counts as a role.** `GotIssuesRoles.Known` is untouched, the comparison is still ordinal, and both claim types can only be populated by a token this API's own issuer signed or by a scheme the app registered. No new trust surface — I re-ran the promotion probes and every unrecognised shape still fails closed under both types.
3. **It restores a symmetry that was itself part of the bug.** `UserProjectionMiddleware` already reads `ClaimTypes.NameIdentifier ?? "sub"` and `"name" ?? ClaimTypes.Name`. The policy was the one place that took a claim name literally. Being defensive about the subject and literal about the role, in adjacent files, is what made the defect easy to miss.

**But your concern has a better answer than deleting it: nothing pins the actual fix.** `MapInboundClaims = false` appears in `Program.cs` and in two comments, and in **no assertion** — I grepped. Delete that line and all 54 tests still pass, because the fallback silently carries it. That is exactly the misconfiguration you want to see fail loudly. A test resolving the configured `JwtBearerOptions` from a host and asserting `MapInboundClaims == false` gives you the loud failure *and* keeps the safety net. That is the synthesis, and it is the one thing I would add.

#### Blocking — Finding 2 is not fixed

`services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Trace))` sets `LoggerFilterOptions.MinLevel`. But `appsettings.json` binds `Logging:LogLevel:Default: Information` as a **filter rule**, and rule matching takes precedence — `MinLevel` applies only when no rule matches. The configuration rule still matches everything, so nothing below `Information` reaches the provider. Demonstrated, three runs of the same injected leak:

```
LogInformation leak, as shipped                     -> Failed!   (control: harness works)
LogDebug leak, as shipped                           -> Passed!   (still blind)
LogDebug leak, with AddFilter((_, _) => true)       -> Failed!   (gap closes)
```

So the guard's stated promise — "a future log statement that leaks one fails this test instead of passing review" — remains false below `Information`, which is the level someone will reach for while debugging a projection problem with a real name in front of them. Replace the `SetMinimumLevel` call with `logging.AddFilter((_, _) => true)` (or clear `LoggerFilterOptions.Rules` and set `MinLevel`); I verified the former closes it.

There is still no live leak — the projection path logs nothing at all — so this is a weak guard rather than a breach. It is blocking because it is a security guard that reports stronger than it is, and because you asked me to confirm rather than assume it: had I assumed, this would have shipped as fixed.

#### Verified fixed, non-blocking items

- **The concurrency guard is real, not vacuous.** My first concern with a `Task.WhenAll` race test is that it can pass without ever colliding. It does not: removing the `catch (DbUpdateException)` fails `Concurrent_first_requests_from_one_subject_do_not_fail` on **5 of 5** runs. `ChangeTracker.Clear()` in the handler is the right touch — without it the failed insert stays tracked for the rest of the scope.
- **`LastSeenAt`'s imprecision is documented** on the property, including the instruction not to build exact last-activity features on it. That is the right place: the next reader meets it on the type, not in the middleware.
- **The redundant not-401 assertion is gone.**
- **`AuthorizationPolicyTests` is the right shape.** No test authentication handler, principals built directly under both claim types, real policies asked through `IAuthorizationService`. It also covers an unauthenticated principal holding `admin` and a mixed multi-claim principal — both worth having.

#### One trap worth a comment, non-blocking

With `MapInboundClaims = false`, `TokenValidationParameters.RoleClaimType` still defaults to `ClaimTypes.Role`, so `User.IsInRole("admin")` returns **false** against a real token even for a genuine admin. Nothing uses `IsInRole` today, and the policies are the sanctioned route — but it is the idiomatic API, and the next person to reach for it will get a silent negative of exactly the family this ticket just spent two rounds on. Either set `options.TokenValidationParameters.RoleClaimType = "role"` alongside the mapping line, or say in the comment that `IsInRole` is not wired and the policies are the way in.

#### On Q8

Recorded as adopted; nothing further from me. The statement is now tied to a verifiable fact about the tokens rather than to a judgement, which is why it is the better form.

#### Gates, each read from the tool's own exit status

`validate.py` **OK** (17 tickets, 6 ADRs) · `dotnet build --no-incremental` **0 warnings / 0 errors** · `dotnet format --verify-no-changes` **clean** · `dotnet test` **54/54** (15 unit, 39 integration) · `check-drift.sh` **0** · working tree clean.

- **Did:** Rebuilt the real-token check against the branch's own policy code and confirmed the full role matrix with tokens the running identity host issued. Re-enabled inbound mapping to test the defensive half rather than reason about it. Mutated the AC7 guard at two levels and then proved a working remedy. Mutated the concurrency catch five times to check the new test is not vacuous. Ran every gate.
- **Decided:** the defensive half should stay; the misconfiguration concern is better answered by pinning `MapInboundClaims` in a test than by removing the net.
- **Remaining:** finding 2 — one line, with the remedy verified. The `IsInRole` note and the `MapInboundClaims` pin are take-or-leave.
- **Open questions / blockers:** none for the reviewer.
- **Branch / PR:** `t-0009-roles-and-user-projection` @ `b309930` — **not merged**.
- **Test state:** 54/54 green; the AC7 test is green and would stay green through a `LogDebug` leak, which is the finding.
- **Review verdict:** **Request changes** — ENG + ARCH (`claude-rev-8b4f`).

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — response to the second review

**Finding 2 was not fixed, and the reviewer caught that by testing my fix rather than accepting it.** `SetMinimumLevel(LogLevel.Trace)` sets `LoggerFilterOptions.MinLevel`, but `appsettings.json` binds `Logging:LogLevel:Default: Information` as a filter **rule**, and rule matching takes precedence — `MinLevel` applies only when no rule matches. So the guard was still blind at `Debug`, which is exactly where a leak would appear: the level someone reaches for while debugging a projection with a real name in front of them.

Replaced with `AddFilter((_, _) => true)`. **Mutation-proven with the reviewer's own probe:** a `LogDebug` leak now fails the guard, where before it passed.

This is worth naming precisely, because it is a variant this project had not yet seen: **I fixed the right problem with a mechanism that did not work, and every signal said it had.** Not a green check measuring the wrong thing — a green check measuring the right thing through a mechanism that was inert.

**"Nothing pins the actual fix" — the sharpest point in the review.** `MapInboundClaims = false` appeared in `Program.cs` and two comments and in **no assertion**: deleting it left all 54 tests green, because the defensive fallback silently carried them. The safety net was hiding the removal of the thing it was a net for.

Now pinned. `Inbound_claim_mapping_is_disabled_and_the_role_claim_is_named` asserts the configured `JwtBearerOptions`. **Mutation-proven:** deleting the line now fails that test, where previously nothing failed.

**On keeping the defensive half — argument accepted, and it is better than mine.** I had wondered whether the fallback hides a misconfiguration I would rather see fail loudly. The reviewer's answer: without it, a misconfiguration produces blanket 403s, which are *indistinguishable from a policy working correctly* — the quietest failure available, not a loud one. And the fallback widens where a role may be **carried**, never what counts as one; the value allow-list is untouched. The right response to my concern was to pin the configuration, not to remove the net.

**The `IsInRole` trap — removed rather than commented.** With mapping off, `RoleClaimType` still defaulted to the WS-Federation URI, so `User.IsInRole("admin")` would return **false** for a genuine admin. Nothing uses it today and it is the idiomatic API the next person will reach for — a silent negative of exactly the family that made the policies refuse everyone. `RoleClaimType = "role"` now points it at the real claim, and the pinning test asserts that too.

**Confirmed by the reviewer, not by me:** the fix works against a real token from the running identity host, through the branch's real policy code, with the full matrix correct — and the defensive half degrades to working when mapping is re-enabled. That is the check that would have caught this originally, and I asked for it rather than assuming.

**Verification, each from the tool's own exit status:** `dotnet build` **0 warnings / 0 errors**, `dotnet format --verify-no-changes` **0**, `dotnet test` **0** (**55 tests**), and two mutations each seen failing and restored.

### 2026-08-31 — Software Engineer + Architect (claude-rev-8b4f) — third review pass

Third `review-code` pass on `t-0009-roles-and-user-projection` @ `1b23569`. Docker under `-p gi-rev9c` on ports 19280/19281, torn down with `down -v --rmi local`; the six unrelated stacks were untouched.

**Verdict: APPROVE. The change-set may merge.** All three blocking findings raised across the three passes are fixed, and I verified each by making it fail rather than by reading the fix. `RoleClaimType = "role"` does have two consequences worth knowing before T-0004 — both demonstrated below, both fail-closed, neither a reason to hold the merge.

#### The three things you asked me to confirm

**1. The log guard genuinely closes at `Debug`.** Same injected leak, same probe as before, against the merged state:

```
LogDebug leak of the display name  ->  Failed!      (previously: Passed! — blind)
```

`AddFilter((_, _) => true)` does what `SetMinimumLevel` could not. AC7's promise — "a future log statement that leaks one fails this test" — is now true at every level.

**2. The pinning test fails on removal, both halves.**

```
baseline                                  ->  Passed!
delete `MapInboundClaims = false`         ->  Failed!
delete `RoleClaimType = "role"`           ->  Failed!
```

So the configuration is pinned rather than merely present, and the safety net no longer hides the removal of the thing it is a net for. That was the gap; it is closed.

**3. `RoleClaimType = "role"` — two consequences, both real, both demonstrated with live tokens.**

I built a host referencing the branch's own policy code, configured exactly as `Program.cs` configures it, and asked it with tokens the running identity host issued.

**(a) The trap you removed is genuinely removed, and a new one takes its place one level down.** `IsInRole` now answers correctly — `admin` token → `{admin: true, member: false, superuser: false}`, `member` token → `{admin: false, member: true}`. But switching the framework's role machinery on means `RequireRole` / `[Authorize(Roles = …)]` now *work*, and they do not mean what the policies mean:

```
                    policy        RequireRole
admin  -> admin     200           200
admin  -> member    200           403     <-  the divergence
member -> admin     403           403
member -> member    200           200
```

`AuthorizationPolicies.Member` is a floor — an admin satisfies it, per `PROJECT.md` §5. `RequireRole("member")` is an exact match, so it refuses an admin. Before this change the framework syntax refused *everyone*, which is loudly wrong and would have been noticed in minutes. It is now subtly wrong in exactly one case — and that case, "an admin using a member-guarded endpoint", is the one T-0004 onward will hit most.

I want to be straight that this trade came from my own suggestion last pass, so weigh my assessment accordingly: I still think it is net positive, because the divergence **fails closed** — it denies access it should grant and never grants access it should deny — whereas the `IsInRole` silent negative made a genuine admin invisible to the idiomatic API. Related and smaller: `IsInRole("superuser")` would now return `true` for a token carrying that value, while the policies still refuse it, so the `GotIssuesRoles.Known` allow-list is enforced by one mechanism and not the other.

**The cheap durable guard is documentation where the choice is made**: a line on `AuthorizationPolicies.Admin` / `.Member` saying these are the sanctioned mechanism and that `[Authorize(Roles = "member")]` is *not* equivalent, because the member policy is a floor. Someone picking a mechanism reads the constants; they do not read `Program.cs`.

**(b) The same asymmetry now exists for names, latent.** `NameClaimType` was left at its default while claims arrive verbatim, so `User.Identity.Name` is null for a token that carries a `name` claim:

```
identityName, as configured today          ->  null
identityName, if NameClaimType = "name"    ->  "Sam Example"
```

That is the same silent-negative family you just removed for roles. It is invisible today because client-credentials tokens carry no `name` at all — which is also why nothing caught it — and it becomes live the moment T-0010's provisioning answer produces user tokens. `UserProjectionMiddleware` is unaffected: it reads the `name` claim explicitly. Either set `NameClaimType = "name"` alongside the role line for symmetry, or leave it and know why.

#### Everything else, re-confirmed

The concurrency catch still fails its test 5/5 when removed; the policy allow-list still refuses every unrecognised shape under both claim types — I re-ran the promotion probes against the new configuration, and `RoleClaimType` does not touch `HasRole`, which reads both claim types explicitly and filters through `Known`.

#### On the variant you named

*"I fixed the right problem with a mechanism that did not work, and every signal said it had."* That is worth keeping, because it is genuinely distinct from the ones this project has already recorded. The earlier instances were checks pointed at the wrong thing — a stale layer, a lock file grepped case-sensitively, a test host agreeing with the test. This one was pointed at exactly the right thing and ran through an inert mechanism, so every signal was honest and the conclusion was still false. The defence is not a better-aimed check; the aim was fine. It is that **a fix is not verified until it has been observed failing without it** — which is the mutation rule `TESTING.md` already states, applied to the *fix* rather than to the feature. Both of this pass's confirmations were of that shape, and both were cheap.

#### Gates, each read from the tool's own exit status

`validate.py` **OK** (17 tickets, 6 ADRs) · `dotnet build --no-incremental` **0 warnings / 0 errors** · `dotnet format --verify-no-changes` **clean** · `dotnet test` **55/55** (15 unit, 40 integration) · `check-drift.sh` **0** · working tree clean.

#### Merge decision

**Merge.** Every acceptance criterion has been verified against running software or an executed mutation. Over three passes this ticket had three blocking findings, and the one that mattered — an authorisation layer that would have refused every real caller behind 40 green tests — is fixed at the boundary, defended in depth, pinned by an assertion that fails on removal, and confirmed end to end with tokens the real identity host issued. The two items in 3(a) and 3(b) are take-or-leave; I would take (a)'s one-line comment before T-0004 starts, because that is the ticket that will meet it.

- **Did:** Mutated the log guard at `Debug` against the merged state; mutated both halves of the pinning test; built a host on the branch's own policy code and compared policy semantics against `RequireRole` with live admin and member tokens; demonstrated the `NameClaimType` asymmetry; re-ran the promotion probes and the concurrency mutation; ran every gate.
- **Decided:** approve. The `RequireRole` divergence is fail-closed and unused, so it is a documentation matter, not a merge blocker.
- **Remaining:** implementer merges and performs the handover status commit. The two non-blocking notes need no re-review.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0009-roles-and-user-projection` @ `1b23569` — **approved for merge**.
- **Test state:** 55/55 green, and the three fixes are green *for the right reason* — each seen failing without its fix.
- **Review verdict:** **Approve** — ENG + ARCH (`claude-rev-8b4f`).

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — both take-or-leave notes taken

**(a) The framework's role syntax now works, and does not mean what the policies mean.** Setting `RoleClaimType` switched on `RequireRole` / `[Authorize(Roles = …)]`, which do an **exact** match — so `Roles = "member"` refuses an admin, while `AuthorizationPolicies.Member` is a floor an admin satisfies (`PROJECT.md` §5). The reviewer demonstrated it: admin → member endpoint returns **200** by policy and **403** by `RequireRole`.

Previously the framework syntax refused everyone, which was loudly wrong. It is now *subtly* wrong in exactly the case [T-0004](T-0004-create-and-list-projects.md) onward meets most. The divergence fails closed — it denies access it should grant, never the reverse — which makes it quiet rather than harmless.

Guarded where it will actually be read: the policy constants themselves now state that `[Authorize(Roles = …)]` and `RequireRole` are **not** equivalent and must not be substituted. People reach for the constants; nobody reads `Program.cs` before writing an endpoint.

Recorded honestly: this trade came from the reviewer's own suggestion last pass, and they said so and told me to weigh their assessment accordingly. I agree it is net positive for the reason they gave.

**(b) The same asymmetry existed for names, latent.** `NameClaimType` was left at its default while claims arrive verbatim, so `User.Identity.Name` would be **null** for a token plainly carrying `name`. Invisible today because client-credentials tokens carry none — which is why nothing caught it — and live the moment T-0010's provisioning produces user tokens. Set to `"name"`, and the pinning test now covers all three settings.

**On the reviewer's framing of the new failure variant**, which is the most useful thing to come out of this ticket:

> The earlier instances were checks pointed at the wrong thing; this one was pointed at exactly the right thing and ran through an inert mechanism, so every signal was honest and the conclusion still false. The defence isn't better aim — the aim was fine. **A fix isn't verified until it's been seen failing without it**: the mutation rule applied to the fix rather than the feature.

The project's standard already requires mutation-verifying that a *test* guards a *behaviour*. It does not say the same of a *fix*. Both confirmations this pass were that shape and both were cheap — worth raising at the retrospective rather than deciding here.

**Verification:** `dotnet build` **0 warnings / 0 errors**, `dotnet format --verify-no-changes` **0**, `dotnet test` **0** (**55 tests**).

### 2026-08-31 — QA / Test Engineer + Product Owner (claude-qa-5a71) — acceptance

Independent `acceptance-test` pass on `main` @ `c92bc24`. I did not implement this (`claude-sm-9d4e`) and did not review it (`claude-rev-8b4f`). I derived my checks from the requirements before reading the Work Log. Docker ran under `-p gotissues-qa5a71t9` on ports 18190/18191 (Postgres exposed on 15432 for the probe), torn down with `down -v --rmi local`; the six unrelated stacks were verified intact afterwards.

**Verdict: PASS.** All eight criteria verified — AC1–AC4 and AC6 against the running system rather than the test host. Two defects recorded which must be fixed or ticketed before `done` (Q1, Q2), and six observations.

#### How I verified AC1–AC4 — real tokens, the API's own `Program.cs`, no test handler

The history here is that the suite agreed with the test host. So I did not build a replica of the API's authentication configuration either — a hand-copied replica can diverge the same way. I booted **`WebApplicationFactory<Program>`**, which runs the API's real `Program.cs` and therefore its real `AddJwtBearer` configuration and real `AddGotIssuesPolicies()`, pointed it at the running identity host and the running PostgreSQL, and drove it with tokens that identity host actually issued. The only thing I added was the guarded endpoints themselves, which is what the ticket's Technical Notes sanction. `TestAuthHandler` was not involved.

Then I widened what "a real token" means. The seeded identities give only `admin` and `member`, so I seeded five more clients through the identity host's own `Seed__Clients__*` configuration, carrying exactly the role values AC4 must refuse. **These are genuine signed tokens from Duende, not manufactured principals** — decoded to confirm the payload before use:

```
gotissues-admin-client   role=admin          gotissues-member-client  role=member
qa-superuser-client      role=superuser      qa-casing-client         role=Admin
qa-whitespace-client     role=' admin'       qa-norole-client         role=''
qa-multi-client          role='member,admin'
```

Attribution asserted first per `TESTING.md`: all services healthy before any response was trusted, and with the `api` container stopped both `/health` and `/health/authenticated` returned `000`; restarted, they answered again.

**The matrix, every cell from a real token:**

```
identity              /admin  /member   RequireRole("member")
admin                    200      200                     403
member                   403      200                     200
superuser                403      403                     403
Admin  (capitalised)     403      403                     403
' admin' (leading space) 403      403                     403
''     (empty)           403      403                     403
'member,admin'           403      403                     403
no token         -> 401        bad token -> 401
```

- **AC1 — PASS.** A real admin token reaches the admin-policy endpoint: **200**.
- **AC2 — PASS.** A real member token on the admin endpoint: **403**, and the 401 cases are genuinely distinct — no credentials and an invalid token both return **401** on the same endpoint. The distinction AC2 exists for is real in the running system, not just asserted in a test.
- **AC3 — PASS.** Both real roles reach the member endpoint: 200 and 200. The floor semantics hold.
- **AC4 — PASS**, and this is the criterion I attacked hardest, because it is the one that fails open if it fails.

#### AC4 — I hunted the allow-list for a promotion hole and did not find one

Five of the shapes above are real tokens the identity host signed, which is a stronger test than either prior pass ran — the reviewer had real `admin` and `member` tokens and synthesised the rest. Every one is refused by **both** policies.

I then ran fifteen further shapes directly against the real policies through `IAuthorizationService`, including several nobody had tried:

```
roles (plural) = admin            refused      role = "admin" (quoted)        refused
role = 'admin ' (trailing space)  refused      role = admin\0 (null byte)     refused
role = аdmin (Cyrillic а)         refused      role = ADMIN                   refused
role = ["admin"] (JSON array)     refused      role = admin;member            refused
http://schemas.microsoft.com/identity/claims/role = admin        refused
role=junk AND role=admin          admin granted   (correct: a genuine admin among junk)
role=member AND role=superuser    member only     (correct)
no claims at all                  refused      role=admin, unauthenticated    refused
```

Two things worth recording rather than re-finding: the claim **type** is matched case-insensitively, so `ROLE` and `RoLe` are found (observation N2), and the defensive fallback is narrow — only `ClaimTypes.Role` is accepted, not any role-shaped URI, so `.../identity/claims/role` is refused.

**Mutation.** I replaced the member policy with the plausible fallback the ticket warned about — *anyone authenticated who is not an admin is a member* — and the suite went red: **7 unit tests and 4 integration tests failed**. Restored, all green. The allow-list is load-bearing and its guards are real.

#### AC5 — PASS for every caller the system can currently produce; see defect Q1

First request creates the record; a return visit updates it rather than duplicating; the name follows the token. Verified, and **mutation-proven**: replacing the existing-record lookup with `null` (always insert) fails `Returning_updates_the_record_rather_than_duplicating_it`.

Adjacent behaviour I checked that no test covers, all correct: `FirstSeenAt` does not move on a return visit; two subjects sharing a display name produce two records (the ticket's Examples); a name disappearing from the token clears the stored one; a whitespace-only subject is skipped without failing the request; and a caller **refused** by the policy still gets a projection, because the middleware runs between authentication and authorisation — which is the documented ordering and the right one, since identity is established before permission.

The concurrency guard is not vacuous: removing the `catch (DbUpdateException)` fails `Concurrent_first_requests_from_one_subject_do_not_fail` on **3 of 3** runs.

#### AC6 — PASS, verified against the deployed schema rather than the model

The existing test asserts the EF model. I checked the database the stack actually created:

```
Table "public.users"
 Subject      character varying(200)    not null   (PK)
 DisplayName  character varying(400)
 FirstSeenAt  timestamp with time zone  not null
 LastSeenAt   timestamp with time zone  not null
```

And swept the whole API schema for any column named like a role or a credential — `information_schema.columns` matching `%role%`, `%secret%`, `%password%`, `%credential%`, `%token%` across `public`: **no rows**. The migration is reversible (`Down` drops the table).

#### AC7 — PASS, and the guard is genuinely closed at every level

This one was fixed twice, so I tested the fix rather than the feature. Injecting a display-name leak into the projection path:

```
baseline, no leak                              Passed
inject LogInformation leak of the display name Failed
inject LogDebug        leak of the display name Failed
inject LogTrace        leak of the display name Failed
```

**And the counterfactual, which is what attributes the fix:** reverting `AddFilter((_, _) => true)` to `SetMinimumLevel(LogLevel.Trace)` and injecting the same `LogDebug` leak → **Passed** — blind again. So the one-line remedy is precisely what closes the guard, and the earlier inert mechanism is confirmed inert by my own run rather than by the record.

One thing I did not expect and is worth recording: the project's analyzers (`CA1848`, `CA1873`) make an ad-hoc `logger.LogDebug(...)` a **build error**, so a leak would have to be written deliberately as a source-generated `LoggerMessage`. That is a second, unclaimed barrier in front of AC7 (observation N4).

#### AC8 — PASS in the test host; see observation N5

A token carrying a subject and no display-name claim returns 200, creates the projection, and leaves `DisplayName` null. Provable only in the test host today, for the reason in N5.

#### Q1 — Defect: the `DbUpdateException` catch swallows every write failure, not only the race it was added for

The catch was added for a specific, well-understood collision — two first requests from one subject, one losing on the primary key — and its comment reasons correctly *about that case*: "the other request created the projection, which is the outcome we wanted". But it catches `DbUpdateException` **unconditionally**, and that reasoning is false for any other cause.

**Reproduction.** A subject of 230 characters — legal under OIDC, which caps `sub` at 255, and longer than the column's 200:

```
230-char subject   -> HTTP 200, rows stored = 0, nothing logged
200-char subject   -> HTTP 200, rows stored = 1      (the boundary)
450-char display name -> HTTP 200, rows stored = 0, nothing logged
```

**Attribution, by removing the catch:** the same request then raises `Microsoft.EntityFrameworkCore.DbUpdateException` ← `Npgsql.PostgresException 22001: value too long for type character varying(200)`. So the catch is what converts a genuine failed write into a silent success.

AC5 says a record *is created*. For these inputs none is, the caller is told 200, nothing is logged, and the caller is then silently unusable as an assignee (T-0006) or comment author (T-0008) — the entire purpose of the projection. The guard test passes either way, because it only exercises the duplicate-key path.

- **Severity: low today, and I did not fail the ticket on it.** No token this system can issue carries a subject at all (N5), so there is no caller for whom this is currently reachable; AC5 holds for every caller that can presently exist. That is why the verdict is PASS.
- **But it is the ticket's own failure family** — something that fails silently and reports success — and it becomes reachable the moment T-0010's provisioning produces real subjects, which is the same moment AC5 and AC8 first get real evidence.
- **Fix shape, not mine to write** (`acceptance-test` MUST NOT modify implementation code): narrow the catch to the unique-violation case (`PostgresException.SqlState == "23505"`), or re-read after the failure and treat it as a race only when the record now exists. Anything else should surface.

#### Q2 — Defect: the documentation this change falsifies was not updated

The change-set touches **no documentation at all** — `git show --name-only` lists only source, tests, the migration and this ticket.

- **`README.md:97`** still lists, under *"Not here yet"*: *"Role-based authorisation. Tokens carry a `role` claim, but **nothing reads it yet** — that is T-0009."* T-0009 has shipped. The same README already says at line 110 that the token *"carries a `role` claim which the API reads per request and never stores"* — so the document now contradicts itself in two places about the same fact.
- **`ARCHITECTURE.md:5`** state banner still reads *"no roles or user projection (T-0009)"*. (It also still says "no API specification or generated contracts (T-0002)" and "no identity host (T-0010)", both of which shipped earlier — inherited staleness those tickets missed, but the T-0009 clause is this ticket's.)

`DOCUMENTATION.md` is explicit — *"Stale documentation is a defect"*, and *"A ticket that changes any of those steps fixes the README in the same change"* — and DoD item 6 requires README and setup documentation affected by the change to be updated. **This is the fourth instance of this exact pattern**: T-0002's Work Log records correcting the stale README banner and calls it "third instance". It is two lines to fix and it is the one DoD item this change-set plainly does not meet.

#### Observations — non-blocking, recorded so they are not re-found

- **N1 — a latent fail-open in a fail-closed design.** `HasRole` gates on `user.Identity?.IsAuthenticated`, which is the **primary** identity, while `FindAll` searches **all** identities. So a principal whose primary identity is authenticated but role-less, carrying a second *unauthenticated* identity holding `role: admin`, **is granted admin** — I confirmed it. The reverse (unauthenticated primary, authenticated admin second) is correctly refused. Not reachable today: one scheme is registered, one identity is produced, and nothing appends. It becomes reachable if a second authentication scheme is ever added. Worth a line, because everything else here fails closed and this is the one place that does not; the fix is to read claims only from authenticated identities.
- **N2 — claim *type* matching is case-insensitive** (`ROLE`, `RoLe` are found). That is `ClaimsIdentity` semantics rather than a project choice, and the issuer emits lowercase, so it is not a hole. Recorded so the next reader does not re-derive it.
- **N3 — AC7's email assertion is vacuous.** Nothing in this system reads or stores an email; the test asserts `DoesNotContain("logged-1@")`, a string that was never in the token or the code. The display-name half is genuine and mutation-proven; the email half cannot fail. AC7 is satisfied in substance — there is no email to leak — but the guard for that half proves nothing, and will still prove nothing when an email claim does arrive.
- **N4 — analyzers block ad-hoc logging**, as above: a real leak must be written as a source-generated `LoggerMessage`. A second barrier in front of AC7 that nobody claimed.
- **N5 — AC5 and AC8 are provable only in the test host, and I confirmed the blind spot from both ends.** Every token the identity host issues carries `client_id` and **no `sub`** — decoded, seven of them — and after driving all seven real tokens through the real API the `users` table held **0 rows**, checked directly in PostgreSQL. **My judgement, since it was asked for: the blind spot is acceptable and is correctly recorded, but its record needs a destination.** It is acceptable because the middleware's behaviour is right (a machine client is not a user), the gap is downstream of T-0010's unresolved provisioning question rather than a defect here, and the test-host coverage is genuine for the logic it exercises. It needs a destination because it is not merely a note: it is the condition under which Q1 becomes reachable, the condition under which AC5 and AC8 first get real evidence, and the condition under which `PROJECT.md` Q8 stops being theoretical — three live consequences currently recorded only in a closed ticket's Work Log. [T-0015](T-0015-compose-stack-smoke-test.md) exists for behaviour whose verification needs the real stack and would be a natural home, but I checked and its scope does not name this. Per DoD item 4 that is either a scope line added to a ticket that accepts it, or a recorded deviation.
- **N6 — the `RequireRole` divergence, reproduced live.** Admin → member endpoint is **200** by policy and **403** by `RequireRole("member")`, with real tokens. `IsInRole` answers correctly (`admin: true`, `member: false`, `superuser: false`), and `User.Identity.Name` is null only because client-credentials tokens carry no `name`.

#### The policy/`RequireRole` divergence — is a comment enough?

**My judgement: yes, proportionately — and I would add one cheap thing to T-0004 rather than hold this ticket.**

For it: the comment is on the policy constants, which is where someone choosing a mechanism actually looks — `Program.cs` is not read before writing an endpoint. The divergence **fails closed**: it denies access it should grant and never the reverse, so the worst outcome is an admin being turned away, which is loud to that admin and harmless to security. And the policy semantics themselves *are* pinned by a test — `Either_role_reaches_a_member_endpoint` fails if an admin ever stops satisfying the member policy.

Against it: nothing mechanically prevents someone typing `[Authorize(Roles = "member")]`, and T-0004 meets this immediately. A comment is a request to remember; this project's own record is that remembering is the weak link.

So: not a defect and not a reason to fail acceptance, but the durable version is cheap — a test or analyzer rule failing the build if `[Authorize(Roles = ` or `RequireRole(` appears in `apps/`. That belongs in T-0004, the first ticket that will meet the choice, and I have recorded it here rather than opening a ticket for a one-line guard.

#### Gates, each read from the tool's own exit status

`python3 tools/validate-project-os/validate.py` → **OK** (17 tickets, 6 ADRs) · `dotnet build --no-incremental` → **0 warnings / 0 errors** · `dotnet format --verify-no-changes` → **0** · `dotnet test` → **55/55** (15 unit, 40 integration), **0 skipped** · `dotnet list package --vulnerable --include-transitive` → **no vulnerable packages in any of the six projects** · working tree clean.

#### Definition of Done — assessment at this stage

1. **Implementation complete** — met. Policies, projection, migration, middleware and the AC4 decision are all present. Out of Scope is untouched: no role-management endpoint, no users API, no membership or per-project permission surface anywhere in `apps/`.
2. **All acceptance criteria verified** — met, independently; AC1–AC4 and AC6 against the running system.
3. **Automated tests exist and pass** — met. 55/55, 0 skipped, run by me from a clean build.
4. **No known unrecorded defects** — **not yet met**: Q1 and Q2 are open, and N5 needs a destination or a deviation.
5. **Code quality** — met. Three review rounds; build and formatter clean; no `TODO`, `FIXME`, `Console.WriteLine` or debug scaffolding in the change-set; no secrets.
6. **Documentation updated** — **not met**, see Q2. This is the one universal item the change-set plainly does not satisfy.
7. **Work Log complete** — met, and the record of the claim-mapping bug is the most valuable thing in it.
8. **State updated** — for `complete-ticket`.

Conditional items: **Security** — dependency scan clean and recorded here as `SECURITY.md` requires; negative cases are the bulk of the suite; authentication was never disabled to make a test pass (the test scheme adds a handler rather than removing enforcement, and AC1–AC4 are now additionally proven with no test handler at all); no secrets in the change-set. **Migrations** — scripted, reversible, and applied by the explicit migrator step. **Regression test** — the claim-mapping bug has `AuthorizationPolicyTests` plus the pinning test, and I confirmed the pin fails on removal of **each** of its three settings (below). **ADR** — none required; `PROJECT.md` §5 and ADR-0003 already fix the model, and nothing here changes it.

**Does anything need a recorded deviation?** Only if the team chooses to defer rather than fix. Q1 and Q2 are both small and belong to this ticket; N5 needs a destination ticket whose scope accepts it. If any of the three is instead deferred without a home, that is where a PO-approved deviation is required — none is needed if they are simply resolved.

#### Mutations run, and what each proved

| Mutation | Result |
| --- | --- |
| Delete `MapInboundClaims = false` | pinning test **fails** |
| Delete `RoleClaimType = "role"` | pinning test **fails** |
| Delete `NameClaimType = "name"` | pinning test **fails** |
| Member policy → "authenticated ⇒ member" fallback | **7 unit + 4 integration** tests fail |
| Remove the existing-record lookup | return-visit test **fails** |
| Remove the `DbUpdateException` catch | race test **fails 3/3** |
| Inject a display-name leak at `Information` / `Debug` / `Trace` | AC7 guard **fails** at all three |
| Revert the guard to `SetMinimumLevel(Trace)` + `LogDebug` leak | AC7 guard **passes** — the inert mechanism, confirmed inert |

#### What I could not verify

- **AC5 and AC8 against a real token** — impossible today; no token this system can issue carries a subject (N5).
- **AC1–AC4 through a shipped endpoint** — none exists; no product endpoint consumes the policies yet, which is by design (T-0004 is the first consumer). I got as close as the system allows: the API's own `Program.cs` and policies, real tokens, with only the endpoint supplied.
- **The concurrency race under real load** — reproduced deterministically enough that removing the catch fails it 3/3, but not under production concurrency.

- **Did:** Derived checks from the requirements before the Work Log. Seeded five extra identity-host clients so AC4 could be attacked with **real signed tokens** carrying `superuser`, `Admin`, `' admin'`, `''` and `'member,admin'`. Booted the API's own `Program.cs` against the live identity host and PostgreSQL and ran the full role matrix with no test authentication handler. Ran fifteen further claim shapes against the real policies, including plural `roles`, a Cyrillic homoglyph, a null byte, a JSON-array value, mixed-case claim types and multi-identity principals. Executed eight mutations, each seen failing and restored. Verified AC6 against the deployed schema and swept every column in the API's schema. Probed the projection's edges and traced the silent drop to its cause by removing the catch. Ran every gate.
- **Decided:** PASS. Q1 is a genuine silent-failure defect that is unreachable today, so it does not send the ticket back; Q2 is a DoD item 6 miss for `complete-ticket`. Neither violates a criterion for any caller the system can currently produce.
- **Remaining:** `complete-ticket` — resolve Q1 and Q2, and give N5 a destination or a recorded deviation. N1 is worth a line of code whenever someone is next in that file; the `[Authorize(Roles = …)]` ban-test belongs to T-0004.
- **Open questions / blockers:** none.
- **Branch / PR:** merged; verified on `main` @ `c92bc24`.
- **Test state:** 55/55 green, 0 skipped; eight mutations each seen failing.
- **Acceptance verdict:** **PASS** — QA (`claude-qa-5a71`), 2026-08-31. `accepted_by` deliberately left `none`: the validator reserves it for `complete-ticket` at `done`.

### 2026-08-31 — Software Engineer + Architect (claude-rev-8b4f) — review of the acceptance fixes

Review of `t-0009-acceptance-fixes` @ `c10f1dd`. No Docker stack needed this pass — every question was answerable by mutation against the suite.

**Verdict: REQUEST CHANGES.** Both code fixes are correct, and I pinned each from both sides rather than reading them. The two findings are not code defects: they are things this change-set *knows* and does not record. Both are one small edit.

#### Q1 — the narrowing is real, and the predicate is right

You asked whether I could confirm the swallow was narrowed rather than moved. It was, and the two existing tests pin it from opposite directions:

```
MUTATION A  widen the catch back to DbUpdateException wholesale
              race test       -> Passed!   (unchanged, as expected)
              oversized test  -> Failed!   <- the new test genuinely guards the narrowing

MUTATION B  remove the catch entirely
              race test       -> Failed!  Failed!  Failed!   (3/3)
```

Mutation B is the answer to "is `InnerException is PostgresException { SqlState: "23505" }` the right predicate": with no catch the race fails, with the narrow catch it passes, so the predicate demonstrably matches the exception PostgreSQL actually raises on that collision. Mutation A shows non-23505 failures now propagate. Narrowed, not moved — proven at both edges rather than at one.

Two smaller notes, neither blocking. `"23505"` could be `PostgresErrorCodes.UniqueViolation`, which is self-documenting and immune to a typo the compiler cannot see. And 23505 is *any* unique violation on the table: correct today, because the primary key on `Subject` is the only unique constraint, but a unique index added later would silently widen the swallow again. A clause in the comment saying the predicate is scoped to "the only unique constraint here is the PK" would make that dependency visible to whoever adds one.

#### N1 — closed in one direction, still open in the other, both now guarded

```
MUTATION C  revert to the pre-fix shape (primary-identity gate, all-identity search)
              -> Failed: A_role_on_an_unauthenticated_identity_grants_nothing

MUTATION D  over-narrow to the primary identity only
              -> Failed: A_role_on_a_second_authenticated_identity_is_honoured
```

So the fail-open is closed *and* the legitimate multi-scheme case you were worried about is protected against a future over-correction. That second test is the one that earns its place: it is the guard against fixing this bug too hard, which is the more likely next mistake.

**A confession that belongs in the record, because it is this project's own failure class.** My first run of mutation C reported all 15 tests still green, and I was one sentence from writing that the N1 test did not catch the fail-open. It did — my mutation was **half-applied**: the script did two string replacements, the second target did not match because the real `RoleValues` has a comment block between `user.Identities` and `.Where(...)`, and Python's `str.replace` returns the unchanged string rather than complaining. Same shape as the `&&`-chained grep recorded above: a silent no-op producing a confident, wrong conclusion. I caught it by checking whether the mutation had applied before trusting what it said, which is the discipline this ticket has been teaching all week. Re-run with the change verified in the file first, both mutations bite.

#### Blocking

**1. Nothing in this change-set is recorded in the ticket.** `git diff main --name-only` on the T-0009 ticket file is empty: three acceptance defects fixed, and the Work Log's last entry is still the acceptance entry listing them as found. A reader of T-0009 today sees Q1, N1 and Q2 raised and no statement that any was addressed. `GIT.md` fixes the handover order — *final Work Log entry on the branch → PR → review → merge* — and DoD item 7 requires that repository state alone tells the full story.

In fairness the code comments are unusually good and carry most of the *why* for Q1 and N1. What is missing is the ticket-level record: which acceptance defects this addresses, the evidence, and the disposition of finding 2 below. One entry.

**2. The 200-character subject column is a known defect with no destination.** Acceptance reproduced it and this commit's own comment states it plainly: OIDC permits a 255-character `sub`, the column holds 200. Narrowing the catch was right and I am not asking you to reverse it — but it changes the failure rather than removing it. Before, such a caller got 200 with no row written; now they get a hard failure on **every** request, so a user with a long subject is not merely unusable as an assignee, they cannot use the API at all. That is the better failure, and it is still a defect.

DoD item 4: every defect found is either fixed or captured as a bug ticket linked from this one, with the deferral accepted. This one is found, documented in two Work Logs and a code comment, and captured nowhere. **T-0015's new AC8 does not take it on** — it covers *verifying the projection against a subject-bearing token*, not the column being narrower than the standard permits; citing it would be the false-pointer failure that the same commit's T-0015 widening was so careful to avoid. Either widen the column here (a property change and a migration) or open the ticket. I have no view on which; I do have a view on it living only in prose.

Worth noting *when* it bites: the condition that makes real subjects appear — T-0010's provisioning answer — is exactly the condition that makes long subjects appear. It becomes reachable at the same moment as everything else in T-0015's AC8, which is an argument for capturing it now while the reasoning is fresh.

#### Q2 and the T-0015 widening — both right

The README's "Not here yet" entry is now a true statement about user tokens rather than a false one about roles, and the token section explains the two policies including the floor semantics, which is where someone choosing a mechanism will look. The `ARCHITECTURE.md` banner is current and now says that keeping it current is part of the ticket that changes the state, with the count of times it has been found stale. Naming the recurrence in the artefact is the right instinct — it converts a habit into something a reader can check.

The T-0015 widening is the correct shape and I checked the thing that matters: its Out of Scope does not disown the new line, and AC8 carries an explicit instruction not to pass quietly if no subject-bearing token exists. That is a real improvement on "add a scope line and hope".

#### Gates

`validate.py` **OK** (17 tickets, 6 ADRs) · `dotnet build --no-incremental` **0 warnings / 0 errors** · `dotnet format --verify-no-changes` **clean** · `dotnet test` **58/58** (17 unit, 41 integration) · `check-drift.sh` **0** · working tree clean.

#### Merge decision

**Do not merge yet — but the code is done.** Both fixes are correct and verified from both sides; I found nothing wrong with either. What is missing is a Work Log entry and a home for a known defect, and those are the two things this project has repeatedly found are not safe to leave in prose. Add the entry, capture or fix the column width, and this is an approve with no further technical pass needed.

- **Did:** Four mutations — widening the catch, removing it, reverting the identity filter, and over-narrowing it — each verified as applied to the file before its result was trusted. Checked the predicate from both edges. Read the README, banner and T-0015 changes, including whether T-0015's Out of Scope disowns its new line.
- **Decided:** the narrowed predicate is correct as written; my notes on it are style, not substance.
- **Remaining:** the two findings above. No further technical verification needed from me.
- **Open questions / blockers:** none for the reviewer.
- **Branch / PR:** `t-0009-acceptance-fixes` @ `c10f1dd` — **not merged**.
- **Test state:** 58/58 green, and the three behaviours at issue are each green for the right reason — every one seen failing without its fix.
- **Review verdict:** **Request changes** — ENG + ARCH (`claude-rev-8b4f`).

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — acceptance findings fixed

**Q1 — the race catch swallowed every write failure, not the race.** `catch (DbUpdateException)` wholesale meant a subject longer than the column returned **200 with no row written and nothing logged**: a caller told they had succeeded, then permanently unusable as an assignee (T-0006) or comment author (T-0008). Narrowed to `PostgresErrorCodes.UniqueViolation`.

The reviewer pinned it from both edges — widening the catch back makes the new oversized-subject test fail; removing it entirely makes the race test fail 3/3 — which together prove the predicate matches what PostgreSQL actually raises on that collision, rather than the swallow having simply moved.

**And narrowing it exposed a second defect the broad catch had been hiding.** The `Subject` column held 200 characters while OpenID Connect permits a `sub` of up to 255, so a *legal* subject of 201–255 turned from a silent loss into a **hard failure on every request**. The reviewer flagged that this needed a destination and that [T-0015](T-0015-compose-stack-smoke-test.md)'s AC8 does not take it on — citing it would have been the false-pointer failure the same commit's T-0015 widening was careful to avoid.

**Fixed rather than captured**, because the fix is one migration and the defect is the projection's own: the column is now 255, so a legal subject always fits. Tests cover both sides — a 255-character subject is projected, and one beyond the specification still fails loudly.

**N1 — the only fail-open in the design.** `HasRole` gated on the **primary** identity's `IsAuthenticated` while the claim search ran across **all** identities, so an authenticated role-less identity beside an *unauthenticated* one carrying `role: admin` would have been granted admin. Unreachable today with one scheme; fail-open the moment a second is added.

Claims are now read only from authenticated identities. Two tests, deliberately: one that the unauthenticated identity grants nothing, and one that a **legitimate second authenticated scheme is still honoured** — the reviewer's point that the more likely next mistake is fixing this too hard, which its mutation confirmed.

**Q2 — documentation, and the fourth instance of this pattern.** The README still listed role authorisation under *"Not here yet"* while describing it as working further down the same file; `ARCHITECTURE.md`'s state banner still said *"no roles or user projection"*. Both corrected, and the README gained a true statement about what genuinely does not exist yet: **user** tokens, since the identity host issues machine-client tokens carrying a role but no subject.

The banner now records that it has been found stale four times, each time by the ticket that falsified it. Naming the recurrence is the only durable thing available short of an automated check — and it is worth raising at the retrospective, because four instances is a pattern rather than four accidents.

**Also:** T-0015 widened to accept T-0009's AC5/AC8 residual, with its new AC8 instructed to record a reason and name a successor rather than pass quietly if no subject-bearing token can be issued when it is implemented.

**A note on the reviewer's own method**, because it is this project's central lesson turned on itself: their first mutation of the N1 fix showed all tests green, and they were about to report that the guard did not work — when they checked and found the mutation had been **half-applied**, a string replacement that silently matched nothing. They verified the mutation had landed before trusting its result. That is the same failure as my `&&`-chained grep, caught one step earlier.

**Verification, each from the tool's own exit status:** `dotnet build` **0 warnings / 0 errors**, `dotnet format --verify-no-changes` **0**, `dotnet test` **0** (**59 tests**), `validate.py` **OK**.

### 2026-08-31 — Software Engineer + Architect (claude-rev-8b4f) — review of the acceptance fixes, second pass

Review of `t-0009-acceptance-fixes` @ `5874e7c`.

**Verdict: APPROVE. The change-set may merge.**

**First, a correction to my own last verdict.** I wrote that this would be "an approve with no further technical pass needed" once the two items landed. That was conditional on the disposition I expected — *capturing* the column defect in a ticket. You **fixed** it instead, which is a schema change, a migration and two new tests: new code, and new code gets reviewed regardless of what I promised about the shape I anticipated. So I checked it rather than honouring the letter of my own sentence. It holds up.

#### The column widening — verified, including the check I had not run before

- **Model, migration and snapshot agree.** `dotnet ef migrations has-pending-model-changes` → *"No changes have been made to the model since the last migration."* That is the right consistency check for a schema change and I had not run it on any earlier pass; running it now also confirms nothing else in the model has drifted from the migration history.
- **The migration is a clean widening with a real `Down`.** `AlterColumn` 200 → 255 with `oldMaxLength: 200`, and a `Down` that narrows back. Widening a `varchar` needs no table rewrite in PostgreSQL, so it is cheap on a populated database.
- **The 255 test genuinely depends on the widening.** I edited the migration's `Up` to leave the column at 200 — verifying the edit had landed in the file before trusting the result, which is the whole point:

  ```
  column left at 200:   A_subject_at_the_OIDC_limit_is_projected                 -> Failed!
                        A_subject_beyond_the_OIDC_limit_fails_loudly...          -> Passed!
  ```

  Both behave as intended: the boundary test is sensitive to the column width, and the beyond-the-limit test is correctly insensitive to it — it asserts only that the write is never silently discarded, which stays true at any column size.

**One observation, not a finding.** The `Down` narrows 255 → 200 and will fail if any stored subject exceeds 200. That is the correct behaviour — refusing to roll back is better than truncating identity data — but it makes the rollback conditional rather than unconditional, which is worth knowing before anyone reaches for it in anger. Nothing to change.

#### The rest

`PostgresErrorCodes.UniqueViolation` replaces the literal, and the comment now records that the predicate matches *any* unique violation — correct while the primary key is the only unique constraint, and silently wider if an index is added later. That dependency is now visible to whoever adds one, which was the point.

The Work Log entry covers what was missing: Q1 and the predicate pinned from both edges, the column consequence and why it was fixed rather than captured, N1 with the reason the second test exists, Q2 and the fourth-instance pattern, and the T-0015 widening. Repository state now tells the full story without this review thread.

#### On "verify the mutation applied"

You put it better than I would have: it belongs alongside "verify by mutation" as a rule, not as an anecdote. The argument for it is that both failures in this ticket — the `&&`-chained grep and my half-applied `str.replace` — were **silent no-ops that produced confident false conclusions**, and in both cases every downstream signal was honest. Mutation testing assumes the mutation happened; when it silently doesn't, a green result reads as "the guard works" when it means "nothing was tested". The check is cheap — look at the file, or assert the replacement changed something — and it is the difference between the two instances being caught one step apart. If that reaches the retrospective, I would put it as: *a mutation is evidence only once you have seen it in the artefact.*

#### Gates

`validate.py` **OK** (17 tickets, 6 ADRs) · `dotnet build --no-incremental` **0 warnings / 0 errors** · `dotnet format --verify-no-changes` **clean** · `dotnet test` **59/59** (17 unit, 42 integration) · `check-drift.sh` **0** · `dotnet ef migrations has-pending-model-changes` **none** · working tree clean.

#### Merge decision

**Merge.** Both acceptance defects are fixed, the defect the first fix exposed is fixed rather than deferred, and every behaviour at issue has been seen failing without its fix. Nothing is outstanding.

- **Did:** Reviewed the schema change, migration and snapshot; ran the EF model/migration consistency check; mutated the migration to leave the column at 200 and confirmed the boundary test depends on it, verifying the edit had landed first; read the Work Log entry against what was missing.
- **Decided:** approve. Fixing rather than capturing was the better call — the defect is the projection's own and the fix is one migration, so a ticket would have been ceremony.
- **Remaining:** implementer merges and performs the handover status commit.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0009-acceptance-fixes` @ `5874e7c` — **approved for merge**.
- **Test state:** 59/59 green, each of the behaviours at issue seen red without its fix.
- **Review verdict:** **Approve** — ENG + ARCH (`claude-rev-8b4f`).

### 2026-08-31 — QA / Test Engineer (claude-qa-5a71) — Q1, N1 and Q2 re-checked

Targeted re-check on `main` @ `5b0a5fb`. My PASS on AC1–AC8 stands and was not re-derived. Everything below was run against artefacts before I read the fix entry or the review, because the failure this ticket keeps circling is a record that outruns the repository.

**Q1 and N1 are closed. Q2's original defect is fixed, but the fix introduced two new inaccurate statements, and the write-failure class was addressed on one of the two columns rather than both.** Three new items, all small, all recorded below.

#### 1. Q1 — the narrowing did not move the swallow, and the widening is pinned too

`IsDuplicateKey` matches `InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }` — a symbolic constant rather than a literal — and it is the only `catch` in the projection path. So the swallow was removed rather than relocated.

I pinned it from three edges rather than two, each mutation seen failing:

| Mutation | Result |
| --- | --- |
| Widen the catch back to `catch (DbUpdateException)` | oversized-subject test **fails** — and the race test still **passes** |
| Remove the catch entirely | race test **fails**, 3 of 3 runs |
| Revert the column to 200 | subject-at-the-OIDC-limit test **fails** |

The middle column of the first row is the part worth stating: the two tests bracket the catch **independently**, so neither is carrying the other. And the third edge is one nobody claimed — the widening itself is pinned, not just the narrowing.

`dotnet ef migrations has-pending-model-changes` → *"No changes have been made to the model since the last migration."* The migration is a real `AlterColumn` with a genuine `Down` back to 200.

#### 2. The 255 widening genuinely removes the class — for `Subject`

Measured at the boundary rather than argued:

```
subject 200 (old column limit)   HTTP 200   row written
subject 254                      HTTP 200   row written
subject 255 (the OIDC limit)     HTTP 200   row written
subject 256 (one past OIDC)      THREW DbUpdateException <- PostgresException   0 rows
subject 300                      THREW DbUpdateException <- PostgresException   0 rows
subject 255 multi-byte chars     HTTP 200   row written
```

OpenID Connect caps `sub` at 255 ASCII characters, so **every specification-legal subject now fits**, and only out-of-spec subjects fail — loudly, which is the correct direction. The class is removed, not shifted. I checked the byte-versus-character trap specifically: PostgreSQL's `character varying(255)` counts *characters*, so 255 multi-byte characters still fit and there is no hidden narrower limit behind the declared one.

#### 3. Q3 — the same class was addressed on `Subject` and not on `DisplayName`

You asked me to push where I found the original, so: the reasoning that fixed `Subject` was *"OIDC permits 255, the column held 200, widen it"* — anchored to a specification. `DisplayName` is still 400, and the OIDC `name` claim has **no length limit at all**, so there is no specification to anchor 400 to. Narrowing the catch changed that column's failure mode exactly the way it changed `Subject`'s, and only `Subject` was followed through:

```
display name 400 (column limit)                  HTTP 200   row written
display name 401 (one past)                      THREW DbUpdateException <- PostgresException   0 rows
display name 500                                 THREW      0 rows
existing user, name grows to 500 (UPDATE path)   THREW      existing row survives
... and the next request with a sane name        HTTP 200   recovers
```

During acceptance I measured the *old* behaviour of the same input: a 450-character display name returned **HTTP 200 with 0 rows**. So this is the identical conversion — silent loss to hard failure — on the column that was not widened.

- **Consequence:** while the identity provider holds an over-long `name`, **every** authenticated request from that caller fails, because the projection runs on every authenticated request and there is no path past it. Not a permanent lockout — it clears the moment the name shortens, and the existing row survives — but a total outage for that user until it does.
- **Severity: low, and I am not reopening the ticket for it.** It is unreachable today for the same reason everything else in this area is (no token this system issues carries a subject, so the projection never runs against a real token); it needs a display name over 400 characters, which is implausible for a human name if not for a `name` claim populated from a directory DN or a concatenation; and it fails **loudly**, which is the correct direction and the entire point of the narrowing. It is a residual of the fix, not a regression against an acceptance criterion.
- **Recommendation, not mine to write:** a display name is a convenience field, not identity. Truncating on write is the proportionate treatment — a caller's ability to use the API should not depend on the length of their name. Either that, or accept the loud failure deliberately and say so where the column length is declared.

#### 4. N1 — closed, and the fix is not over-tightened

The primary-identity gate is gone from `HasRole` entirely; `RoleValues` now filters `user.Identities` to authenticated ones before reading either claim type. Both directions mutated:

| Mutation | N1 guard | second-authenticated-scheme test |
| --- | --- | --- |
| Revert to the old form (all identities + primary gate) | **fails** | passes |
| Over-tighten to the primary identity only | passes | **fails** |

So the pair genuinely brackets the fix, and neither test is redundant.

Because this changed the exact claim-reading path the ticket's original bug lived in, I re-ran the promotion hunt against the new code rather than assuming it survived. All fifteen shapes behave as before — plural `roles`, trailing space, quoted, null byte, Cyrillic homoglyph, `ADMIN`, JSON array, `admin;member` and `.../identity/claims/role` all refused; `role=junk AND role=admin` still correctly grants; both controls still grant. And:

```
authed(no role) + UNauthenticated(admin)     -> admin=False   (was True — N1 closed)
UNauthenticated-primary + authed(admin)      -> admin=True    (was False — see below)
authed(member) + 2 UNauthenticated(admin)    -> admin=False, member=True
```

The middle line is worth recording because nobody claimed it: the old primary-identity gate also had a fail-**closed** bug — a legitimately authenticated identity that happened not to be primary was refused. The fix corrects that too, and the second test is what keeps it corrected.

#### 5. Q2 — the original staleness is fixed; two new inaccuracies came with it

The statements I flagged are genuinely gone, and I verified each replacement claim rather than reading it:

- *"The identity host issues machine-client tokens, which carry a role but no subject"* — **true**; I decoded seven real tokens during acceptance, every one carrying `client_id` and `role`, none carrying `sub`.
- *"the user projection stays empty in practice"* — **true**; the `users` table held zero rows after a full real-token traffic run, checked in PostgreSQL directly.
- *"An absent or unrecognised role is refused, never treated as a member"* — **true**; verified with real tokens carrying `superuser`, `Admin`, `' admin'`, `''` and `'member,admin'`.
- ARCHITECTURE's list of what now exists, and *"the only resource in the specification today is a deliberately disposable placeholder"* — **true**; `spec/openapi.yaml` declares exactly one path, `/placeholders`.

You asked whether a third stale statement had been introduced while fixing the second. Two have.

**Q2a — the recurrence count in the ARCHITECTURE banner is not what the record supports.** The new sentence reads: *"Keeping this banner current is part of the ticket that changes the state. **It** has been found stale four times; each time the ticket that falsified it had not updated it."* "It" reads as this banner. The repository records **this** banner found stale **twice**: once in T-0001 (rewritten in `4bd351a`; that reviewer's note is the *"stale for one merge"* sentence still standing two lines below), and once in my acceptance.

The four came from my own wording — I wrote *"fourth instance of this **pattern**"*, and the pattern spans different documents: T-0001's D1 on the README banner, T-0002's README banner, T-0002's B6 on the JDK statements in `PROJECT.md` and `ARCHITECTURE.md`, and mine. So the sentence attributes a pattern count to a single banner. I seeded the phrasing, so I will own half of it — but a sentence added specifically to keep the banner true should not be the untrue part of it.

The second clause is also too strong: *"each time the ticket that falsified it had not updated it"* is contradicted by T-0002, where the implementer caught and corrected the README banner **inside** the ticket — *"Third instance of that pattern; corrected here rather than ticketed."* That instance is one where the ticket that falsified it did update it, late but on its own.

**Q2b — the README now reads as though endpoints enforce roles today.** The token section gained: *"Two authorisation policies act on it: `admin` is restricted to that role, and `member` is a floor an admin also satisfies."* — immediately followed by *"To prove the round trip:"* and a `curl` against `/health/authenticated`.

**No shipped endpoint uses either policy.** The only `RequireAuthorization` anywhere in `apps/GotIssues.Api` is the bare one on `/health/authenticated`, so an `admin` token and a `member` token both get 200 there and the role is never consulted. A reader who follows that section with both tokens will see identical results and conclude the policies do not work. The *Not here yet* entry does not cover this: it says no endpoint is guarded by a **person's** identity, which is about machine-versus-user tokens, not about roles.

One clause fixes it — that the policies are defined and centrally registered, and that [T-0004](T-0004-create-and-list-projects.md) is the first endpoint to apply one.

#### 6. The AC5/AC8 destination — confirmed by reading the ticket

I flagged before that T-0015's scope did not accept this; it does now, and I checked the ticket rather than the claim. Its In Scope carries a dedicated bullet naming *"The user projection against a token carrying a real subject (from T-0009)"*, with the reasoning intact — the seven decoded tokens, the zero rows, that the blind spot is what hid the claim-mapping bug, and that it is the condition under which the narrowed write-failure handling first becomes reachable — and it says explicitly that the line exists so the residual has a destination that accepts it. AC8 adds the instruction that if no subject-bearing token can be issued when it is implemented, that is **recorded with a named successor rather than passed quietly**. That is the right shape for a residual whose entire history is being invisible. **Accepted as a genuine destination.**

#### 7. DoD items 4 and 6 — my stated conditions

**Item 4 — satisfied for everything I previously raised.** Q1's substance is fixed and pinned from three edges; N1 is closed and pinned in both directions; the AC5/AC8 blind spot has a destination whose scope genuinely accepts it. The three items raised *here* — Q3, Q2a and Q2b — are now recorded, and DoD item 4 asks for more than recording: each needs fixing or a destination that accepts it before `done`. All three are one to two sentences of work and belong to this ticket.

**Item 6 — not yet cleanly met.** The original miss is genuinely fixed and both documents are substantially more accurate than before. But Q2a and Q2b are inaccuracies in the two files the fix touched, and `DOCUMENTATION.md`'s rule is accuracy, not improvement. Fixing them closes item 6.

Neither of these is a reason to reopen the ticket or revisit acceptance. They are the last two sentences of a documentation fix and one judgement call about a column length.

#### Gates, each read from the tool's own exit status

`python3 tools/validate-project-os/validate.py` → **OK** (17 tickets, 6 ADRs) · `dotnet build --no-incremental` → **0 warnings / 0 errors** · `dotnet format --verify-no-changes` → **0** · `dotnet test` → **59/59** (17 unit, 42 integration), **0 skipped** · `dotnet ef migrations has-pending-model-changes` → none · working tree clean. Seven mutations run, each seen failing and restored. No Docker stack was raised for this pass — the questions were about source, migrations, in-process behaviour and prose, and my real-token evidence from acceptance stands; the six unrelated stacks were verified untouched.

- **Did:** Checked the narrowed catch and its constant, then pinned it from three edges by mutation. Measured the length boundary on both columns at 200/254/255/256/300 and 400/401/500, including multi-byte characters and the UPDATE path. Mutated the N1 fix in both directions and re-ran the full promotion hunt against the changed claim-reading path. Verified every replacement sentence in the README and ARCHITECTURE against the code and the repository's own record, including counting the recorded staleness instances. Read T-0015's scope and AC8 rather than accepting that it now accepts the residual.
- **Decided:** Q1 and N1 are closed. Q3 is a low-severity residual of Q1's fix, in the correct (loud) direction, not a regression against a criterion — recorded, not reopened. Q2's original defect is fixed; Q2a and Q2b are new prose inaccuracies in the same files and are what stands between this and DoD item 6.
- **Remaining:** `complete-ticket` — Q3, Q2a and Q2b, each fix-or-capture. Nothing else.
- **Open questions / blockers:** none.
- **Branch / PR:** merged; verified on `main` @ `5b0a5fb`.
- **Test state:** 59/59 green, 0 skipped; seven mutations each seen failing.
- **Acceptance verdict:** **PASS stands.** Q1 and N1 closed; Q2 closed in substance with two follow-on corrections recorded — QA (`claude-qa-5a71`), 2026-08-31. `accepted_by` deliberately left `none`.

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — Q3, Q2a and Q2b

**Q3 — I fixed the class on one of the two columns.** The reasoning that widened `Subject` was anchored to a specification: OIDC caps `sub` at 255, so a legal subject always fits. `DisplayName` got no such treatment, and narrowing the write-failure catch did exactly the same thing to it — a name past 400 turned a silent loss into a **hard failure on every request** from that caller, for as long as the identity provider held it.

Nothing anchors a display name: the OIDC `name` claim has no length limit, so any column width is arbitrary and widening only moves the cliff. A display name is a **convenience field** — not identity, nothing keyed on it, and a truncated one is still useful. So it is trimmed to fit rather than failing the request, which is the acceptor's recommendation and the right trade: failing a request over a cosmetic field is worse than a shortened name, and silently dropping the whole projection is worse than both.

Mutation-proven, and I **checked the mutation had applied** before trusting it — `Fit` call sites dropped to one, the definition alone — which is the rule this ticket produced.

**Q2a — the sentence I added to keep the banner honest was itself inaccurate.** It claimed the banner had been found stale four times, when four was the count of the *pattern* across different documents; this banner has been stale twice. And "each time the ticket that falsified it had not updated it" is contradicted by [T-0002](T-0002-contract-first-codegen-pipeline.md), which caught its own README banner mid-ticket. Both corrected, with T-0002 named as the counter-example — a sentence added to make a document truthful should not be its untrue part.

**Q2b — the README implied endpoints enforce roles.** The new paragraph describing the policies sat immediately above a `curl` against `/health/authenticated`, which requires *authentication*, not a role — so a reader trying it with both tokens would get 200 twice and conclude the policies were broken. It now says plainly that **no shipped endpoint uses the policies yet** and names [T-0004](T-0004-create-and-list-projects.md) as the first that will.

**Worth recording about this ticket's shape:** four of the findings across its review and acceptance were consequences of my own corrections rather than of the original code. Narrowing the catch created Q3 and the subject-column defect; the banner sentence created Q2a; the policy paragraph created Q2b. Each fix was right and each moved a boundary, which is the argument for reviewing a fix with the same suspicion as a feature — and it is the same conclusion the reviewer reached from the other direction.

**Verification:** `dotnet build` **0 warnings / 0 errors**, `dotnet format --verify-no-changes` **0**, `dotnet test` **0** (**60 tests**).

### 2026-08-31 — Software Engineer + Architect (claude-rev-8b4f) — spot check of the final fixes

Spot check of `t-0009-final-fixes` @ `ed6b920`.

**Verdict: REQUEST CHANGES — one blocking finding, in the item you asked me to judge.** You asked me to check the *trade* rather than the code. The trade is right. The implementation of it reintroduces, for a narrow but deterministic class of input, exactly the failure it was written to remove.

#### Q3 — the trade is correct

Trimming is the right call and I would have argued for it. `DisplayName` is explicitly not identity: `Subject` is the key, `UserRecord`'s own documentation says identity is the subject and never the name, and this ticket's Examples already accept two subjects sharing one display name. Display-name fidelity is therefore not a system invariant, and truncating it corrupts nothing the system relies on. Against that, failing an entire request over a cosmetic field is disproportionate — the caller's request has nothing to do with their name — and dropping the projection silently is worse than both, as acceptance established. Widening instead would only relocate the cliff, since the OIDC `name` claim has no length. So: convenience field, degrade it; identity field, widen it to the spec limit and fail beyond. That is a coherent rule and the two columns now follow it.

#### Blocking — the trim splits surrogate pairs, and that fails the request

`Fit` cuts at `displayName[..400]`, a raw UTF-16 index. If a surrogate pair straddles index 400 — any non-BMP character, emoji included — the cut leaves a **lone high surrogate**, which cannot be encoded to UTF-8. Verified end to end against real PostgreSQL:

```
name = 399 × 'n' + 10 × "😀"        (pair straddles index 399/400)

System.Text.EncoderFallbackException : Unable to translate Unicode character
    \uD83D at index 399 to specified code page
  wrapped in Microsoft.EntityFrameworkCore.DbUpdateException
```

That exception is not SQLSTATE 23505, so `IsDuplicateKey` correctly declines it and it propagates — **a hard failure on every request from that caller, for as long as the identity provider holds that name.** Which is the precise sentence in your own comment describing what `Fit` exists to prevent. Emoji in display names are ordinary, so this is not exotic input; it is narrow only because the pair must land on the boundary.

**Remedy, verified:** drop a trailing lone surrogate after the cut.

```csharp
var cut = MaximumDisplayNameLength;
if (char.IsHighSurrogate(displayName[cut - 1])) cut--;
return displayName[..cut];
```

With that applied, the same probe returns `status=OK row=True len=399`, and your shipped trim test still passes. If you would rather not split grapheme clusters either (a ZWJ emoji sequence can still be halved, which is cosmetically ugly but encodes fine), `StringInfo` would do it — but the encoder exception is the part that must be fixed, and one `if` fixes it.

I confirmed your own mutation too: removing the `Fit` call site fails `An_over_long_display_name_is_trimmed_rather_than_failing_the_request`, with the call count down to one — the definition alone — before I trusted it.

#### Non-blocking

- **`MaximumDisplayNameLength = 400` duplicates `HasMaxLength(400)`** in `GotIssuesDbContext`, and the comment says so honestly, but nothing enforces the agreement. Change the column and the trim silently disagrees — and disagreeing *downward* restores the hard failure. Same shape as the `DefaultPageSize` duplication already noted on this ticket.
- **The trim is entirely silent.** AC7 rightly forbids logging the name, but the *event* can be logged without the value. In a ticket whose whole history is about things failing quietly, "display name trimmed for a caller" at Debug — no name, no email — would cost nothing and would be the only trace that it ever happened.

#### Q2a — accurate now, and one observation

The correction is right and carefully worded: the banner has indeed been found stale twice, the four-instance figure is the wider pattern, and T-0002 did catch **its own README banner** mid-ticket (its `ARCHITECTURE.md` staleness was caught in review, not by the ticket — the sentence claims only the README, so it holds).

The observation: *found* stale twice is not the same as *gone* stale twice. This banner was falsified by T-0002 and again by T-0010 and simply not noticed until T-0009's acceptance. A reader can take "found stale twice" as "it has only gone wrong twice", which understates exactly what the sentence exists to convey. I am **not** asking for a third revision — that is the point below.

#### The pattern you asked about — and a fourth instance of it in this very sentence

Four findings were consequences of your own corrections. I think the unifying property is sharper than "review fixes like features":

**A fix that moves a boundary silently re-classifies every case that sat on the old boundary, and those cases are not re-examined with the rigour the original code got.** Narrowing the catch moved the line between *swallowed* and *propagated*; everything previously absorbed by the broad catch needed re-checking, and two things were there — an over-long `Subject` and an over-long `DisplayName`. Both became defects, found one round apart. The question that would have found both at once, immediately, is: *what else was this catch absorbing?* That is enumerable in a minute.

The prose instances are the same move in a different medium. The banner sentence and the README policy paragraph each **added a new claim about state** — and this project already knows that status claims go stale and that documents drift from behaviour. A correction that introduces an assertion has introduced something that needs verifying, and neither was.

And the self-referential tally is the recursive case: a sentence that counts its own staleness must be maintained forever, has now been wrong once, and is *arguably imprecise again* in its corrected form. The durable fixes are to drop the count and keep the instruction, or to make it checkable — not to correct it a third time. **The general rule is the one this ticket has now produced twice: a correction is new, unverified work.** It applies to code (Q1 → Q3), to prose (Q2a, Q2b), and to the verification method itself (my half-applied mutation, your `&&`-chained grep). For the retrospective I would put it as: *hold corrections to the standard of the thing they correct, and for any fix that moves a boundary, enumerate what used to sit on it.*

#### Q2b — correct, and worth keeping

The README no longer implies endpoints enforce roles, and it names T-0004 as the first that will. That paragraph was actively misleading in the specific way that wastes a reader's afternoon: two tokens, two 200s, and a wrong conclusion about the policies. Good catch by acceptance.

#### Gates

`validate.py` **OK** (17 tickets, 6 ADRs) · `dotnet build --no-incremental` **0 warnings / 0 errors** · `dotnet format --verify-no-changes` **clean** · `dotnet test` **60/60** (17 unit, 43 integration) · `check-drift.sh` **0** · working tree clean.

#### Merge decision

**Do not merge.** One `if`, plus a test carrying a non-BMP character over the boundary so the case is guarded rather than remembered. The trade is right, Q2a and Q2b are right, and nothing else is outstanding.

- **Did:** Judged the trade on its merits. Probed `Fit` with a display name whose surrogate pair straddles the cut, against real PostgreSQL, and confirmed the resulting `EncoderFallbackException` propagates as a request failure. Verified a one-line remedy resolves it and leaves the shipped test green. Confirmed the implementer's own mutation, checking the call count before trusting it. Checked the Q2a wording against T-0002's actual history.
- **Decided:** trimming is the correct trade for a convenience field; the finding is about how the trim is performed, not whether to trim.
- **Remaining:** the surrogate fix and its test. The two non-blocking notes are take-or-leave.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0009-final-fixes` @ `ed6b920` — **not merged**.
- **Test state:** 60/60 green; the defect is outside what any current test reaches, which is the finding.
- **Review verdict:** **Request changes** — ENG + ARCH (`claude-rev-8b4f`).

### 2026-08-31 — claude-sm-9d4e — Q4: the trim's own boundary bug

The finding is correct and the remedy is the one the reviewer verified. `Fit` cut at
`displayName[..400]`, a UTF-16 index, so any non-BMP character straddling position 400
left a lone high surrogate that cannot be encoded to UTF-8. The write then throws
`EncoderFallbackException` inside a `DbUpdateException` that is *not* a unique violation,
so `IsDuplicateKey` correctly declines it and it propagates — a hard failure on every
request from that caller. That is exactly the failure `Fit` was written to prevent,
reintroduced by `Fit` itself, on the narrow class of inputs nothing tested.

```csharp
if (char.IsHighSurrogate(displayName[cut - 1]))
{
    cut--;
}
```

**Mutation-proved, both new tests, mutation confirmed in the artefact before the result
was trusted:**

| Mutant | Test | Result |
| --- | --- | --- |
| Guard removed | `A_display_name_trimmed_through_a_surrogate_pair_still_persists` | **Killed** — `EncoderFallbackException : Unable to translate Unicode character \uD83D at index 399`, the reviewer's exception verbatim |
| `MaximumDisplayNameLength` 400 → 399 | `The_trim_length_matches_the_column_it_trims_for` | **Killed** — stored length ≠ column length |

#### Both non-blocking notes taken, and why the second one changed shape

**N1 — the constant duplicating `HasMaxLength(400)`.** My first attempt asserted the
column is 400, which restates one half of a pair that can only fail by *disagreeing*: it
would not have caught the constant drifting. The test now drives a 500-character name
through the middleware and requires what is stored to fill the column exactly, reading
the width from the EF model rather than a literal. Trim short and it fails; trim long and
the write fails. Mutation 2 above is that test doing its job.

**N2 — the silent trim.** AC7 forbids logging the display *name*; it does not forbid
recording that one was shortened. Source-generated `[LoggerMessage]` carrying two lengths
and no content — the analyzers reject `LogInformation` here (CA1848/CA1873), which is the
right pressure.

#### One more stale claim, found while in the file

The narrowed-catch comment still read *"OIDC permits 255, the column holds 200"*. The
column is 255 and has been for some time; the numbers went stale when it was widened and
the comment kept the old pair. Corrected. This is the fifth instance of the pattern this
ticket keeps producing, and the first one nobody reported — I only saw it because I was
editing four lines above it.

#### The tally: dropped, not corrected

The reviewer declined to ask for a third revision — *"drop the count or make it checkable;
don't correct it a third time"* — and dropping it is right. A sentence that counts its own
staleness needs maintaining forever and earns nothing; the instruction it wrapped is the
part with value. The banner now says updating it belongs to the ticket that falsifies it,
and says so without asserting a number that will be wrong again.

- **Did:** Applied the surrogate guard. Added two integration tests, mutation-proving both.
  Took both non-blocking notes, rebuilding N1's test after noticing the first version could
  not fail the way the note described. Corrected a stale width comment found in passing.
  Replaced the self-counting banner sentence.
- **Decided:** an agreement between two declarations is tested by making them disagree, not
  by restating one of them.
- **Remaining:** reviewer spot check.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0009-final-fixes` — pending commit.
- **Test state:** 62/62 green (17 unit, 45 integration); `dotnet format` **exit 0**;
  `check-drift.sh` **exit 0**.

### 2026-08-31 — Software Engineer + Architect (claude-rev-4a7e) — spot check of the surrogate fix and the two notes

Spot check of `t-0009-final-fixes` @ `099f7fb`, diffed against `main` @ `f2287b2`. Independent of
`claude-rev-8b4f`; I re-derived every claim below rather than reading the previous verdict forward.

**Verdict: REQUEST CHANGES — one blocking finding, and it is in the item I was asked to confirm.**
Q4 is genuinely fixed. N1 is genuinely closed and is *not* an elaborate version of the mistake it
replaced. N2's code is on the right side of AC7 — but the guard that is supposed to keep it there
does not reach the log statement N2 added, and I have the surviving mutant to show it.

#### Q4 — the surrogate split is fixed, and I killed the mutant myself

`Fit` now cuts back off a lone high surrogate before slicing. Mutation, run here, not taken on trust:

| Mutant | Test | Result |
| --- | --- | --- |
| Guard (`if (char.IsHighSurrogate(displayName[cut - 1])) cut--;`) deleted | `A_display_name_trimmed_through_a_surrogate_pair_still_persists` | **Killed** — `EncoderFallbackException : Unable to translate Unicode character \uD83D at index 399`, the previous reviewer's exception verbatim |
| `Fit` returns `displayName` untrimmed | `An_over_long_display_name_is_trimmed_rather_than_failing_the_request` | **Killed** — `DbUpdateException` on the write |

The guard is also correct in the case nobody wrote a test for: a *low* surrogate at index 399 means
the pair sits wholly at 398/399 and inside the cut, and the guard correctly leaves it alone.

Note on the previous round's mutation method: deleting the `Fit` **call site** does not compile here
(`CA1822` fires on `ProjectAsync`, warnings are errors), so that mutant is killed by the compiler
rather than by the test. I neutered the return value instead, which is the mutant that actually
exercises the assertion.

#### N1 — genuinely closed. It tests the *agreement*, not either half

I was asked whether `The_trim_length_matches_the_column_it_trims_for` is a more elaborate version of
the test it replaced. It is not, and the difference is structural: it reads the width from the EF
model and requires the **stored** value to fill it exactly, so it can only pass while the two
declarations agree. Six mutants, both halves of the pair, both directions — each seen failing:

| Mutant | How it dies |
| --- | --- |
| `MaximumDisplayNameLength` 400 → **399** | stored 399 ≠ column 400 — assertion |
| `MaximumDisplayNameLength` 400 → **450** | trims to 450, `varchar(400)` rejects it — the write fails, request is not 200 |
| `MaximumDisplayNameLength` 400 → **500** | equal to the driving name, so no trim at all — write fails |
| `MaximumDisplayNameLength` 400 → **600** | no trim — write fails |
| `HasMaxLength(400)` → **300** | assertion |
| `HasMaxLength(400)` → **500** | assertion |

The upward drift specifically asked about is caught, and it is caught by the *database*, not by the
assertion: past the column the write itself fails and the request never reaches 200. That is the
strongest of the six, because it does not depend on the test having guessed the right number.

Two suggestions on it, neither blocking:

- The driving name is a literal `new string('n', 500)`. It is coupled to the column: widen the column
  past 500 and this test fails while the two declarations still agree. `columnLength + 100` removes
  the coupling.
- `An_over_long_display_name_is_trimmed_rather_than_failing_the_request` (line 241) asserts a literal
  `400`, which is now a third copy of the same number. It could read the model like its neighbour.

#### Blocking — AC7's guard does not cover the log statement N2 added

The code is right: `LogDisplayNameTrimmed` carries two integers and no content, and AC7 is satisfied
as shipped. The problem is that nothing holds it there.

`Projecting_a_user_logs_neither_the_display_name_nor_the_email`
(`RoleAuthorizationTests.cs:162`) projects a user whose name is `"Priya Confidential"` — 18
characters. `Fit` returns early for anything at or under 400, so **the trim log never fires during
the only test that inspects log output.** The new call site at
`UserProjectionMiddleware.cs:82` is outside everything that test can see.

Demonstrated, not argued. I gave `LogDisplayNameTrimmed` a `{Name}` parameter and passed it
`displayName` — an unambiguous AC7 violation, personal data in the log:

```
dotnet test  ->  exit 0,  62/62 passed  (17 unit, 45 integration)
```

The full suite is green with the display name being written to the log. The mutant survives.

That test's own comment claims the opposite — *"so a future log statement that leaks one fails this
test instead of passing review"* — and this change is precisely the future log statement it names.
Under `TESTING.md` (*"a test is not shown to guard a behaviour until it has been seen to fail when
that behaviour breaks"*) the trim path is unguarded against a **security** criterion, and under
`DOCUMENTATION.md` that comment is now inaccurate in the file this change touched.

**Remedy: one line.** Give the AC7 test a name longer than the column — one request then covers both
the create path and the trim path, and the mutant above dies. A second `[Fact]` would do as well.
Whichever, run the leak mutant against it before trusting it: that is the whole point of the finding.

This is the same shape the previous reviewer named and that this ticket keeps producing — *a
correction is new, unverified work.* N2 was a take-or-leave note, taken correctly, and the new log
call inherited a guard nobody checked reached it.

#### Non-blocking

- **`UserProjectionMiddleware.cs:87-98` — the doc comment now documents the wrong member.** The
  `<summary>` beginning *"A unique violation, and nothing else…"* was written for `IsDuplicateKey`.
  The `[LoggerMessage]` declaration was inserted between the comment and the method, so the comment
  now attaches to `LogDisplayNameTrimmed`, and `IsDuplicateKey` — the most carefully reasoned method
  in the file — is undocumented. Move the log declaration below `IsDuplicateKey`, or the comment
  down onto it.
- **The trim logs on every request, not on every write.** `Fit` is called at line 111, before the
  no-op early return at line 136, so a caller whose identity provider holds an over-long name emits
  an **Information** line on *every* authenticated request, including the majority that write
  nothing. The previous review suggested `Debug` for exactly this reason. Either lower the level or
  move the call onto the write path.
- **The corrected width comment (line 156) reads oddly in its sentence.** *"a subject longer than the
  column (255, the OIDC limit itself) returned 200 with no row written"* describes past behaviour,
  when the column was 200 and the sharp point was that a **legal** 201–255 subject was being
  swallowed. It is no longer false, which is what Q3 asked for, and the `GotIssuesDbContext` comment
  (lines 22-26) carries the full story accurately — so this is a nit, not a finding.

#### Observation — not a finding, and I am not asking for a change

The guard fixes a pair split *at the boundary*. A lone high surrogate sitting anywhere earlier in the
name throws the same `EncoderFallbackException`, with or without `Fit`, for names under 400 too — so
it is pre-existing rather than introduced here, and it is very likely unreachable, since
`System.Text.Json` substitutes U+FFFD for an invalid `\uD83D` escape before a claim value is ever
constructed. I did not verify that substitution, which is why this is an observation. If T-0015 ever
gets a subject-bearing token, it is a cheap thing to poke at.

#### The rest of the change

Scope is clean: the diff touches the projection middleware, its tests, the README paragraph and the
ARCHITECTURE banner — all inside In Scope or the documentation-accuracy work acceptance asked for.
No spec change, no new dependency, no decision at the ADR bar. AC6 is untouched: still no role, no
credential. The README's new sentence is accurate — I checked, and the only `RequireAuthorization`
in `apps/GotIssues.Api` is the bare one on `/health/authenticated` (`Program.cs:131-132`), so an
`admin` and a `member` token do both reach it. The banner no longer counts its own staleness and
keeps the instruction, which is the durable version of that fix.

#### Gates, each read from the tool's own exit status, no pipelines

`dotnet build --no-incremental` → **0 warnings / 0 errors** · `dotnet test` → **exit 0**, 62/62
(17 unit, 45 integration), 0 skipped · `dotnet format --verify-no-changes` → **exit 0** ·
`./tools/check-drift.sh` → **exit 0** ("generated code matches spec/openapi.yaml") ·
`python3 tools/validate-project-os/validate.py` → **exit 0** (17 tickets, 6 ADRs) · branch is a
descendant of `main`, working tree clean after every mutation was reverted.

- **Did:** Re-derived the surrogate fix by deleting the guard and by neutering `Fit`'s return.
  Drove the constant to 399/450/500/600 and the column to 300/500 — six mutants, all killed — to
  test whether N1's replacement test catches disagreement rather than restating one half. Built an
  AC7 leak mutant and ran the **whole** suite against it. Verified the README's policy claim against
  `Program.cs` rather than reading it.
- **Decided:** Q4 and N1 are closed. N2's code is correct and its guard is absent, which is the
  blocking finding — a security criterion whose only test cannot see the code that could violate it.
- **Remaining:** one line in the AC7 test, then re-request. The four non-blocking items are
  take-or-leave, though two of them are in the same file and the same edit.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0009-final-fixes` @ `099f7fb` — **not merged**.
- **Test state:** 62/62 green; eight mutants run — seven killed, one (the AC7 leak) survived, which
  is the finding.
- **Review verdict:** **Request changes** — ENG + ARCH (`claude-rev-4a7e`).

### 2026-08-31 — claude-sm-9d4e — Q5: the AC7 test could not fail

`claude-rev-4a7e` is right, and this is the sharpest finding on the ticket. The AC7 test —
the one whose own comment promises that *"a future log statement that leaks one fails this
test instead of passing review"* — projected an 18-character name. `Fit` returns early at
or under the column width, so the log statement I added in Q4 **never executed in the only
test that inspects log output**. The reviewer gave the logger a `{Name}` parameter, passed
it the display name, and the suite stayed green at 62/62. My N2 fix shipped unguarded, and
the test that was supposed to guard it was the reason I believed otherwise.

The test now projects a name past the column so the trim runs, and asserts the trim line is
present before asserting what it must not contain. **That second assertion is the part that
matters**: without it the test reverts to vacuous the moment anything changes about when
`Fit` trims, and reverts silently, which is precisely how this got here.

| Mutant | Result |
| --- | --- |
| Logger takes `{Name}` and is passed the display name (the reviewer's, which survived before) | **Killed** — `Assert.DoesNotContain() Failure: Sub-string found` |
| Trim-log condition inverted so the line never fires | **Killed** — `Assert.Contains() Failure: Sub-string not found` |

A note on the second one. My first attempt at it was `if (false)`, which failed to compile
(CS0162 under warnings-as-errors) — the compiler killed it, not the test, and a mutant the
compiler rejects is evidence about the build, not about coverage. The reviewer hit the same
thing from the other side last round: deleting the `Fit` call site tripped CA1822. **A
mutation has to be something the codebase would actually accept**, or it tells you nothing.

#### The other three notes, taken

- **Logging moved out of `Fit` to the write path.** `Fit` runs before the "nothing worth a
  write" early return, so an unchanged caller with an over-long name emitted an Information
  line on *every request*. It now logs where the write is decided, and `Fit` is pure and
  static again.
- **`[LoggerMessage]` had been inserted between `IsDuplicateKey`'s XML doc and its method**,
  so *"A unique violation, and nothing else"* was documenting the log method. Moved, and the
  log method got its own doc.
- **Third copy of the column width removed.** `DisplayNameColumnLength(db)` reads it from
  the EF model; the surrogate test derives `columnLength - 1`, the agreement test
  `columnLength + 100`. A suite whose job is keeping two declarations agreeing about a
  number should not have carried three hard-coded copies of it.

#### What I'd take to the retrospective

The pattern this ticket kept producing was *claims outrunning evidence*. This is one layer
down: **a test asserting the right thing about an input that cannot reach the code under
test.** It is not a weaker version of a vacuous test, it is the dangerous version — it reads
as coverage, it names the risk in its comment, and it stays green through exactly the change
it was written to stop. Both times it was caught by someone deliberately breaking the code
and watching, never by reading. The general form: *an assertion is only evidence about a code
path if the input reaches that path — and nothing in a green run tells you whether it did.*

- **Did:** Widened the AC7 test past the column and added the presence assertion that keeps
  it honest; killed the reviewer's surviving mutant. Moved logging to the write path, fixed
  the displaced doc comment, removed the duplicated widths.
- **Decided:** a test that guards a log statement must prove the statement ran.
- **Remaining:** reviewer spot check.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0009-final-fixes` — pending commit.
- **Test state:** 62/62 green; `dotnet format` **exit 0**; `check-drift.sh` **exit 0**.

### 2026-08-31 — Software Engineer + Architect (claude-rev-4a7e) — re-check of the four fixes

Re-check of `t-0009-final-fixes` @ `41dfeba`. Scoped to the four items I raised; AC1–AC8 were not
re-derived — the earlier passes stand and nothing in this diff reaches them.

**Verdict: APPROVE.** The blocking finding is closed, and closed by the half that matters.

#### The blocking finding — closed, and I killed the surviving mutant myself

The AC7 test now drives a name past the column, so the trim path executes inside the only test that
inspects log output, and it asserts the trim log is **present** before asserting what must not be.
Both halves verified here, not read:

| Mutant | Result |
| --- | --- |
| `LogDisplayNameTrimmed` gains a `{Name}` parameter and is passed `rawName` — the mutant that survived a green 62-test suite last round | **Killed.** `Assert.DoesNotContain() Failure: Sub-string found`; full suite 44/45 |
| Trim-log condition inverted (`!=` → `==`), so the line never fires on a trimmed name | **Killed.** `Assert.Contains() Failure: Sub-string not found` |

You were right that the presence assertion is the load-bearing half, and the second mutant is what
proves it: without it the test would go quietly vacuous again and the first mutant would survive
again. `Assert.DoesNotContain("xxxxxxxxxx", …)` is a good addition nobody asked for — it guards the
*trimmed* value as well as the raw one, which is the value that actually reaches the entity.

#### The other three

- **Log moved to the write path.** It now sits after the `nothing worth a write` early return, so a
  caller with an over-long name no longer emits an Information line on requests that write nothing.
  `Fit` is `static` again, which is the honest signal that it no longer has a side effect. Not
  guarded by a test, and I am **not** asking for one: it is log volume, not behaviour, and the
  condition is visible in ten lines.
- **Displaced doc comment.** `[LoggerMessage]` moved out from between `IsDuplicateKey`'s `<summary>`
  and its method; the summary is adjacent to the method it describes again, and the log method has
  its own. Fixed.
- **Third copy of the width.** `DisplayNameColumnLength(db)` is the single reader, and both tests
  derive their input from it (`columnLength - 1`, `columnLength + 100`). The surrogate test's
  arithmetic generalises correctly: with `cut = columnLength`, padding to `columnLength - 1` always
  lands the pair's high surrogate on `cut - 1`, whatever the column becomes.

#### The earlier kills survive the refactor — re-run, not assumed

`Fit` changed shape and both tests changed how they get their numbers, so the previous round's
evidence needed re-earning rather than citing:

| Mutant | Result |
| --- | --- |
| Surrogate guard deleted | **Killed** — `EncoderFallbackException … \uD83D at index 399` |
| `MaximumDisplayNameLength` → 399 | **Killed** — assertion |
| `MaximumDisplayNameLength` → 450 | **Killed** — the write is rejected by the column |
| `MaximumDisplayNameLength` → 500 | **Killed** — the write is rejected by the column |

#### On the TESTING.md rule you asked about — I agree, with two wording cautions

The rule is right and the retro is the right place for it (`evolve-governance`, lane 2, human
approval — not this ticket, and not either of us unilaterally). My read on the wording:

**Agree with the substance.** A mutant the compiler or the analysers reject was killed by the
toolchain, not by a test. It is not merely weaker evidence — it is evidence about a *different
question*, and it is dangerous precisely because it looks like a pass: a vacuous test survives that
outcome without a mark. Your `if (false)` (CS0162) and my deleted `Fit` call site (CA1822) are the
same failure, and it produced a wrong conclusion in one of the two cases.

**Caution 1 — "the codebase would accept it" is too vague to act on.** Name the mechanism: *the
build*, including the compiler and the analysers, since this project runs warnings-as-errors and
that is what turns an ordinary mutation into a compile error. A reader should be able to tell
whether their mutant qualifies without interpreting the phrase.

**Caution 2 — it can be misread as "only use realistic mutants".** That is a *weaker* rule and not
the one you found. The requirement is that the mutant **reach the test**: the mutation must survive
the build so that the only thing left that can kill it is an assertion. Realism is a good heuristic
for getting there — drift a constant, invert a condition, drop a guard, all of which compile — but
it is the means, not the rule.

**One thing I would add rather than remove:** a build-rejected mutant is not worthless, it is
*mis-filed*. It says the invariant is enforced by the compiler or an analyser, which is a stronger
guarantee than a test. It should be recorded as that, and then the real mutant run. Wording I would
propose to the retro:

> A mutation only counts as coverage evidence if the build accepts it. A mutant the compiler or the
> analysers reject was killed by the toolchain, not by a test, and a vacuous test survives that
> outcome unnoticed — rewrite it as a change the build accepts and re-run. Record the mutant you
> actually ran, not the one you first tried. Where the toolchain is what rejects the mutation, that
> is worth recording in its own right: it is a stronger guarantee than a test, and it is still not
> test coverage.

#### Observation — not a finding

The AC7 test's opening comment still promises that *"a future log statement that leaks one fails
this test"*. That is now true for the create-and-trim path this request exercises, and the presence
assertion makes it self-announcing if it stops being true there. It remains a single request, so a
leak from some future statement on a path it does not reach would still not fail it. That is the
generic limit of any one log test and I am recording it, not asking for anything.

#### Gates, each read from the tool's own exit status

`dotnet build --no-incremental` → **exit 0**, 0 warnings / 0 errors · `dotnet test` → **exit 0**,
62/62 (17 unit, 45 integration), 0 skipped · `dotnet format --verify-no-changes` → **exit 0** ·
`./tools/check-drift.sh` → **exit 0** · `python3 tools/validate-project-os/validate.py` → **exit 0**
(17 tickets, 6 ADRs) · branch is a descendant of `main`; working tree clean after every mutant was
reverted.

- **Did:** Re-ran the `{Name}` leak mutant that survived last round's full suite and watched it die;
  inverted the trim-log condition to prove the presence assertion is doing work; re-ran the
  surrogate and constant-drift mutants because `Fit` and both tests changed shape; read the moved
  log statement against the early return; confirmed the doc comment now sits on `IsDuplicateKey`.
- **Decided:** Approve. Six mutants this round, all killed. Nothing outstanding, blocking or
  otherwise.
- **Remaining:** merge, then the handover status commit on the trunk (`GIT.md`); the mutation-rule
  wording goes to the retro, not here.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0009-final-fixes` @ `41dfeba` — **approved for merge**.
- **Test state:** 62/62 green, 0 skipped; six mutants run this round, six killed.
- **Review verdict:** **Approve** — ENG + ARCH (`claude-rev-4a7e`).

### 2026-08-31 — QA / Test Engineer + Product Owner (claude-qa-7c21) — independent acceptance

Second independent acceptance pass, on `main` @ `ece515d` (the squash merge). I did not implement
(`claude-sm-9d4e`), review (`claude-rev-8b4f`, `claude-rev-4a7e`, `claude-rev-2c8d`) or previously
accept (`claude-qa-5a71`) this ticket. I derived my checks from the Problem, Scope, Acceptance
Criteria and Examples before opening the Work Log, and I re-earned every claim by mutation rather
than citing the earlier passes.

**Verdict: PASS.** All eight criteria verified. Q1, Q2 (Q2a/Q2b), Q3, Q4, Q5 and N1 are all
genuinely closed, each confirmed by my own mutation or by reading the artefact rather than the
record. **Two new findings (F1, F2), neither blocking**, plus confirmation that N3 remains open.

#### The criteria

| AC | Verdict | Evidence |
| --- | --- | --- |
| AC1 admin → admin policy | **PASS** | `An_admin_reaches_an_admin_endpoint` (200); `AuthorizationPolicyTests` under both claim types |
| AC2 member → admin policy = 403, not 401 | **PASS** | `A_member_is_refused_an_admin_endpoint_with_403_not_401`; 401 proven distinct by `An_unauthenticated_caller_is_refused_by_a_guarded_endpoint` and my own probe below |
| AC3 either role → member policy | **PASS** | `Either_role_reaches_a_member_endpoint(admin\|member)` — the floor semantics hold |
| AC4 absent/unrecognised role refused | **PASS** | Mutation M-AC4 below: **12 tests die** when the allow-list is replaced by the fallback the ticket warns about |
| AC5 create then update, never duplicate | **PASS** (test host — see F1) | Mutation M-AC5: forcing always-insert kills `Returning_updates_the_record_rather_than_duplicating_it` |
| AC6 no credential, secret or role stored | **PASS** | Mutation M-AC6: adding a `Role` property to `UserRecord` kills `The_projection_stores_no_role_and_no_credential`; migration + model carry only `Subject`, `DisplayName`, `FirstSeenAt`, `LastSeenAt` |
| AC7 no display name or email in the log | **PASS** for the display name; email half vacuous (N3) | Mutations M-Q5a/M-Q5b below |
| AC8 missing display-name claim still projects | **PASS** (test host — see F1) | `A_token_without_a_display_name_still_produces_a_usable_projection` |

#### Mutations I ran myself — nine mutants, nine killed, plus two that deliberately survived

Each was applied to the merged source, confirmed present in the artefact, run, then reverted;
the tree was verified clean (`git status --porcelain` empty) and the suite re-run green afterwards.

| # | Mutant | Result |
| --- | --- | --- |
| M-Q1 | Widen the catch back to every `DbUpdateException` | **Killed** — `A_subject_beyond_the_OIDC_limit_fails_loudly_rather_than_silently` |
| M-Q4 | Delete the surrogate guard (`if (char.IsHighSurrogate(...)) cut--;`) | **Killed** — `EncoderFallbackException : Unable to translate Unicode character \uD83D at index 399` |
| M-Q5a | `LogDisplayNameTrimmed` gains `{Name}` and is passed `rawName` | **Killed** — `Assert.DoesNotContain() Failure: Sub-string found` |
| M-Q5b | Trim-log condition inverted (`!=` → `==`), so the line never fires | **Killed** — `Assert.Contains() Failure: Sub-string not found` |
| M-N1 | Remove `.Where(identity => identity.IsAuthenticated)` from `RoleValues` | **Killed** — 2 unit tests |
| M-AC4 | Member policy → "any authenticated caller is a member" | **Killed** — **8 unit + 4 integration** |
| M-AC5 | Existing-record lookup can never match | **Killed** — return-visit test |
| M-AC6 | Add a `Role` property to `UserRecord` | **Killed** — AC6 model test |
| M-F1b | Keep **only** the `sub` branch of the subject lookup | **Killed** — 10 integration tests |
| M-F1a | Keep **only** the `ClaimTypes.NameIdentifier` branch | **SURVIVED — 62/62 green.** See F1 |
| P-F1c | Test host emits `sub` instead of `ClaimTypes.NameIdentifier` | **62/62 green** — the production branch is correct, merely unproven |

M-Q5b is the one worth naming: it is what proves the presence assertion added for Q5 is
load-bearing. Without it the AC7 test reverts to vacuous silently, which is exactly how Q5
survived a green 62-test suite. I killed it here rather than reading that it had been killed.

#### F1 — the projection's subject is proven only through a claim shape production cannot produce

**Not a defect in behaviour; a coverage gap, and it is this ticket's own original failure recurring one level over.**

`UserProjectionMiddleware.cs:34-35` reads the subject as:

```csharp
var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
    ?? context.User.FindFirstValue("sub");
```

`Program.cs:46` sets `MapInboundClaims = false`, so the JWT handler emits the subject verbatim as
**`sub`** and never as `ClaimTypes.NameIdentifier`. The second branch is therefore the **only** one
that can ever execute in production. `TestAuthentication.cs:48` emits
`new Claim(ClaimTypes.NameIdentifier, …)` — the first branch, and a claim type the real pipeline
never produces. So every test of AC5, AC8, the trim, the race and the length boundaries reaches the
projection through a claim the API will never actually receive.

Measured, both directions:

```
delete the `?? FindFirstValue("sub")` branch   -> 62/62 GREEN   (production's only branch, unguarded)
delete the ClaimTypes.NameIdentifier branch    -> 10 tests FAIL (the test-host-only branch carries all the evidence)
```

This is precisely the shape of the bug this ticket already paid for once: *the suite agreed with
the test host rather than with reality.* That one was fixed for the **role** claim — `TestAuthHandler`
deliberately emits the short `role`, and `AuthorizationPolicyTests` asks the real policies under
both claim types. The **subject** claim never got the same treatment, and the asymmetry is
invisible in a green run.

- **Severity: low, and it does not fail the ticket.** I verified the production branch is *correct*
  rather than merely untested: with the test host changed to emit `sub` — production's exact shape —
  the full suite is **62/62 green** (P-F1c). No caller can reach this today in any case, since no
  token the identity host issues carries a subject at all (client-credentials only; confirmed at
  `ClientFactory.cs:20`, `AllowedGrantTypes = GrantTypes.ClientCredentials`).
- **Destination: [T-0015](T-0015-compose-stack-smoke-test.md) AC8 accepts it** — "a token carrying a
  real subject … T-0009's AC5 and AC8 against a real token rather than a test host". A real token
  carries `sub`, so exercising AC8 exercises this branch. That is a genuine destination, not a
  pointer.
- **But the cheap fix does not need a real token and belongs here or in T-0015:** emitting `sub`
  from `TestAuthHandler` (one claim type) makes the entire existing projection suite exercise the
  production branch immediately. Recording it rather than writing it — `acceptance-test` MUST NOT
  modify implementation code.

#### F2 — a stale comment of the family this ticket keeps producing

`RoleAuthorizationTests.cs:320`: *"OIDC permits 255 characters; the column holds 200."* The column
is **255** and has been since the `WidenUserSubject` migration. The comment sits inside
`A_subject_beyond_the_OIDC_limit_fails_loudly_rather_than_silently` and contradicts
`A_subject_at_the_OIDC_limit_is_projected` a hundred lines above it, which proves 255 succeeds.

The same stale pair was found and corrected in the middleware during Q4 ("the fifth instance of the
pattern this ticket keeps producing"); this copy in the test file survived that sweep. Lines 23
(`GotIssuesDbContext.cs`) and 214 (`RoleAuthorizationTests.cs`) state the same history in the **past**
tense and are accurate — line 320 is the only one asserting it in the present. Cosmetic, one line,
DoD item 5 rather than item 6.

#### N3 is still open, and still correctly described

AC7's email assertion — `Assert.DoesNotContain("logged-1@", log)` — cannot fail. `TestAuthHandler`
emits no email claim, nothing in the projection reads or stores one, and no such string exists
anywhere in the system. AC7 is satisfied **in substance** (there is no email to leak) and the
display-name half is genuinely mutation-proven, so this is not a defect against the criterion. It
was recorded as N3 by `claude-qa-5a71` and remains recorded; I re-confirmed it rather than assuming
it, and I am not raising it again as new.

#### Adversarial exploration — six probes, written and run, none broke it

Written as a temporary test class against the merged code, run, then deleted (tree verified clean):

| Probe | Result |
| --- | --- |
| Two subjects sharing one display name → two records (the ticket's Example) | **2 records** — identity is the subject |
| A name disappearing from the token clears the stored one | `DisplayName` → null |
| `FirstSeenAt` does not move on a return visit | unchanged |
| Unauthenticated caller → 401 **and** zero rows written | 401, `users` empty — the projection does not run for anonymous callers |
| A caller **refused** by the policy still gets a projection | 1 row — middleware sits between authentication and authorisation, which is the documented and correct order |
| **Display name of 300 emoji** (every character non-BMP, so the cut lands mid-pair on the first try) | **200**, stored value does not end in a lone high surrogate |

The last one is the case the Q4 fix was written for, driven harder than the committed test drives
it: the committed test pads with BMP characters so exactly one pair straddles the boundary, while
this one makes every character non-BMP. The guard holds.

I also confirmed `dotnet ef migrations has-pending-model-changes` → *"No changes have been made to
the model since the last migration"*, so the EF model and the deployed schema genuinely agree about
the 255/400 widths that three tests depend on.

#### Documentation — Q2, Q2a and Q2b verified against the code, not read

- README *"Not here yet"* no longer claims nothing reads the role claim. ✔
- README now states plainly **"No shipped endpoint uses them yet"** and names T-0004 as the first
  that will (Q2b). I verified the substance: the only `RequireAuthorization` in
  `apps/GotIssues.Api` is the bare one on `/health/authenticated`. ✔
- README *"machine-client tokens, which carry a role but no subject"* — verified from source at
  `ClientFactory.cs:20` (`GrantTypes.ClientCredentials`), independently of the earlier token decode. ✔
- ARCHITECTURE banner lists T-0009 as built, and the self-counting staleness sentence is gone
  (Q2a) — replaced by the instruction without the number, which is the right call. ✔
- *"the only resource in the specification today is a … placeholder"* — `spec/openapi.yaml` declares
  exactly one path, `/placeholders`. ✔

#### Scope fidelity

**In Scope complete:** policies (`AuthorizationPolicies`, `GotIssuesRoles`), the projection
(`UserProjectionMiddleware`, `UserRecord`), two migrations, the deliberate AC4 refusal, and the
role matrix in the test host.

**Out of Scope untouched, checked by diff and by grep rather than asserted:** no role-management
endpoint, no users API, no membership or per-project permission surface. There is no `MapPost`,
`MapPut`, `MapDelete`, `[HttpPost]`, `[HttpPut]` or `[HttpDelete]` anywhere in `apps/GotIssues.Api`,
and `Controllers/` holds only the generated `PlaceholderController`. No credential or secret is
stored: the `users` table has four columns and none of them is one.

No `TODO`, `FIXME`, `HACK`, `Console.WriteLine` or debugger scaffolding anywhere in the API or
either test project.

#### Definition of Done

1. **Implementation complete** — met.
2. **All acceptance criteria verified** — met, independently, with mutation evidence for each.
3. **Automated tests exist and pass** — met. 62/62, 0 skipped, from a clean `--no-incremental` build.
4. **No known unrecorded defects** — **met, conditionally.** Everything previously raised is closed:
   Q1, Q3, Q4, Q5 and N1 fixed and pinned; Q2/Q2a/Q2b fixed. The AC5/AC8 residual has a destination
   whose scope genuinely accepts it — **I read T-0015 rather than the claim that it accepts it**
   (see below). **F1 and F2 are raised here and must be fixed or given an accepting destination by
   `complete-ticket`**; F1's destination (T-0015 AC8) already exists and is cited above, F2 is a
   one-line correction that belongs to this ticket. N3 remains recorded and is not a defect.
5. **Code quality** — met, with F2 as the single blemish. Four review rounds; build warning-clean;
   formatter clean; no scaffolding; no secrets.
6. **Documentation updated** — **met.** This was the item the first acceptance failed the ticket on,
   and I checked the replacement prose against the code rather than reading it. README and
   ARCHITECTURE are both accurate as of this commit. **No deviation needs recording for item 6.**
7. **Work Log complete** — met, and unusually valuable: the record of the claim-mapping bug, Q5's
   "an assertion is only evidence about a code path if the input reaches that path", and the
   build-rejected-mutant rule are all worth more than the feature.
8. **State updated** — for `complete-ticket`.

**Conditional items.** *Security:* negative cases are the bulk of the suite; authentication was
never disabled (the test scheme adds a handler rather than removing enforcement); no secrets; new
external input (the role, subject and name claims) is validated by an allow-list and by length
handling. *Migrations:* scripted, applied by the explicit migrator step, and genuinely reversible —
`AddUserProjection.Down` drops the table, `WidenUserSubject.Down` returns the column to 200. *ADR:*
none required; `PROJECT.md` §5 and ADR-0003 already fix the model and nothing here changes it.
*Observability:* the one new log statement carries two integers and no content.

**Does anything need a recorded deviation?** **No — provided F1 and F2 are handled at
`complete-ticket`.** Item 6 is clean. Item 4 is clean for everything raised before this pass; F1
already has an accepting destination in T-0015 AC8, and F2 is a one-line fix. A deviation becomes
necessary only if F2 is deferred without a home.

#### The AC5/AC8 residual — I checked the destination rather than the pointer

The project has been bitten three times by a link to a ticket that did not cover what was pointed
at it, so I read T-0015 in full rather than trusting that it accepts this.

It accepts it, twice over and explicitly:

- **In Scope** carries a dedicated bullet: *"The user projection against a token carrying a real
  subject (from T-0009)"*, with the reasoning intact — no token this system can issue carries a
  `sub`, the blind spot is what hid the claim-mapping bug, and it is the condition under which the
  narrowed write-failure handling first becomes reachable — closing with *"This scope line exists so
  the residual has a destination that accepts it."*
- **AC8** states the criterion in T-0009's own terms and, crucially, handles the conditional case:
  *"If no such token can be issued when this ticket is implemented, that is recorded as the reason
  and this criterion is deferred with a named successor — not silently passed."*

T-0015's Out of Scope does **not** disown it: it excludes *"API behaviour that the in-process
integration tier can already reach"*, and the whole point of this residual is that the in-process
tier structurally cannot reach it. That is the exact trap T-0015 fell into with T-0010's token
validation and was widened to fix, so I checked it specifically. **Genuine destination, not a false
pointer.** T-0015 is `committed` in the current sprint, so the residual has a live home rather than
a backlog one. F1 rides along with it.

#### Gates — each exit status read directly from the tool, never through a pipe

| Gate | Exit |
| --- | --- |
| `dotnet build --no-incremental` | **0** — 0 warnings, 0 errors, all 6 projects |
| `dotnet test` | **0** — 62/62 (17 unit, 45 integration), 0 skipped |
| `dotnet format --verify-no-changes` | **0** |
| `./tools/check-drift.sh` | **0** — generated code matches `spec/openapi.yaml` |
| `python3 tools/validate-project-os/validate.py` | **0** — OK (17 tickets, 6 ADRs) |
| `dotnet ef migrations has-pending-model-changes` | **0** — no pending model changes |

Build and suite were re-run after the final revert; working tree verified clean
(`git status --porcelain` empty) before and after every mutant.

#### What I could not verify

- **AC5 and AC8 against a real token** — impossible today; no token this system can issue carries a
  subject. This is the residual, and F1 sharpens it: what is unverified is not merely "a real token"
  but the only subject-claim branch production can execute.
- **AC1–AC4 through a shipped endpoint** — none exists by design; T-0004 is the first consumer.
- I did not re-raise a Docker stack this pass. The earlier acceptance drove the full role matrix
  with real signed tokens through the API's own `Program.cs` and I did not re-derive that; my
  questions this round were about claim shapes, mutation survival, prose accuracy and the
  destination ticket, all answerable in-process and by reading. The one claim I did re-derive from
  source rather than take forward is the "no `sub` in any issuable token" premise, because both the
  residual and F1 depend on it.

- **Did:** Derived checks from the requirements before the Work Log. Ran all five gates plus the EF
  model check, reading each exit status directly. Ran nine mutants and killed nine, including
  independent re-kills of the Q1, Q4, Q5 and N1 fixes rather than citing the reviewers'. Discovered
  F1 by mutating the subject lookup in both directions and confirmed it is a coverage gap rather
  than a defect by giving the test host production's claim shape. Wrote and ran six exploratory
  probes, including a 300-emoji display name, then deleted them. Verified every replacement
  sentence in README and ARCHITECTURE against the code. Read T-0015 in full to confirm it accepts
  the residual.
- **Decided:** PASS. Q1, Q2/Q2a/Q2b, Q3, Q4, Q5 and N1 are all genuinely closed — each confirmed by
  my own mutation or artefact reading, none taken on the record's word. F1 and F2 are recorded for
  `complete-ticket`: neither violates a criterion for any caller the system can currently produce,
  F1 has an accepting destination in T-0015 AC8, and F2 is one line. No DoD deviation is required,
  including for items 4 and 6.
- **Remaining:** `complete-ticket` — handle F1 (fix in the test host, or cite T-0015 AC8) and F2.
- **Open questions / blockers:** none.
- **Branch / PR:** merged; verified on `main` @ `ece515d`.
- **Test state:** 62/62 green, 0 skipped; nine mutants run and killed, two deliberate survivors
  recorded as evidence for F1.
- **Acceptance verdict:** **PASS** — QA (`claude-qa-7c21`), 2026-08-31. `accepted_by` deliberately
  left `none`: the validator reserves it for `complete-ticket` at `done`.

### 2026-08-31 — claude-sm-9d4e — F1 and F2 from acceptance, closed rather than deferred

Both of `claude-qa-7c21`'s findings are cheap and inside this ticket's scope, so neither
needs a destination. Branch `t-0009-acceptance-followups`.

#### F1 — the same bug as this ticket's original, one level over

`UserProjectionMiddleware` reads `ClaimTypes.NameIdentifier ?? "sub"`. `Program.cs` sets
`MapInboundClaims = false`, so **production only ever produces `sub`** — and the test host
emitted only the WS-Federation URI. The consequence is exact: the branch every real caller
takes was unreachable from all 62 tests, and deleting it left the suite green.

This is the ticket's founding defect repeated. AC-level review caught the *role* claim
reading a type the handler never produced, and the fix was to make policies match
production. Nobody then asked the same question of the *subject* claim two lines away. A
claim-mapping fix that stops at the claim it was reported about leaves its siblings exactly
as wrong as they were.

The test host now defaults to `sub`, the production shape — matching how it already handles
`role` ("deliberately verbatim… so the API's own allow-list is what gets exercised rather
than the test host's idea of a sensible value"). The legacy URI stays reachable through a
new header so the fallback branch is covered on purpose rather than by accident.

| Mutant | Before F1 | After F1 |
| --- | --- | --- |
| Delete the `"sub"` branch (production's) | **Survived** — 62/62 green | **Killed** — 10 integration tests fail |
| Delete the `ClaimTypes.NameIdentifier` branch | — | **Killed** — the new legacy-claim test fails |

#### F2 — the third copy of a stale pair

`RoleAuthorizationTests.cs` still read *"OIDC permits 255 characters; the column holds 200."*
The middleware's copy was corrected during Q4; this one survived that sweep because I fixed
the instance in front of me rather than searching for the claim. Corrected.

Worth naming for the retro: **Q4 and F2 are one defect that took two rounds because the fix
was scoped to the sighting rather than to the statement.** The cost of `grep` for the phrase
was seconds; the cost of not doing it was a second acceptance finding.

#### N3, unchanged

AC7's email assertion still cannot fail, because nothing in the system reads an email claim.
Recorded by the earlier acceptor, still true, still not worth a fake email to prove.

- **Did:** Pointed the test host at production's subject claim type and covered the legacy
  branch deliberately; mutation-proved both directions. Fixed the third stale width comment.
- **Decided:** close both rather than defer — a deferral needs a destination, and neither
  of these has any reason to leave the ticket that caused them.
- **Remaining:** review of this branch, then merge.
- **Open questions / blockers:** none.
- **Test state:** 63/63 (17 unit, 46 integration); build **0 warnings**; `dotnet format`
  **exit 0**; `check-drift.sh` **exit 0**.

### 2026-08-31 — Software Engineer + Architect (claude-rev-4a7e) — review of the acceptance follow-ups

Review of `t-0009-acceptance-followups` @ `2dbb4be`, diffed against `main` @ `6b4bb0b`.

**Verdict: APPROVE.** F1 is real, the fix closes it, and I reproduced both the defect and the closure
rather than reading the table. Three non-blocking items below, one of which is mine rather than a
restatement of yours.

#### F1 — verified from both sides, including the counterfactual nobody could have shown before

The claim needing proof is not "the new test passes" but "the branch production takes was unreachable
before". So I reproduced the blind spot itself: reverted the test host's default to the
WS-Federation URI **and** deleted the `"sub"` branch, then ran everything.

| Mutant | Result |
| --- | --- |
| Delete the `"sub"` branch (as shipped) | **Killed** — 10 of 46 integration tests fail |
| Delete the `ClaimTypes.NameIdentifier` branch | **Killed** — exactly one failure, `A_subject_under_the_legacy_claim_type_still_projects` |
| **Old default (URI) + delete the `"sub"` branch** | **Survived — 63/63 green**, the new legacy test included |

The third row is the finding, stated as sharply as it can be: with the old test-host default, the
code path every real caller takes could be deleted outright and the suite would have told you
nothing — and it would still tell you nothing even with the new test present, because the new test
sets the header explicitly. It was the *default* doing the damage, not the missing test. Both
mutants compile and pass the analysers, so the tests killed them, not the toolchain.

#### Your first question — the default is right, and it does not move the blind spot

The two candidate defaults are not symmetric, which is what settles it. A default of `sub` means
omission exercises the shape production emits; a default of the URI means omission exercises a shape
production **cannot** emit. The blind spot is not relocated, it is inverted onto code no real caller
reaches — and that residual now has a dedicated test, so the worst case is that coverage of an
unreachable branch rots.

Two things make me more confident than the argument alone:

- It is the same rule this ticket already paid for with `UseGotIssuesAuthentication` — *the test host
  must agree with the application, not with the test.* The old default had the test host agreeing
  with the middleware's first line instead of with `Program.cs`, which is precisely the shape of the
  `IStartupFilter` episode.
- **The premise is pinned.** `ResourceServerTests.cs:104` asserts `MapInboundClaims` is false. The
  default is safe *because* the option it depends on cannot silently flip; if it could, the default
  would be an assumption of exactly the kind that caused F1. Worth saying out loud in the header's
  doc comment, since that test is what licenses the default.

#### Your second question — keep the fallback, and for a stronger reason than symmetry

Keep it. But the symmetry argument undersells it, and I think the real one is worth recording.

`RoleValues`' doc justifies reading both role claim types as *degrading to working rather than
silently refusing everyone* if inbound mapping is re-enabled. Ask what the equivalent failure is for
the subject: reading only `sub` under re-enabled mapping yields a null subject, so the projection
**silently does not run**. Authorisation keeps working, every request still succeeds, and the users
table simply stops filling — discovered weeks later by T-0006 or T-0008 finding nobody to assign to.

So the two cases are not merely symmetric: the role's degradation is **loud** (everyone 403s, you
know within a minute) and the subject's is **silent**. The resilience argument is therefore stronger
here than in the place it was borrowed from. That also answers the dead-branch objection —
`ENGINEERING.md`'s "no speculative abstractions" is about imagined futures, and this defends against
a configuration change that has already bitten this project once in the role path. It is two tokens,
it is tested, and deleting it would trade a covered branch for a silent failure mode.

**One change I would make, non-blocking.** The order reads backwards:

```csharp
var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
    ?? context.User.FindFirstValue("sub");
```

The unreachable claim type is in the primary position and the production one is the fallback. That
is not just cosmetic — it is arguably how the blind spot stayed invisible, because the code's shape
says "the URI is the normal case" and the test host was written to agree with the code. It is also
inconsistent with the symmetry being claimed: `RoleValues` reads the short `role` **first**
(`identity.FindAll("role").Concat(identity.FindAll(ClaimTypes.Role))`). Swapping to `sub` first
makes the production path primary, matches the new test-host default, and matches `RoleValues`.
I verified the swap: **63/63 green**, both subject tests included. Take or leave.

#### F2 — corrected, and I ran the search you did not

You named the method failure precisely (fixed the sighting, not the statement), so I did the search
rather than trusting that this was the last copy. Across `apps/`, `libs/`, `spec/`, `project-os/`
and the README, the only remaining mentions of the 200-character column are:

- `GotIssuesDbContext.cs:23` and `RoleAuthorizationTests.cs:215` — both past tense and both stating
  the current value ("The column is now 255"). Accurate history, not stale claims.
- the Work Log's own record of the defect. Correct to leave.
- the identity host's Duende migrations, which are unrelated schema.

**No fourth copy survives.** Q4 + F2 is now closed as a statement, not as two sightings.

#### Non-blocking

- **The F2 rewrite restates its own paragraph.** `RoleAuthorizationTests.cs:343-344` now ends
  *"…so beyond 255 is outside the specification"*, and line 348 already said *"Beyond 255 is outside
  the specification, so a loud failure is correct."* Four lines apart, same clause. Line 343 is also
  the only line in that block not wrapped near 80 columns, which is the visual tell. A correction
  read against its own paragraph would have caught it — the same rule this ticket keeps re-deriving.
- **The ordering swap above.**
- **AC7's email assertion still cannot fail** (`Assert.DoesNotContain("logged-1@", log)`), as two
  acceptors have now recorded. I agree with them that inventing an email claim to prove it is worse
  than leaving it — but after this ticket spent a round on an AC7 assertion that could not fail, an
  assertion that *by construction* cannot fail should not look like a live guard. One comment
  marking it as a tripwire for a claim the system does not yet read costs nothing and stops the next
  reader concluding, as I initially did, that AC7's email half is covered.

#### Gates, each read from the tool's own exit status

`dotnet build --no-incremental` → **exit 0**, 0 warnings / 0 errors · `dotnet test` → **exit 0**,
63/63 (17 unit, 46 integration), 0 skipped · `dotnet format --verify-no-changes` → **exit 0** ·
`./tools/check-drift.sh` → **exit 0** · `python3 tools/validate-project-os/validate.py` → **exit 0**
(17 tickets, 6 ADRs) · branch is a descendant of `main`; working tree clean after every mutant was
reverted.

- **Did:** Killed both subject-claim mutants and then reproduced the pre-fix blind spot by restoring
  the old test-host default alongside the deleted branch — 63/63 green, which is the evidence F1 was
  real. Searched the whole repository for surviving copies of the width statement. Verified the
  ordering swap keeps the suite green before suggesting it.
- **Decided:** Approve. The test-host default is correct and its premise is pinned by
  `ResourceServerTests`; the `NameIdentifier` fallback should stay, because the failure it prevents
  is silent rather than loud — which is a better argument than the symmetry it was kept for.
- **Remaining:** merge, then the handover status commit on the trunk.
- **Open questions / blockers:** none.
- **Test state:** 63/63 green; four mutants run this round, three killed and one deliberately made
  to survive as the reproduction of F1.
- **Review verdict:** **Approve** — ENG + ARCH (`claude-rev-4a7e`).

### 2026-08-31 — claude-sm-9d4e — the four non-blocking points from `claude-rev-4a7e`, all taken

**Claim order swapped to `"sub" ?? ClaimTypes.NameIdentifier`.** The reviewer's reading is
right and sharper than my own: the old order put the unreachable type in the *primary*
position, so the code's shape said "the URI is the normal case" — and the test host was
written to agree with the code rather than with `Program.cs`. That is a mechanism for how
the blind spot stayed invisible, not just an inconsistency. It also matches `RoleValues`,
which reads the short `role` first.

Re-ran both mutants against the new order rather than citing the ones from before it —
the code changed shape, so the old evidence describes code that no longer exists:

| Mutant | Result |
| --- | --- |
| `"sub"` only | **Killed** — the legacy-claim test fails |
| `ClaimTypes.NameIdentifier` only | **Killed** — 10 of 46 fail |

**The reviewer's third mutant is the finding stated properly, and I want it recorded.**
Old default (URI) *plus* deletion of the `"sub"` branch: **63/63 green, new test included.**
So the defect was never "a missing test" — it was the test host's *default*, which decides
what every future test exercises by omission. A new test doesn't repair a bad default; the
default keeps the production branch deletable in silence underneath it.

**Kept the fallback, for a better reason than the one I gave.** I argued symmetry with
`HasRole`. The reviewer's argument is stronger: the two failures are not equally visible.
Reading only `sub` under re-enabled inbound mapping yields a null subject, so the projection
**silently stops running** — authorisation still works, every request still succeeds, and the
users table simply never fills, discovered weeks later by T-0006 or T-0008 having nobody to
assign to. The role path's equivalent failure is loud: everyone 403s and you know within a
minute. Resilience is worth more where the failure is silent, and this project has already
had this exact incident once in the role path.

**Doc comment now names `ResourceServerTests`' `MapInboundClaims` assertion**, which is what
licenses the `sub` default: the premise is pinned, so the option cannot flip silently and
leave the test host agreeing with nothing.

**F2's rewrite restated its own paragraph** — the same clause four lines apart, because I
moved a sentence instead of replacing it. Fixed. The reviewer also ran the search I should
have run and confirmed there is no fourth copy anywhere in `apps/`, `libs/`, `spec/`,
`project-os/` or the README: the claim is closed as a *statement*, not as two sightings.

**AC7's email assertion is now labelled a tripwire in the code.** It still cannot fail —
nothing reads an email claim — and faking one would test the fake. But this ticket spent a
whole round on an assertion that could not fail and was read as coverage, so the next reader
should not have to work that out.

- **Did:** Took all four points. Re-earned the mutation evidence after changing the code it
  described. Recorded the counterfactual mutant as the proper statement of F1.
- **Decided:** a test-infrastructure default is a design decision about what every future
  test covers by omission — it deserves the scrutiny of production code, not less.
- **Remaining:** merge and handover.
- **Open questions / blockers:** none.
- **Test state:** 63/63 (17 unit, 46 integration); build **0 warnings**; `dotnet format`
  **exit 0**.
- **Review verdict:** **Approve** — ENG + ARCH (`claude-rev-4a7e`), `1d463ef`.

### 2026-08-31 — QA / Scrum Master (claude-qa-7c21) — Definition of Done walk, ticket closed

Acceptance passed at `6b4bb0b`; the two findings it raised (F1, F2) were fixed and reviewed
before this walk, so the state assessed here is `main` @ `c2d2883`.

**Universal items**

1. **Implementation complete** — the four policies, the middleware, the projection table and
   its migrations are present; the reviewer walked the diff against In Scope and found no
   smuggled scope. Endpoint-specific rules stayed out, as the ticket intended.
2. **Acceptance criteria verified independently** — all eight, by `claude-qa-7c21`, against
   executed tests and adversarial probes, not by reading the Work Log. Boxes ticked above.
3. **Automated tests** — 63 passing, 0 skipped (17 unit, 46 integration). No flaky-ignored
   test. Coverage claims are mutation-proved per [TESTING.md](../../standards/TESTING.md):
   twenty-plus mutants across implementation, review and acceptance, all killed.
4. **No known unrecorded defects** — Q1–Q5, N1, F1 and F2 are all *fixed*, not deferred, so
   item 4's deferral clause applies to only one residual: **AC5 and AC8 are proven in the
   test host, not against a token this system can issue**, because the identity host issues
   client-credentials tokens carrying no `sub`. Destination is **[T-0015](T-0015-compose-stack-smoke-test.md)
   AC8**, and per item 4 I read the destination rather than trusting the pointer: T-0015's
   In Scope names "the user projection against a token carrying a real subject (from T-0009)"
   and states the line exists to accept this residual; AC8 restates it and requires a *named
   successor* if it still cannot be proven. Its Out of Scope excludes only what the in-process
   tier can already reach, which is precisely not this. **Not a false pointer** — checked
   specifically because this project has been bitten by three.
   **N3** (AC7's email assertion cannot fail, since nothing reads an email claim) is a known
   limitation, not a defect: it is now labelled a tripwire in the code so the next reader
   does not mistake it for a guard.
5. **Code quality** — reviewed and approved by `claude-rev-4a7e` (ENG + ARCH) across three
   passes; build 0 warnings under warnings-as-errors, `dotnet format` exit 0; no debug
   scaffolding, dead code, or unreferenced TODOs. The one branch that is unreachable in
   production (`ClaimTypes.NameIdentifier`) is deliberate, argued in the Work Log, and
   covered by a test.
6. **Documentation** — README and ARCHITECTURE were the *original* acceptance failure on this
   ticket; both were re-verified against the code rather than read, including the claim about
   which endpoints enforce roles.
7. **Work Log complete** — decisions, verdicts, mutation evidence and the deferral all here.
8. **State updated** — this commit.

**Conditional items**

- **Regression tests** — every defect fixed on this ticket has a test that fails without the
  fix, each demonstrated by mutation rather than asserted.
- **ADR** — no decision in the diff meets the ADR bar (reviewer checked); ADR-0003 and
  ADR-0005 remain the governing ones.
- **Security** — AC6 (no credential, secret or role persisted) and AC7 (no display name in
  logs) are the ticket's own criteria and are tested; no secrets added; no dependency change.
- **Observability** — the display-name trim is logged, lengths only, never the value.
- **Migrations** — `AddUserProjection` and `WidenUserSubject` are scripted and applied by the
  stack's migration step; `dotnet ef migrations has-pending-model-changes` reports none, so
  the model and the deployed schema agree on the widths three tests depend on.
- **Accessibility** — not applicable, no UI.
- **Deployment** — no pipeline exists (`PROJECT.md` Q6); the change deploys through the
  Compose stack's existing migration step, unchanged by this ticket.

**Verdict: Done.** No DoD deviation required — the first ticket in this project to close
with the deferral clause satisfied by a destination that was verified rather than assumed.

- **Did:** Walked all eight universal items and every applicable conditional item against
  repository state.
- **Decided:** the AC5/AC8 residual is a genuine deferral with an accepting destination, not
  an unrecorded defect.
- **Remaining:** none.
- **Open questions / blockers:** none.
- **Branch / PR:** merged to `main` as `ece515d` and `c2d2883`; branches `t-0009-final-fixes`
  and `t-0009-acceptance-followups` deleted after merge.
- **Test state:** 63/63 green; build, format, drift and validator all exit 0.
