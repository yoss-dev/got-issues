---
id: T-0015
title: Automated coverage for behaviour that needs the real Compose stack
type: technical
status: in-acceptance
priority: normal
owner: none
implemented_by: claude-sm-9d4e
accepted_by: none
depends_on: [T-0003, T-0010]
adrs: [ADR-0003]
created: 2026-08-30
updated: 2026-08-31
---

# T-0015: Automated coverage for behaviour that needs the real Compose stack

## Problem / Context

Raised by independent acceptance of [T-0003](T-0003-automated-test-harness.md) (`claude-qa-3f7c`, 2026-08-30).

T-0003's harness closes most of [T-0001](T-0001-runnable-compose-stack.md)'s Definition of Done deviation: it proves migrations are applied by the project's own migrations, that `/health` reports the database's real state, and — the one that mattered most — that the API does **not** migrate on startup.

What it cannot reach is everything that is true of the **Compose stack rather than the application**: T-0001's AC1 (cold start from a clean clone, all services healthy), AC6 (restart against an existing volume is non-destructive), and AC7 (a slow or absent database delays startup rather than crashing it). `WebApplicationFactory` starts the app in-process; it never runs `docker compose`.

Those three criteria were verified **by hand, twice**, and remain verified only by hand. The acceptor's point is the sharp one: that residual currently lives as Work Log prose, which is exactly the pattern DoD item 4 forced into tickets on T-0001. Hence this ticket.

**Widened 2026-08-30 during T-0010's review.** [T-0010](T-0010-duende-identity-host.md) handed this ticket a second residual of the same shape — token validation against a real issuer — and the reviewer (`claude-rev-2c8d`) caught that the ticket as written **did not accept it**: the original Out of Scope excluded "anything about the API's own behaviour", which is exactly what token validation is. So T-0010's AC3, three of AC4's four refusals (expired, wrong audience, unknown signing key — the one its own ticket calls "the one that matters"), and the identity host's no-migrate-on-startup property were pointing at a ticket that disowned them.

A false pointer is worse than no ticket, because it reads as covered. The scope below now genuinely accepts both residuals: **everything whose verification requires the real Compose stack running**, whether that behaviour belongs to the stack or to the API behind it. The unifying constraint is the harness limit — `WebApplicationFactory` runs the app in-process and can drive neither `docker compose` nor a live identity host.

## Desired Outcome

The Compose stack's own start-up, restart, and dependency-ordering behaviour is verified by something repeatable rather than by a person remembering to try it.

## User / Business Value

These are the behaviours that break silently and expensively: a base-image change, a health-condition edit, or a migration-step regression would today be caught only if someone happened to run a cold start. The value is protecting the claim the README makes to anyone cloning the repository.

## Scope

### In Scope

- An automated check that drives the real `compose.yaml` — not `WebApplicationFactory` — covering:
  - **Stack behaviour (from T-0001):** cold start on an empty volume reaching healthy; restart against an existing volume preserving data and re-running the migration step as a no-op; the API tolerating a database that is slow or absent.
  - **Token validation against a real issuer (from T-0010):** a token issued by the identity host is accepted, and the refusals that need a real issuer to construct — expired, wrong audience, and a token signed by an unknown key.
  - **The identity host does not migrate or seed on ordinary startup** — the analogue of T-0001's AC5 for that host, currently unguarded by any test.
  - **The user projection against a token carrying a real subject (from [T-0009](T-0009-role-authorisation-and-user-projection.md)).** T-0009's AC5 and AC8 are proven only in its test host, because **no token this system can currently issue carries a `sub`** — the identity host issues machine-client tokens. Acceptance confirmed it from both ends: seven real tokens decoded, none with a subject; the `users` table held zero rows after a full traffic run. That blind spot is what hid T-0009's claim-mapping bug, and it is the condition under which its narrowed write-failure handling first becomes reachable. **This scope line exists so the residual has a destination that accepts it** — it is conditional on user tokens existing, which follows T-0010's unanswered provisioning question.
- A decision on where it lives and how it is invoked. It is materially slower than `dotnet test` and must not be dragged into the habitual suite ([TESTING.md](../../standards/TESTING.md): the habitual tier stays fast). **Refinement's recommendation, not a constraint** — see Technical Notes.
- Documentation of how to run it.

### Out of Scope

- Replacing anything in T-0003's harness; this covers what that harness structurally cannot.
- CI wiring — there is no CI (`PROJECT.md` Q6).
- API behaviour that the in-process integration tier **can** already reach — anonymous refusal, endpoint presence, health semantics. Those belong in T-0003's tier and are already covered there. The line is not "stack versus API"; it is "needs the real stack versus does not".

## Acceptance Criteria

- [x] AC1: Given an empty volume, when the check runs, then it starts the stack from `compose.yaml` and asserts every service reaches a healthy state, failing if any does not.
- [x] AC2: Given data written to an existing volume, when the stack is restarted, then the check asserts the data survives and the migration step exits zero having applied nothing.
- [x] AC3: Given a database that is slow or absent, when the API starts, then the check asserts the API waits rather than exiting, and becomes healthy once the database arrives.
- [x] AC4: Given a deliberately broken stack (for example the migration step removed, or a health condition dropped), when the check runs, then it **fails** — proven by mutation, not by observing a green run.
- [x] AC5: Given the habitual `dotnet test` suite, when it runs, then this check is not part of it, and the README says how to run it separately.
- [x] AC6: Given the identity host running against the stack, when a token it issued is presented to the protected endpoint, then the request is accepted; and when an **expired** token, a **wrong-audience** token, or one signed by an **unknown key** is presented, then each is refused with 401.
- [x] AC7: Given the identity host started **without** its migration step against an empty schema, when it runs, then it creates no tables and seeds nothing — the analogue of T-0001 AC5 for the identity host.
- [ ] AC8: Given a token carrying a real subject, when an authenticated request is made, then a user projection is created for that subject and updated (not duplicated) on return — T-0009's AC5 and AC8 against a real token rather than a test host. **If no such token can be issued when this ticket is implemented, that is recorded as the reason and this criterion is deferred with a named successor** — not silently passed. **Deferred 2026-08-31 to [T-0018](T-0018-user-subject-tokens.md)** (its AC2 carries this criterion verbatim): no token this system issues carries a `sub`, evidenced by a decoded token in the Work Log.

## Examples / Scenarios

- Cold start, empty volume: all services healthy.
- Restart with data: row survives, migrator a no-op.
- Migration step removed from `compose.yaml`: the check fails (AC4).
- Base image tag changed to something broken: the check fails.

## Dependencies

**T-0003** — reuses its conventions and its container tooling.
**T-0010** — the identity host whose token validation and startup behaviour this covers.

Relates to [T-0012](T-0012-pin-container-base-images.md): once base images are digest-pinned, this check is what would catch a bad pin.

## Risks / Unknowns

- **This is a slow test by nature** — image builds and container startup. If it becomes slow enough to be skipped it protects nothing, which is why AC5 keeps it out of the habitual tier and why it needs an obvious way to run.
- Driving `docker compose` from a test harness is awkward; an alternative is a shell script with assertions. Refinement should choose deliberately rather than defaulting to whichever is nearer to hand.
- **AC3 is hard to make deterministic** — "slow database" is a race by construction. It may need an injected delay rather than luck.
- **The split seam, if this overruns, is stack (AC1–AC3) versus identity (AC6–AC7) — and it only works *after* the harness exists.** Splitting earlier just relocates the expensive part, which is standing the harness up at all; both halves need the same one (`claude-rev-2c8d`, 2026-08-30).
- **AC4 is what makes the other six trustworthy** — mutation-proven rather than green-run-proven. If anything is trimmed under pressure, not that one.
- **AC6's expired-token case needs either a short-lived token lifetime configured for the test or clock control.** Minting an expired token requires the issuer's cooperation; this is the refusal case most likely to be quietly dropped.
- **The expired-token assertion is subject to a ~5-minute `ClockSkew`, and the number matters.** No `ClockSkew` is configured, so the framework default applies. Measured during T-0010's acceptance (`claude-qa-3f7c`, 2026-08-30): **200 at 268 s past `exp`, 401 at 328 s.** An implementer who mints a 1-second token and asserts 401 immediately will get **200**, and will then either believe the harness is broken or loosen the assertion into a false pass. Either configure `ClockSkew` explicitly for the test or wait past the window — decide which, and say so in the test.
- Overlaps [T-0014](T-0014-correct-testing-standard-commands.md) if the standard ends up describing this command too.

## Technical Notes

**Where it lives — a recommendation, not a constraint.** An xUnit project (`apps/GotIssues.SmokeTests`) reusing T-0003's conventions, but **kept out of the solution's default test run** and invoked through a thin `tools/smoke.sh` wrapper. That keeps `dotnet test` meaning exactly what it means today (AC5), keeps real assertions and one report format rather than hand-rolled shell comparisons, and gives one obvious documented entry point.

*The honest cost:* a project outside the solution is easy to let rot, since nothing compiles it by accident. The mitigations are that the wrapper is the documented command and that [T-0014](T-0014-correct-testing-standard-commands.md) will name it in `TESTING.md`. The alternative — a trait filter with the habitual command becoming `dotnet test --filter "Category!=Smoke"` — keeps the project in the solution but changes the command every other document already documents. Implementation may reverse this; it should say why.

**Driving Compose from a test.** The check must exercise the real `compose.yaml`, not a copy: a smoke test against a duplicated compose file verifies the duplicate. Use its own project name (`-p`) so it cannot collide with a developer's running stack, and tear down what it starts — [TESTING.md](../../standards/TESTING.md)'s attribution rule applies here more than anywhere, since this test's entire subject is *a running stack on localhost*.

## Testing Notes

AC4 is the criterion that keeps this honest: a stack check that has only ever been seen green proves nothing. Mutate the compose file, watch it fail, revert.

## Relevant ADRs & Documentation

- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — the Compose constraint and the explicit migration step
- [TESTING.md](../../standards/TESTING.md) — tiers, and keeping the habitual suite fast
- [T-0001](T-0001-runnable-compose-stack.md) — the criteria this covers, currently verified only by hand

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-31 during `refinement-session`. All nine universal items hold. Item 7 (sizing) is the one that needed argument and is recorded in the Work Log. Item 5: depends on T-0003 and T-0010, both `done`. Conditional items: no personal data; no UX; no architectural decision at the ADR bar — this builds verification, not system structure. No exceptions applied.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`.

---

## Work Log

### 2026-08-30 — Software Engineer (claude-sm-9d4e)

- **Did:** Created from T-0003's acceptance finding, so T-0001's Compose-level coverage residual is tracked rather than left looking settled in a Work Log.
- **Decided:** none — scope deliberately limited to what `WebApplicationFactory` structurally cannot reach.
- **Remaining:** Refinement. The main open choice is harness-driven versus script-driven.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-30 — Software Engineer (claude-sm-9d4e)

- **Did:** Widened during T-0010's review. The reviewer found that T-0010 was handing this ticket a residual it explicitly disowned — the original Out of Scope excluded "anything about the API's own behaviour", which is what token validation is. Retitled, scope and Out of Scope rewritten around the real constraint (needs the running stack), and AC6/AC7 added for the token round trip, the issuer-dependent refusals, and the identity host's no-migrate-on-startup guard.
- **Decided:** widened this ticket rather than creating a second one. Both residuals need the same thing — a harness that drives the real `compose.yaml` — and two tickets would have built it twice.
- **Remaining:** Refinement. Now covers two tickets' residuals, so sizing deserves a fresh look; splitting along the stack/token seam is the obvious option if it is too large.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — Business Analyst (claude-sm-9d4e) — refinement

Perspectives applied: Product Owner, Business Analyst, Software Engineer, Architect, QA, Security.

**Sizing — the item that needed a verdict.** Seven acceptance criteria looks like too much, and the reviewer flagged it as possibly oversized. It is not, and the reason is structural: **AC1–AC3 and AC6–AC7 all reduce to "drive compose, assert one thing" once the harness exists.** The whole cost of this ticket is standing that harness up — a runner that can bring the stack up under its own project name, wait for health, assert, and tear down. After that, each criterion is a few lines.

That also settles how it splits if it does overrun: **the seam is stack (AC1–AC3) versus identity (AC6–AC7), and it only works *after* the harness exists.** Splitting earlier relocates the expensive part instead of dividing it, because both halves need the same harness. Recorded in Risks by the reviewer; confirmed here as the refinement verdict rather than left as a note.

**This ticket now answers to a rule that did not exist when it was written.** [TESTING.md](../../standards/TESTING.md) gained a requirement on 2026-08-31 that coverage claims be verified by mutation — break the behaviour, watch the check fail, restore. **AC4 already required exactly that**, having been written from the same evidence that produced the rule. The alignment is worth noting because it means AC4 is no longer this ticket's own idea of rigour; it is the project standard, and dropping it under pressure would now be a standards violation rather than a judgement call.

**ENG:** recorded a recommendation for where the check lives — an xUnit project outside the solution's default run, invoked via a wrapper — with the honest cost (an unreferenced project can rot) and the alternative (a trait filter that changes the habitual command every other document names). Marked clearly as a recommendation; implementation may reverse it with reasons.

**ENG:** added the instruction that the check must drive the *real* `compose.yaml`. A smoke test against a copied compose file verifies the copy, which is the most plausible way for this ticket to produce a check that passes while proving nothing.

**QA:** the attribution rule now in `TESTING.md` applies here more sharply than anywhere else in the project, because this test's entire subject is a running stack on `localhost` — the exact situation that produced two false passes in SPRINT-001.

**What this ticket discharges when it lands:** the DoD item 3 deviations recorded on [T-0001](T-0001-runnable-compose-stack.md) and [T-0010](T-0010-duende-identity-host.md). Both were approved on the basis that this ticket would close them, so it carries more weight than its position in the backlog suggests.

**DoR verdict: `ready`.**

### 2026-08-31 — Software Engineer (claude-sm-9d4e)

- **Did:** Added the T-0009 user-projection residual to scope and AC8, from T-0009's acceptance (`claude-qa-5a71`).
- **Decided:** widened this ticket rather than pointing at it without checking — its scope named stack-dependent verification but not this, and citing it regardless would have been the false-pointer failure DoD item 4 exists to prevent, which this project has now made three times.
- **Decided:** AC8 carries an explicit instruction for the case where no subject-bearing token can be issued when it is implemented: record the reason and name a successor, rather than pass it quietly. The residual's whole history is that it was invisible.
- **Remaining:** Refinement.
- **Open questions / blockers:** none.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — claimed, with the implementation plan

Claimed at `a21302b`. Dependencies verified `done` in their own files, not assumed from the
sprint table: T-0003 and T-0010 both read `status: done`.

#### Approach

Refinement's recommendation taken: an xUnit project `apps/GotIssues.SmokeTests`, kept **out
of `GotIssues.slnx`** so `dotnet test` keeps meaning exactly what it means today (AC5), and
invoked through `tools/smoke.sh`. If it turns out the solution file globs rather than lists,
I will say so and fall back to the trait filter, recording why.

The check drives the real `compose.yaml` with `-f`, never a copy — a smoke test against a
duplicated compose file verifies the duplicate.

#### Files expected to change

| Path | Why |
| --- | --- |
| `apps/GotIssues.SmokeTests/` (new) | the check itself |
| `tools/smoke.sh` (new) | the one documented entry point |
| `README.md` | how to run it (AC5) |
| `project-os/architecture/ARCHITECTURE.md` | a new verification tier is a state change |

#### Test plan, criterion by criterion

| AC | How it is verified |
| --- | --- |
| AC1 | `compose up -d --wait` on fresh volumes; assert every long-running service healthy and both migrators exited 0 |
| AC2 | write a row, `compose down` (volumes kept), `up` again; assert the row survives, `__EFMigrationsHistory` is unchanged in count, migrator exit 0 |
| AC3 | start `api` with `--no-deps` and no database; assert the container stays running rather than exiting, and reports unhealthy; start postgres and the migrator; assert it becomes healthy |
| AC4 | derive a mutated compose file *from the real one* (migration step neutered; `service_healthy` condition dropped) and assert the same assertions fail |
| AC5 | the project is outside the solution; `dotnet test` at the root is unchanged; README documents `tools/smoke.sh` |
| AC6 | a genuine token from the token endpoint is accepted; expired, wrong-audience and unknown-key tokens each refused 401 |
| AC7 | start `identity` with `--no-deps` against an empty schema; assert no tables and no seeded rows |
| AC8 | attempt a token carrying `sub`; if the identity host cannot issue one, defer with a **named successor** per the criterion |

#### Three things I want to settle before writing code, not at acceptance

**1. Ports — the project's own repeated defect.** `compose.yaml` publishes `${API_PORT:-8080}`
and `${IDENTITY_PORT:-8081}`. A distinct `-p` project name does **not** prevent a host port
collision, and this project has produced a port-collision false pass twice, the second time
by the person who had just written up the first. The check will publish on **ephemeral ports**
(`API_PORT=0`), discover the real port with `docker compose port`, and assert container
`running`/`healthy` before trusting any HTTP response, per TESTING.md's attribution rule.

**2. AC6's expired token, and a genuine finding underneath it.** The API sets no `ClockSkew`,
so `JwtBearer` keeps the default **five-minute** grace: a token five minutes past `exp` is
still accepted. Waiting out that window would make the check unusable, so the three refusal
cases will be **minted against the identity host's real signing key** (read from the
`identity-keys` volume) with `exp` well outside the window — deterministic, no waiting, and no
production change smuggled in under a coverage ticket. The accepted case uses a genuine token
from the token endpoint, so the happy path is never synthetic.

The five-minute grace itself is a **decision nobody made** — it is the `JwtBearer` default, not
a choice recorded anywhere. It is out of scope here (this ticket adds verification, it does not
change the resource server), so I will raise it as its own ticket rather than fix it in passing.

**3. AC8 will probably defer, and the successor does not exist yet.** Client-credentials tokens
carry no `sub` by construction, and no ticket in the backlog covers user provisioning. If the
identity host cannot be made to issue a subject-carrying token without inventing a
provisioning model — which would be scope creep of the worst kind — I will create the successor
ticket through `create-ticket` and name it in AC8, as the criterion itself demands.

#### Risks

- **Cost is entirely in standing the harness up**, per refinement; the per-criterion work is
  small once containers can be driven and asserted against.
- **Image builds dominate the runtime.** The check must build the same images the stack uses,
  so first run is minutes. That is inherent, and it is why AC5 keeps this out of the habitual
  tier.
- **AC3 is a race by construction.** Driving it with `--no-deps` makes it deterministic:
  the API is started with nothing to wait for and must be observed *not exiting*.

- **Did:** Claimed the ticket; verified dependencies from their own files; loaded
  `compose.yaml`, `TESTING.md`, both `Program.cs` migrate paths and the seeder before planning.
- **Decided:** ephemeral ports and container-state assertions before any HTTP trust; refusal
  tokens minted against the real signing key rather than waiting out the clock-skew window.
- **Remaining:** implementation.
- **Open questions / blockers:** none blocking; two items (the five-minute grace, AC8's
  successor) will become tickets rather than silent fixes.
- **Test state:** not started.

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — built, and what the mutations changed

Branch `t-0015-compose-smoke-test`. Eleven checks, `tools/smoke.sh`, about three minutes.

Refinement's recommendation held: `GotIssues.slnx` lists projects explicitly rather than
globbing, so `apps/GotIssues.SmokeTests` genuinely stays out of `dotnet test` — still 63
tests in about four seconds, unchanged (AC5).

#### AC4 did its job before it was even finished, and the finding is the interesting part

The first mutation — the migration step neutered so it exits 0 without applying anything —
**passed the check**. Not a flaw in the mutation: a flaw in the check I had just written.

`/health` probes connectivity (`DatabaseHealthCheck` calls `CanConnectAsync`), which is
exactly what T-0001 AC3 asked of it and exactly right for its purpose. It says nothing about
the schema. So a stack whose database has **no tables at all** reports every service healthy,
and a check built on service health alone reports a healthy stack. AC1 would have "passed"
on a database with nothing in it.

`AssertSchemaMigratedAsync` now asserts the migration step's *effect* — the history table
exists in `public`, it has rows, and `users` exists. That is what makes the mutation fail.

Two things worth keeping from this:

1. **Service health cannot stand in for migrations having run.** Nothing but the schema can
   speak for the schema. The check was measuring a green signal from the wrong source, which
   is the same family as SPRINT-001's seven instances.
2. **A mutation that passes is worth more than one that fails.** A failing mutant confirms
   what you believed; this one corrected it. Had AC4 been trimmed under pressure — the risk
   the ticket names explicitly — the check would have shipped unable to detect the single
   failure it exists to catch, while reporting green.

A second finding of the same shape, smaller: the first schema assertion counted
`__EFMigrationsHistory` across all schemas and expected exactly one. The identity host keeps
its own history table of that name in the `identity` schema, so a healthy stack counts two
and an unmigrated one counts one — the assertion would have been satisfied in precisely the
case it exists to catch. Qualified by schema.

#### Mutation evidence, each mutant confirmed present in the artefact before the result was trusted

| Criterion | Mutant | Result |
| --- | --- | --- |
| AC4 | migration step neutered (exits 0, applies nothing) | **Killed** — after the check was fixed; *survived* before, which is the finding above |
| AC4 | API healthcheck disabled | **Killed** — `up --wait` returns 0 with nothing to wait for, so only the explicit `compose ps` assertion catches it |
| AC2 | restart discards its volume | **Killed** — the surviving row is gone |
| AC3 | database present after all | **Killed** — "reports healthy with no database at all" |
| AC6 | expired token given a future `exp` | **Killed** — accepted, so the 401 was attributable to expiry |
| AC6 | wrong-audience token given the right audience | **Killed** — accepted |
| AC6 | unknown-key token replaced by a genuine one | **Killed** — accepted, so the 401 was attributable to the signature |
| AC7 | identity migrator allowed to run | **Killed** — tables appear |

The three AC6 mutants matter together: each refusal is attributable to the single defect it
names rather than to a resource server that refuses everything. The accepted case stayed
green throughout, which is the other half of that argument.

#### Decisions

**Ephemeral ports, always.** `compose.yaml` publishes 8080/8081, and a distinct `-p` project
name does not stop a host port collision — this project produced that exact false pass twice.
Every stack publishes on port 0 and the harness discovers the real port. The attribution test
stops the API container and requires `/health` to *stop answering*; without it, a 200 proves
only that something on localhost is alive.

**Refusal tokens are minted against the identity host's real signing key**, read from the
running container, with `exp` an hour in the past. The API leaves `JwtBearer`'s default
five-minute clock skew, so a freshly-expired token is still valid and a check that waited out
that window would take longer than the whole suite. The accepted case is always a genuine
token from the token endpoint — a happy path proved with a synthetic token proves nothing
about the issuer.

**The five-minute grace is now [T-0019](T-0019-token-clock-skew.md)**, not a fix smuggled in
here. This ticket adds verification; changing token validation under a coverage ticket is how
a decision stops being visible.

**AC4's mutations arrive as Compose override files**, so the base is the real `compose.yaml`
with one thing deliberately broken. Mutating a copy would prove the copy was broken.

**AC3 is driven with `--no-deps`.** "Slow database" is a race by construction; starting the
API with no database at all turns it into a fact.

#### AC8 — deferred, with the successor named as the criterion requires

No token this system can issue carries a `sub`. Evidence rather than assertion — a genuine
member token decoded from the running stack:

```json
{"aud":"gotissues-api","client_id":"smoke-member-client","exp":…,"iat":…,
 "iss":"http://localhost:8081","jti":"…","nbf":…,"role":"member","scope":["gotissues.api"]}
```

`AllowedGrantTypes = GrantTypes.ClientCredentials` authenticates a *client*, not a person, so
this is what the grant type means rather than a defect. Making one requires deciding how
people authenticate, which is a system-shaping decision and plainly outside a ticket about
coverage.

Successor: **[T-0018](T-0018-user-subject-tokens.md)**, created and registered, carrying this
criterion as its AC2 and the decoded token as its evidence. Its scope is deliberately "make a
subject-carrying token possible and proven", not a provisioning model — picking the grant
type here would have smuggled the decision into a deferral.

#### Honest cost of the chosen layout

Out of the solution means **nothing compiles this project by accident**: the pre-merge gates
build and format the solution, and would not notice this project failing to compile.
`tools/smoke.sh --build-only` exists for that, and the README says so. The mitigation is real
but it is a habit, not a gate — worth naming rather than leaving for someone to discover.

- **Did:** Built the harness and eight criteria; mutation-proved every criterion that
  discharges another ticket's deferral; fixed the check when AC4 showed it was weaker than
  claimed; created T-0018 and T-0019.
- **Decided:** as above — ephemeral ports, minted refusals, overrides for mutations,
  `--no-deps` for AC3, and raising the clock-skew default rather than fixing it in passing.
- **Remaining:** review, then acceptance.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0015-compose-smoke-test`.
- **Test state:** `tools/smoke.sh` **11/11**, exit 0, 2m56s. Root `dotnet test` **63/63**
  unchanged; build 0 warnings; `dotnet format` exit 0 for both the solution and the smoke
  project; `check-drift.sh` exit 0; `validate.py` exit 0 (19 tickets, 6 ADRs).
- **For QA to probe:** whether the check would notice a *partial* migration (one migration
  applied, one missing) — the schema assertion checks that the history is non-empty, not that
  it matches the migrations on disk.

### 2026-08-31 — Software Engineer + Architect (claude-rev-6d21) — code review

Reviewed `t-0015-compose-smoke-test` @ `be97874` against `main`. I did not implement this
ticket (`implemented_by`/`owner`: `claude-sm-9d4e`). Architect perspective added because the
change introduces a new verification tier and asserts across the system's start-up, schema
and token-validation boundaries.

**Verdict: REQUEST CHANGES.** Four blocking findings, all in `BrokenStackTests` /
`IdentityStartupTests` — that is, in AC4 and AC7, the two criteria whose job is to make the
others believable. Everything else in this change is sound and, where I could break it, it
resisted. The findings are cheap to fix; none of them require redesign.

#### Gates I ran myself, each exit code read from the tool and not from a pipeline

| Command | Exit | Result |
| --- | --- | --- |
| `./tools/smoke.sh` | **0** | 11 passed, 0 failed, 3 m 03 s |
| `dotnet test` (root) | **0** | 17 + 46 = **63**, 10.7 s wall — unchanged, seconds not minutes (**AC5 holds**) |
| `dotnet build --no-incremental` | **0** | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | **0** | solution |
| `dotnet format apps/GotIssues.SmokeTests/…csproj --verify-no-changes` | **0** | the project outside the solution |
| `./tools/check-drift.sh` | **0** | generated code matches the spec |
| `python3 tools/validate-project-os/validate.py` | **0** | 19 tickets, 6 ADRs |
| `dotnet list … package --vulnerable --include-transitive` | **0** | no vulnerable packages |

`GotIssues.slnx` lists projects explicitly (no globbing), so AC5's mechanism is real and not
a convention someone must remember.

#### What I verified independently, rather than reading

**AC4's central claim — that `AssertSchemaMigratedAsync` is what kills the neutered
migrator — is true, and I reproduced the whole chain by hand** on my own Compose project
with the same override:

- `docker compose up --wait` **exits 0**; `compose ps` reports `postgres`, `identity`, `api`
  all `running`/`healthy` and both one-shots `exited 0`. So `AssertStackHealthyAsync` passes
  against a stack whose database has no tables — it cannot catch this mutant.
- `GET /health` returns **200** `{"status":"Healthy","database":"database reachable"}` on
  that same empty database. The implementer's reading of the original survival is exactly
  right: health is connectivity, and connectivity cannot speak for schema.
- `select count(*) … table_schema='public' and table_name='__EFMigrationsHistory'` → **0**,
  and `… table_name='users'` → **0**. So the first assertion in `AssertSchemaMigratedAsync`
  is the one that fires. **The fix is right, not merely sufficient.**
- The **unqualified** count — the earlier, weaker form — returns **1** on the broken stack,
  because the identity host's own history table sits in the `identity` schema. The second
  finding in the Work Log is real and I saw it: qualifying by schema was necessary, and
  asserting `users` as a second independent witness is the correct belt-and-braces.

**AC6's minted tokens are a legitimate proof, and I proved it the hard way.** I minted my
own tokens in a standalone script (pure-Python RS256, no reuse of `TokenFactory`) using the
identity host's real key read from the running container, and called
`/health/authenticated` on the discovered ephemeral port:

| Token | Result |
| --- | --- |
| genuine, from `/connect/token` | **200** |
| **control:** minted, real key, right audience, `exp` +30 m | **200** |
| minted, real key, right audience, `exp` −1 h | **401** |
| minted, real key, right audience, `exp` **−60 s** | **200** |
| minted, real key, audience `some-other-api` | **401** |
| minted, **fresh RSA key**, right audience | **401** |
| no token | **401** |

The control is the whole argument: a minted token that differs from a genuine one in nothing
but its lack of `sub` is **accepted**. Minting is therefore not what causes any refusal, so
each 401 is attributable to the single named defect. This does not test the test.

The `−60 s` row is worth keeping: it is direct, reproduced evidence of the five-minute
`ClockSkew` grace, and it shows the hour-past-`exp` margin was necessary rather than
decorative. **[T-0019](T-0019-token-clock-skew.md)'s premise is confirmed empirically.**

The genuine token I decoded carries no `sub` either — first-hand corroboration of AC8's
deferral evidence.

**AC7 is true today.** Started with `--no-deps` against an empty schema, the identity host
stays `running` (health `starting`, its config-store check failing on
`relation "identity.Clients" does not exist`) and creates **zero** tables in any non-system
schema. The behaviour is correct; the *test* has a gap — see blocking finding 4.

**Attribution generally is well handled.** Ephemeral publishing plus `docker compose port`
discovery means the host port is definitionally owned by this project's container, and the
`api` endpoint is additionally proved by stopping the container and requiring `/health` to
stop answering. I could not find a way for AC1, AC2, AC3 or AC6 to pass against a stack that
never started, or against somebody else's.

---

### Blocking findings

**B1 — the AC4 mutation stacks are never torn down, and the failure is swallowed.**
`apps/GotIssues.SmokeTests/BrokenStackTests.cs:73` declares `await using var stack`, whose
disposal runs **after** the `finally` at `:92-94` deletes the override file. `DisposeAsync`
(`Infrastructure/ComposeStack.cs:245-253`) then runs
`docker compose … --file <deleted file> down`, which cannot resolve the file.
`DisposeAsync` discards the `CommandResult` — it is the one place in the codebase that does
not call `EnsureSucceeded` — so the failure is silent.

Reproduced, not inferred. I emptied Docker of every `gotissues-*` project and volume, ran
`./tools/smoke.sh` to a green 11/11, and immediately afterwards:

```
gotissues-smoke-health-condition-dropped   exited(2), running(3)
gotissues-smoke-migration-step-removed-1   exited(2), running(3)
+ four orphaned volumes
```

The other four stacks tear down correctly; only the two that use override files leak. I then
ran the exact `down` command the harness runs and got
`open /var/folders/…/gotissues-smoke-health-condition-dropped-….yaml: no such file or
directory`, exit **1**.

This is the ticket's own Technical Note (*"tear down what it starts"*) unmet, and it is what
produced the five stale mutation stacks I found on this machine before I started. Fix:
delete the override file after disposal (or hold it for the object's lifetime), and make
`DisposeAsync` surface a failed `down` rather than discard it.

**B2 — the two AC4 stacks do not have project names of their own.**
`BrokenStackTests.cs:74` builds `$"gotissues-smoke-{label}-{Guid.NewGuid():N}"[..40]`. The
prefix for `health-condition-dropped` is **41 characters**, so the truncation removes the
GUID *entirely* and every run uses the fixed name
`gotissues-smoke-health-condition-dropped`. `migration-step-removed`'s prefix is 39, leaving
**one hex character** — sixteen possible names.

Evidence it has already happened here: `docker compose ls` listed a single
`gotissues-smoke-health-condition-dropped` project carrying **two different override config
files**, i.e. two separate runs sharing one project, its containers and its volumes.

[TESTING.md](../../standards/TESTING.md) is explicit — verification against a running service
"runs under its own project name (`docker compose -p <name>`), **so it cannot collide with
another stack**" — and this ticket's Technical Notes repeat it. A fixed name collides with
every other run of itself. The 40-character truncation also has no cause I can find: Compose
project names carry no such limit.

**B3 — `RunCheckAgainstAsync` treats *any* exception as "the check failed", so AC4 can pass
for reasons that have nothing to do with the mutation.** `BrokenStackTests.cs:87` catches
everything but `OutOfMemoryException`, and `:43` / `:57` assert only `failure is not null`.
The `try` block includes `BuildAsync().EnsureSucceeded(...)` and `up.EnsureSucceeded(...)`,
so **both AC4 tests pass on a machine where `docker compose build` fails, or where `up`
fails for any reason at all** — including a concurrent run tearing down the shared-name stack
underneath them (B1 + B2 make that reachable rather than theoretical).

The implementer applied precisely this standard to AC6 — three mutants so that each 401 is
attributable to one named defect — and it is not applied to AC4, which is the criterion the
other seven rest on. A check whose pass condition is "something went wrong" is a green signal
measured from the wrong source, the family [RETRO-SPRINT-001](../../delivery/retrospectives/RETRO-SPRINT-001.md)
records seven times. Fix: let build/`up` failures propagate as failures, catch only the
assertion exception, and assert on its identity — the migration mutant must fail *in
`AssertSchemaMigratedAsync`* ("migrations history table does not exist"), the healthcheck
mutant *in `AssertStackHealthyAsync`* ("reports health ''"). Both messages already exist and
are distinctive.

**B4 — AC7 never asserts the identity host actually ran.**
`IdentityStartupTests.cs:29-35` starts `identity --no-deps`, waits 25 s, and asserts the
non-system table count is `0`. A container that exited on startup produces the identical
observation. The criterion says "*when it runs*, then it creates no tables"; the test checks
the consequent and not the antecedent, so the day the host starts failing fast against a
missing schema — a plausible and arguably desirable change — this test keeps passing while
proving nothing.

I verified the host **is** running today (state `running`, health `starting`), so this is a
gap in the evidence rather than a false result. The AC7 mutant (migrator allowed to run)
does not close it: it proves tables-appearing is detected, not that the host was alive. Fix
is two lines — assert `ServiceAsync("identity")` is `IsRunning` both before and after the
wait, so the zero is attributable to a host that ran and declined to migrate.

---

### Non-blocking notes and suggestions

**N1 — the layout cost is honestly named, but its mitigation is not yet owned anywhere.**
`--build-only` plus the README is exactly the mitigation refinement accepted, so this is not
a finding against the implementation. But it is a habit, not a gate: nothing in
`GIT.md`'s pre-merge list fails when `apps/GotIssues.SmokeTests` stops compiling. I checked
[T-0014](T-0014-correct-testing-standard-commands.md) rather than assume: its scope accepts
*naming* the command in `TESTING.md`, and for `GIT.md` accepts only correcting the drift
command — it does **not** accept "add `smoke.sh --build-only` to the merge gates". If the
team wants the gate, that needs a scope line, per DoD item 4. Flagging so it is a decision
rather than a drift.

**N2 — dependency check not recorded.** `GotIssues.SmokeTests.csproj:24` adds
`Microsoft.IdentityModel.JsonWebTokens` 8.19.2 as a new direct reference.
[SECURITY.md](../../standards/SECURITY.md) requires the vulnerability check noted in the Work
Log; [ENGINEERING.md](../../standards/ENGINEERING.md) requires a short justification. I ran
it: **no vulnerable packages**, and the package is already transitive via
`Microsoft.AspNetCore.Authentication.JwtBearer`, so the risk is nil — but the record is the
requirement. `xunit`, `xunit.runner.visualstudio` and `Microsoft.NET.Test.Sdk` versions match
the two existing test projects exactly, which is the right instinct.

**N3 — credential-shaped literals in the repository.** `Infrastructure/ComposeStack.cs:50-56`
commits `smoke-local-only`, `smoke-admin-secret`, `smoke-member-secret`. They authenticate
nothing beyond a stack that exists for three minutes, and `.env.example` next door uses
`replace-with-a-local-value` placeholders precisely to avoid committing values of this shape.
SECURITY.md's wording is absolute ("including in tests, fixtures"). Deriving them per run
(`$"smoke-{Guid.NewGuid():N}"`) costs one line and removes the ambiguity.

**N4 — one schema query is unqualified.** `Infrastructure/StackCheck.cs:69` runs
`select count(*) from "__EFMigrationsHistory"` without a schema, relying on `search_path`,
while the two queries around it are explicitly qualified — and the reason they were qualified
is that an unqualified name resolved to the wrong table. Qualify it too, for the same reason.

**N5 — raw SQL not justified in the Work Log.** `RestartTests.cs:23` interpolates into an
`insert`. The value is a GUID the test generates, so there is no injection surface; SECURITY.md
nonetheless asks that raw SQL be justified in the ticket. One sentence covers it (psql over
`compose exec` is the only way to reach a database that publishes no port).

**N6 — `tools/smoke.sh:35`** uses `"${@:-}"`, which under `set -u` expands to *one empty-string
argument* when none were given (verified). `dotnet test` currently ignores it. `"$@"` is the
correct form.

**N7 — AC8's successor is named only in the Work Log.** The criterion line still reads
`- [ ] AC8: …` with no destination on it. Given this project has made the false-pointer
mistake three times, the successor belongs on the criterion itself, e.g.
"**Deferred to [T-0018](T-0018-user-subject-tokens.md)** (2026-08-31)". The deferral is
otherwise correctly made — see point 4 below.

**N8 — attribution proved for `api` only.** `AssertHealthAnswersFromThisStackAsync` is
applied to `api`; the identity host's token endpoint is trusted on port discovery alone.
Structurally that is sound (Docker owns the ephemeral port for the lifetime of the
container), so this is a note, not a finding — but TESTING.md's third bullet is stated for
"any verification against a running service", and AC6's accepted case is one.

**N9 — I endorse the implementer's own "For QA to probe".** `applied != "0"` cannot see a
*partial* migration. It is correctly out of scope here and correctly surfaced rather than
buried.

**N10 — commit lane.** `be97874` carries the new ticket files `T-0018`/`T-0019` and the
`BACKLOG.md` reordering alongside source. [GIT.md](../../standards/GIT.md) puts delivery
state (tickets, backlog) in lane 1 and allows only the ticket's **Work Log** to travel with
the code. `ARCHITECTURE.md` riding along has precedent (`ece515d`, `f7fe8a1`); new tickets
and backlog order do not. The resulting state is consistent and the validator is green, so
this is a process note, not a defect in the change.

---

### The six judgements I was asked for

1. **AC4 — verified, and the fix is right.** Reproduced above: the health assertions pass on
   the neutered stack, `/health` returns 200 on an empty database, and the *first* query in
   `AssertSchemaMigratedAsync` is what fires. The unqualified variant really would have
   returned 1 and passed. The self-reported survival is the most valuable thing in this Work
   Log. But the mutation *tests* that carry AC4 are the weakest code in the change — B1, B2,
   B3 all live there — and that is why the verdict is Request changes rather than Approve.

2. **Can it be fooled?** For AC1, AC2, AC3 and AC6: no, I could not fool them. Attribution is
   established before any HTTP response is trusted (ephemeral ports + `compose port` +
   container-state assertions + the stop-and-recheck), and no assertion I could find passes
   against a stack that never started. For **AC7 it can** (B4: a host that never ran creates
   no tables), and for **AC4 it can pass in the wrong direction** (B3: any failure reads as
   the mutation being caught).

3. **AC6's minted tokens — legitimate.** The control token settles it: minted-correct is
   accepted, so minting is not what produces the refusals and each 401 is attributable to
   the one variable named. The accepted case uses a genuine token, which is the other half.
   Given the measured five-minute grace (confirmed: −60 s → 200, −1 h → 401), waiting the
   window out would have cost more than the whole suite. This is the right call, and the
   reasoning is recorded where the next reader will find it.

4. **AC8's deferral — the destination genuinely accepts it.** I read
   [T-0018](T-0018-user-subject-tokens.md) rather than trusting the pointer. Its **In Scope**
   contains "Proving **T-0015 AC8** with such a token: a projection is created on first
   request and updated, not duplicated, on return", and its **AC2** carries the criterion
   verbatim and cites `T-0015 AC8, T-0009 AC5/AC8`. Nothing in its Out of Scope disowns it.
   This is a real destination, not a false pointer — and the decoded token is recorded as
   evidence rather than asserted. Only N7 (naming it on the criterion line) is missing.

5. **The layout decision — sound; the mitigation is adequate but unowned.** Keeping the
   project out of `GotIssues.slnx` is the right trade: it makes AC5 structural rather than a
   convention, and `.slnx` listing projects explicitly means it cannot drift back in by
   accident. `--build-only` plus README is exactly the mitigation refinement accepted, and
   the cost is stated rather than hidden. It is not a rot *risk* that blocks this ticket, but
   it is not guarded either, and T-0014's scope does not currently accept the guard — see N1.

6. **Scope fidelity — T-0019 was the required call, not merely a defensible one.** Setting
   `ClockSkew` here would have changed the resource server's token validation under a ticket
   whose In Scope is verification, and SECURITY.md requires Security-persona review of "any
   change touching token validation, scopes, or roles" during **both refinement and
   acceptance** — a fix in passing would have bypassed a mandatory review on a security
   control, inside a ticket nobody would think to look in. Raising it is not scope avoidance;
   fixing it would have been scope creep with a compliance edge. The same reasoning holds for
   T-0018. I found no scope drift anywhere in the diff: every file changed is named in the
   plan, and every criterion has code behind it.

**Summary:** the harness is well built, the attribution discipline is real, and the AC4
self-correction is exactly the behaviour the standard asks for. Fix B1–B4 on the branch and
re-request review; I expect a short second pass.

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — the four blocking findings, and what B1 says about this ticket

All four taken. `claude-rev-6d21` reproduced each one rather than reasoning about it, and B1
in particular is a defect I would have shipped.

#### B1 — the mutation stacks leaked on every run

`await using` disposes *after* the enclosing `finally`, so the override file was deleted
before teardown, and `docker compose --file <deleted> down` failed. It was invisible because
`DisposeAsync` was **the one place in the harness that discarded a `CommandResult`** — the
single spot not covered by the rule the rest of the file was written around.

That is the ticket's own subject turned on itself. This check exists because verification
against a running stack must be attributable and its tool exit codes actually read; the
harness enforcing that rule broke it in its own teardown. Both fixed: the override file now
outlives the stack, and `DisposeAsync` reads the result and fails loudly.

Verified the reviewer's way, not by reasoning: Docker emptied of `gs-*` projects, full run,
then `docker compose ls --all` and `docker volume ls` — **nothing left behind**, twice.

#### B2 — the "unique" project names were not unique

`[..40]` truncated `gs-health-condition-dropped-<guid>` to exactly the prefix, dropping the
GUID entirely, so every run of that mutation shared one project, its containers and its
volumes. The reviewer found a single project carrying two different override config files:
it had already happened.

My first fix capped at 48 instead of 40, which keeps 20 hex characters for the longest label.
That is the same bug with a larger number, failing the same silent way — a cap chosen to be
"big enough" is exactly what 40 was. **No cap now.**

#### B3 — AC4 could be satisfied by anything going wrong

The tests asserted only `failure is not null`, with `BuildAsync` inside the `try`. A failed
image build, or a leftover stack torn down underneath (B1 + B2 together), would have *proved*
the check works. I applied precisely this standard to AC6 — each refusal attributable to one
named defect — and did not apply it to AC4, the criterion the other seven rest on.

Now: building is setup and sits outside the `try`; only `XunitException` counts as "the check
failed"; a harness fault propagates.

| Mutant | Result |
| --- | --- |
| Override made a no-op, so the check passes | **Killed** — AC4 fails, as it must when nothing is broken |
| `docker compose build` given an invalid flag | **Killed** — `docker compose build exited 1`; previously this would have *passed* both AC4 tests |

#### B4 — AC7 proved nothing about whether the host ran

A container that crashed on startup also creates no tables, and would have satisfied the
count. Now asserts the identity host is **running** first — deliberately not *healthy*, since
a host pointed at an unprepared schema is expected to be unhealthy, and requiring health here
would assert the wrong thing.

#### Non-blocking notes taken

Attribution now proved for the identity host as well as the API (N8) — AC6 trusts what that
container says as much as AC1 trusts the API, so it needs the same proof. The one
schema-unqualified query among qualified neighbours (N4). Credential-shaped literals renamed
to `not-a-secret-throwaway-stack` (N3). Raw SQL justified in place: postgres publishes no host
port, and a black-box check should not link against the application's data layer (N5).
`"${@:-}"` passed an empty argument to `dotnet test` (N6). T-0018 named on the AC8 line itself,
not only in the Work Log (N7).

**N10, the lane violation, is fixed properly rather than waved through.** T-0018, T-0019 and
`BACKLOG.md` were process-lane files sitting in a source-lane commit; GIT.md permits only the
Work Log to travel with code. They are now on the trunk (`1354c52`), removed from this branch.

**N1 has a destination.** The rot risk — nothing compiles this project by accident — is real
and the mitigation is a habit, not a gate. The reviewer checked T-0014 rather than assuming,
and its scope did not accept it; a scope line was added there (same trunk commit) so the
residual points somewhere that takes it, per DoD item 4.

#### What I would take to the retrospective

Every blocking finding here was **the harness failing the standard it exists to enforce**:
a discarded exit code, an unattributable name, and an assertion satisfied by any failure. I
wrote all three while quoting TESTING.md in the comments beside them. The rule seems to be
that applying a standard to the system under test is easy, and applying it to the thing doing
the testing is what gets skipped — because the test code feels like the place the rules come
from rather than a place they apply to.

- **Did:** Fixed B1–B4, took every non-blocking note, moved the process-lane files to the trunk.
- **Decided:** no cap on project names at all — a bigger cap is the same defect.
- **Remaining:** re-review.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0015-compose-smoke-test`.
- **Test state:** `tools/smoke.sh` **12/12**, exit 0, 3m15s, **no leaked projects or volumes**.
  Root `dotnet test` **63/63**; build 0 warnings; `dotnet format` exit 0 (solution and smoke
  project); `check-drift.sh` exit 0; `validate.py` exit 0.

### 2026-08-31 — Software Engineer + Architect (claude-rev-6d21) — re-review of `13658f2`

**Verdict: REQUEST CHANGES** — but only two items remain, one of them a `git rebase`.
B1–B4 are genuinely fixed and I reproduced the fixes rather than reading them. The new
blocking item (B5) is one I could not have found before this pass, because the branch did not
have it before this pass.

#### Gates, re-run on `13658f2`, each exit code read from the tool

| Command | Exit | Result |
| --- | --- | --- |
| `./tools/smoke.sh` | **0** | **12** passed, 3 m 16 s |
| `dotnet test` (root) | **0** | 17 + 46 = **63** — AC5 still holds |
| `dotnet build --no-incremental` | **0** | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | **0** | solution |
| `dotnet format apps/…SmokeTests.csproj --verify-no-changes` | **0** | smoke project |
| `./tools/check-drift.sh` | **0** | no drift |
| `python3 tools/validate-project-os/validate.py` | **1** | **10 findings — see B5** |

#### B1 — fixed, and I reproduced the proof rather than accepting it

`await using var stack` now lives *inside* the `try`, so disposal runs before the `finally`
deletes the override file, and `DisposeAsync` calls `EnsureSucceeded` on the `down`. I
emptied Docker of every `gs-*`/`gotissues-*` project and volume, ran `./tools/smoke.sh` to a
green 12/12, and checked `docker compose ls --all` and `docker volume ls` immediately after:
**nothing left behind**. On the previous commit the same procedure left two projects and four
volumes. The finding is closed on evidence.

The Work Log's framing is right and worth keeping: `DisposeAsync` was the one place in the
harness that discarded a `CommandResult`, in a check whose subject is that exit codes get
read.

#### B2 — fixed, and fixed the right way

`ComposeProject.UniqueName` applies **no cap at all**. Rejecting the 48-character version is
the correct call and the reasoning is exactly right: a cap chosen to be big enough is the
same defect with a larger number, failing the same silent way. I watched the names in flight
during my run — `gs-running-b6efe677b8c647238506d1734e1824ec`,
`gs-restart-4356220220104a709ace622d8612bd1e` — full 32 hex characters, every call site
converted.

#### B3 — fixed

`BuildAsync` is outside the `try`; only `XunitException` is caught. A missing Docker CLI, a
failed build, or a stack torn down underneath now fails AC4 instead of satisfying it. The two
mutants chosen are the right two — "nothing is broken, so the check passes" and "the harness
itself fails" are precisely the two ways this test could have lied.

#### B4 — the substantive half is fixed; the log assertion should go

`Assert.True(identity.IsRunning, …)` is the fix, and declining to require *healthy* is the
correct judgement: a host against an unprepared schema is expected to be unhealthy, and
asserting health would assert the wrong thing.

**On the log assertion you asked me to judge: it is worse than none, and I would not take the
brittle version either.** `docker compose logs identity` prefixes *every* line with the
container name. I ran the AC7 scenario by hand and the raw output begins:

```
identity-1  | info: Duende.IdentityServer.Startup[0]
identity-1  |       Starting Duende IdentityServer version 8.0.6+build…
```

So `logs.Combined.Contains("identity")` is satisfied by the service name **the test itself
chose**, on any line at all. It reduces to "the container emitted ≥ 1 line", which
`IsRunning` above it already implies — while its failure message claims "no evidence it
reached startup". An assertion that reads as evidence of startup and measures the presence of
its own argument is the SPRINT-001 family in miniature, and it is in the criterion whose job
is to be believable.

**There is a third option, neither weak nor brittle.** In
[`apps/GotIssues.IdentityHost/Program.cs`](../../../apps/GotIssues.IdentityHost/Program.cs)
the `--migrate` branch `return`s **before** `MapHealthChecks`, `UseIdentityServer` and
`RunAsync`. Therefore *any HTTP response at all* from that container proves execution passed
the point where a migrate-on-startup host would have migrated — which is exactly the claim
you say you want, with no log text and no EF error string in it. I verified it in the AC7
scenario: identity published on an ephemeral port, `GET /health` → **503 `Unhealthy`**, and
zero tables. 503 is the *expected* answer here, so the assertion must accept **any** status
code rather than a particular one — `AssertHealthAnswersFromThisStackAsync` cannot be reused
as-is because it requires 200.

Deleting the log assertion outright also closes this finding. I am asking for "not that
assertion", not for a particular replacement.

#### B5 — NEW BLOCKING: the branch fails a mandatory merge gate

`python3 tools/validate-project-os/validate.py` **exits 1** on this branch with 10 findings:

```
✗ BACKLOG.md: Active row T-0018 has no ticket file
✗ BACKLOG.md: Active row T-0019 has no ticket file
✗ project-os/product/BACKLOG.md: broken link -> tickets/T-0018-user-subject-tokens.md
✗ project-os/product/BACKLOG.md: broken link -> tickets/T-0019-token-clock-skew.md
✗ project-os/product/tickets/T-0015-…: broken link -> T-0018-user-subject-tokens.md   (×4)
✗ project-os/product/tickets/T-0015-…: broken link -> T-0019-token-clock-skew.md      (×2)
```

The N10 fix removed the two ticket **files** from the branch but the branch still carries the
`BACKLOG.md` rows that point at them, and the ticket's own AC8 line and Work Log link to them
too. `git rev-list --left-right --count main...HEAD` is `1 3` — the branch is one commit
behind `1354c52`, the trunk commit that holds those files.

[GIT.md](../../standards/GIT.md) makes the validator a pre-merge gate and says a red validator
is a defect in the process state, fixed before proceeding; it also says to keep ticket
branches current by rebasing on `main`. **The fix is `git rebase main`** — the branch's
`BACKLOG.md` is byte-identical to the trunk's, so nothing conflicts, and every link resolves
once the two ticket files are present in the tree.

The N10 remedy itself was right; it just landed half-applied. Worth noting plainly, since it
is the same shape as everything else in this review: the Work Log records `validate.py`
**exit 0**, which cannot have been measured on this branch. It was measured next door, on a
checkout where the files exist — a green signal read from the wrong working tree, one
directory over from the check that exists to stop exactly that.

#### Non-blocking notes from this pass

**N11 — a throwing `DisposeAsync` can now mask a real failure.** Making disposal assert is
correct, but if a test body throws *and* `down` also fails, the disposal exception replaces
the original and the genuine assertion failure is lost. AC4's path returns rather than
throws, so it is unaffected; AC2, AC3 and AC7 are exposed. Not worth restructuring for —
recording it so the first person to see a confusing teardown failure knows why.

**N12 — the identity attribution test restarts a container the API depends on.** It is safe
today (the signing key is persisted on the `identity-keys` volume, ports are re-discovered
per call, and `WaitForHealthyAsync` gates the return), and my run was green with it in place.
Noting the coupling because a future change to key persistence would make it flaky rather
than red.

#### Everything else from the first pass

N1, N3, N4, N5, N6, N7, N8 and N10 all taken. N8's identity attribution is a real
strengthening rather than a box tick — AC6 trusts what that container says, so it needed the
same proof AC1 gets, and the reasoning in the test says so. N7 now puts T-0018 on the AC8 line
itself. N1's destination was added to T-0014's scope on the trunk, which is the DoD item 4
discipline applied correctly.

**What remains: rebase on `main` (B5), and remove or replace the log assertion (B4).** Neither
touches the harness design. Re-request review after and I expect to approve.

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — B5, and the assertion that measured its own argument

#### B5 — I recorded a gate result I had not measured here

The Work Log said `validate.py` **exit 0**. It was exit **1** on this branch, with ten
findings, and the reviewer is right about how that happened: I ran it in the primary
checkout, one directory over. Two lanes, two working copies, and I read the gate from the
wrong one — the precise failure TESTING.md's attribution rule exists to prevent, committed
in the ticket that builds attribution into a test harness. Every gate in this entry was run
in this worktree.

The fix was not the rebase, either. Rebasing replayed my own commit's *deletion* of T-0018
and T-0019 on top of the trunk that now contains them, so the branch still removed them and
the validator still failed. The branch now restores both files from `main`, and
`git diff main -- <those files> BACKLOG.md` is **empty**: this branch touches no process-lane
file at all, which is what the lane rule actually asks for.

#### AC7's log assertion — dropped, and the reviewer's third option taken

The assertion required the identity container's logs to contain `"identity"`. But
`docker compose logs` prefixes every line with the service name — a name **this test chose** —
so it was satisfied by any line whatsoever, reduced to "the container emitted output" (which
`IsRunning` already implies), and carried a message claiming evidence of startup. An
assertion that reads as evidence while measuring the presence of its own argument is this
project's recurring defect in miniature, and I wrote it *while fixing* three others of the
same family.

The replacement is better than either option I offered. In the identity host's `Program.cs`
the `--migrate` branch `return`s **before** the host maps health checks or serves anything —
so **any HTTP response at all** proves execution reached the point where a migrate-on-startup
host would have migrated. That is the exact claim, with no log text and no brittle EF error
string. The status is deliberately not asserted: 503 is the correct answer from a host
pointed at a schema nobody prepared, and requiring 200 would assert the wrong thing.

| Mutant | Old log assertion | New HTTP assertion |
| --- | --- | --- |
| `sh -c "sleep 300"` — runs, never serves | **FAIL** — 0 bytes of log output | **Killed** — never answered `/health` |
| `sh -c "echo 'Starting Duende IdentityServer'; sleep 300"` — runs, logs plausibly, never serves | **PASS** — 45 bytes containing `identity` | **Killed** — no HTTP response |

**Corrected after review, and the correction matters more than the fix did.** My first table
claimed the `sleep 300` mutant showed the log assertion "could not have detected this",
adding that a sleeping container still emits its startup line. It does not: with that
entrypoint the app never runs and the container emits **nothing**, so the old assertion would
have failed too. That mutant kills the new assertion — which is what it was run for — but it
says nothing about the new one being *stronger*, which is what I claimed it said.

The second row is the mutant that actually carries that claim, and `claude-rev-6d21` ran both
rather than accepting the table. Under TESTING.md the mutation record *is* the evidence, so a
table overstating what a mutant proves is precisely the failure this ticket keeps producing —
this time in the document written to prove I had stopped producing it.

#### N11 and N12, recorded rather than fixed

**N11:** a throwing `DisposeAsync` can mask a real failure — if a test body throws *and*
teardown fails, the disposal exception replaces the original. That is a genuine trade: the
alternative is the silent leak B1 was. AC4 returns rather than throws, so it is unaffected;
AC2/AC3/AC7 are exposed. Left as is, recorded so the first confusing teardown failure is
legible to whoever meets it.

**N12:** the identity attribution test restarts a container the API depends on. Safe today —
the signing key persists in a volume, ports are re-discovered per call, and
`WaitForHealthyAsync` gates the return — but a change to key persistence would make it flaky
rather than red, which is the worse failure mode.

- **Did:** Replaced AC7's log assertion and mutation-proved the replacement; restored the
  process-lane files so the branch no longer deletes them; re-ran every gate in this worktree.
- **Decided:** record N11's trade rather than restructure disposal — the silent leak is the
  worse failure.
- **Remaining:** re-review.
- **Open questions / blockers:** none.
- **Branch / PR:** `t-0015-compose-smoke-test`, rebased onto `main`.
- **Test state, all measured in this worktree:** `tools/smoke.sh` **12/12** exit 0, 3m17s,
  no leaked projects or volumes · root `dotnet test` **63/63** exit 0 · build **0 warnings** ·
  `dotnet format` exit 0 (solution) and exit 0 (smoke project) · `check-drift.sh` exit 0 ·
  `validate.py` **exit 0** (19 tickets, 6 ADRs).

### 2026-08-31 — Software Engineer + Architect (claude-rev-6d21) — third pass, `d45da1f`

**Verdict: APPROVE.** Every blocking finding from both previous passes is closed, and I
verified each closure by reproduction rather than by reading the diff. One correction to the
Work Log's mutation record is recorded below with the evidence — it is a documentation fix,
not a code defect, and needs no further review pass.

#### Gates, all run in this worktree (`/Users/yoss/work/got-issues--t-0015`), exit codes read from the tool

| Command | Exit | Result |
| --- | --- | --- |
| `./tools/smoke.sh` | **0** | 12 passed, 3 m 16 s |
| `dotnet test` (root) | **0** | 17 + 46 = **63** — AC5 holds |
| `dotnet build --no-incremental` | **0** | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | **0** | solution |
| `dotnet format apps/…SmokeTests.csproj --verify-no-changes` | **0** | smoke project |
| `./tools/check-drift.sh` | **0** | no drift |
| `python3 tools/validate-project-os/validate.py` | **0** | 19 tickets, 6 ADRs |

Docker was empty of `gs-*`/`gotissues-*` projects and volumes before the run and empty again
after it — checked both times, not assumed.

#### B5 — closed, and the diagnosis in the response is the right one

`git rev-list --left-right --count main...HEAD` is now `0 5`, and
`git diff --name-only main -- project-os/` returns exactly two files: `ARCHITECTURE.md` and
this ticket. Nothing process-lane, so the branch neither adds nor deletes trunk state.

**The distinction drawn in the response is worth keeping past this ticket:** "move the files
to the trunk" and "stop the branch deleting them" are different fixes, and a rebase performs
the first while faithfully replaying the second. A branch can be current with `main` and still
remove what `main` holds. `git diff main -- <path>` is the check that catches it; the
validator is what makes it visible.

The self-report on the gate is the part I would keep for the retrospective: the result was
measured in the primary checkout and recorded as though measured here. That is TESTING.md's
attribution rule failing across working copies rather than across containers — the same
defect one abstraction level up, in the ticket that builds attribution into a harness.

#### B4 — closed, and the replacement is better than what I proposed

`StackCheck.WaitForAnyResponseAsync` is status-agnostic, polls to a deadline, and reports the
last transport error on failure. Called after `IsRunning` and before the table count, it says
exactly what AC7 needs: the host reached the point of serving, and still created nothing. The
comment records why the old assertion was wrong, which is worth as much as the fix.

#### One correction to the mutation record

The mutation table says of `entrypoint: sleep 300`: *"The log assertion could not have
detected this: a sleeping container still emits its startup line."* **That is not so, and I
checked rather than assumed.** With that entrypoint the application never runs, so it emits
nothing at all; `docker compose logs identity` returned **0 bytes**, and the old assertion
would have **failed** on this mutant too. The service-name prefix appears only on lines the
container actually produces.

The mutant is still valid — it kills the new assertion, which is what it was run to show. It
just does not demonstrate the new assertion is *stronger* than the old one. The mutant that
does is one line different, and I ran it:

| Mutant | Old log assertion | New HTTP assertion |
| --- | --- | --- |
| `entrypoint: ["/bin/sh","-c","sleep 300"]` | **FAIL** (logs are 0 bytes) | **FAIL** — `never answered /health` |
| `entrypoint: ["/bin/sh","-c","echo 'Starting Duende IdentityServer'; sleep 300"]` | **PASS** (45 bytes, contains `identity`) | **FAIL** — no HTTP response at all |

The second row is the evidence that the replacement is strictly stronger: a container that
starts, logs plausibly, and never serves satisfies the old assertion and is caught by the new
one. Recording it here so the claim in the table is not the version that survives — under
TESTING.md the mutation record *is* the evidence, so a mutation table that overstates what a
mutant proves is the same failure mode as the assertion it was written about. Correcting the
sentence is a Work Log edit; it does not need another review pass.

#### The earlier findings, confirmed closed

- **B1** — clean-Docker → green run → `compose ls --all` and `volume ls` both empty. Twice now.
- **B2** — no cap; `gs-<label>-<32 hex>` at every call site.
- **B3** — build outside the `try`, only `XunitException` caught.
- **N1, N3–N8, N10** — all taken. **N11 and N12** correctly recorded rather than fixed;
  N11's trade is the right way round, since the alternative to a loud teardown is precisely
  the silent leak B1 was.

#### Where this leaves the ticket

AC1–AC7 each have a check behind them, and the two that underwrite the rest — AC4's mutations
and AC7's startup guard — now hold to the same attribution standard the harness applies to
everything else. AC8 is deferred to [T-0018](T-0018-user-subject-tokens.md), whose scope and
AC2 genuinely accept it (verified in the first pass by reading T-0018, not the pointer). The
recorded DoD deviations on [T-0001](T-0001-runnable-compose-stack.md) and
[T-0010](T-0010-duende-identity-host.md) are discharged by criteria I could break on demand
and watched fail.

Three passes, and every blocking finding in all three was the harness failing the standard it
exists to enforce — a discarded exit code, a name that could collide, an assertion satisfied
by anything going wrong, another satisfied by its own argument, and a gate read from the
wrong working copy. That pattern is worth taking to the retrospective more than any individual
fix.

**Approved for merge.** Merge, then the `os:` status commit on the trunk per
[GIT.md](../../standards/GIT.md)'s handover sequence.

### 2026-08-31 — QA / Test Engineer (claude-qa-9b3e) — independent acceptance

**Verdict: PASS.** AC1–AC7 each verified against the merged code at `7bda6bc` on `main` in the
primary checkout; AC8 correctly deferred to a destination that genuinely accepts it. Seven
findings recorded below, none blocking — F1 and F2 are verified coverage gaps in the schema
assertion (F1 is the implementer's own "For QA to probe", now reproduced and extended), F3 is
a partial application of the review's B3, and F4–F7 are smaller. One item needs a decision at
`complete-ticket`, named at the end.

I did not implement this ticket (`implemented_by: claude-sm-9d4e`) and I did not review it
(`claude-rev-6d21`). I derived my checks from the Problem/Scope/AC sections before reading the
Work Log, and I re-ran or reconstructed every claim I report rather than citing the entry above.

#### Gates, every exit code read from the tool itself — no pipeline, no grep

| Command | Exit | Result |
| --- | --- | --- |
| `./tools/smoke.sh` | **0** | 12 passed, 0 failed, **3 m 23 s** |
| `dotnet test` (root) | **0** | 17 + 46 = **63**, **14.7 s** wall — AC5 holds |
| `dotnet build --no-incremental` | **0** | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` | **0** | solution |
| `dotnet format apps/GotIssues.SmokeTests/GotIssues.SmokeTests.csproj --verify-no-changes` | **0** | the project outside the solution |
| `./tools/check-drift.sh` | **0** | generated code matches `spec/openapi.yaml` |
| `python3 tools/validate-project-os/validate.py` | **0** | 19 tickets, 6 ADRs |
| `./tools/smoke.sh --build-only` | **0** | the rot guard works |
| `./tools/smoke.sh --list-tests` | **0** | 12 tests listed — N6's `"${@+"$@"}"` genuinely forwards arguments |
| `dotnet list … package --vulnerable --include-transitive` | **0** | no vulnerable packages |

**No leaks — measured, not assumed.** `docker compose ls --all` and `docker volume ls` captured
before the smoke run and again after it are **byte-identical**: the same two foreign projects
(`docker`, `pool-care`) and the same 28 volumes, no `gs-*` project and no `postgres-data` /
`identity-keys` volume left behind. This machine was running six other containers including
four PostgreSQL instances throughout, which is the collision condition the attribution rule
exists for. I repeated the check after my own eight hand-built stacks: Docker is back to its
starting state exactly.

#### Criterion by criterion — what I did, not what I read

**AC1 — PASS.** `AC1_a_cold_start_on_an_empty_volume_brings_every_service_up_healthy` green in
my run. Verified non-vacuous by standing the stack up myself: `up --wait` exit 0 with
`postgres`/`identity`/`api` `running`+`healthy` and `migrator`/`identity-migrator` `exited 0`.
The assertion reads `compose ps --format json` rather than inferring health from `up --wait`,
which F3 below shows still matters.

**AC2 — PASS, verified by hand end to end.** Inserted `qa9b3e-50b2d8b0698e40a081e433ac44767627`
into `users`, `docker compose down` **keeping volumes**, `up -d --wait`:

- the row survived (`select count(*) … where "Subject" = …` → **1**);
- `public."__EFMigrationsHistory"` → **4** before and **4** after;
- `migrator` `exited 0`.

The migrator's own log on that restart reads `No migrations were applied. The database is
already up to date.` **immediately followed by** `Migrations applied.` — which independently
confirms `RestartTests.cs:44-46`: the log genuinely cannot distinguish the two cases, so
asserting from the history count was the correct call and not a stylistic preference.

**AC3 — PASS, verified by hand, both halves.** Started `api` with `--no-deps` and **no database
at all**: after 25 s the container is `running` (not exited, not crash-looping), health
`starting`, `GET /health` → **503** `{"status":"Unhealthy","checks":{"database":{"status":"Unhealthy","description":"database not reachable"}}}`.
Then brought the database up: `GET /health` → **200**, and — the part the test does not assert
but which is the actual claim — **the same container id** (`35ed4f9ba6a3` before and after) with
`RestartCount 0`. It waited; it was not restarted into health. T-0001 AC7 holds.

**AC4 — PASS, and I reproduced the whole chain rather than trusting the table.**

*Mutant 1, migration step neutered (`entrypoint: ["/bin/sh","-c","exit 0"]`), my own project:*

| Observation | Value |
| --- | --- |
| `docker compose up --wait` | **exit 0** |
| `compose ps` | api/identity/postgres `running`+`healthy`, both one-shots `exited 0` |
| `GET /health` on the discovered ephemeral port | **200** `{"status":"Healthy","checks":{"database":{"status":"Healthy","description":"database reachable"}}}` |
| `… table_schema='public' and table_name='__EFMigrationsHistory'` | **0** ← the first assertion in `AssertSchemaMigratedAsync` is what fires |
| the **unqualified** variant the implementer rejected | **1** ← it really would have passed here |
| `… table_name='users'` | **0** |

So `AssertStackHealthyAsync` genuinely cannot catch this mutant, `/health` genuinely reports
Healthy against a database with no tables, and the kill is attributable to the *right*
assertion. The self-reported survival in the Work Log is accurate and the fix is right rather
than merely sufficient.

*Mutant 2, `api` healthcheck disabled:* `up --wait` **exit 0** — confirmed, `--wait` has nothing
to wait for — and `compose ps` reports `api` with `Health: ''`, so only the explicit state
assertion catches it. The claim at `BrokenStackTests.cs:52-55` is exact.

*Two further mutations of my own, neither shipped:* removing `api.depends_on` entirely
(`!reset null`) and weakening it to `condition: service_started` both make `up --wait` **exit 1**
(`container … migrator-1 exited (0)`), so the check fails on them too. Ordering mutations are
killed, though by `--wait` semantics rather than by an assertion that names ordering.

**AC5 — PASS.** Root `dotnet test` is **63 tests in 14.7 s**, and the only assemblies it loads
are `GotIssues.Api.UnitTests.dll` and `GotIssues.Api.IntegrationTests.dll`. `GotIssues.slnx`
lists projects explicitly (no globbing); there is no `Directory.Build.props`/`.targets` at the
root, and the only reference to `GotIssues.SmokeTests` anywhere outside its own directory is
`tools/smoke.sh:21`. AC5's mechanism is structural, not a convention someone must remember.
The README section (`README.md:75-88`) documents both `tools/smoke.sh` and `--build-only`, and
both work.

**AC6 — PASS, verified with a token minter I wrote myself.** I did not reuse `TokenFactory`.
I read the identity host's JWK out of the running container, implemented RS256 in pure Python
(PKCS#1 v1.5 + SHA-256, `hashlib` only), generated an **independent 2048-bit RSA keypair**, and
called `/health/authenticated` on the discovered ephemeral port:

| Token | Result |
| --- | --- |
| genuine, from `/connect/token` | **200** |
| **control:** minted, real key, `aud=gotissues-api`, `exp` +30 m | **200** |
| minted, real key, `exp` −1 h | **401** |
| minted, real key, `exp` **−60 s** | **200** |
| minted, real key, `aud=some-other-api` | **401** |
| minted, real key, `iss=http://evil:9999` | **401** |
| minted, **my own independent RSA key** | **401** |
| genuine token with its last signature character altered | **401** |
| garbage (`not.a.token`) | **401** |
| no token | **401** |

The **control row is the whole argument** and it holds: a token that differs from a genuine one
only in having been minted by me is **accepted**, so minting is not what causes any refusal and
each 401 is attributable to the single named defect. The **−60 s row** reproduces the
`ClockSkew` grace first-hand: a token a minute past `exp` is still accepted, so the hour-long
margin in `TokenFactory.ExpiredTokenAsync` is necessary rather than decorative, and an
implementer following the risk note without the numbers would indeed have got a 200.
[T-0019](T-0019-token-clock-skew.md)'s premise is confirmed for the third time, by a third party.

**AC7 — PASS, verified by hand.** `postgres` up alone: **0** non-system tables. `identity`
started `--no-deps` against that empty schema: after 30 s the container is **running**, it
**serves HTTP** (`GET /health` → **503** `Unhealthy`, the correct answer against a schema nobody
prepared), and there are still **0** non-system tables — the `identity` schema is not even
created (`information_schema.schemata` → 0). The assertion is falsifiable: on a migrated stack
the same query returns **42**. The reviewer's third option is correctly implemented — I confirmed
in [`Program.cs`](../../../apps/GotIssues.IdentityHost/Program.cs) that the `--migrate` branch
`return`s at line 58, *before* `MapHealthChecks` (line 62), so any HTTP response does prove
execution passed the point a migrate-on-startup host would have migrated. T-0001 AC5's analogue
holds for this host.

**AC8 — correctly deferred, and the destination accepts it.** I decoded a genuine member token
from my own running stack rather than citing the one in the Work Log:

```json
{"aud":"gotissues-api","client_id":"smoke-member-client","exp":1788189300,"iat":1788185700,
 "iss":"http://localhost:8081","jti":"9E3AC95CDA8EED51100AC9E9E5A81166","nbf":1788185700,
 "role":"member","scope":["gotissues.api"]}
```

No `sub`. The deferral's premise is fact, not assertion. I then read
[T-0018](T-0018-user-subject-tokens.md) itself, not the pointer to it: its **In Scope** contains
*"Proving **T-0015 AC8** with such a token: a projection is created on first request and updated,
not duplicated, on return"*; its **AC2** carries the criterion in the same words and cites
`T-0015 AC8, T-0009 AC5/AC8`; its **Testing Notes** put the proof in this ticket's smoke tier;
nothing in its **Out of Scope** disowns it (it excludes a login UI, real employee data, and
provisioning policy — none of which is the criterion). It is registered in `BACKLOG.md` at
position 7 with a ticket file that exists. **This is a real destination, not a false pointer.**

**Attribution (not a criterion, but what makes the rest mean anything) — PASS.** Both HTTP
services are proved by stopping the container and requiring `/health` to stop answering. I
confirmed the mechanics hold up: `docker compose stop api` then `start api` **re-publishes on a
new ephemeral port** (55554 → 55556), and every call site re-discovers the port via
`BaseAddressAsync`, so the restart in the attribution test cannot poison a later test in the
same fixture. `docker compose port` on a stopped service exits **1**, and the address in that
test is captured before the stop, so the sequence is sound.

---

### Findings

**F1 (non-blocking, verified) — the check reports a healthy migrated stack against a schema
that is missing an application table.** This is the implementer's own "For QA to probe",
reproduced and then extended past what was disclosed.

`StackCheck.AssertSchemaMigratedAsync` (`Infrastructure/StackCheck.cs:56-80`) asserts three
things: `public."__EFMigrationsHistory"` exists, its row count `!= "0"`, and `public.users`
exists. It never reconciles the applied migrations against the migrations on disk, and it names
only one of the API's two application tables.

*Reproduction A — partial migration (the disclosed case).* On a healthy stack I rolled back
`20260831072035_WidenUserSubject` alone: `alter table users alter column "Subject" type
character varying(200)` and deleted its history row. Result — history rows **3** (code expects
4), `users."Subject"` is **varchar(200)** where the model says 255, and **all three assertions
pass**.

*Reproduction B — a missing application table (not disclosed).* On a fully migrated stack I ran
`drop table public.placeholder_records cascade` — the only product table the API has today,
created by `20260831001215_InitialSchema`. Result: every service `healthy`, `GET /health` →
**200**, and **all three assertions pass**. The check reports a green, migrated stack against a
schema the API cannot serve from.

*Why it is a finding and not a failure:* AC4 requires that *a* deliberately broken stack makes
the check fail, proven by mutation. Two mutants do exactly that and I killed both myself. AC1 is
about service health, and the schema assertion is an addition beyond it. So no criterion is
violated. But the ticket's own AC4 risk says this criterion is what makes the other six
trustworthy, and B is a wider hole than the note in the Work Log describes. **Recommendation:**
assert the applied migration ids against the migrations in
`apps/GotIssues.Api/Data/Migrations`, which closes both reproductions with one query — as a
scope line on a follow-up, not in this ticket.

**F2 (non-blocking) — the service lists are fixed, so "every service" is a list someone must
remember to update.** `StackCheck.cs:13-16` hard-codes `LongRunningServices` and
`OneShotServices`. AC1 says *"asserts every service reaches a healthy state"*. A service added
to `compose.yaml` tomorrow is asserted by `up --wait` (generically, for `running` or a declared
health condition) but not by the explicit `compose ps` assertion — and mutant 2 above is the
proof that `up --wait` alone is not enough. Low severity today (five services, all listed).
Deriving the lists from `compose config --services` would make it self-maintaining.

**F3 (non-blocking) — AC4's tests still do not pin *why* the check failed, which is half of the
review's B3.** `BrokenStackTests.cs:39-62` asserts only `failure is not null`.
`claude-rev-6d21`'s B3 asked for two things: stop counting harness faults as evidence, **and**
*"assert on its identity — the migration mutant must fail in `AssertSchemaMigratedAsync` …, the
healthcheck mutant in `AssertStackHealthyAsync`"*. The first was done (`BuildAsync` is outside
the `try` at `:86`, only `XunitException` is caught at `:95`); the second was not, and the
re-review recorded B3 as fixed.

This is not theoretical. `EnsureSucceeded` raises `XunitException`, and
`(await stack.UpAsync()).EnsureSucceeded("docker compose up --wait")` is **inside** the `try` at
`:90` — so a `docker compose up --wait` failure for *any* reason still counts as "the mutation
was caught". I hit exactly that outcome twice while probing: both of my ordering mutations
failed at `up --wait`, not at an assertion. Today the mutants do fail for the right reason —
I verified both by hand, above — so AC4 holds. The residual is that the test cannot tell the
difference, so it can rot silently, in the criterion whose whole job is to not do that. The fix
is the one the reviewer already wrote: assert on the message, both of which are distinctive
(`"migrations history table does not exist"`, `"reports health ''"`).

**F4 (non-blocking) — `HostPortAsync` accepts a port lookup whose output literally says
`invalid IP`.** `Infrastructure/ComposeStack.cs:176-190` calls `docker compose port`, requires
exit 0, then parses everything after the last `:`. I hit a real state where
`docker compose port identity 8080` printed **`invalid IP:0`** and exited **0** — after a
`docker compose restart identity`, while the container was temporarily running with no published
port (`docker ps` showed bare `8080/tcp`). `EnsureSucceeded` passes, `int.TryParse("0")` passes,
and the method returns port **0**, so `BaseAddressAsync` yields `http://localhost:0`.

I traced every consumer and **none turns this into a false pass**: `AssertHealthAnswersFromThisStackAsync`
demands 200 from that address first, `WaitForAnyResponseAsync` ends in `Assert.Fail`, and
`TokenFactory` asserts on the response. It is a legibility defect — the failure surfaces as a
connection error against `localhost:0` instead of "Docker reported no published port". Rejecting
a parsed port of `0`, or requiring the output to start with an address, is one line.

**F5 (non-blocking, observation) — the identity host reports `Healthy` with zero clients.**
`ConfigurationStoreHealthCheck` uses `Clients…FirstOrDefaultAsync()`, which returns `null`
without throwing, so an empty-but-present configuration store is `Healthy`. The check would pass
`AssertStackHealthyAsync` on a host that can issue no tokens — the shape T-0010's review found.
It is **caught by AC6's accepted-token test**, so the suite is not fooled, and I could not
actually reach the state by hand: deleting all clients and restarting `identity` re-runs
`identity-migrator` through the declared dependency and re-seeds them, which is a good property.
Recording it because it is the one place `AssertStackHealthyAsync` alone would be fooled.

**F6 (non-blocking) — no criterion covers signing-key persistence across a restart.** T-0010's
AC9 was verified by hand with a token minted *before* a restart still returning 200 after it.
AC2 covers data and migrations, not the `identity-keys` volume, and nothing in this tier
re-presents a pre-restart token. Outside this ticket's ACs, so not a defect against it — but it
is the one T-0010 stack property that remains hand-verified, and N12 already notes the harness
depends on that volume.

**F7 (non-blocking, note) — the mutation record in the Work Log is now accurate, and I checked
the corrected claim rather than the correction.** The `sleep 300` row's original claim (that the
old log assertion "could not have detected this") was wrong and is corrected in place, with the
`echo … ; sleep 300` mutant carrying the "strictly stronger" argument instead. I re-read
`docker compose logs` behaviour and confirm the reasoning: the service-name prefix appears only
on lines the container actually emits, so a silent container yields 0 bytes. No further action —
noting that the correction survived into the merged text, which is where it needed to be.

---

### Definition of Done

| Item | Verdict |
| --- | --- |
| 1 Implementation complete | **Pass.** Every In Scope bullet is built: a check driving the real `compose.yaml` (not a copy — `RepositoryRoot.ComposeFile`, with mutations layered as overrides), a deliberate decision on where it lives and how it is invoked, and documentation. Out of Scope is untouched, diff-checked: `git show --name-only 7bda6bc` touches **no** file under `apps/GotIssues.Api`, `apps/GotIssues.IdentityHost`, `libs/`, `spec/` or `compose.yaml`, adds no CI, and removes nothing from T-0003's tier. The T-0009 scope bullet was explicitly conditional on user tokens existing; they do not, and it is deferred rather than smuggled |
| 2 All AC verified independently | **Pass.** AC1–AC7 verified above by a session that neither implemented nor reviewed this ticket, each against the running stack or an executed test; AC8 deferred with a destination I read myself |
| 3 Automated tests exist and pass | **Pass.** 12 smoke checks + 63 root tests, all green, run by me. Every criterion maps to at least one test (AC1→1, AC2→1, AC3→1, AC4→2, AC6→4, AC7→1, plus 2 attribution tests; AC5 is structural absence and is verified as such). Coverage claims are mutation-proven per TESTING.md, and I killed the two AC4 mutants myself. **This is the item that discharges T-0001's and T-0010's deviations — see below** |
| 4 No known unrecorded defects | **Pass, with one decision owed** — see the paragraph after this table. Existing residuals all have destinations that accept them, each verified by reading the destination: AC8 → T-0018 (In Scope + AC2), the rot guard → T-0014 (scope line added 2026-08-31, quoted in that file), `ClockSkew` → T-0019 (AC1–AC3). N11 and N12 are recorded trade-offs, correctly not fixed |
| 5 Code quality | **Pass.** Reviewed over three passes by `claude-rev-6d21`, an independent session, ending in APPROVE. `dotnet format` exit 0 for both the solution and the out-of-solution project; build 0 warnings with `TreatWarningsAsErrors`; no TODO/FIXME/`Console.WriteLine`/`Skip=` anywhere in the new project; `bin`/`obj` ignored |
| 6 Documentation updated | **Pass.** `README.md` gains a "The stack check" section naming both commands, why the tier exists, and the ephemeral-port rationale; `ARCHITECTURE.md`'s state banner is updated by the ticket that falsified it, which is the thing that has repeatedly been left stale here. `TESTING.md`'s tier table does not yet name `tools/smoke.sh` — that is explicitly T-0014's scope and has a scope line there |
| 7 Work Log complete | **Pass**, and unusually so: three review rounds, the AC4 mutant that survived and corrected the check, the gate read from the wrong working copy, and the mutation table that overstated its own mutant are all recorded against the author. A stranger could reconstruct every decision |
| 8 State updated | For `complete-ticket` |

**Conditional items.** *Security* — applied as the acceptance-side review SECURITY.md requires
for anything touching token validation: no production authentication code is changed (diff-verified);
no key material is tracked (`git ls-files` finds no `.jwk`/`.pem`/`.pfx`/`tempkey`); the signing
key is read out of a throwaway container at runtime and never written to the repository; the
credential-shaped constants are `not-a-secret-throwaway-stack` against a stack that lives three
minutes and dies with its volume; the one new package has no vulnerabilities and is already
transitive via `Microsoft.AspNetCore.Authentication.JwtBearer`. *Migrations* — none added; the
non-destructive restart is now asserted rather than remembered. *ADR* — none required: this adds
verification, not system structure, which refinement and the Architect-perspective review both
concluded. *Observability*, *Accessibility*, *Deployment* — not applicable.

**The decision item 4 owes `complete-ticket`.** F1 is a *verified* gap: I broke a stack twice in
ways the check reports green. It is a limitation of new coverage rather than a defect in the
product, and reproduction A was disclosed by the implementer before review — but reproduction B
is wider than the disclosure, and this project's rule is that a residual either gets a
destination whose scope accepts it or an explicit PO deviation. **Neither exists for F1 today.**
It should be one or the other before `done`; my recommendation is a scope line on a follow-up
ticket asserting the applied migration ids against the migrations on disk, which closes F1, F2
and F3's fragility together. F3 alone would also be a defensible one-line addition to this
ticket if the PO prefers to close it here.

#### The two deferrals this ticket exists to discharge

**[T-0001](T-0001-runnable-compose-stack.md) DoD item 3 — discharged.** Its deviation was
"ships with manual verification only", bounded by the promise that the gap would close.
Its AC1 (cold start on a clean clone, every service healthy), AC6 (restart against an existing
volume is non-destructive) and AC7 (a slow or absent database delays startup rather than
crashing it) are now AC1, AC2 and AC3 here — automated, run by me, and each one I broke on
purpose and watched fail. AC1's own health assertion I falsified by disabling a healthcheck;
AC2's data survival is a property I re-proved by hand with my own row; AC3 I proved by the
container id and `RestartCount 0` rather than by the health flag alone.

**[T-0010](T-0010-duende-identity-host.md) DoD item 3 — discharged.** Its deviation was that
"the headline behaviour of this ticket — a token issued by a real issuer being accepted, and the
three refusals that need one to construct — is not automated", naming T-0015 AC6/AC7 as owner.
All four are automated and green, and I re-verified every one of them with a minter and an RSA
keypair of my own, including the control that makes the refusals attributable. AC7 covers the
identity host's no-migrate-on-startup property, which T-0010's acceptance verified by hand and
recorded as unguarded; it is now guarded and I confirmed the guard can fail.

Both deferrals are discharged **by criteria I could break on demand and watched fail**, not by a
green run. F1 does not touch either: the partial-migration and missing-table gaps are in the
schema assertion added on top of AC1, and nothing in T-0001's or T-0010's deviation depended on it.

- **Did:** Verified AC1–AC7 independently at `7bda6bc`; ran all ten gates reading each exit code
  from its own tool; reproduced both shipped AC4 mutants by hand and confirmed each fails at the
  *correct* assertion; ran four mutations of my own; re-derived AC6 with a pure-Python RS256
  minter and an independent RSA keypair; decoded a genuine token to confirm AC8's premise; read
  T-0018 to confirm the deferral's destination; measured Docker before and after.
- **Decided:** PASS. Seven findings recorded, none blocking; F1 needs a destination or a
  recorded deviation at `complete-ticket`.
- **Remaining:** `complete-ticket` — settle F1, then `status: done`, `accepted_by: claude-qa-9b3e`,
  sprint and backlog updated.
- **Open questions / blockers:** none.
- **Test state:** `tools/smoke.sh` **12/12** exit 0, 3m23s, **no leaked projects or volumes**
  (Docker byte-identical before and after) · root `dotnet test` **63/63** exit 0 in 14.7 s ·
  `dotnet build --no-incremental` exit 0, 0 warnings · `dotnet format` exit 0 (solution) and
  exit 0 (smoke project) · `check-drift.sh` exit 0 · `validate.py` exit 0 (19 tickets, 6 ADRs) ·
  `smoke.sh --build-only` exit 0 · no vulnerable packages.
- **`accepted_by` deliberately left `none`** — reserved for `complete-ticket` at `done`, per the
  convention this project has followed since T-0002.

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — F1, F2 and F3 closed rather than deferred

Acceptance passed the ticket but left DoD item 4 owing one decision: **F1 was a verified gap
with neither a destination nor a recorded deviation**, and the acceptor found it wider than I
had disclosed. Branch `t-0015-acceptance-followups`.

#### F1 — a named list can only find what its author already thought of

`AssertSchemaMigratedAsync` checked that a *fixed pair* of tables existed and that the
migrations history was non-empty. The acceptor broke it twice:

- **rolled back `WidenUserSubject`** so `Subject` was `varchar(200)` — the disclosed limit;
- **dropped `placeholder_records` entirely** — *not* disclosed, and every service still
  reported healthy, `/health` still returned 200, and all three assertions still passed.

I wrote that assertion to fix exactly this class of defect — service health standing in for
migrations having run — and reintroduced it one layer down. The enumerated list of tables was
the same mistake as the enumerated service list beside it (F2), and it fails the same way: it
is a claim about the author's memory, not about the schema.

**The schema is now compared against what a clean migration produces.** A scratch database is
migrated with the stack's *own* migration step, and full column signatures — table, column,
type, length — are compared. A missing table, a missing column, an unapplied migration and a
rolled-back width change all differ, and none of them needs to have been anticipated. The
reference is asserted non-empty first, because a migration step that does nothing would
otherwise agree with a database where nothing was done.

| Acceptance reproduction | Before | Now |
| --- | --- | --- |
| `drop table placeholder_records` | **passed** | **Killed** — `Missing: placeholder_records.…` |
| `Subject` rolled back to `varchar(200)` | **passed** | **Killed** — expects `character varying(255)` |

Both re-run against a live stack, green *before* the mutation and red after, rather than
reasoned about.

#### F2 — "every service" meant "every service someone remembered"

`LongRunningServices` and `OneShotServices` were hard-coded. The services now come from
`docker compose config --services`, and each must be either running-and-healthy or exited 0.
The next service added is covered without anyone remembering to add it — and a service that
is long-running but declares no healthcheck now fails, which is the right pressure.

#### F3 — half of the review's B3 was still open, and I recorded it as fixed

AC4's tests asserted only `failure is not null` while `up --wait`'s `EnsureSucceeded` sat
inside the `try`. Both mutants failed for the right reason, but the tests could not tell — so
the claim "B3 fixed" was true of the half I had looked at. Each AC4 test now asserts the
failure its own mutation causes (`"migration step"`, `"health"`), so an unrelated fault can no
longer read as the mutation being caught.

That is the third time on this ticket that a fix was scoped to the instance in front of me
rather than to the statement I was making about it.

#### F4–F7

**F4** (`docker compose port` can return `invalid IP:0` with exit 0) is legibility only — no
consumer turns it into a false pass, and the acceptor said so. Left. **F5** (identity reports
healthy with zero clients) is a finding about the identity host, not this check, and AC6
catches it; it belongs to whoever revisits that health check. **F6** and **F7** need nothing.

- **Did:** Replaced the enumerated schema and service checks with comparisons that need no
  list; closed the open half of B3; re-ran both acceptance reproductions against a live stack.
- **Decided:** close F1 here rather than defer — a deferral needs a destination, and inventing
  one for a gap I could fix in an afternoon would be using the process to avoid the work.
- **Remaining:** re-review, then acceptance confirmation.
- **Open questions / blockers:** none.
- **Test state, all measured in this worktree:** `tools/smoke.sh` **12/12** exit 0, 3m29s, no
  leaks · root `dotnet test` **63/63** · build **0 warnings** · `dotnet format` exit 0
  (solution and smoke project) · `check-drift.sh` exit 0 · `validate.py` exit 0.

### 2026-08-31 — Software Engineer + Architect (claude-rev-6d21) — review of `t-0015-acceptance-followups` @ `166d7c4`

**Verdict: REQUEST CHANGES** — one blocking finding, a single string literal. F1 and F2 are
right and I reproduced F1's fix against both of the acceptor's mutations. F3 is right in
intent but its second assertion reintroduces the defect it exists to close, and I have the
command output to prove it.

#### Gates, all run in this worktree (`/Users/yoss/work/got-issues--t-0015b`)

| Command | Exit | Result |
| --- | --- | --- |
| `./tools/smoke.sh` | **0** | 12 passed, 3 m 23 s |
| `dotnet test` (root) | **0** | 17 + 46 = **63** |
| `dotnet build --no-incremental` | **0** | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` (solution / smoke project) | **0** / **0** | — |
| `./tools/check-drift.sh` | **0** | no drift |
| `python3 tools/validate-project-os/validate.py` | **0** | 19 tickets, 6 ADRs |

Docker empty of `gs-*`/`gotissues-*` before and after. Branch is `0 5`… `0 1` against `main`
and touches one process-lane file (this ticket) — lane discipline clean.

#### F1 — the fix is right, and I reproduced both of the acceptor's mutations against it

I built the reference the way the code does (scratch database, `compose run --rm --no-deps`
with an overridden connection string, then the column-signature query) and compared:

- **Clean stack:** reference and live are both 9 column signatures, **identical** — the check
  passes, and the reference is non-empty, so the guard is doing its job.
- **Reproduction B (`drop table public.placeholder_records cascade`):** now caught —
  `Missing: placeholder_records.CreatedAt timestamp with time zone, placeholder_records.Id
  uuid, placeholder_records.Label text`. The **old** assertions on the same database still
  reported history-exists 1, rows 4, users-exists 1 — all three green.
- **Reproduction A (`Subject` → `varchar(200)`, history row deleted):** now caught —
  `Missing: users.Subject character varying(255)` / `Unexpected: users.Subject character
  varying(200)`. The old assertions still passed with history rows 3.

The diagnosis in the Work Log is the valuable part and it is correct: the assertion written to
stop service health standing in for migrations having run reintroduced the same shape one
layer down, as an enumerated list. Comparing against what the migration step actually produces
is the only form that needs nothing to have been anticipated.

**Residual worth naming, not fixing:** the signature covers `information_schema.columns` —
table, column, type, length. Indexes, constraints, defaults and nullability are not compared,
so a migration that only adds an index or a foreign key produces an identical signature. That
is a far narrower gap than the one it replaces, and widening it is a judgement for whoever
next needs it.

#### F2 — agreed, and the pressure is the right way round

`compose config --services` is the correct source: AC1's wording is "every service reaches a
healthy state", and a hard-coded list makes that "every service someone remembered". A
long-running service that declares no healthcheck now fails, and it **should** — a service
whose health nobody declared cannot be asserted healthy, so passing it would be the check
lying. I would not soften it.

The consequence worth surfacing: the smoke tier is now a de facto policy gate on
`compose.yaml`, and the person who will trip over it is whoever adds the next service. That
reasoning currently lives in a doc comment inside the test. One sentence in the README's stack
check section would put it where a compose author looks. Non-blocking.

#### B6 (BLOCKING) — F3's health assertion is satisfied by an unrelated failure

`BrokenStackTests.cs` now asserts `Assert.Contains("health", failure!.Message, Ordinal)`.
`EnsureSucceeded` builds its message as `"{what} exited {code}\n--- stdout ---…--- stderr ---…"`,
i.e. **it embeds the raw docker output**, and `up --wait`'s `EnsureSucceeded` is still inside
the `try`. Docker's own failure text for an unhealthy dependency contains the word.

I ran it rather than argued it. With `postgres`'s healthcheck forced to fail — a mutation with
nothing whatever to do with the API's health condition — `docker compose up --wait` exits **1**
and its stderr ends:

```
Container gs-r7-upfail-postgres-1 Error dependency postgres failed to start
dependency failed to start: container gs-r7-upfail-postgres-1 is unhealthy
```

`Assert.Contains("health", …)` is **satisfied** by `unhealthy`. So
`AC4_the_check_fails_when_the_api_health_condition_is_dropped` reports the mutant killed while
**no assertion in the check ran at all**. That is precisely the outcome F3 was written to
prevent, and the acceptor's own note — *"both of my ordering mutations failed at `up --wait`,
not at an assertion"* — is the same path arriving from a different direction.

`"migration step"` is not matched by that output, so the sibling test is sound today, but it
carries the same structural fragility: a substring match against a message that can contain
arbitrary tool output.

**Fix:** assert a literal that exists only in the assertion being claimed —
`"Every service must either be running and healthy"` for the health mutant, and something like
`"produced no schema at all"` for the migration mutant. Both strings are already in the code.
Moving `up --wait`'s `EnsureSucceeded` outside the inner `try` alongside `BuildAsync` would
also close it, since neither current mutant is meant to be caught by `--wait`; either fix is
fine, the literal is the more flexible one.

**My share of this.** B3 named both halves and I recorded B3 as fixed having verified only the
first. The acceptor caught what I closed early, and the token that replaced it is weaker than
the one B3 quoted — partly because F1's rewrite removed the distinctive strings B3 pointed at.
Checking that a fix addresses the whole finding is the reviewer's job, not the acceptor's.

#### The two judgements you asked for

**1. The scratch-database coupling — accept it, and it is the same question as B6.** The cost
is real: `AssertSchemaMigratedAsync` now needs the migrator to be *runnable*, not merely to
have run. I would take that trade, for three reasons. It is the only formulation that requires
nothing to have been anticipated, which is the whole of F1. The new dependency is on the same
artefact the criterion is about — a migrator that cannot run is a genuine stack defect, and
`EnsureSucceeded("migrating the reference database")` reports it loudly. And 7 seconds on
3 m 29 s is not a consideration.

The one place the coupling bites is inside `RunCheckAgainstAsync`, where an `XunitException`
**is** the evidence: there, a `compose run` failure would masquerade as the mutant being
caught. That is contained only by the message assertions actually being distinctive — so the
coupling is safe *conditional on* B6 being fixed, and unsafe without it. Your two questions
are one question.

**2. F4 — I agree with the acceptor, and your discomfort is still well founded.** I traced
every consumer independently and could not construct a false pass either: a parsed port of `0`
yields `http://localhost:0`, and every path that could reach it fails loudly rather than
quietly — `AssertHealthAnswersFromThisStackAsync` demands 200 from the address it captured
*before* stopping anything, `WaitForAnyResponseAsync` ends in `Assert.Fail`, and
`TokenFactory.CallAuthenticatedAsync` asserts on a response that cannot arrive.

But that conclusion is a property of **today's consumers, not of the parser**. `HostPortAsync`
is a public helper on a type built for reuse, and the next consumer inherits a function that
returns a plausible-looking `0` when Docker told it `invalid IP`. Rejecting a parsed port of
`0` is one line, costs nothing, and turns a connection error against `localhost:0` into
"Docker reported no published port". I would take it — as a suggestion, not a condition. F4
stays legibility, not correctness.

#### Everything else

F5 and F6 are correctly scoped out and correctly recorded — F5's "the one place
`AssertStackHealthyAsync` alone would be fooled" is a genuinely useful observation about the
limits of health as evidence, and F6 names the last hand-verified T-0010 property. F7 confirms
the mutation-record correction survived into the merged text, which is where it needed to be.

**Closing B6 is a one-line change; re-request and I expect to approve on sight.**

### 2026-08-31 — Software Engineer (claude-sm-9d4e) — B6: my marker matched docker's own vocabulary

`Assert.Contains("health", …)` was checked against a message that **embeds raw docker
output**, and docker says `container … is unhealthy` when an unrelated dependency fails to
start. `unhealthy` contains `health`. So with postgres's healthcheck forced red — nothing to
do with the API's health condition, which is what that mutation is about — the AC4 test
reported the mutant killed **while no assertion in the check had run at all**.

That is F3's defect surviving F3's fix. I replaced "asserts only that something threw" with
"asserts a word that anything might say", which is the same claim with a thinner disguise.
The marker now has to be text only that assertion produces:

| Test | Marker |
| --- | --- |
| migration step removed | `produced no schema at all` |
| API health condition dropped | `Every service must either be running and healthy` |

And `up --wait` joins `BuildAsync` outside the inner `try`. Both mutations produce a stack
that starts cleanly — the breakage is what the assertions find afterwards — so a failure to
start is a harness fault and must fail the test rather than be counted as evidence.

| Mutant | Before | Now |
| --- | --- | --- |
| postgres healthcheck forced red (unrelated to the mutation under test) | **passed** — reported the mutant killed | **Killed** — `docker compose up --wait exited 1` |

#### The reviewer's share, and mine

`claude-rev-6d21` recorded that B3 named two halves and it verified one before marking it
fixed. Mine is worse than that: I have now made the *same* narrowing three times on this
ticket — B3's second half, F2 after Q4, and this. Each time the fix matched the sentence in
front of me rather than the claim I was making. The generalisation I would put in the retro:
**when a finding says "X is satisfied by anything", the fix is not a narrower X — it is
asking what else could satisfy the new one.** I never asked, and docker's vocabulary answered.

#### Also taken

**F4** — `HostPortAsync` now rejects a parsed `0`. The acceptor and the reviewer both showed
no consumer turns it into a false pass, and both are right; it is a public helper, and the
next consumer inherits a function that hands back a plausible port when docker printed
`invalid IP`. One line.

**F1's residual, named where it will be read:** the signature covers
`information_schema.columns` only — not indexes, constraints, defaults or nullability — so a
migration adding only an index produces an identical signature. In the doc comment, not just
here.

**F2's consequence documented:** the smoke tier is now a de facto policy gate on
`compose.yaml` — a long-running service must declare a healthcheck. That reasoning lived in a
doc comment inside a test; it is now a README sentence, where a compose author would look.

- **Did:** Replaced the substring markers with text unique to each assertion; moved `up --wait`
  into setup; took F4 and documented F1's residual and F2's consequence.
- **Decided:** markers must be strings only the assertion can emit — matching a word that
  appears in tool output is not an assertion about the tool's behaviour.
- **Remaining:** re-review, then acceptance confirmation of F1.
- **Open questions / blockers:** none.
- **Test state, measured in this worktree:** `tools/smoke.sh` **12/12** exit 0, 3m30s, no
  leaks · root `dotnet test` **63/63** · build **0 warnings** · `dotnet format` exit 0 both ·
  `check-drift.sh` exit 0 · `validate.py` exit 0.

### 2026-08-31 — Software Engineer + Architect (claude-rev-6d21) — re-review of `2181aa0`

**Verdict: APPROVE.** B6 is closed, and I closed it against the scenario rather than against
the diff. F4, F1's residual and F2's consequence were all taken. Nothing outstanding.

#### Gates, all run in this worktree (`/Users/yoss/work/got-issues--t-0015b`)

| Command | Exit | Result |
| --- | --- | --- |
| `./tools/smoke.sh` | **0** | 12 passed, 3 m 29 s, no leaked projects or volumes |
| `dotnet test` (root) | **0** | 17 + 46 = **63** |
| `dotnet build --no-incremental` | **0** | 0 warnings, 0 errors |
| `dotnet format --verify-no-changes` (solution / smoke project) | **0** / **0** | — |
| `./tools/check-drift.sh` | **0** | no drift |
| `python3 tools/validate-project-os/validate.py` | **0** | 19 tickets, 6 ADRs |

Branch is `0 3` against `main` and touches one process-lane file (this ticket).

#### B6 — closed, verified by running the scenario that used to make it pass

I forced `postgres`'s healthcheck red in `compose.yaml` — the same unrelated mutation whose
docker output I captured last pass — and ran **only**
`AC4_the_check_fails_when_the_api_health_condition_is_dropped`:

```
Failed GotIssues.SmokeTests.BrokenStackTests.AC4_the_check_fails_when_the_api_health_condition_is_dropped [12 s]
 docker compose up --wait exited 1.
Failed! - Failed: 1, Passed: 0, Skipped: 0, Total: 1
```

Exit **1**. Under the previous commit that same stack made the test report the mutant killed
with no assertion having run. `compose.yaml` was restored immediately and the tree is clean;
the failing run also tore its own stack down, so B1's fix holds on the failure path too.

The markers are sound on inspection as well as in practice: `produced no schema at all` and
`Every service must either be running and healthy` each appear in exactly one assertion
message in `StackCheck.cs` and nowhere else. They are English sentences the assertion
composes, not vocabulary another tool shares — which is the property the previous two attempts
lacked.

Moving `up --wait` out beside `BuildAsync` is the right call and the reasoning holds: neither
current mutant is meant to be caught by `--wait` — both produce a stack that starts cleanly —
so a start failure is a harness fault by construction. The comment says so, which is what a
future mutant author needs to read before relying on `--wait` to kill something.

**The generalisation in the response is the most valuable thing in this ticket, and I would
put it in the retro verbatim:** *when a finding says "X is satisfied by anything", the fix is
not a narrower X — it is asking what else could satisfy the new one.* Three of my own findings
on this ticket were instances of it, and the reason it kept recurring is that a narrower
predicate looks like progress while the question that decides it is never asked. It also
implicates me: I proposed `"reports health ''"` in B3 without asking what else could produce
those characters.

#### F4, and the two documentation items

`HostPortAsync` now rejects a parsed `0` with the mapping quoted in the message. Worth noting
for whoever reads this next: the new `Assert.True` cannot be mistaken for a mutation kill,
because `RunCheckAgainstAsync`'s inner `try` wraps only `AssertStackHealthyAsync` and
`AssertSchemaMigratedAsync`, and neither performs a port lookup. I checked the call graph
rather than assuming it.

F1's known limit — columns only, so an index-only migration produces an identical signature —
is now in the doc comment on `AssertSchemaMigratedAsync`, which is where someone deciding
whether to trust the check will actually be looking. F2's consequence is a README sentence
telling a compose author that a long-running service must declare a healthcheck. Both were
suggestions and both landed in the right place rather than the convenient one.

#### Where this leaves T-0015

AC1–AC7 each have a check behind them; AC8 is deferred to
[T-0018](T-0018-user-subject-tokens.md), whose scope and AC2 genuinely accept it. The schema
assertion no longer depends on anyone having enumerated the right tables, "every service" no
longer means "every service someone remembered", and AC4's two tests now fail when the thing
they name is not what broke. The DoD item 4 decision acceptance left open is closed by fixing
F1 rather than by finding it a destination, which is the better answer.

Across five review passes every blocking finding was the harness failing a standard it exists
to enforce — a discarded exit code, a name that could collide, an assertion satisfied by any
failure, one satisfied by its own argument, a gate read from the wrong working copy, and a
marker docker also emits. None was a defect in what the check tests; all were defects in what
the check would accept as evidence. That is the pattern for the retrospective.

**Approved for merge.** Merge, then `complete-ticket`.
