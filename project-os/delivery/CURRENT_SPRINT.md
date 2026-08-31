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
| [T-0001](../product/tickets/T-0001-runnable-compose-stack.md) | Runnable Docker Compose stack with API skeleton and PostgreSQL | blocked | claude-sm-9d4e | PO decision (see Blockers) |
| [T-0011](../product/tickets/T-0011-spike-aspnetcore-generator-viability.md) | SPIKE: is OpenAPI Generator's aspnetcore output workable on ASP.NET Core 10? | in-acceptance | none | — |
| [T-0003](../product/tickets/T-0003-automated-test-harness.md) | Automated test harness — xUnit, WebApplicationFactory, and PostgreSQL via Testcontainers | committed | none | T-0001 |
| [T-0010](../product/tickets/T-0010-duende-identity-host.md) | Duende IdentityServer host in the stack, with the API as resource server | committed | none | T-0001 |

## Blockers & Escalations

- **T-0011 — verdict is "supersede", so the loop stopped as planned.** The spike found that OpenAPI Generator's `aspnetcore` templates emit **synchronous** controller methods with no `CancellationToken` (`operationIsAsync=true` is silently ignored, confirmed across three configurations), which collides with ENGINEERING.md's async rule on every endpoint permanently. **ADR-0006 is drafted as `Proposed`**, recommending NSwag for server contracts and OpenAPI Generator for clients. This sprint's Notes said a "supersede" verdict stops the loop rather than absorbing it, because T-0002's scope changes materially (WoW §7). Awaiting the maintainer: accept ADR-0006, reject it, or ask for NSwag to be verified first. **T-0002 is now stale-Ready** and must be re-refined against whichever way this goes.

- **T-0001 — awaiting a PO decision on AC1.** Implementation is complete and independently reviewed (`claude-rev-2c8d`, request-changes); every finding is resolved except one. AC1 requires `docker compose up` to work from a clean clone "with no further manual steps", but `.env` is git-ignored by design, so PostgreSQL will not initialise without it. Resolving it means either committing a default credential (forbidden outright by SECURITY.md), amending AC1 to permit the documented `cp .env.example .env` (a PO artifact I may not change), or switching PostgreSQL to trust authentication (technically clean, but a security posture lowered to pass a criterion). **Recommended default: amend AC1** — the ticket's own In Scope already mandates `.env.example`, which implies copying it. Full escalation in the ticket Work Log. Blocked since 2026-08-30; unblocked by the maintainer choosing one option.

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

---

## `run-sprint` exit report — 2026-08-30 (claude-sm-9d4e)

**Loop stopped: no eligible work remains.** T-0003 and T-0010 both require T-0001 to be `done`, and T-0001 is blocked. Two decisions are batched below; both are recorded in full in their tickets.

**Progressed**

- **T-0001** — implemented, independently reviewed by `claude-rev-2c8d` (request-changes), all findings resolved except one. Now `blocked` on a PO decision. Branch `t-0001-runnable-compose-stack`, 5 commits, not merged.
- **T-0011** — spike complete, verdict delivered, `in-acceptance`. Awaiting an independent acceptance session.

**Gates honoured.** Review ran as a separate session with its own identity and re-verified every criterion against its own clean clone rather than trusting the implementer — which is how the AC1 false pass was caught. Nothing was merged; nothing reached `done`.

**Artifacts created:** ADR-0005 (Accepted, during refinement), ADR-0006 (**Proposed**, from the spike). No discovered work; no deferred defects.

**Consequence to note:** T-0002 is `ready` but **stale** — it was refined against ADR-0004, which ADR-0006 would supersede. It must be re-refined before it is planned, whichever way the decision goes.

**Validator:** OK (11 tickets, 6 ADRs). Working tree clean, trunk and branch both consistent.

**Not run:** acceptance for T-0011, and the re-review of T-0001 after its fixes. Both are waiting on the decisions, not on capacity.
