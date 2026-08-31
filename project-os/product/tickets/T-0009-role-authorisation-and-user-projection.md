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
