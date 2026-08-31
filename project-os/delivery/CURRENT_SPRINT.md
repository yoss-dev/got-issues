# No active sprint

The last sprint closed on **2026-08-31**. Nothing is committed and nothing is in progress.

**Next step: [`plan-sprint`](../skills/plan-sprint/SKILL.md).** It selects from
[BACKLOG.md](../product/BACKLOG.md) and writes this file from
[SPRINT_TEMPLATE.md](../templates/SPRINT_TEMPLATE.md). The next sprint number is
**SPRINT-003**.

## Where the last sprint left things

**[SPRINT-002](sprints/SPRINT-002.md) — drained, three of three done**
([RETRO-SPRINT-002](retrospectives/RETRO-SPRINT-002.md)). The contract-first premise is
running code with a drift gate, and nothing in the system is verified only by hand: the DoD
deviations recorded on [T-0001](../product/tickets/T-0001-runnable-compose-stack.md) and
[T-0010](../product/tickets/T-0010-duende-identity-host.md) are discharged.

**The first product capability is unblocked.**
[T-0004](../product/tickets/T-0004-create-and-list-projects.md) has no outstanding
dependencies and is `ready`; [T-0005](../product/tickets/T-0005-create-and-read-issues.md)
follows it. Every ticket completed so far has been foundation — this is the point where the
project starts answering what it is *for* rather than whether it can run.

**Three governance proposals are awaiting human approval**
([RETRO-SPRINT-002](retrospectives/RETRO-SPRINT-002.md), Improvement actions). They are not
blockers for planning, but they change how the next sprint's work is verified, so approving
them before `plan-sprint` is worth a moment.

**Two decisions were raised rather than taken**:
[T-0018](../product/tickets/T-0018-user-subject-tokens.md) (how people authenticate — no
token this system issues carries a subject) and
[T-0019](../product/tickets/T-0019-token-clock-skew.md) (the resource server's five-minute
clock-skew grace, a framework default nobody chose). Both need refinement before they can be
committed.
