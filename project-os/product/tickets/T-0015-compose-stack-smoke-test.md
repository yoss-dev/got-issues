---
id: T-0015
title: Automated coverage for behaviour that needs the real Compose stack
type: technical
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0003, T-0010]
adrs: [ADR-0003]
created: 2026-08-30
updated: 2026-08-30
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
- A decision on where it lives and how it is invoked. It is materially slower than `dotnet test` and should not be dragged into the habitual suite ([TESTING.md](../../standards/TESTING.md): the habitual tier stays fast).
- Documentation of how to run it.

### Out of Scope

- Replacing anything in T-0003's harness; this covers what that harness structurally cannot.
- CI wiring — there is no CI (`PROJECT.md` Q6).
- API behaviour that the in-process integration tier **can** already reach — anonymous refusal, endpoint presence, health semantics. Those belong in T-0003's tier and are already covered there. The line is not "stack versus API"; it is "needs the real stack versus does not".

## Acceptance Criteria

- [ ] AC1: Given an empty volume, when the check runs, then it starts the stack from `compose.yaml` and asserts every service reaches a healthy state, failing if any does not.
- [ ] AC2: Given data written to an existing volume, when the stack is restarted, then the check asserts the data survives and the migration step exits zero having applied nothing.
- [ ] AC3: Given a database that is slow or absent, when the API starts, then the check asserts the API waits rather than exiting, and becomes healthy once the database arrives.
- [ ] AC4: Given a deliberately broken stack (for example the migration step removed, or a health condition dropped), when the check runs, then it **fails** — proven by mutation, not by observing a green run.
- [ ] AC5: Given the habitual `dotnet test` suite, when it runs, then this check is not part of it, and the README says how to run it separately.
- [ ] AC6: Given the identity host running against the stack, when a token it issued is presented to the protected endpoint, then the request is accepted; and when an **expired** token, a **wrong-audience** token, or one signed by an **unknown key** is presented, then each is refused with 401.
- [ ] AC7: Given the identity host started **without** its migration step against an empty schema, when it runs, then it creates no tables and seeds nothing — the analogue of T-0001 AC5 for the identity host.

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
- Overlaps [T-0014](T-0014-correct-testing-standard-commands.md) if the standard ends up describing this command too.

## Testing Notes

AC4 is the criterion that keeps this honest: a stack check that has only ever been seen green proves nothing. Mutate the compose file, watch it fail, revert.

## Relevant ADRs & Documentation

- [ADR-0003](../../architecture/adr/ADR-0003-initial-technology-stack.md) — the Compose constraint and the explicit migration step
- [TESTING.md](../../standards/TESTING.md) — tiers, and keeping the habitual suite fast
- [T-0001](T-0001-runnable-compose-stack.md) — the criteria this covers, currently verified only by hand

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined.

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
