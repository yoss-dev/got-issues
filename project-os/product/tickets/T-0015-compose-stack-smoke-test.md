---
id: T-0015
title: Automated smoke test for the Compose stack's own behaviour
type: technical
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: [T-0003]
adrs: [ADR-0003]
created: 2026-08-30
updated: 2026-08-30
---

# T-0015: Automated smoke test for the Compose stack's own behaviour

## Problem / Context

Raised by independent acceptance of [T-0003](T-0003-automated-test-harness.md) (`claude-qa-3f7c`, 2026-08-30).

T-0003's harness closes most of [T-0001](T-0001-runnable-compose-stack.md)'s Definition of Done deviation: it proves migrations are applied by the project's own migrations, that `/health` reports the database's real state, and — the one that mattered most — that the API does **not** migrate on startup.

What it cannot reach is everything that is true of the **Compose stack rather than the application**: T-0001's AC1 (cold start from a clean clone, all services healthy), AC6 (restart against an existing volume is non-destructive), and AC7 (a slow or absent database delays startup rather than crashing it). `WebApplicationFactory` starts the app in-process; it never runs `docker compose`.

Those three criteria were verified **by hand, twice**, and remain verified only by hand. The acceptor's point is the sharp one: that residual currently lives as Work Log prose, which is exactly the pattern DoD item 4 forced into tickets on T-0001. Hence this ticket.

## Desired Outcome

The Compose stack's own start-up, restart, and dependency-ordering behaviour is verified by something repeatable rather than by a person remembering to try it.

## User / Business Value

These are the behaviours that break silently and expensively: a base-image change, a health-condition edit, or a migration-step regression would today be caught only if someone happened to run a cold start. The value is protecting the claim the README makes to anyone cloning the repository.

## Scope

### In Scope

- An automated check that drives the real `compose.yaml` — not `WebApplicationFactory` — covering: cold start on an empty volume reaching healthy; restart against an existing volume preserving data and re-running the migration step as a no-op; and the API tolerating a database that is slow or absent.
- A decision on where it lives and how it is invoked. It is materially slower than `dotnet test` and should not be dragged into the habitual suite ([TESTING.md](../../standards/TESTING.md): the habitual tier stays fast).
- Documentation of how to run it.

### Out of Scope

- Replacing anything in T-0003's harness; this covers what that harness structurally cannot.
- CI wiring — there is no CI (`PROJECT.md` Q6).
- Anything about the API's own behaviour, which the existing integration tier covers.

## Acceptance Criteria

- [ ] AC1: Given an empty volume, when the check runs, then it starts the stack from `compose.yaml` and asserts every service reaches a healthy state, failing if any does not.
- [ ] AC2: Given data written to an existing volume, when the stack is restarted, then the check asserts the data survives and the migration step exits zero having applied nothing.
- [ ] AC3: Given a database that is slow or absent, when the API starts, then the check asserts the API waits rather than exiting, and becomes healthy once the database arrives.
- [ ] AC4: Given a deliberately broken stack (for example the migration step removed, or a health condition dropped), when the check runs, then it **fails** — proven by mutation, not by observing a green run.
- [ ] AC5: Given the habitual `dotnet test` suite, when it runs, then this check is not part of it, and the README says how to run it separately.

## Examples / Scenarios

- Cold start, empty volume: all services healthy.
- Restart with data: row survives, migrator a no-op.
- Migration step removed from `compose.yaml`: the check fails (AC4).
- Base image tag changed to something broken: the check fails.

## Dependencies

**T-0003** — reuses its conventions and its container tooling.

Relates to [T-0012](T-0012-pin-container-base-images.md): once base images are digest-pinned, this check is what would catch a bad pin.

## Risks / Unknowns

- **This is a slow test by nature** — image builds and container startup. If it becomes slow enough to be skipped it protects nothing, which is why AC5 keeps it out of the habitual tier and why it needs an obvious way to run.
- Driving `docker compose` from a test harness is awkward; an alternative is a shell script with assertions. Refinement should choose deliberately rather than defaulting to whichever is nearer to hand.
- **AC3 is hard to make deterministic** — "slow database" is a race by construction. It may need an injected delay rather than luck.
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
