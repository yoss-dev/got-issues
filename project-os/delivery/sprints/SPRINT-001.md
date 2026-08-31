<!-- Archived from delivery/CURRENT_SPRINT.md at the close of SPRINT-001, 2026-08-31.
     Content is verbatim EXCEPT relative links, which gained one ../ level: this file
     sits a directory deeper than the original, so a byte-for-byte copy leaves every
     link broken and the validator red. See RETRO-SPRINT-001, Process observations. -->

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
| [T-0001](../../product/tickets/T-0001-runnable-compose-stack.md) | Runnable Docker Compose stack with API skeleton and PostgreSQL | done | none | — |
| [T-0011](../../product/tickets/T-0011-spike-aspnetcore-generator-viability.md) | SPIKE: is OpenAPI Generator's aspnetcore output workable on ASP.NET Core 10? | done | none | — |
| [T-0003](../../product/tickets/T-0003-automated-test-harness.md) | Automated test harness — xUnit, WebApplicationFactory, and PostgreSQL via Testcontainers | done | none | — |
| [T-0010](../../product/tickets/T-0010-duende-identity-host.md) | Duende IdentityServer host in the stack, with the API as resource server | done | none | — |

## Blockers & Escalations

*(none — T-0001's AC1 escalation was resolved by the PO on 2026-08-30: amend AC1. Recorded in the ticket Work Log.)*

## Discovered / Unplanned Work

*(none)*

## Notes

**Goal confirmed by the human Product Owner (2026-08-30)**, along with the scope and the continuous-flow cadence.

**Capacity is unmeasured.** This is the first sprint; there is no archived throughput to plan against. `plan-sprint` prescribes conservative commitment without history, and this commitment is one ticket beyond the conservative option — a deliberate maintainer choice, recorded so the retrospective can judge whether it was right rather than re-litigating it from memory.

**T-0010 is the high-risk selection.** Refinement recorded it as the least certain estimate in the backlog: Duende IdentityServer's configuration surface is large and its documentation assumes context a first setup does not have. If anything misses the goal, this is the likely candidate. Its recorded split seam is seeding (AC8–AC10) — but splitting strands T-0009's admin-policy tests, so the seam is a last resort rather than a convenience.

**T-0001 and T-0003 must be sequenced in that order, and both must land.** T-0001 cannot satisfy [DoD](../../governance/DEFINITION_OF_DONE.md) item 3 (automated tests exist and pass) on its own: the harness depends on it, so it ships with manual verification only. T-0003's AC8 covers T-0001's stack behaviour and closes that gap. If T-0003 slips out of this sprint, T-0001 completes only with a **recorded PO deviation** — that is the decision to bring back to the maintainer, not one to make quietly at completion time.

**T-0011 does not serve the goal directly** and was admitted because it is independent, time-boxed at four hours, and cannot crowd anything out. Its verdict decides whether [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) survives, which shapes SPRINT-002 before it is planned. **If the verdict is "supersede", stop and re-plan rather than absorbing it** — T-0002's scope changes materially, and that is a WoW §7 conversation, not a mid-sprint adjustment.

**Deliberately not committed:** T-0002 (gated on T-0011's verdict — committing it would be planning around an unanswered question) and T-0009 (its dependencies T-0003 and T-0010 are both in this sprint; committing it too would mean three dependent tickets stacked behind an unproven estimate).

**Solo mode.** Per [GIT.md](../../standards/GIT.md), one agent at a time on this repository, and review before merge means an independent session running `review-code` against the branch diff with the verdict recorded in the ticket's Work Log.

---

## `run-sprint` exit report — 2026-08-30 (claude-sm-9d4e)

**Loop stopped: no eligible work remains.** T-0003 and T-0010 both require T-0001 to be `done`, and T-0001 is blocked. Two decisions are batched below; both are recorded in full in their tickets.

**Progressed**

- **T-0001** — implemented, independently reviewed by `claude-rev-2c8d` (request-changes), all findings resolved except one. Now `blocked` on a PO decision. Branch `t-0001-runnable-compose-stack`, 5 commits, not merged.
- **T-0011** — spike complete, `in-acceptance`. Verdict after correction: **ADR-0004 stands**; the generator is viable with the right flags. Awaiting an independent acceptance session, which should scrutinise the corrected findings rather than the first pass.

**Gates honoured.** Review ran as a separate session with its own identity and re-verified every criterion against its own clean clone rather than trusting the implementer — which is how the AC1 false pass was caught. Nothing was merged; nothing reached `done`.

**Artifacts created:** ADR-0005 (Accepted, during refinement), ADR-0006 (**Proposed**, from the spike). No discovered work; no deferred defects.

**Correction, same day:** the spike's blocking finding was wrong. The `aspnetcore` generator *can* emit `async Task<IActionResult>`; it needs `operationIsAsync` **and** `operationResultTask`, and the spike set only the first. The maintainer challenged it and was right. **ADR-0004 stands, ADR-0006 is Rejected, and T-0002 is not stale** — it remains Ready and inherits the working generator configuration recorded in T-0011. Only **one** decision (T-0001's AC1) is now outstanding.

**Validator:** OK (11 tickets, 6 ADRs). Working tree clean, trunk and branch both consistent.

**Not run:** acceptance for T-0011, and the re-review of T-0001 after its fixes. Both are waiting on the decisions, not on capacity.
