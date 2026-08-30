---
id: T-NNNN
title: <short outcome-oriented title>
type: feature   # feature | bug | technical | spike | chore
status: backlog # backlog | ready | committed | in-progress | blocked | in-acceptance | done | dropped
priority: normal # critical | high | normal | low  (order in BACKLOG.md is the fine-grained ranking)
owner: none     # agent/human identifier while in-progress; reset to none on handback
implemented_by: none # set (trunk commit) at handover to in-acceptance; the implementing engineer's id
accepted_by: none    # set by complete-ticket at done; MUST differ from implemented_by
depends_on: []  # ticket IDs that must be done first, e.g. [T-0012]
adrs: []        # related ADR IDs, e.g. [ADR-0003]
created: YYYY-MM-DD
updated: YYYY-MM-DD
---

# T-NNNN: <Title>

<!--
Instructions (delete after filling in):
- Describe outcomes and constraints. Do NOT prescribe implementation unless it is a genuine constraint.
- Sections marked (optional) may be removed if empty; all others are required for `ready`.
- Links below are relative to this file's final home, product/tickets/ (they resolve after instantiation).
- Bugs: Problem/Context holds reproduction steps + expected vs. observed + severity.
- Spikes: Acceptance Criteria are replaced by the question, time box, and output form (see DoR exceptions).
-->

## Problem / Context

*Why this work exists. What hurts, for whom, what happens if we do nothing. Link the originating idea (IDEA-NNN) if any.*

## Desired Outcome

*The observable end state, in one or two sentences. Not the steps to get there.*

## User / Business Value

*Who benefits and how; reference user personas by name where relevant.*

## Scope

### In Scope

- …

### Out of Scope

- …

## Acceptance Criteria

*Each criterion independently verifiable by QA without asking the author. Prefer Given/When/Then for behavior.*

- [ ] AC1: …
- [ ] AC2: …

## Examples / Scenarios

*Concrete examples, edge cases, and counter-examples discovered in refinement. These seed the tests.*

## Technical Notes *(optional)*

*Known constraints, pointers into the codebase, suggested approach clearly marked as suggestion.*

## Dependencies

*Beyond `depends_on` frontmatter: external services, credentials, human input, design assets.*

## Risks / Unknowns

*What could invalidate this ticket or surprise the implementer. Empty is a claim, not a default.*

## Testing Notes

*How this will be verified: test level(s), any manual verification needed, non-obvious setup.*

## Relevant ADRs & Documentation

*Links the implementer must read before starting (ADRs also listed in frontmatter).*

## Definition of Ready

- [ ] Meets [DoR](../../governance/DEFINITION_OF_READY.md) — checked during refinement; note applied exceptions here.

## Definition of Done

- [ ] Meets [DoD](../../governance/DEFINITION_OF_DONE.md) — checked by `complete-ticket`; deviations require recorded PO/human approval.

---

## Work Log

*Append-only, newest entry last. Every working session that touches this ticket ends with an entry. Format:*

### YYYY-MM-DD — <persona> (<agent/human id>)

- **Did:** …
- **Decided:** … *(significant decisions + why; "none")*
- **Remaining:** … *(what a successor must do next)*
- **Open questions / blockers:** … *(or "none")*
- **Branch / PR:** … *(during implementation and at handover; "n/a" otherwise)*
- **Test state:** … *(what was run, result)*
