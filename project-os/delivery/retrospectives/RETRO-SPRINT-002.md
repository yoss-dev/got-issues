# RETRO-SPRINT-002

## Sprint summary

- **Goal:** *The contract-first premise is proven, and nothing in the system is verified only by hand.* — **achieved.** [T-0002](../../product/tickets/T-0002-contract-first-codegen-pipeline.md) turned [ADR-0004](../../architecture/adr/ADR-0004-contract-first-openapi-code-generation.md) from a governing document into a running pipeline with a drift gate; [T-0015](../../product/tickets/T-0015-compose-stack-smoke-test.md) closed the hand-verification debt, discharging the DoD deviations recorded on [T-0001](../../product/tickets/T-0001-runnable-compose-stack.md) and [T-0010](../../product/tickets/T-0010-duende-identity-host.md).
- **Committed:** 3 tickets; **Done:** 3; **returned to backlog:** 0; **discovered work items:** 4 ([T-0016](../../product/tickets/T-0016-generation-output-ownership.md), [T-0017](../../product/tickets/T-0017-automated-contract-conformance-tier.md), [T-0018](../../product/tickets/T-0018-user-subject-tokens.md), [T-0019](../../product/tickets/T-0019-token-clock-skew.md)), plus one scope widening on [T-0014](../../product/tickets/T-0014-correct-testing-standard-commands.md).
- **Flow:** 0 `blocked` episodes, 0 escalations to the human, 0 acceptance failures. 19 recorded review verdicts across the three tickets, **10 of them Request-changes** (T-0002: 2 of 4 · T-0009: 5 of 10 · T-0015: 3 of 5).

### Previous retro's actions

| # | Action | Status | Evidence |
| --- | --- | --- | --- |
| 1 | Coverage claims falsifiable by mutation | **Done, and load-bearing** | Mutation is now how findings get settled rather than argued. It caught the bug that mattered most this sprint: T-0015's AC4 mutation *passed*, revealing that `/health` reports healthy on a database with no tables, so the check could not have detected a missing migration step. Two gaps surfaced — see Actions 1. |
| 2 | DoD item 4: a deferral counts only if the destination's scope accepts it | **Done, and effective** | **Zero false pointers this sprint** against three in SPRINT-001. T-0015's AC8 → T-0018 was verified by *reading T-0018*, independently, by two sessions; T-0015's rot risk was given a home by widening T-0014's scope rather than pointing at it; T-0009's F1/F2 were fixed rather than deferred. |
| 3 | Attribution rule for checks against a running service | **Done — and violated by the harness that implements it** | Applied correctly to the system under test (ephemeral ports, container asserted healthy before any HTTP response is trusted, stop-and-recheck). Then broken inside the checking code itself: see Defect analysis, B1 and B5. |
| 4 | Archive step reconciled with the validator | **Done, and immediately under-applied** | This retro's archive was re-based correctly on the second attempt: my first pass handled links already beginning with a parent-directory hop and missed a bare relative one. Validator caught it. A rule applied to the link shape I happened to think of. |

## What worked

**Independent sessions produced findings no self-review would have.** Every blocking finding this sprint came from a session that had not written the code. The sharpest examples: acceptance dropped `placeholder_records` and watched T-0015's schema check pass ([T-0015](../../product/tickets/T-0015-compose-stack-smoke-test.md) F1); review reproduced a leak by emptying Docker, running to green, and finding two stacks still up (B1); acceptance re-derived T-0015's AC6 with **its own RSA keypair and token minter**, refusing to reuse the shipped `TokenFactory`, and in doing so confirmed the five-minute clock-skew grace first-hand.

**Reviewers reproduced rather than reasoned.** `claude-rev-6d21` captured real `docker compose` output to prove that `"health"` matches `container … is unhealthy` (B6) rather than asserting it might. `claude-qa-9b3e` traced a mutant end to end instead of assuming where it landed. This is the difference between a review that finds defects and one that produces opinions.

**Fixing beat deferring where the fix was cheap.** T-0009's F1/F2 and T-0015's F1–F4 were all closed in place. DoD item 4 makes deferral expensive on purpose — you must read the destination and cite the accepting line — and the observable effect is that small residuals get fixed instead of parked.

**Raising rather than absorbing held under pressure.** The five-minute clock-skew default was found while building a test for it. Fixing it in passing would have been one line and would have bypassed the Security review [SECURITY.md](../../standards/SECURITY.md) requires for changes to token validation. It became [T-0019](../../product/tickets/T-0019-token-clock-skew.md) with the decision framed and not pre-empted.

## What caused friction

**Ten Request-changes verdicts across three tickets.** Not distributed evenly: T-0009 took 5 and T-0015 took 3 over five review passes. The cost was real, and so was the yield — every one of them found something. But two of those rounds existed only because an earlier fix had been scoped too narrowly (below), and those were avoidable.

**Correction rounds are where the defects came from.** T-0009's surrogate-pair bug was introduced *by* the fix for the over-long-name finding. T-0015's B6 was introduced by the fix for F3, which was itself the unfixed half of B3. SPRINT-001 produced the rule that *a correction is new, unverified work*; this sprint shows it holds even when the correction is one line.

**The lane split cost a real gate.** T-0015's [B5](../../product/tickets/T-0015-compose-stack-smoke-test.md): `validate.py` was recorded as exit 0 having been run in the primary checkout while the branch lived in a worktree — where it was exit 1 with ten findings. Two working copies is the correct model for the two lanes, and it introduces exactly one new failure mode: reading a gate from the wrong one.

## Defect & rework analysis

Every blocking finding this sprint, by what was actually wrong:

| Ticket | Finding | What the defect was |
| --- | --- | --- |
| T-0009 | Q4 | A display-name trim that split surrogate pairs — a hard failure on every request from that caller |
| T-0009 | Q5 | The AC7 log test projected an 18-character name, so the trim never ran: a logger given the display name outright survived 62 green tests |
| T-0009 | F1 | Test host emitted `ClaimTypes.NameIdentifier`; production emits only `sub`. Deleting the production branch left the suite green |
| T-0015 | B1 | Mutation stacks leaked every run — teardown failed against a deleted file, invisible because `DisposeAsync` was the one place a command result was discarded |
| T-0015 | B2 | Name truncation dropped the GUID entirely, so runs shared a Compose project |
| T-0015 | B3 | AC4 asserted only that *something* threw: a failed image build would have proved the check works |
| T-0015 | B4 | AC7 could not distinguish a host that declined to migrate from one that never started |
| T-0015 | B5 | A gate result read from the wrong working copy |
| T-0015 | B6 | `Assert.Contains("health", …)` matched docker's own `container … is unhealthy` |
| T-0015 | F1 | The schema check verified a named list of tables; a database missing one entirely passed with every service healthy |

**The pattern, stated exactly.** SPRINT-001's finding was *the repository claiming more than the code delivered* — a documentation-shaped fault. **This sprint's is the same fault relocated into the verification itself: none of the ten was a defect in *what* was tested; every one was a defect in what was accepted as evidence.** That is the worse form, because a check that accepts weak evidence does not merely record a false claim, it manufactures one — and then the claim carries a green test as its proof.

**The second-order pattern, and it is mine.** Three times on T-0015 a fix was scoped to the sentence in front of me rather than to the claim I was making: B3's second half stayed open while I reported B3 fixed; F2 repeated Q4's stale comment because I fixed the sighting rather than searching for the statement; B6 replaced "asserts that something threw" with "asserts a word anything might say". The generalisation, which `claude-rev-6d21` endorsed and admitted walking into from the other side by proposing `"reports health ''"` without asking what else could emit those characters:

> **When a finding says "X is satisfied by anything", the fix is not a narrower X. It is asking what else could satisfy the replacement.**

**Two gaps in the mutation rule itself**, both found by using it:

1. **A mutant the build rejects is evidence about the build, not about coverage.** Twice a mutation failed to compile — `if (false)` under CS0162, and deleting a call site under CA1822 — and each time the *compiler* killed it, not the test. That is a stronger guarantee than a test, but it is a different claim, and filing it as coverage overstates what is guarded.
2. **A mutation record can overstate what its mutant proves.** T-0015's first AC7 table claimed a `sleep 300` mutant showed the old assertion could not detect the failure. It could: with that entrypoint the app emits nothing, so the old assertion failed too. The mutant that separates them needs an `echo` first. Under [TESTING.md](../../standards/TESTING.md) the mutation record *is* the evidence, so a table overstating a mutant is the same defect as the assertion it documents.

## Process & governance observations

**The standards were applied to the system under test and not to the thing doing the testing.** [TESTING.md](../../standards/TESTING.md) already requires exit codes to be read from the tool and verification to be attributable. T-0015's harness enforces both rules on the stack — and broke both internally: `DisposeAsync` discarded the one result nobody checked (B1), and a gate was read from the wrong working copy (B5). The rules are written as though test code is where the rules come from rather than somewhere they apply.

**DoD item 4 is doing real work and is worth its cost.** It converted two would-be residuals into a verified successor ([T-0018](../../product/tickets/T-0018-user-subject-tokens.md)) and a widened scope ([T-0014](../../product/tickets/T-0014-correct-testing-standard-commands.md)), and made "fix it" the cheaper path for everything small.

**Five review passes on one ticket is not a process failure.** It looks like one. Each pass found something real, and the ticket it protected discharges two other tickets' Definitions of Done. The signal to watch is not the count but whether later rounds find *new* classes of defect or re-find the same one — here, rounds 4 and 5 re-found the same narrowing, which is the finding above, not an argument for fewer rounds.

**A limit stated too narrowly is a claim outrunning evidence.** T-0015's schema check documented four limits and omitted two that acceptance then demonstrated (L1: the migration step is its own oracle, so a defect *in the step* cancels on both sides; L2: precision and scale are outside the signature). Both are now on the assertion itself, where the decision to trust it gets made.

## Improvement actions

| # | Action | Owner | Lands as |
| --- | --- | --- | --- |
| 1 | **Two amendments to the mutation rule in [TESTING.md](../../standards/TESTING.md).** (a) *A mutant only counts if the build accepts it* — a mutant rejected by the compiler or an analyser is evidence that the invariant is enforced by the build, which is stronger than a test; record it as that, then run one the build accepts. (b) *A mutation record states what the mutant proves* — a mutant killed by both the old and the new code demonstrates the new code works, not that it is stronger; showing strength needs a mutant the old code survives. | maintainer (approval) + agent (drafting) | `evolve-governance` proposal — **project-agnostic, worth upstreaming per [FOUNDATION.md](../../FOUNDATION.md)** |
| 2 | **The harness is subject to the standards it enforces.** Add to [TESTING.md](../../standards/TESTING.md): test infrastructure is held to the same attribution and exit-code rules as the code it checks — every command result is read including teardown, and gates are run **in the working copy under test**. Add the working-copy point to `implement-ticket`'s verification step, since the two-lane worktree model is what makes it easy to get wrong. | maintainer (approval) + agent (drafting) | `evolve-governance` proposal — **project-agnostic, worth upstreaming** |
| 3 | **Add the narrowing rule to `review-code`.** When a finding is *"X is satisfied by anything"*, the fix must enumerate what else could satisfy the replacement; a narrower predicate is not evidence that the gap is closed. Both the implementer and the reviewer walked into this in the same sprint, which makes it a checklist item rather than a lesson. | maintainer (approval) + agent (drafting) | `evolve-governance` proposal — **project-agnostic, worth upstreaming** |

*Three actions, all governance, all requiring human approval. Deliberately no tooling ticket: the two questions this sprint raised already exist as [T-0018](../../product/tickets/T-0018-user-subject-tokens.md) and [T-0019](../../product/tickets/T-0019-token-clock-skew.md), and adding a fourth aspirational action would dilute three that are ready to land.*
