# SPRINT-002

## Goal

**The contract-first premise is proven, and nothing in the system is verified only by hand.**

One outcome with two halves that are the same claim: the project's central bet — that an API can be designed in a specification and generated from it — stops being an ADR and becomes running code; and the verification debt SPRINT-001 knowingly took on is repaid, so no criterion still rests on someone having once run a command. It is the tiebreaker for mid-sprint decisions: work that makes the contract real or makes verification automatic serves the goal; work that adds product surface does not, this sprint.

## Dates

**Continuous flow — no fixed end date.** The sprint closes when the goal is met. SPRINT-001 established one data point of throughput (4 items, one a 4-hour spike) — not enough to estimate against, and the retro noted that count came with four review rounds on a single ticket.

## Committed Work

All three are independent: none blocks another, and all three can start immediately. That is deliberate — SPRINT-001's plan noted that stacking dependent tickets behind an unproven estimate was the thing to avoid.

| Ticket | Title | Status | Owner | Blocked by |
| --- | --- | --- | --- | --- |
| [T-0002](../product/tickets/T-0002-contract-first-codegen-pipeline.md) | Contract-first pipeline — OpenAPI spec, code generation, and drift check | done | none | — |
| [T-0009](../product/tickets/T-0009-role-authorisation-and-user-projection.md) | Role-based authorisation and the user projection from token claims | done | none | — |
| [T-0015](../product/tickets/T-0015-compose-stack-smoke-test.md) | Automated coverage for behaviour that needs the real Compose stack | done | none | — |

## Blockers & Escalations

*(none)*

## Discovered / Unplanned Work

Four tickets, all raised rather than absorbed:

| Ticket | Source | Why it was not fixed in place |
| --- | --- | --- |
| [T-0016](../product/tickets/T-0016-generation-output-ownership.md) | T-0002 | outside that ticket's scope |
| [T-0017](../product/tickets/T-0017-automated-contract-conformance-tier.md) | T-0002 | a tier TESTING.md defines and nothing automates |
| [T-0018](../product/tickets/T-0018-user-subject-tokens.md) | T-0015 | AC8's **named successor**; issuing a subject-carrying token means deciding how people authenticate — a system-shaping decision, not a coverage one |
| [T-0019](../product/tickets/T-0019-token-clock-skew.md) | T-0015 | the resource server keeps JwtBearer's five-minute grace, a framework default nobody chose; changing token validation under a coverage ticket would have bypassed the Security review SECURITY.md requires |

[T-0014](../product/tickets/T-0014-correct-testing-standard-commands.md)'s scope was widened
to accept T-0015's rot risk: `apps/GotIssues.SmokeTests` sits outside the solution so the
habitual suite stays fast, which means nothing compiles it by accident.

## Notes

### Run report — SPRINT-002 drained, 2026-08-31

**All three committed tickets are `done`.** No ticket was blocked, none was parked, and no
gate was skipped: each carries an independent review verdict, an independent acceptance
(`accepted_by` differs from `implemented_by` in both cases), and a recorded DoD walk.

| Ticket | Evidence |
| --- | --- |
| [T-0002](../product/tickets/T-0002-contract-first-codegen-pipeline.md) | done earlier in the sprint; first ticket needing no DoD deviation |
| [T-0009](../product/tickets/T-0009-role-authorisation-and-user-projection.md) | `ece515d`, `c2d2883` · review `claude-rev-4a7e` · acceptance `claude-qa-7c21` |
| [T-0015](../product/tickets/T-0015-compose-stack-smoke-test.md) | `7bda6bc`, `d32d3cb`, `921bf95` · review `claude-rev-6d21` (five passes) · acceptance `claude-qa-9b3e` |

**Two DoD deviations discharged.** T-0001's and T-0010's were approved on the explicit basis
that T-0015 would close them; they are now AC1–AC3 and AC6–AC7 of a check that was broken on
demand and watched to fail.

**The product chain is unblocked.** [T-0004](../product/tickets/T-0004-create-and-list-projects.md)
has no outstanding dependencies and is the first product capability eligible to start — the
sprint's stated payoff.

**No decisions are pending.** Nothing requires a human answer to continue; the two questions
this sprint raised became T-0018 and T-0019, each with the decision framed rather than
pre-empted.

**Validator green.** 19 tickets, 6 ADRs.

**What the sprint's findings had in common.** SPRINT-001's retrospective found that every
blocking finding was *the repository claiming more than the code delivered*. This sprint's
were the same fault relocated **into the verification itself**: a test asserting the right
thing about an input that could not reach the code; a test host emitting a claim shape
production cannot produce; a discarded exit code in the one place that checked exit codes; an
assertion satisfied by any failure; another satisfied by its own argument; a gate read from
the wrong working copy. None was a defect in *what* was tested — all were defects in what was
accepted as evidence. That is worse than a documentation-shaped problem, because a check that
accepts weak evidence manufactures false claims instead of merely recording them.

**Recommended next:** `retrospective`. Closing a sprint is a human-visible act and is not
run automatically.

**Goal and scope confirmed by the human Product Owner (2026-08-31)**, choosing foundations and verification debt over reaching a product endpoint this sprint.

**T-0002 is the sprint's centre of gravity.** [ADR-0004](../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) has governed every ticket since bootstrap without ever being exercised: no specification exists, no code has been generated, and the drift check that [GIT.md](../standards/GIT.md) and [TESTING.md](../standards/TESTING.md) both name as a merge gate is prose. [T-0011](../product/tickets/T-0011-spike-aspnetcore-generator-viability.md) de-risked the generator and its re-refinement folded those findings in as constraints — but the premise itself is still unproven. If it fails here, it fails cheaply; if it is never tried, the PoC has not answered its own question.

**T-0015 discharges two recorded DoD deviations** — on [T-0001](../product/tickets/T-0001-runnable-compose-stack.md) and [T-0010](../product/tickets/T-0010-duende-identity-host.md). Both were approved on the explicit basis that this ticket would close them, so it carries more weight than a backlog-position reading suggests. Its AC4 (mutation-proven, not green-run-proven) is now also a standards requirement rather than the ticket's own idea of rigour.

**T-0009 unblocks the product chain — now realised.** Done 2026-08-31; with T-0002 also done, [T-0004](../product/tickets/T-0004-create-and-list-projects.md) has no outstanding dependencies and is the first product capability eligible to start. That was the sprint's stated payoff, and T-0015 has since closed too — the sprint is drained.

**Not committed, deliberately:** [T-0004](../product/tickets/T-0004-create-and-list-projects.md) and [T-0005](../product/tickets/T-0005-create-and-read-issues.md), both `ready`. T-0004 depends on two tickets *in this sprint*, and T-0005 depends on T-0004 — committing either would stack dependent work behind unproven estimates, which SPRINT-001 explicitly avoided and which remains the right call while capacity is one data point.

**Watch items, from refinement rather than guesswork.** T-0002 must handle a generator that emits `net8.0` and drags a vulnerable `Newtonsoft.Json` transitively even when told not to; its option string is load-bearing as a whole. T-0015's cost is entirely in standing the harness up — after that each criterion is a few lines — and if it overruns, the split seam only works *after* the harness exists.

**Governance changes from [RETRO-SPRINT-001](retrospectives/RETRO-SPRINT-001.md) take effect this sprint:** coverage claims must be verified by mutation, verification against a running service must be attributable to the process under test, and a deferral counts as captured only when the destination ticket's scope accepts it. The retro predicted how we would notice these working — *fewer blocking review findings of the "claim outran evidence" type, which was 4 of 4 last sprint*. That is the number to look at in RETRO-SPRINT-002.

**Solo mode.** One agent at a time; review before merge means an independent session running `review-code` with the verdict recorded in the Work Log ([GIT.md](../standards/GIT.md)).
