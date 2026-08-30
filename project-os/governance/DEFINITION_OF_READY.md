# Definition of Ready (DoR)

**Question answered:** when is a work item sufficiently understood to enter a sprint?

A ticket is marked `ready` only when every applicable item below holds. Readiness is evaluated during [`refine-ticket`](../skills/refine-ticket/SKILL.md) and re-verified by [`pick-up-ticket`](../skills/pick-up-ticket/SKILL.md). Marking a ticket Ready to keep work moving is forbidden — "not ready, because X" is a valid and useful refinement result.

## Universal criteria (all ticket types)

1. **Problem is clear.** The problem or desired outcome is stated such that a reader who has only the repository understands *why* this work exists.
2. **Value is identifiable.** Someone can say who benefits and how; work whose value nobody can articulate goes back to the backlog or `IDEAS.md`.
3. **Acceptance criteria are verifiable.** Each criterion is concrete enough that an independent QA persona could pass or fail it without asking the author. "Works correctly" and "is fast" do not qualify; "returns HTTP 429 after the 10th request within 60 s" does.
4. **Scope is bounded.** In Scope / Out of Scope are filled in; the ticket names an outcome, not a wandering theme.
5. **Dependencies are known.** `depends_on` lists blocking tickets/decisions; none of them makes starting pointless. External dependencies (credentials, third parties, human input) are identified.
6. **Constraints are surfaced.** Relevant `PROJECT.md` constraints, ADRs, and standards are linked in the ticket, not assumed to be discovered later.
7. **Appropriately sized.** Completable within a small fraction of a sprint (guideline: ≤ 2–3 focused days). Larger work is split before it is Ready.
8. **Testable.** It is clear how the outcome will be verified, and the Testing Notes say anything non-obvious about how.
9. **No known blocker** currently prevents implementation from starting.

## Conditional criteria (when applicable)

- **Architectural questions resolved** — if the ticket raises a decision that meets the [ADR bar](../architecture/adr/README.md), the ADR exists (at least `Proposed`) or the ticket is explicitly a spike to resolve it.
- **UX/design available** — if the ticket has user-facing UI, the interaction is described or designed well enough to implement without inventing UX.
- **Security/privacy identified** — if the ticket touches auth, personal data, secrets, or external input, the concern is named in the ticket (and reflected in acceptance criteria where needed).
- **Data/migration impact identified** — if the ticket changes persistent data shape.

## Explicit exceptions

The DoR bends for two categories; it does not disappear:

- **Urgent production bugs** may enter the sprint with only: reproduction steps (or best available evidence), expected vs. observed behavior, severity, and a verifiable "fixed means" statement. Everything else may be completed after mitigation. The addition is recorded under Discovered / Unplanned Work in the sprint per [WoW §7](WAY_OF_WORKING.md).
- **Spikes** (time-boxed investigations) need: the question to answer, why it matters now, a time box, and the form of the output (usually notes in the ticket plus follow-up tickets/ADR). Spikes do not need acceptance criteria about product behavior — the deliverable is knowledge, and the time box is the scope.

Chores with genuinely trivial scope (dependency bump, config tweak) may use an abbreviated ticket, but still need a verifiable done-statement.
