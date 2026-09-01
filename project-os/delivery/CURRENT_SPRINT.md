# No active sprint

SPRINT-003 closed on 2026-08-31: goal achieved, all three committed tickets `done`, nothing
returned to the backlog. It is archived at [SPRINT-003.md](sprints/SPRINT-003.md) and reviewed in
[RETRO-SPRINT-003](retrospectives/RETRO-SPRINT-003.md).

**The next sprint is SPRINT-004.** Start it with [`plan-sprint`](../skills/plan-sprint/SKILL.md),
which sets one goal and commits Ready work against it.

## Where the project stands

The **MVP is delivered and running**: a project can be created and listed, issues created within it
and read by key, and an issue's type, status, priority and assignee changed through
`PATCH /issues/{issueKey}`. Everything runs under Docker Compose with Duende IdentityServer issuing
tokens and role authorisation enforced by policy.

## What SPRINT-004 should weigh

Not a plan — `plan-sprint` decides, and the Product Owner sets the goal. These are the facts that
should be in front of whoever does.

- **[T-0022](../product/tickets/T-0022-adopt-clean-architecture-layering.md) sits at backlog
  position 1 and is unrefined.** It implements [ADR-0010](../architecture/adr/ADR-0010-clean-architecture-layering.md),
  which the maintainer accepted, and it was explicitly sequenced *after* the MVP by their decision.
  Its own Work Log notes that its scope grew when T-0006 shipped in the pre-refactor shape, so it
  should be sized against the code as built rather than its description. Every product ticket after
  it copies what it produces.
- **Product work is ready and unblocked:** [T-0007](../product/tickets/T-0007-list-and-filter-issues.md)
  (list and filter issues) and [T-0008](../product/tickets/T-0008-comment-on-an-issue.md) (comments).
  Both were written before ADR-0010 and would be built in the shape T-0022 replaces.
- **Three of the open tickets are now checks** —
  [T-0025](../product/tickets/T-0025-documentation-truth-sweep.md),
  [T-0026](../product/tickets/T-0026-self-reporting-gate-runner.md),
  [T-0027](../product/tickets/T-0027-specification-authoring-lint.md) — the last two created by
  [RETRO-SPRINT-003](retrospectives/RETRO-SPRINT-003.md). Whoever schedules them should decide
  whether they share a home rather than each inventing one.
- **[RETRO-SPRINT-003](retrospectives/RETRO-SPRINT-003.md) action 1 needs maintainer approval
  before it can be applied** — a governance change naming the verification technique that found
  two-thirds of last sprint's defects. It is not blocked on anything else.

## Blockers & Escalations

*(none)*
