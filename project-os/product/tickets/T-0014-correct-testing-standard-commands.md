---
id: T-0014
title: Correct the stale commands and prerequisites across the standards
type: technical
status: ready
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: []
adrs: []
created: 2026-08-30
updated: 2026-08-31
---

# T-0014: Correct the stale commands and prerequisites across the standards

## Problem / Context

Raised during T-0001's independent review (`claude-rev-2c8d`, 2026-08-30).

[`standards/TESTING.md`](../../standards/TESTING.md) tells agents exactly what to run before claiming green:

```
dotnet build
dotnet test
./tools/generate.sh && git diff --exit-code
```

Two of those do not exist. The harness arrives in [T-0003](T-0003-automated-test-harness.md) and `tools/generate.sh` in [T-0002](T-0002-contract-first-codegen-pipeline.md). The standard also contains a parenthetical saying the paths are "established by the first implementation ticket… correct this section in that ticket" — which T-0001 declined to do, correctly: `project-os/standards/` is governance, and [GIT.md](../../standards/GIT.md) routes it through [`evolve-governance`](../../skills/evolve-governance/SKILL.md) with human approval. Editing a standard from inside a source ticket is exactly the silent-governance-rewrite the rule prevents.

So the correction needs its own change with the right approval. Until then a standard states, with authority, commands that fail.

**This ticket is an `evolve-governance` change, not ordinary source work.** It requires human approval and travels lane 2 per [GIT.md](../../standards/GIT.md).

## Desired Outcome

`TESTING.md` describes commands that actually work at the time of reading, and stops assigning its own correction to a ticket that may not perform it.

## User / Business Value

A standard that tells agents to run non-existent commands trains them to ignore it or to fabricate green results. Precision here protects every later verification claim.

## Scope

### In Scope

- **`TESTING.md`** — correct the *How to run the suite* section so each command either works now or is explicitly marked as arriving with a named ticket; remove the parenthetical delegating the fix to "the first implementation ticket", which created the ambiguity. Note that `dotnet build` and `dotnet test` now work, and the drift check is `./tools/check-drift.sh`, not the command currently cited.
- **`GIT.md`** — its merge-gate list cites the old drift command. Same correction.
- **`DOCUMENTATION.md`** — its prerequisites still name a JDK, which [T-0002](T-0002-contract-first-codegen-pipeline.md) removed by running the generator from a container image. (The same statement in `PROJECT.md` §5 and `ARCHITECTURE.md` is delivery state and was corrected in T-0002 directly; these three are governance and need this ticket's approval route.)
- Follow `evolve-governance`: justification, the right approval, and a durable record.
- **Name `tools/smoke.sh` in [TESTING.md](../../standards/TESTING.md)'s tier table, and decide whether `tools/smoke.sh --build-only` belongs in [GIT.md](../../standards/GIT.md)'s pre-merge gate list.** Added 2026-08-31 from [T-0015](T-0015-compose-stack-smoke-test.md)'s review: `apps/GotIssues.SmokeTests` sits outside `GotIssues.slnx` so the habitual suite stays fast, which means **nothing compiles it by accident** — the gates build and format the solution and would not notice it failing to compile. The mitigation today is a documented command, which is a habit rather than a gate. This scope line exists so that residual has a destination that accepts it.

### Out of Scope

- Any section of a standard that is *accurate*. This ticket corrects statements falsified by delivered work, not general editing.
- Building the tooling itself (T-0002, T-0003).

**Widened 2026-08-31.** The original Out of Scope read *"any other standard"* — while T-0002 and T-0003 were routing three stale statements here. Two of them were therefore pointed at a ticket that disowned them, which is the failure [DoD](../../governance/DEFINITION_OF_DONE.md) item 4 was amended to prevent the day before, citing the RETRO-SPRINT-001 instance of exactly this. Caught by `claude-rev-8b4f` during T-0002's review.

## Acceptance Criteria

- [ ] AC1: Given each corrected standard, when every command it documents is run against the repository at that time, then each either works or is explicitly labelled as not yet existing with the ticket that delivers it.
- [ ] AC1b: Given the standards, when they are searched for a JDK prerequisite, then none asserts one — generation runs from a container image.
- [ ] AC2: Given the section, when read after T-0002 and T-0003 land, then it needs no further correction — it must not re-create a promise about future tooling that goes stale.
- [ ] AC3: Given the change, when it is merged, then it carries the human approval `evolve-governance` requires, recorded in the change.
- [ ] AC4: Given [TESTING.md](../../standards/TESTING.md)'s tier table, when it is read, then it names the smoke tier and `tools/smoke.sh` — the tier exists ([T-0015](T-0015-compose-stack-smoke-test.md)) and the standard that defines tiers does not mention it.
- [ ] AC5: Given [GIT.md](../../standards/GIT.md)'s pre-merge gate list, when it is read, then it states a decision about `tools/smoke.sh --build-only`: either it is a gate, or it is deliberately not one with the reason. `apps/GotIssues.SmokeTests` sits outside `GotIssues.slnx`, so **nothing compiles it by accident** — today the only guard is a documented command, which is a habit rather than a gate.

## Dependencies

Human approval (governance change). No ticket dependencies — deliberately, so the standard can be corrected before or after the tooling exists.

## Risks / Unknowns

- ~~Timing: if this lands before T-0002/T-0003~~ — **resolved**: both landed 2026-08-31, so the honest text is the real commands. AC2 still applies to any *new* promise the correction might introduce.
- **AC5 asks for a decision, not a correction, and that decision has a cost either way.** Making `--build-only` a gate adds a step to every merge for a project that is not compiled by the other gates; leaving it out accepts that the smoke project can rot until someone runs it. Both are defensible; silently keeping the status quo is not, because the status quo was never chosen.
- The same latent problem may exist in other standards written at bootstrap. **Out of scope here**, but worth a look during the retrospective.

## Testing Notes

Verified by literally running each documented command in a clean clone and confirming the text matches the outcome. That is the whole test, and it is the one thing that cannot be skipped: this ticket exists because commands were documented without being run.

## Relevant ADRs & Documentation

- [TESTING.md](../../standards/TESTING.md) — the file being corrected
- [GIT.md](../../standards/GIT.md) — why this is lane 2
- [`evolve-governance`](../../skills/evolve-governance/SKILL.md) — the procedure to follow

## Definition of Ready

- [x] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — evaluated 2026-08-31 during `refinement-session`. All nine universal items hold; the timing risk that shaped item 9 is resolved now that T-0002 and T-0003 have landed. Conditional items: none — this changes documents, not behaviour. It travels lane 2 and needs human approval (AC3), which is a process constraint rather than a DoR exception.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`.

---

## Work Log

### 2026-08-30 — Software Engineer (claude-sm-9d4e)

- **Did:** Created to capture a T-0001 review finding the implementer declined to fix in place, because doing so would have edited a governance document from inside a source ticket. The reviewer endorsed that reasoning and withdrew the finding as a T-0001 defect — but DoD item 4 still requires it captured somewhere, and this is that somewhere.
- **Decided:** Typed `technical` with the governance route stated in the body; the change itself must go through `evolve-governance` with human approval.
- **Remaining:** Refinement, then an `evolve-governance` change.
- **Open questions / blockers:** needs human approval by nature.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — Software Engineer (claude-sm-9d4e)

- **Did:** Widened after `claude-rev-8b4f` found that T-0002 and T-0003 were routing three stale statements here while this ticket's Out of Scope said *"any other standard"* — so two of the three had no owner while reading as covered.
- **Decided:** widened rather than created a second ticket. All three are the same act (correct a statement delivered work falsified) needing the same `evolve-governance` approval; splitting would mean two approvals for one edit session.
- **Worth recording:** this is the **false-pointer failure the DoD was amended to prevent, made one day after the amendment, by the person who wrote it.** The rule works — a reviewer applied it and caught this — but the rule alone did not stop me making the mistake. That belongs in the next retrospective, not just here.
- **Remaining:** Refinement, then an `evolve-governance` change.
- **Open questions / blockers:** needs human approval by nature.
- **Branch / PR:** n/a
- **Test state:** n/a — not started.

### 2026-08-31 — Refinement (claude-sm-9d4e) — PO · BA · ENG · ARCH · QA

Applied all perspectives; folded in the two obligations this ticket acquired after it was written — naming the smoke tier in TESTING.md's tier table (AC4) and deciding whether `--build-only` is a merge gate (AC5). The second is a decision the ticket must force rather than a correction it can make: nothing compiles `apps/GotIssues.SmokeTests` by accident, and the current guard is a documented command, which is a habit.

- **Did:** Full refine-ticket pass across every applicable perspective.
- **Decided:** recorded inline above and in the ticket body.
- **Remaining:** implementation.
- **Open questions / blockers:** none.
- **DoR verdict:** **ready.**
- **Branch / PR:** n/a
- **Test state:** n/a — not started.
