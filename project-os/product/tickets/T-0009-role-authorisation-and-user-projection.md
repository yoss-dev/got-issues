---
id: T-0009
title: Role-based authorisation and the user projection from token claims
type: feature
status: in-acceptance
priority: high
owner: none
implemented_by: claude-sm-9d4e
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
