# Testing Standards

What "tested" means on this project. Applied by the Software Engineer persona during implementation and enforced independently by the QA persona during acceptance.

## Universal rules

### What must be tested

- **Every acceptance criterion maps to at least one automated test** where automation is feasible; where it is not (visual look-and-feel, exploratory concerns), the ticket's Testing Notes say how it is verified instead.
- **New logic gets unit tests**; behavior that spans components gets at least one integration-level test through the real seams.
- **Every fixed bug gets a regression test that fails without the fix.** No exceptions — this is how the same bug is prevented from returning.
- Edge cases surfaced during refinement (ticket *Examples / Scenarios*) are the test designer's primary checklist.

### How tests must behave

- Deterministic: a test that flakes is fixed or quarantined *with a ticket* the same day; an ignored flaky test without a ticket is a DoD violation.
- Independent: tests do not depend on execution order or leftover state.
- Fast enough to run habitually; slow suites are split so the habitual tier stays fast.
- Tests assert *behavior/outcomes*, not implementation details; a refactor that preserves behavior should not break them.
- Test code is production code: same review, same standards, no copy-paste sprawl.

### The gate

- The full relevant suite passes before a ticket enters `in-acceptance`. "Passes on my machine" with red CI is red.
- Coverage numbers are a signal, not a goal; an untested critical path fails review regardless of the percentage.

## Project-specific rules

> ⚠ **Replace during `bootstrap-project`.**

- **Test frameworks & runners:** *TBD* `[open]`
- **Test tiers & where they live:** *TBD — e.g., unit next to code, integration in `tests/`, E2E in `e2e/`.* `[open]`
- **How to run the suite(s):** *TBD — exact commands agents must run before claiming green.* `[open]`
- **Coverage expectations:** *TBD or "no numeric target"* `[open]`
- **Test data & fixtures policy:** *TBD — no production data in tests `[default]`.*
