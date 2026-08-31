# SPRINT-001

## Goal

**The Got Issues stack runs from a clean clone with a working token round-trip, proved by automated tests.**

One outcome: a person who clones this repository can bring the whole system up, obtain a token, and see the behaviour verified by a suite rather than by assertion. It is the proof of concept's first real answer to *can we run this in-house?* — and it is the tiebreaker for every mid-sprint decision. Work that does not serve it waits.

## Dates

**Continuous flow — no fixed end date.** The sprint closes when the goal is met. There is no throughput history to estimate against, so the first retrospective measures what actually happened rather than performance against a guess (maintainer's decision, 2026-08-30).

## Committed Work

Sequenced: T-0001 first — everything except the spike depends on it.

| Ticket | Title | Status | Owner | Blocked by |
| --- | --- | --- | --- | --- |
| [T-0001](../product/tickets/T-0001-runnable-compose-stack.md) | Runnable Docker Compose stack with API skeleton and PostgreSQL | committed | none | — |
| [T-0011](../product/tickets/T-0011-spike-aspnetcore-generator-viability.md) | SPIKE: is OpenAPI Generator's aspnetcore output workable on ASP.NET Core 10? | committed | none | — |
| [T-0003](../product/tickets/T-0003-automated-test-harness.md) | Automated test harness — xUnit, WebApplicationFactory, and PostgreSQL via Testcontainers | committed | none | T-0001 |
| [T-0010](../product/tickets/T-0010-duende-identity-host.md) | Duende IdentityServer host in the stack, with the API as resource server | committed | none | T-0001 |

## Blockers & Escalations

*(none)*

## Discovered / Unplanned Work

*(none)*

## Notes

**Goal confirmed by the human Product Owner (2026-08-30)**, along with the scope and the continuous-flow cadence.

**Capacity is unmeasured.** This is the first sprint; there is no archived throughput to plan against. `plan-sprint` prescribes conservative commitment without history, and this commitment is one ticket beyond the conservative option — a deliberate maintainer choice, recorded so the retrospective can judge whether it was right rather than re-litigating it from memory.

**T-0010 is the high-risk selection.** Refinement recorded it as the least certain estimate in the backlog: Duende IdentityServer's configuration surface is large and its documentation assumes context a first setup does not have. If anything misses the goal, this is the likely candidate. Its recorded split seam is seeding (AC8–AC10) — but splitting strands T-0009's admin-policy tests, so the seam is a last resort rather than a convenience.

**T-0001 and T-0003 must be sequenced in that order, and both must land.** T-0001 cannot satisfy [DoD](../governance/DEFINITION_OF_DONE.md) item 3 (automated tests exist and pass) on its own: the harness depends on it, so it ships with manual verification only. T-0003's AC8 covers T-0001's stack behaviour and closes that gap. If T-0003 slips out of this sprint, T-0001 completes only with a **recorded PO deviation** — that is the decision to bring back to the maintainer, not one to make quietly at completion time.

**T-0011 does not serve the goal directly** and was admitted because it is independent, time-boxed at four hours, and cannot crowd anything out. Its verdict decides whether [ADR-0004](../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) survives, which shapes SPRINT-002 before it is planned. **If the verdict is "supersede", stop and re-plan rather than absorbing it** — T-0002's scope changes materially, and that is a WoW §7 conversation, not a mid-sprint adjustment.

**Deliberately not committed:** T-0002 (gated on T-0011's verdict — committing it would be planning around an unanswered question) and T-0009 (its dependencies T-0003 and T-0010 are both in this sprint; committing it too would mean three dependent tickets stacked behind an unproven estimate).

**Solo mode.** Per [GIT.md](../standards/GIT.md), one agent at a time on this repository, and review before merge means an independent session running `review-code` against the branch diff with the verdict recorded in the ticket's Work Log.
