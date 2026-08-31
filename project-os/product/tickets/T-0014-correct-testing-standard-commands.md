---
id: T-0014
title: Correct TESTING.md's suite commands to match reality
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

# T-0014: Correct TESTING.md's suite commands to match reality

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

- Correct the *How to run the suite* section so each command either works now or is explicitly marked as arriving with a named ticket.
- Remove or rewrite the parenthetical delegating the fix to "the first implementation ticket", which created the ambiguity.
- Follow `evolve-governance`: justification, the right approval, and a durable record.

### Out of Scope

- Any other section of TESTING.md; any other standard.
- Building the tooling itself (T-0002, T-0003).

## Acceptance Criteria

- [ ] AC1: Given TESTING.md's *How to run the suite* section, when each listed command is run against the repository at that time, then it either works or is explicitly labelled as not yet existing with the ticket that delivers it.
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
