# RETRO-SPRINT-001

Held 2026-08-31, immediately after the sprint drained. Evidence is the repository: tickets, Work Logs, the archived [SPRINT-001](../sprints/SPRINT-001.md), and `git log`. Where an observation is inference rather than record, it says so.

## Sprint summary

- **Goal:** *The Got Issues stack runs from a clean clone with a working token round trip, proved by automated tests.* — **achieved.** `docker compose up` brings up five services; two seeded identities issue role-bearing tokens; the API accepts them and refuses everything else; 16 tests run in ~5 s.
- **Committed:** 4 tickets ([T-0001](../../product/tickets/T-0001-runnable-compose-stack.md), [T-0011](../../product/tickets/T-0011-spike-aspnetcore-generator-viability.md), [T-0003](../../product/tickets/T-0003-automated-test-harness.md), [T-0010](../../product/tickets/T-0010-duende-identity-host.md)). **Done:** 4. **Returned to backlog:** 0. **Discovered work added to the sprint:** 0.
- **Follow-up tickets created from review and acceptance:** 4 — T-0012, T-0013, T-0014, T-0015.
- **Previous retro's actions:** none — this is the first sprint.
- **Throughput baseline established:** 4 tickets, one of them a 4-hour spike, across a single working session. The plan noted the commitment was *one ticket beyond* the conservative option; that judgement proved right.

## What worked

**Independent sessions found things no amount of self-review did.** Every blocking finding this sprint came from a session that did not write the code. The reviewer raised 4 blocking findings across three tickets; the acceptor raised 1 defect and 1 finding that changed a follow-up ticket. None was found by the implementer.

**Verifying by mutation rather than by reading.** This is the single most valuable practice the sprint produced. Take a claim, ask what would have to be true for it to be false, then make it false:

- [T-0003](../../product/tickets/T-0003-automated-test-harness.md): `Database.Migrate()` inserted into startup → the AC5 guard went red. Without that check the coverage claim was false and nobody would have known.
- [T-0010](../../product/tickets/T-0010-duende-identity-host.md): the `AnyAsync` guard removed → unique-index violation, establishing that idempotence has two independent guarantees; a client deleted then re-seeded → distinguishes a per-entity guard from a global one, which neither earlier check could.
- [T-0010](../../product/tickets/T-0010-duende-identity-host.md): the `identity` schema dropped → `/health` 200 → 503, proving the new check can fail where the old one could not.

**Escalating rather than deciding alone.** Four escalations reached the human PO and all four were answered and transcribed before being acted on (WoW §13). Two changed the outcome materially: amending T-0001's AC1 (the alternatives each broke [SECURITY.md](../../standards/SECURITY.md) or lowered a security posture), and **rejecting T-0010's proposed scope deviation** — which turned out to buy three things unavailable under the design the implementer proposed, including two AC4 refusal cases the reviewer had concluded could not be isolated by hand.

**The standards did real work on their first day.** `TreatWarningsAsErrors` failed the build on CA1848 within an hour of existing, and again on a vulnerable transitive `SSH.NET` in T-0003 and `Newtonsoft.Json` in T-0011. The [DoR](../../governance/DEFINITION_OF_READY.md) sizing guideline forced T-0001's split into T-0010 before implementation, not during.

**Time-boxing the riskiest unknown.** [T-0011](../../product/tickets/T-0011-spike-aspnetcore-generator-viability.md) was created during refinement specifically because [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) deferred its own biggest risk to "the first real endpoint". The spike answered it before T-0002 was built around it — and its first verdict was *wrong*, caught by the human, which is itself the strongest argument for having run it early and cheaply rather than mid-implementation.

## What caused friction

**Four review rounds on T-0001 and three on T-0010.** Not wasted — every round found something real — but the cost is visible: T-0001 alone carries 10 review-session entries and 5 acceptance entries in its Work Log.

**The identity host consumed disproportionate effort**, exactly as refinement predicted when it flagged T-0010 as the sprint's least certain estimate. The specific cost: Duende's DbContexts resolve store options from the application service provider attached to `DbContextOptions`, not the constructor — an undiagnosed API detail that led the implementer to propose dropping a scope item. The estimate risk was correctly identified in advance; the *response* to it was wrong.

**One blocked episode**, T-0001 on AC1, resolved within the same session (commits `44cb8e1` → `d19afae`). Short, correctly recorded, and the blocker was a genuine precedence conflict rather than a missing decision.

**Three DoD item-3 deviations across four tickets.** T-0001 (no harness yet), T-0003 (AC7's governance half), T-0010 (token validation needs a running stack). All three were recorded and PO-approved rather than silently passed — but three deviations in a four-ticket sprint is a signal about sequencing, not just about honesty.

## Defect & rework analysis

**No acceptance failed.** Every ticket passed acceptance on its first attempt; all rework came from *review*, i.e. before merge, which is where it is cheapest.

**Every blocking finding was the same defect.** The reviewer's own summary, and the evidence supports it: *the repository claiming more than the code delivered.*

| Ticket | The claim | The reality |
| --- | --- | --- |
| T-0001 | AC1 verified | The evidence run had `.env` already present; the literal criterion was never exercised |
| T-0003 | "closes T-0001's DoD deviation" | Every test migrated first, so a startup migration would have gone undetected |
| T-0010 | residual owned by T-0015 | T-0015's Out of Scope explicitly disowned it |
| T-0010 | identity host healthy | The check could not fail; it reported 200 on a host that could issue no tokens |

**None of these was a bug in the ordinary sense. None would have failed a build.** They are gaps between the record and reality, and only adversarial verification found them.

**A second recurring pattern, seven instances: a green signal measured from the wrong source.** `curl` answered by a different stack holding port 8080 (twice — the second time by the implementer *after* writing up the first as a lesson); `grep`'s exit code read as `dotnet format`'s; `dotnet format` exit 2 seen and not gated on; a Docker probe that evidenced a *running* daemon rejecting a bad mount; four of six env-var simulations that left the suite green because Testcontainers falls back to a working socket by design; and an acceptance pass that succeeded only because the README's sections were run out of order.

**A third pattern: one event falsified three documents at once.** T-0001 landing made every "arrives with the first implementation ticket" claim false simultaneously — the README banner, `spec/README.md`, and `ARCHITECTURE.md`'s state banner. They were found by *sweeping* for the shape of the claim, not by spot-checking the one reported symptom.

## Process & governance observations

**The two-lane git model held under pressure.** 40 process-lane commits and 16 source-lane commits, with the one predicted Work Log merge conflict occurring on T-0001 and resolving exactly as [GIT.md](../../standards/GIT.md) prescribes. The lane rule also correctly *stopped* work twice: T-0001 declined to fix `TESTING.md` and T-0003 declined again, both because `project-os/standards/` is governance — producing [T-0014](../../product/tickets/T-0014-correct-testing-standard-commands.md) rather than a silent edit.

**DoD item 4 did real work, repeatedly.** It converted Work-Log prose into owned tickets three times: T-0001's deferrals → T-0012/T-0013/T-0014; T-0003's coverage residual → T-0015. Without it, four real gaps would exist only as narrative.

**But a ticket can be handed a residual it does not accept.** [T-0015](../../product/tickets/T-0015-compose-stack-smoke-test.md) was created for T-0001's residual and then pointed at by T-0010 — while its Out of Scope explicitly excluded what T-0010 was sending it. DoD item 4 requires a *link*; it does not require the destination to accept. **A false pointer reads as covered and is worse than no ticket.** This is a genuine gap in the rule as written.

**`accepted_by` semantics tripped both acceptance sessions.** Each was instructed to set it and each correctly refused, because the validator reserves it for `complete-ticket` at `done` ([TICKET_TEMPLATE](../../templates/TICKET_TEMPLATE.md)). The validator caught it both times; the skill text did not prevent it. Cheap to fix in the brief, not in the framework.

**The archive instruction and the validator contradict each other.** `retrospective` step 1 says to copy `CURRENT_SPRINT.md` to `delivery/sprints/` **verbatim**, and its Validation says "archived byte-for-byte". But the archive sits one directory deeper, so a byte-for-byte copy leaves every relative link broken — `python3 tools/validate-project-os/validate.py` reported **7 broken links** immediately after this sprint's archive. Following the skill literally makes the validator red; satisfying the validator means not following it literally.

Resolved here by re-basing the links one level and marking the file with a header saying exactly what differs from verbatim. The framework should say which of the two wins. Folded into the same `evolve-governance` batch as the actions below; project-agnostic and worth upstreaming.

**ADR-0006 is a useful artefact in its rejected state.** It records a decision proposed on wrong evidence and rejected two hours later, body preserved. That is the ADR system working as intended rather than a wasted document.

## Improvement actions

Three actions, each owned and each landing somewhere concrete.

| # | Action | Owner | Lands as |
| --- | --- | --- | --- |
| 1 | **Make "verify by mutation" an explicit expectation** for coverage claims — an implementer asserting that a test guards behaviour X states how they made it fail. Add to `standards/TESTING.md` and to `implement-ticket`'s engineering-verification step. | maintainer (approval) + agent (drafting) | `evolve-governance` proposal — **project-agnostic, worth upstreaming per [FOUNDATION.md](../../FOUNDATION.md)** |
| 2 | **Close the false-pointer gap in DoD item 4**: a deferred item is only "captured" when the destination ticket's scope *accepts* it, and whoever defers must cite the accepting scope line. | maintainer (approval) + agent (drafting) | `evolve-governance` proposal — **project-agnostic, worth upstreaming** |
| 3 | **Add an attribution rule for clean-clone verification** to `standards/TESTING.md`: any check against a locally-served endpoint must bind the response to the process under test — own Compose project name, assert the container id healthy, and stop it to confirm the endpoint dies. | maintainer (approval) + agent (drafting) | `evolve-governance` proposal — **project-agnostic, worth upstreaming** |

**Deliberately not actions.** The four review rounds on T-0001 are not a problem to fix — every round found something real, and the alternative was shipping those defects. The three DoD deviations are a sequencing consequence (the harness necessarily follows the stack it tests), already bounded by T-0015. Neither becomes an action.

**Note on scope:** all three actions are governance changes requiring human approval and lane-2 review. None is an implementation ticket, because none of this sprint's friction was implementation friction — the code that shipped was sound. The friction was in what the repository *claimed* about it.

---

## Governance changes applied — 2026-08-31

All four are **rule-content changes**, so per [WoW §15](../../governance/WAY_OF_WORKING.md) each needs the owning persona's and a human's approval.

**Approved by:** the human maintainer (Product Owner), 2026-08-31, instructing *"update the framework with your recommendation"* after reading this retrospective.
**Applied by:** `claude-sm-9d4e` (Scrum Master persona), in one atomic commit so the rulebook is never self-contradictory between commits.
**Route:** solo mode, so a direct trunk commit with the approval recorded here rather than a reviewed PR ([GIT.md](../../standards/GIT.md), *Remotes and solo mode*).

| # | Change | Artifacts touched |
| --- | --- | --- |
| 1 | **Coverage claims must be falsifiable.** A claim that a test guards a behaviour is verified by mutation — break it, watch the test fail, restore, record both. A claim another ticket's DoD depends on is mutated first. | `standards/TESTING.md` (new section + a gate line), `skills/implement-ticket` (step 5) |
| 2 | **Verification must be attributable.** Checks against a running service bind the response to the process under test — own project name, container asserted healthy first, attribution confirmed by stopping it. Tool exit codes are read from the tool, not a pipeline. | `standards/TESTING.md` (new section), `skills/implement-ticket` (step 5) |
| 3 | **DoD item 4 closes the false-pointer gap.** A deferral is captured only when the destination ticket's scope accepts it, with the accepting line cited — adding one if none exists. | `governance/DEFINITION_OF_DONE.md` (item 4) |
| 4 | **The archive step is reconciled with the validator.** "Copy verbatim" and link integrity contradicted each other; the skill now requires re-basing links one level and heading the archive with what differs. | `skills/retrospective` (step 1 + Validation) |

**How we would notice these working.** Changes 1 and 2 should show up as *fewer blocking review findings of the "claim outran evidence" type* — that was 4 of 4 this sprint. If SPRINT-002's reviews still find them at the same rate, the rules are being read and not applied, which is a different problem needing a different fix. Change 3 should show up as deferrals citing a scope line. Change 4 is binary: the validator is green after the next archive, or it is not.

**Compatibility.** No existing ticket, sprint or ADR needs touching: the changes bind future verification and future deferrals. SPRINT-001's own tickets were not retro-fitted, deliberately — rewriting closed records to match new rules would destroy the evidence the rules came from.

**Foundation classification.** All four are project-agnostic — none mentions Got Issues, its stack, or its constraints — and are recorded as `Proposed` upstream contributions in [FOUNDATION.md](../../FOUNDATION.md). They came from a first sprint on a new stack, which is where a framework's blind spots surface most cheaply.
