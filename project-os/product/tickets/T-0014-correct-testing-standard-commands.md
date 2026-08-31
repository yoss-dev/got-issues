---
id: T-0014
title: Correct the stale commands and prerequisites across the standards
type: technical
status: backlog
priority: normal
owner: none
implemented_by: none
accepted_by: none
depends_on: []
adrs: []
created: 2026-08-30
updated: 2026-08-30
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

### Out of Scope

- Any section of a standard that is *accurate*. This ticket corrects statements falsified by delivered work, not general editing.
- Building the tooling itself (T-0002, T-0003).

**Widened 2026-08-31.** The original Out of Scope read *"any other standard"* — while T-0002 and T-0003 were routing three stale statements here. Two of them were therefore pointed at a ticket that disowned them, which is the failure [DoD](../../governance/DEFINITION_OF_DONE.md) item 4 was amended to prevent the day before, citing the RETRO-SPRINT-001 instance of exactly this. Caught by `claude-rev-8b4f` during T-0002's review.

## Acceptance Criteria

- [ ] AC1: Given each corrected standard, when every command it documents is run against the repository at that time, then each either works or is explicitly labelled as not yet existing with the ticket that delivers it.
- [ ] AC1b: Given the standards, when they are searched for a JDK prerequisite, then none asserts one — generation runs from a container image.
- [ ] AC2: Given the section, when read after T-0002 and T-0003 land, then it needs no further correction — it must not re-create a promise about future tooling that goes stale.
- [ ] AC3: Given the change, when it is merged, then it carries the human approval `evolve-governance` requires, recorded in the change.

## Dependencies

Human approval (governance change). No ticket dependencies — deliberately, so the standard can be corrected before or after the tooling exists.

## Risks / Unknowns

- Timing: if this lands before T-0002/T-0003, the honest text is "not yet"; if after, it is the real commands. Either is fine, but the wording must not need a third edit — hence AC2.
- The same latent problem may exist in other standards written at bootstrap. **Out of scope here**, but worth a look during the retrospective.

## Testing Notes

Verified by literally running each documented command in a clean clone and confirming the text matches the outcome.

## Relevant ADRs & Documentation

- [TESTING.md](../../standards/TESTING.md) — the file being corrected
- [GIT.md](../../standards/GIT.md) — why this is lane 2
- [`evolve-governance`](../../skills/evolve-governance/SKILL.md) — the procedure to follow

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — not yet refined.

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
